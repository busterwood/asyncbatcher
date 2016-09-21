using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BusterWood.AsyncBatcher
{
    static class Timeout
    {
        public static readonly TimeSpan Never = TimeSpan.FromMilliseconds(-1);
    }

    static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DontWait(this Task t) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, Func<TKey, TValue> valueFactory)
        {
            TValue value;
            if (!dic.TryGetValue(key, out value))
            {
                value = valueFactory(key);
                dic.Add(key, value);
            }
            return value;
        }
    }
}
