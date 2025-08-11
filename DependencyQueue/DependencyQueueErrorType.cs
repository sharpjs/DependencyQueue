// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

/// <summary>
///   Types of <see cref="DependencyQueue{T}"/> validation errors.
/// </summary>
public enum DependencyQueueErrorType
{
    /// <summary>
    ///   One or more entries require a topic that no entries provide.
    ///   The error object is a <see cref="DependencyQueueUnprovidedTopicError{T}"/>.
    /// </summary>
    UnprovidedTopic,

    /// <summary>
    ///   The dependency graph contains a cycle.
    ///   The error object is a <see cref="DependencyQueueCycleError{T}"/>.
    /// </summary>
    Cycle
}
