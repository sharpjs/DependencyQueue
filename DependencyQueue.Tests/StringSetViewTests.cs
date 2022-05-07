/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

namespace DependencyQueue;

using Collection = SortedSet<string>;
using View       = StringSetView;
using Inner      = String;
using Outer      = String;
using Lock       = AsyncMonitor.Lock;

[TestFixture]
public class StringSetViewTests
    : CollectionViewTests<Collection, Inner, View, Outer, View.Enumerator>
{
    private protected override Inner ItemA { get; } = "a";
    private protected override Inner ItemB { get; } = "b";

    private protected override Collection CreateCollection()
        => new() { ItemA, ItemB };

    private protected override View CreateView(Collection collection, Lock @lock)
        => new(collection, @lock);

    private protected override Collection Unwrap(View view)
        => view.Set;

    private protected override Inner Unwrap(Outer view)
        => view;
}
