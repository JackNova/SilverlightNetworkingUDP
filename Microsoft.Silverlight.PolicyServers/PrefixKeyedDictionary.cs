using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Silverlight.PolicyServers
{
    // An IValueSetDictionary whose keys are url prefixes.  Allows querying the dictionary with a full
    // url to find the set of values associated with the best-matching prefix.
    // 
    // Prefix                                Matches
    //  *                                     everything not matched by a more specific rule
    //  https://                              all https apps not matched by a more specific rule
    //  https://www.contoso.com/              all apps from contoso.com not mached by a more specific rule
    //  https://www.contoso.com/apps/         all from the apps/ directory of contoso.com not mached by a more specific rule
    //  https://www.contoso.com/apps/app.xap  only this specific xap

    internal class PrefixKeyedDictionary<TValue> : IValueSetDictionary<string, TValue>
    {
        private ValueSetDictionary<string, TValue> innerCollection;

        public PrefixKeyedDictionary()
        {
            innerCollection = new ValueSetDictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
        }

        private PrefixKeyedDictionary(ValueSetDictionary<string, TValue> innerCollection)
        {
            this.innerCollection = innerCollection;
        }

        public int Count
        {
            get { return innerCollection.Count; }
        }

        public bool IsReadOnly
        {
            get { return innerCollection.IsReadOnly; }
        }

        public ICollection<string> Keys
        {
            get { return innerCollection.Keys; }
        }

        public ICollection<ICollection<TValue>> Values
        {
            get { return innerCollection.Values; }
        }

        public ICollection<KeyValuePair<string, TValue>> KeyValuePairs
        {
            get { return innerCollection.KeyValuePairs; }
        }

        public ICollection<TValue> this[string key]
        {
            get
            {
                return innerCollection[key];
            }
            set
            {
                innerCollection[key] = value;
            }
        }

        public PrefixKeyedDictionary<TValue> MakeReadOnlyCopy()
        {
            if (innerCollection.IsReadOnly)
            {
                return this;
            }

            return new PrefixKeyedDictionary<TValue>(innerCollection.MakeReadOnlyCopy());
        }

        public bool Add(string key, TValue value)
        {
            return innerCollection.Add(key, value);
        }

        public void Add(string key, ICollection<TValue> value)
        {
            innerCollection.Add(key, value);
        }

        public bool Contains(string key, TValue value)
        {
            return innerCollection.Contains(key, value);
        }

        public bool ContainsKey(string key)
        {
            return innerCollection.ContainsKey(key);
        }

        public bool Remove(string key, TValue value)
        {
            return innerCollection.Remove(key, value);
        }

        public bool Remove(string key)
        {
            return innerCollection.Remove(key);
        }

        public void Clear()
        {
            innerCollection.Clear();
        }

        public bool TryGetValue(string key, out ICollection<TValue> value)
        {
            return innerCollection.TryGetValue(key, out value);
        }

        public bool TryGetValueByPrefixMatch(string key, out ICollection<TValue> value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            int longestMatch = -1;
            ICollection<TValue> longestMatchValue = null;

            foreach (KeyValuePair<string, ICollection<TValue>> pair in innerCollection)
            {
                int matchLength = MatchLength(key, pair.Key);
                if (matchLength > longestMatch)
                {
                    longestMatch = matchLength;
                    longestMatchValue = pair.Value;

                    if (matchLength == Int32.MaxValue)
                    {
                        // won't find anything better than an exact match, quit early
                        break;
                    }
                }
            }

            value = longestMatchValue;
            return (longestMatch > -1);
        }

        public ICollection<TValue> GetValueByPrefixMatch(string key)
        {
            ICollection<TValue> rval;
            if (!TryGetValueByPrefixMatch(key, out rval))
            {
                throw new KeyNotFoundException();
            }
            return rval;
        }

        public IEnumerator<KeyValuePair<string, ICollection<TValue>>> GetEnumerator()
        {
            return innerCollection.GetEnumerator();
        }

        void ICollection<KeyValuePair<string, ICollection<TValue>>>.
            Add(KeyValuePair<string, ICollection<TValue>> item)
        {
            innerCollection.Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<string, ICollection<TValue>>>.
            Contains(KeyValuePair<string, ICollection<TValue>> item)
        {
            return ((ICollection<KeyValuePair<string, ICollection<TValue>>>)innerCollection).Contains(item);
        }

        void ICollection<KeyValuePair<string, ICollection<TValue>>>.
            CopyTo(KeyValuePair<string, ICollection<TValue>>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, ICollection<TValue>>>)innerCollection).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, ICollection<TValue>>>.
            Remove(KeyValuePair<string, ICollection<TValue>> item)
        {
            return ((ICollection<KeyValuePair<string, ICollection<TValue>>>)innerCollection).Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static int MatchLength(string key, string prefix)
        {
            Debug.Assert(key != null, "key cannot be null");
            Debug.Assert(prefix != null, "prefix cannot be null");

            if (String.Equals(prefix, "*", StringComparison.Ordinal))
            {
                // it matches, but it's the loosest match
                return 0;
            }

            if (prefix.EndsWith("/", StringComparison.Ordinal))
            {
                // it's a directory, do a prefix match
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return prefix.Length;
                }
            }
            else
            {
                // it's not a directory: do an exact match
                if (String.Equals(key, prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return Int32.MaxValue;
                }
            }

            // otherwise it didn't match
            return -1;
        }
    }
}
