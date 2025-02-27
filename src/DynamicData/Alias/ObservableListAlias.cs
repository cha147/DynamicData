// Copyright (c) 2011-2020 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace DynamicData.Alias;

/// <summary>
/// Observable cache alias names.
/// </summary>
public static class ObservableListAlias
{
    /// <summary>
    /// Projects each update item to a new form using the specified transform function.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TDestination">The type of the destination.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="transformFactory">The transform factory.</param>
    /// <returns>An observable which emits the change set.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// source
    /// or
    /// valueSelector.
    /// </exception>
    public static IObservable<IChangeSet<TDestination>> Select<TSource, TDestination>(this IObservable<IChangeSet<TSource>> source, Func<TSource, TDestination> transformFactory)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (transformFactory is null)
        {
            throw new ArgumentNullException(nameof(transformFactory));
        }

        return source.Transform(transformFactory);
    }

    /// <summary>
    /// Equivalent to a select many transform. To work, the key must individually identify each child.
    /// **** Assumes each child can only have one parent - support for children with multiple parents is a work in progresses.
    /// </summary>
    /// <typeparam name="TDestination">The type of the destination.</typeparam>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="manySelector">The selector for the enumerable.</param>
    /// <returns>An observable which emits the change set.</returns>
    public static IObservable<IChangeSet<TDestination>> SelectMany<TDestination, TSource>(this IObservable<IChangeSet<TSource>> source, Func<TSource, IEnumerable<TDestination>> manySelector)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (manySelector is null)
        {
            throw new ArgumentNullException(nameof(manySelector));
        }

        return source.TransformMany(manySelector);
    }

    /// <summary>
    /// Filters the source using the specified valueSelector.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="predicate">The valueSelector.</param>
    /// <returns>An observable which emits the change set.</returns>
    /// <exception cref="System.ArgumentNullException">source.</exception>
    public static IObservable<IChangeSet<T>> Where<T>(this IObservable<IChangeSet<T>> source, Func<T, bool> predicate)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return source.Filter(predicate);
    }

    /// <summary>
    /// Filters source using the specified filter observable predicate.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="predicate">The predict for deciding on items to filter.</param>
    /// <returns>An observable which emits the change set.</returns>
    /// <exception cref="System.ArgumentNullException">source
    /// or
    /// filterController.</exception>
    public static IObservable<IChangeSet<T>> Where<T>(this IObservable<IChangeSet<T>> source, IObservable<Func<T, bool>> predicate)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return source.Filter(predicate);
    }
}
