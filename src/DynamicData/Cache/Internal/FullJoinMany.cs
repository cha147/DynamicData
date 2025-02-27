// Copyright (c) 2011-2020 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

using DynamicData.Kernel;

namespace DynamicData.Cache.Internal;

internal class FullJoinMany<TLeft, TLeftKey, TRight, TRightKey, TDestination>
    where TLeftKey : notnull
    where TRightKey : notnull
{
    private readonly IObservable<IChangeSet<TLeft, TLeftKey>> _left;

    private readonly Func<TLeftKey, Optional<TLeft>, IGrouping<TRight, TRightKey, TLeftKey>, TDestination> _resultSelector;

    private readonly IObservable<IChangeSet<TRight, TRightKey>> _right;

    private readonly Func<TRight, TLeftKey> _rightKeySelector;

    public FullJoinMany(IObservable<IChangeSet<TLeft, TLeftKey>> left, IObservable<IChangeSet<TRight, TRightKey>> right, Func<TRight, TLeftKey> rightKeySelector, Func<TLeftKey, Optional<TLeft>, IGrouping<TRight, TRightKey, TLeftKey>, TDestination> resultSelector)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _rightKeySelector = rightKeySelector ?? throw new ArgumentNullException(nameof(rightKeySelector));
        _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
    }

    public IObservable<IChangeSet<TDestination, TLeftKey>> Run()
    {
        var emptyCache = Cache<TRight, TRightKey>.Empty;

        var rightGrouped = _right.GroupWithImmutableState(_rightKeySelector);
        return _left.FullJoin(rightGrouped, grouping => grouping.Key, (leftKey, left, grouping) => _resultSelector(leftKey, left, grouping.ValueOr(() => new ImmutableGroup<TRight, TRightKey, TLeftKey>(leftKey, emptyCache))));
    }
}
