﻿// Copyright (c) 2011-2020 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace DynamicData.Aggregation;

/// <summary>
/// Maximum and minimum value extensions.
/// </summary>
public static class MaxEx
{
    private enum MaxOrMin
    {
        Max,

        Min
    }

    /// <summary>
    /// Continually calculates the maximum value from the underlying data source.
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="valueSelector">The value selector.</param>
    /// <param name="emptyValue">The value to use when the underlying collection is empty.</param>
    /// <returns>
    /// A distinct observable of the maximum item.
    /// </returns>
    public static IObservable<TResult> Maximum<TObject, TResult>(this IObservable<IChangeSet<TObject>> source, Func<TObject, TResult> valueSelector, TResult emptyValue = default)
        where TResult : struct, IComparable<TResult>
    {
        return source.ToChangesAndCollection().Calculate(valueSelector, MaxOrMin.Max, emptyValue);
    }

    /// <summary>
    /// Continually calculates the maximum value from the underlying data source.
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="valueSelector">The value selector.</param>
    /// <param name="emptyValue">The value to use when the underlying collection is empty.</param>
    /// <returns>
    /// A distinct observable of the maximum item.
    /// </returns>
    public static IObservable<TResult> Maximum<TObject, TKey, TResult>(this IObservable<IChangeSet<TObject, TKey>> source, Func<TObject, TResult> valueSelector, TResult emptyValue = default)
        where TKey : notnull
        where TResult : struct, IComparable<TResult>
    {
        return source.ToChangesAndCollection().Calculate(valueSelector, MaxOrMin.Max, emptyValue);
    }

    /// <summary>
    /// Continually calculates the minimum value from the underlying data source.
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="valueSelector">The value selector.</param>
    /// <param name="emptyValue">The value to use when the underlying collection is empty.</param>
    /// <returns>A distinct observable of the minimums item.</returns>
    public static IObservable<TResult> Minimum<TObject, TResult>(this IObservable<IChangeSet<TObject>> source, Func<TObject, TResult> valueSelector, TResult emptyValue = default)
        where TResult : struct, IComparable<TResult>
    {
        return source.ToChangesAndCollection().Calculate(valueSelector, MaxOrMin.Min, emptyValue);
    }

    /// <summary>
    /// Continually calculates the minimum value from the underlying data source.
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="valueSelector">The value selector.</param>
    /// <param name="emptyValue">The value to use when the underlying collection is empty.</param>
    /// <returns>
    /// A distinct observable of the minimums item.
    /// </returns>
    public static IObservable<TResult> Minimum<TObject, TKey, TResult>(this IObservable<IChangeSet<TObject, TKey>> source, Func<TObject, TResult> valueSelector, TResult emptyValue = default)
        where TKey : notnull
        where TResult : struct, IComparable<TResult>
    {
        return source.ToChangesAndCollection().Calculate(valueSelector, MaxOrMin.Min, emptyValue);
    }

    private static IObservable<TResult> Calculate<TObject, TResult>(this IObservable<ChangesAndCollection<TObject>> source, Func<TObject, TResult> valueSelector, MaxOrMin maxOrMin, TResult emptyValue = default)
        where TResult : struct, IComparable<TResult>
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (valueSelector is null)
        {
            throw new ArgumentNullException(nameof(valueSelector));
        }

        return source.Scan(
            default(TResult?),
            (state, latest) =>
            {
                var current = state;
                var requiresReset = false;

                foreach (var change in latest.Changes)
                {
                    var value = valueSelector(change.Item);
                    current ??= value;

                    if (change.Type == AggregateType.Add)
                    {
                        int isMatched = maxOrMin == MaxOrMin.Max ? 1 : -1;
                        if (value.CompareTo(current.Value) == isMatched)
                        {
                            current = value;
                        }
                    }
                    else
                    {
                        // check whether the max / min has been removed. If so we need to look
                        // up the latest from the underlying collection
                        if (value.CompareTo(current.Value) != 0)
                        {
                            continue;
                        }

                        requiresReset = true;
                        break;
                    }
                }

                if (requiresReset)
                {
                    var collection = latest.Collection;
                    if (collection.Count == 0)
                    {
                        current = default;
                    }
                    else
                    {
                        current = maxOrMin == MaxOrMin.Max ? collection.Max(valueSelector) : collection.Min(valueSelector);
                    }
                }

                return current;
            }).Select(t => t ?? emptyValue).DistinctUntilChanged();
    }

    private static IObservable<ChangesAndCollection<TObject>> ToChangesAndCollection<TObject, TKey>(this IObservable<IChangeSet<TObject, TKey>> source)
        where TKey : notnull
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source.Publish(
            shared =>
            {
                var changes = shared.ForAggregation();
                var data = shared.ToCollection();
                return data.Zip(changes, (d, c) => new ChangesAndCollection<TObject>(c, d));
            });
    }

    private static IObservable<ChangesAndCollection<TObject>> ToChangesAndCollection<TObject>(this IObservable<IChangeSet<TObject>> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source.Publish(
            shared =>
            {
                var changes = shared.ForAggregation();
                var data = shared.ToCollection();
                return data.Zip(changes, (d, c) => new ChangesAndCollection<TObject>(c, d));
            });
    }

    private class ChangesAndCollection<T>
    {
        public ChangesAndCollection(IAggregateChangeSet<T> changes, IReadOnlyCollection<T> collection)
        {
            Changes = changes;
            Collection = collection;
        }

        public IAggregateChangeSet<T> Changes { get; }

        public IReadOnlyCollection<T> Collection { get; }
    }
}
