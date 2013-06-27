using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Silverlight.PolicyServers
{
    // Implements the KeyValuePairs property of ValueSetDictionary, allowing iteration across all key/value
    // pairs in the dictionary (in which a given key may appear many times with different values).
    internal class KeyValuePairCollection<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>
    {
        private IValueSetDictionary<TKey, TValue> dictionary;

        public KeyValuePairCollection(IValueSetDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        public int Count
        {
            get
            {
                int rval = 0;
                foreach (KeyValuePair<TKey, ICollection<TValue>> pair in dictionary)
                {
                    rval += pair.Value.Count;
                }
                return rval;
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item.Key, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<TKey, ICollection<TValue>> pair in dictionary)
            {
                foreach (TValue value in pair.Value)
                {
                    array[arrayIndex] = new KeyValuePair<TKey, TValue>(pair.Key, value);
                    arrayIndex += 1;
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(dictionary);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return true; }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private IEnumerator<KeyValuePair<TKey, ICollection<TValue>>> outerEnumerator;
            private IEnumerator<TValue> innerEnumerator;
            private KeyValuePair<TKey, TValue> current;

            public Enumerator(IValueSetDictionary<TKey, TValue> dictionary)
            {
                outerEnumerator = dictionary.GetEnumerator();
                current = new KeyValuePair<TKey, TValue>();
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get { return current; }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    if (innerEnumerator != null)
                    {
                        if (innerEnumerator.MoveNext())
                        {
                            current = new KeyValuePair<TKey, TValue>(
                                outerEnumerator.Current.Key, innerEnumerator.Current);
                            return true;
                        }
                    }

                    if (!outerEnumerator.MoveNext())
                    {
                        return false;
                    }

                    innerEnumerator = outerEnumerator.Current.Value.GetEnumerator();
                }
            }

            public void Reset()
            {
                innerEnumerator = null;
                outerEnumerator.Reset();
            }

            public void Dispose()
            {
                innerEnumerator.Dispose();
                outerEnumerator.Dispose();
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}
