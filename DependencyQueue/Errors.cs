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

internal static class Errors
{
    internal static Exception ArgumentNull(string name)
        => new ArgumentNullException(name);

    internal static Exception ArgumentEmpty(string name)
        => new ArgumentException("Argument cannot be empty.", name);

    internal static Exception ArgumentContainsNull(string name)
        => new ArgumentException("Argument cannot contain a null item.", name);

    internal static Exception ArgumentContainsEmpty(string name)
        => new ArgumentException("Argument cannot contain an mepty item.", name);

    internal static Exception ArgumentOutOfRange(string name)
        => new ArgumentOutOfRangeException(name);

    internal static Exception ObjectDisposed(string? name)
        => new ObjectDisposedException(name);

    internal static Exception NoCurrentEntry()
        => new InvalidOperationException(
            "The builder does not have a current entry.  " +
            "Use the NewEntry method to begin building an entry."
        );

    internal static Exception NotValid()
        => new InvalidOperationException(
            "The queue state is invalid or has not been validated.  " +
            "Use the Validate method and correct any errors it returns."
        );
}
