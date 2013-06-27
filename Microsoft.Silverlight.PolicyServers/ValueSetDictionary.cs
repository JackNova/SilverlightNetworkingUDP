using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Silverlight.PolicyServers
{
    // A simple implementation of IValueSetDictionary that stores sets of values in a HashSet
    internal class ValueSetDictionary<TKey, TValue> : IValueSetDictionary<TKey, TValue>
    {
        private Dictionary<TKey, ICollection<TValue>> innerCollection;
        private IEqualityComparer<TKey> keyComparer;
        private IEqualityComparer<TValue> valueComparer;

        private KeyValuePairCollection<TKey, TValue> keyValuePairs;
        private bool isReadOnly;

        public ValueSetDictionary() 
            : this(0, null, null) 
        {
        }

        public ValueSetDictionary(IEqualityComparer<TKey> keyComparer)
            : this(0, keyComparer, null)
        {
        }

        public ValueSetDictionary(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
            : this(0, keyComparer, valueComparer)
        {
        }

        public ValueSetDictionary(int capacity, IEqualityComparer<TKey> keyComparer, 
            IEqualityComparer<TValue> valueComparer)
        {
            this.innerCollection = new Dictionary<TKey, ICollection<TValue>>(capacity, keyComparer);
            this.keyComparer = keyComparer;
            this.valueComparer = valueComparer;
        }

        public int Count
        {
            get { return innerCollection.Count; }
        }

        public bool IsReadOnly
        {
            get { return isReadOnly; }
        }

        public ICollection<TKey> Keys
        {
            get { return innerCollection.Keys; }
        }

        public ICollection<ICollection<TValue>> Values
        {
            get { return innerCollection.Values; }
        }

        public ICollection<KeyValuePair<TKey, TValue>> KeyValuePairs
        {
            get
            {
                if (keyValuePairs == null)
                {
                    keyValuePairs = new KeyValuePairCollection<TKey, TValue>(this);
                }
                return keyValuePairs;
            }
        }

        public ICollection<TValue> this[TKey key]
        {
            get
            {
                return innerCollection[key];
            }
            set
            {
                HashSet<TValue> copy = new HashSet<TValue>(value, valueComparer);
                innerCollection[key] = copy;
            }
        }

        public ValueSetDictionary<TKey, TValue> MakeReadOnlyCopy()
        {
            if (isReadOnly)
            {
                return this;
            }

            ValueSetDictionary<TKey, TValue> copy = new ValueSetDictionary<TKey, TValue>(
                innerCollection.Count, keyComparer, valueComparer);

            copy.isReadOnly = true;

            foreach (KeyValuePair<TKey, ICollection<TValue>> pair in innerCollection)
            {
                copy.innerCollection.Add(pair.Key, new ReadOnlyHashSet<TValue>(
                    pair.Value, valueComparer));
            }

            return copy;
        }

        public void Add(TKey key, ICollection<TValue> value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            CheckReadOnly();

            if (value.Count > 0)
            {
                ICollection<TValue> innerValue = GetOrCreateValueSet(key);
                foreach (TValue v in value)
                {
                    innerValue.Add(v);
                }
            }
        }

        public bool Add(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            CheckReadOnly();

            ICollection<TValue> innerValue = GetOrCreateValueSet(key);
            if (!innerValue.Contains(value))
            {
                innerValue.Add(value);
                return true;
            }
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return innerCollection.ContainsKey(key);
        }

        public bool Contains(TKey key, TValue value)
        {
            ICollection<TValue> valueSet;
            if (innerCollection.TryGetValue(key, out valueSet))
            {
                return valueSet.Contains(value);
            }
            else
            {
                return false;
            }
        }

        public bool Remove(TKey key)
        {
            CheckReadOnly();
            return innerCollection.Remove(key);
        }

        public bool Remove(TKey key, TValue value)
        {
            CheckReadOnly();

            ICollection<TValue> valueSet;
            if (innerCollection.TryGetValue(key, out valueSet))
            {
                return valueSet.Remove(value);
            }
            else
            {
                return false;
            }
        }

        public bool TryGetValue(TKey key, out ICollection<TValue> value)
        {
            return innerCollection.TryGetValue(key, out value);
        }

        public void Clear()
        {
            CheckReadOnly();
            innerCollection.Clear();
        }

        public IEnumerator<KeyValuePair<TKey, ICollection<TValue>>> GetEnumerator()
        {
            return innerCollection.GetEnumerator();
        }

        void ICollection<KeyValuePair<TKey, ICollection<TValue>>>.
            Add(KeyValuePair<TKey, ICollection<TValue>> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, ICollection<TValue>>>.
            Contains(KeyValuePair<TKey, ICollection<TValue>> item)
        {
            return ((ICollection<KeyValuePair<TKey, ICollection<TValue>>>)innerCollection).Contains(item);
        }

        void ICollection<KeyValuePair<TKey, ICollection<TValue>>>.
            CopyTo(KeyValuePair<TKey, ICollection<TValue>>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, ICollection<TValue>>>)innerCollection).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, ICollection<TValue>>>.
            Remove(KeyValuePair<TKey, ICollection<TValue>> item)
        {
            CheckReadOnly();
            return ((ICollection<KeyValuePair<TKey, ICollection<TValue>>>)innerCollection).Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();
        }

        private ICollection<TValue> GetOrCreateValueSet(TKey key)
        {
            ICollection<TValue> rval;
            if (!innerCollection.TryGetValue(key, out rval))
            {
                rval = new HashSet<TValue>(valueComparer);
                innerCollection.Add(key, rval);
            }

            return rval;
        }

        private void CheckReadOnly()
        {
            if (isReadOnly)
            {
                throw new NotSupportedException("Collection is read-only");
            }
        }
    }
}
