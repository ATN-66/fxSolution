using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MetaQuotes.Client.Indicator.To.Mediator;

public static class BlockingCollectionExtensions
{
    public static async IAsyncEnumerable<T> GetConsumingAsyncEnumerable<T>(this BlockingCollection<T> collection, [EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            while (collection.TryTake(out var item)) yield return item;
            await Task.Delay(10, ct).ConfigureAwait(false);
        }
    }
}