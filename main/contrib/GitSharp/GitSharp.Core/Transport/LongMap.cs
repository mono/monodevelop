using System.Collections.Generic;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Simple Map&lt;long,Object&gt; helper for <see cref="IndexPack"/>.
    /// </summary>
    /// <typeparam name="V">type of the value instance</typeparam>
    public class LongMap<V>
    {
        readonly Dictionary<long, V> _map = new Dictionary<long, V>();

        public bool containsKey(long key)
        {
            return _map.ContainsKey(key);
        }

        public V get(long key)
        {
            return _map.get(key);
        }

        public V remove(long key)
        {
            return _map.remove(key);
        }

        public V put(long key, V value)
        {
            return _map.put(key, value);
        }
    }
}
