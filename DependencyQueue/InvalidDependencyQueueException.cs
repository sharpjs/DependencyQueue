// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

using E = Errors;

/// <summary>
///   The exception thrown when a <see cref="DependencyQueue{T}"/> operation
///   fails because the queue is invalid.
/// </summary>
public class InvalidDependencyQueueException : InvalidOperationException
{
    /// <summary>
    ///   Initializes a new <see cref="InvalidDependencyQueueException"/>
    ///   instance with the specified validation errors.
    /// </summary>
    /// <param name="errors">
    ///   The validation errors that make the queue invalid.
    /// </param>
    public InvalidDependencyQueueException(IReadOnlyList<DependencyQueueError> errors)
        : base("The dependency queue is invalid.")
    {
        Errors = errors ?? throw E.ArgumentNull(nameof(errors));
    }

    /// <summary>
    ///   Gets the validation errors that make the queue invalid.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Each error in the list is one of the following types:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///       <term><see cref="DependencyQueueCycleError{T}"/></term>
    ///       <description>The dependency graph contains a cycle.</description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="DependencyQueueUnprovidedTopicError{T}"/></term>
    ///       <description>A topic is required but not provided.</description>
    ///     </item>
    ///   </list>
    /// </remarks>
    public IReadOnlyList<DependencyQueueError> Errors { get; }
}
