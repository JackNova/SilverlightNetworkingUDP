using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Silverlight.PolicyServers
{
    // A read-only wrapper for a HashSet, used to disallow modification of the configuration after
    // it has been passed to the MulticastPolicyServer
    internal class ReadOnlyHashSet<T> : ICollection<T>
    {
        private HashSet<T> hashSet;

        public ReadOnlyHashSet(ICollection<T> values, IEqualityComparer<T> comparer)
        {
            hashSet = new HashSet<T>(values, comparer);
        }

        public int Count
        {
            get { return hashSet.Count; }
        }

        public bool Contains(T item)
        {
            return hashSet.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            hashSet.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return hashSet.GetEnumerator();
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException("Collection is read-only");
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException("Collection is read-only");
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException("Collection is read-only");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return hashSet.GetEnumerator();
        }
    }
}
