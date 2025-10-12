// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

internal static class TestExtensions
{
    public static void ShouldBeValid<T>(this DependencyQueue<T> queue)
    {
        queue.Validate().ShouldBeEmpty();
    }

    public static void ShouldNotHaveReadyItems<T>(this DependencyQueue<T> queue)
    {
        queue.ReadyItems.ShouldBeEmpty();
    }

    public static void ShouldHaveReadyItems<T>(
        this DependencyQueue<T>         queue,
        params DependencyQueueItem<T>[] items)
    {
        queue.ReadyItems.ShouldBe(items);
    }

    public static void ShouldHaveTopicCount<T>(this DependencyQueue<T> queue, int expected)
    {
        queue.Topics.Count.ShouldBe(expected);
    }

    public static void ShouldHaveTopic<T>(
        this DependencyQueue<T>   queue,
        string                    name,
        DependencyQueueItem<T>[]? providedBy = null,
        DependencyQueueItem<T>[]? requiredBy = null)
    {
        var topics = queue.Topics;
        topics.Keys.ShouldContain(name);

        var topic = topics[name];
        topic.Name      .ShouldBe(name);
        topic.ProvidedBy.ShouldBe(providedBy ?? []);
        topic.RequiredBy.ShouldBe(requiredBy ?? []);
    }
}
