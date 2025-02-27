// Copyright (c) 2011-2020 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using DynamicData.Kernel;

namespace DynamicData.Cache.Internal;

internal sealed class DynamicCombiner<TObject, TKey>
    where TKey : notnull
{
    private readonly IObservableList<IObservable<IChangeSet<TObject, TKey>>> _source;

    private readonly CombineOperator _type;

    public DynamicCombiner(IObservableList<IObservable<IChangeSet<TObject, TKey>>> source, CombineOperator type)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _type = type;
    }

    public IObservable<IChangeSet<TObject, TKey>> Run()
    {
        return Observable.Create<IChangeSet<TObject, TKey>>(
            observer =>
            {
                var locker = new object();

                // this is the resulting cache which produces all notifications
                var resultCache = new ChangeAwareCache<TObject, TKey>();

                // Transform to a merge container.
                // This populates a RefTracker when the original source is subscribed to
                var sourceLists = _source.Connect().Synchronize(locker).Transform(changeSet => new MergeContainer(changeSet)).AsObservableList();

                var sharedLists = sourceLists.Connect().Publish();

                // merge the items back together
                var allChanges = sharedLists.MergeMany(mc => mc.Source).Synchronize(locker).Subscribe(
                    changes =>
                    {
                        // Populate result list and check for changes
                        UpdateResultList(resultCache, sourceLists.Items.AsArray(), changes);

                        var notifications = resultCache.CaptureChanges();
                        if (notifications.Count != 0)
                        {
                            observer.OnNext(notifications);
                        }
                    });

                // when an list is removed, need to
                var removedItem = sharedLists.OnItemRemoved(
                    mc =>
                    {
                        // Remove items if required
                        ProcessChanges(resultCache, sourceLists.Items.AsArray(), mc.Cache.KeyValues);

                        if (_type == CombineOperator.And || _type == CombineOperator.Except)
                        {
                            var itemsToCheck = sourceLists.Items.SelectMany(mc2 => mc2.Cache.KeyValues);
                            ProcessChanges(resultCache, sourceLists.Items.AsArray(), itemsToCheck);
                        }

                        var notifications = resultCache.CaptureChanges();
                        if (notifications.Count != 0)
                        {
                            observer.OnNext(notifications);
                        }
                    }).Subscribe();

                // when an list is added or removed, need to
                var sourceChanged = sharedLists.WhereReasonsAre(ListChangeReason.Add, ListChangeReason.AddRange).ForEachItemChange(
                    mc =>
                    {
                        ProcessChanges(resultCache, sourceLists.Items.AsArray(), mc.Current.Cache.KeyValues);

                        if (_type == CombineOperator.And || _type == CombineOperator.Except)
                        {
                            ProcessChanges(resultCache, sourceLists.Items.AsArray(), resultCache.KeyValues.ToArray());
                        }

                        var notifications = resultCache.CaptureChanges();
                        if (notifications.Count != 0)
                        {
                            observer.OnNext(notifications);
                        }
                    }).Subscribe();

                return new CompositeDisposable(sourceLists, allChanges, removedItem, sourceChanged, sharedLists.Connect());
            });
    }

    private bool MatchesConstraint(MergeContainer[] sources, TKey key)
    {
        if (sources.Length == 0)
        {
            return false;
        }

        switch (_type)
        {
            case CombineOperator.And:
                {
                    return sources.All(s => s.Cache.Lookup(key).HasValue);
                }

            case CombineOperator.Or:
                {
                    return sources.Any(s => s.Cache.Lookup(key).HasValue);
                }

            case CombineOperator.Xor:
                {
                    return sources.Count(s => s.Cache.Lookup(key).HasValue) == 1;
                }

            case CombineOperator.Except:
                {
                    bool first = sources.Take(1).Any(s => s.Cache.Lookup(key).HasValue);
                    bool others = sources.Skip(1).Any(s => s.Cache.Lookup(key).HasValue);
                    return first && !others;
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(key));
        }
    }

    private void ProcessChanges(ChangeAwareCache<TObject, TKey> target, MergeContainer[] sourceLists, IEnumerable<KeyValuePair<TKey, TObject>> items)
    {
        // check whether the item should be removed from the list (or in the case of And, added)
        if (items is IList<KeyValuePair<TKey, TObject>> list)
        {
            // zero allocation enumerator
            foreach (var item in EnumerableIList.Create(list))
            {
                ProcessItem(target, sourceLists, item.Value, item.Key);
            }
        }
        else
        {
            foreach (var item in items)
            {
                ProcessItem(target, sourceLists, item.Value, item.Key);
            }
        }
    }

    private void ProcessItem(ChangeAwareCache<TObject, TKey> target, MergeContainer[] sourceLists, TObject item, TKey key)
    {
        var cached = target.Lookup(key);
        var shouldBeInResult = MatchesConstraint(sourceLists, key);

        if (shouldBeInResult)
        {
            if (!cached.HasValue)
            {
                target.AddOrUpdate(item, key);
            }
            else if (!ReferenceEquals(item, cached.Value))
            {
                target.AddOrUpdate(item, key);
            }
        }
        else
        {
            if (cached.HasValue)
            {
                target.Remove(key);
            }
        }
    }

    private void UpdateResultList(ChangeAwareCache<TObject, TKey> target, MergeContainer[] sourceLists, IChangeSet<TObject, TKey> changes)
    {
        foreach (var change in changes.ToConcreteType())
        {
            ProcessItem(target, sourceLists, change.Current, change.Key);
        }
    }

    private class MergeContainer
    {
        public MergeContainer(IObservable<IChangeSet<TObject, TKey>> source)
        {
            Source = source.Do(Clone);
        }

        public Cache<TObject, TKey> Cache { get; } = new();

        public IObservable<IChangeSet<TObject, TKey>> Source { get; }

        private void Clone(IChangeSet<TObject, TKey> changes)
        {
            Cache.Clone(changes);
        }
    }
}
