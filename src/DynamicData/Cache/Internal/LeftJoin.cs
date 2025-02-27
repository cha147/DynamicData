﻿// Copyright (c) 2011-2020 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using DynamicData.Kernel;

namespace DynamicData.Cache.Internal;

internal class LeftJoin<TLeft, TLeftKey, TRight, TRightKey, TDestination>
    where TLeftKey : notnull
    where TRightKey : notnull
{
    private readonly IObservable<IChangeSet<TLeft, TLeftKey>> _left;

    private readonly Func<TLeftKey, TLeft, Optional<TRight>, TDestination> _resultSelector;

    private readonly IObservable<IChangeSet<TRight, TRightKey>> _right;

    private readonly Func<TRight, TLeftKey> _rightKeySelector;

    public LeftJoin(IObservable<IChangeSet<TLeft, TLeftKey>> left, IObservable<IChangeSet<TRight, TRightKey>> right, Func<TRight, TLeftKey> rightKeySelector, Func<TLeftKey, TLeft, Optional<TRight>, TDestination> resultSelector)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _rightKeySelector = rightKeySelector ?? throw new ArgumentNullException(nameof(rightKeySelector));
        _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
    }

    public IObservable<IChangeSet<TDestination, TLeftKey>> Run()
    {
        return Observable.Create<IChangeSet<TDestination, TLeftKey>>(
            observer =>
            {
                var locker = new object();

                // create local backing stores
                var leftCache = _left.Synchronize(locker).AsObservableCache(false);
                var rightCache = _right.Synchronize(locker).ChangeKey(_rightKeySelector).AsObservableCache(false);

                // joined is the final cache
                var joined = new ChangeAwareCache<TDestination, TLeftKey>();

                var leftLoader = leftCache.Connect().Select(
                    changes =>
                    {
                        foreach (var change in changes.ToConcreteType())
                        {
                            switch (change.Reason)
                            {
                                case ChangeReason.Add:
                                case ChangeReason.Update:
                                    // Update with left (and right if it is presents)
                                    var left = change.Current;
                                    var right = rightCache.Lookup(change.Key);
                                    joined.AddOrUpdate(_resultSelector(change.Key, left, right), change.Key);
                                    break;

                                case ChangeReason.Remove:
                                    // remove from result because a left value is expected
                                    joined.Remove(change.Key);
                                    break;

                                case ChangeReason.Refresh:
                                    // propagate upstream
                                    joined.Refresh(change.Key);
                                    break;
                            }
                        }

                        return joined.CaptureChanges();
                    });

                var rightLoader = rightCache.Connect().Select(
                    changes =>
                    {
                        foreach (var change in changes.ToConcreteType())
                        {
                            var right = change.Current;
                            var left = leftCache.Lookup(change.Key);

                            switch (change.Reason)
                            {
                                case ChangeReason.Add:
                                case ChangeReason.Update:
                                    {
                                        if (left.HasValue)
                                        {
                                            // Update with left and right value
                                            joined.AddOrUpdate(_resultSelector(change.Key, left.Value, right), change.Key);
                                        }
                                        else
                                        {
                                            // remove if it is already in the cache
                                            joined.Remove(change.Key);
                                        }
                                    }

                                    break;

                                case ChangeReason.Remove:
                                    {
                                        if (left.HasValue)
                                        {
                                            // Update with no right value
                                            joined.AddOrUpdate(_resultSelector(change.Key, left.Value, Optional<TRight>.None), change.Key);
                                        }
                                        else
                                        {
                                            // remove if it is already in the cache
                                            joined.Remove(change.Key);
                                        }
                                    }

                                    break;

                                case ChangeReason.Refresh:
                                    // propagate upstream
                                    joined.Refresh(change.Key);
                                    break;
                            }
                        }

                        return joined.CaptureChanges();
                    });

                return new CompositeDisposable(leftLoader.Merge(rightLoader).SubscribeSafe(observer), leftCache, rightCache);
            });
    }
}
