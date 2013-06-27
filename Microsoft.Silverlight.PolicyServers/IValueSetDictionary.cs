using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Silverlight.PolicyServers
{
    // Maps a single key onto one or more values.  In addition to looking up all values
    // for a given key, supports enumerating the entire collection of key/value pairs (in 
    // which a given key may appear more than once)
    public interface IValueSetDictionary<TKey, TValue>
        : IDictionary<TKey, ICollection<TValue>>
    {
        ICollection<KeyValuePair<TKey, TValue>> KeyValuePairs { get; }

        bool Add(TKey key, TValue value);
        bool Contains(TKey key, TValue value);
        bool Remove(TKey key, TValue value);
    }
}
