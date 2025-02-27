﻿// Copyright (c) 2011-2020 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// ReSharper disable CheckNamespace
namespace DynamicData;

/// <summary>
///  A grouped change set.
/// </summary>
/// <typeparam name="TObject">The source object type.</typeparam>
/// <typeparam name="TKey">The type of the key.</typeparam>s
/// <typeparam name="TGroupKey">The value on which the stream has been grouped.</typeparam>
public interface IGroupChangeSet<TObject, TKey, TGroupKey> : IChangeSet<IGroup<TObject, TKey, TGroupKey>, TGroupKey>
    where TKey : notnull
    where TGroupKey : notnull
{
}
