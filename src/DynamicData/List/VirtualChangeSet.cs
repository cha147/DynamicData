// Copyright (c) 2011-2020 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DynamicData;

internal class VirtualChangeSet<T> : IVirtualChangeSet<T>
{
    private readonly IChangeSet<T> _virtualChangeSet;

    public VirtualChangeSet(IChangeSet<T> virtualChangeSet, IVirtualResponse response)
    {
        _virtualChangeSet = virtualChangeSet ?? throw new ArgumentNullException(nameof(virtualChangeSet));

        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    public int Refreshes => _virtualChangeSet.Refreshes;

    public IVirtualResponse Response { get; }

    int IChangeSet.Adds => _virtualChangeSet.Adds;

    int IChangeSet.Capacity
    {
        get => _virtualChangeSet.Capacity;
        set => _virtualChangeSet.Capacity = value;
    }

    int IChangeSet.Count => _virtualChangeSet.Count;

    int IChangeSet.Moves => _virtualChangeSet.Moves;

    int IChangeSet.Removes => _virtualChangeSet.Removes;

    int IChangeSet<T>.Replaced => _virtualChangeSet.Replaced;

    int IChangeSet<T>.TotalChanges => _virtualChangeSet.TotalChanges;

    public IEnumerator<Change<T>> GetEnumerator()
    {
        return _virtualChangeSet.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
