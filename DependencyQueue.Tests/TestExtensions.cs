// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

internal static class TestExtensions
{
    public static void ShouldBeValid<T>(this DependencyQueue<T> queue)
    {
        queue.Validate().ShouldBeEmpty();
    }

    public static void ShouldNotHaveReadyEntries<T>(this DependencyQueue<T> queue)
    {
        queue.ReadyEntries.ShouldBeEmpty();
    }

    public static void ShouldHaveReadyEntries<T>(
        this DependencyQueue<T>          queue,
        params DependencyQueueEntry<T>[] entries)
    {
        queue.ReadyEntries.ShouldBe(entries);
    }

    public static void ShouldHaveTopicCount<T>(this DependencyQueue<T> queue, int expected)
    {
        queue.Topics.Count.ShouldBe(expected);
    }

    public static void ShouldHaveTopic<T>(
        this DependencyQueue<T>    queue,
        string                     name,
        DependencyQueueEntry<T>[]? providedBy = null,
        DependencyQueueEntry<T>[]? requiredBy = null)
    {
        var topics = queue.Topics;
        topics.Keys.Should().Contain(name);

        var topic = topics[name];
        topic.Name      .ShouldBe(name);
        topic.ProvidedBy.ShouldBe(providedBy ?? []);
        topic.RequiredBy.ShouldBe(requiredBy ?? []);
    }
}
