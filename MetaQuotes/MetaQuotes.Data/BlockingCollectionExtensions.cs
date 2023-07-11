/*+------------------------------------------------------------------+
  |                                                  MetaQuotes.Data |
  |                                  BlockingCollectionExtensions.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MetaQuotes.Data;

public static class BlockingCollectionExtensions
{
    public static async IAsyncEnumerable<T> GetConsumingAsyncEnumerable<T>(this BlockingCollection<T> collection, [EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            while (collection.TryTake(out var item))
            {
                yield return item;
            }

            try
            {
                await Task.Delay(10, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}