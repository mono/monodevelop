namespace Sharpen
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	internal class ConcurrentHashMap<T, U> : AbstractMap<T, U>, IEnumerable, ConcurrentMap<T, U>, IDictionary<T, U>, IEnumerable<KeyValuePair<T, U>>, ICollection<KeyValuePair<T, U>>
	{
		private Dictionary<T, U> table;

		public ConcurrentHashMap ()
		{
			table = new Dictionary<T, U> ();
		}

		public ConcurrentHashMap (int initialCapacity, float loadFactor, int concurrencyLevel)
		{
			table = new Dictionary<T, U> (initialCapacity);
		}

		public override void Clear ()
		{
			lock (table) {
				table = new Dictionary<T, U> ();
			}
		}

		public override bool ContainsKey (object name)
		{
			return table.ContainsKey ((T)name);
		}

		public override ICollection<KeyValuePair<T, U>> EntrySet ()
		{
			return this;
		}

		public override U Get (object key)
		{
			U local;
			table.TryGetValue ((T)key, out local);
			return local;
		}

		protected override IEnumerator<KeyValuePair<T, U>> InternalGetEnumerator ()
		{
			return table.GetEnumerator ();
		}

		public override bool IsEmpty ()
		{
			return table.Count == 0;
		}

		public override U Put (T key, U value)
		{
			lock (table) {
				U old = Get (key);
				Dictionary<T, U> newTable = new Dictionary<T, U> (table);
				newTable[key] = value;
				table = newTable;
				return old;
			}
		}

		public U PutIfAbsent (T key, U value)
		{
			lock (table) {
				if (!ContainsKey (key)) {
					Dictionary<T, U> newTable = new Dictionary<T, U> (table);
					newTable[key] = value;
					table = newTable;
					return value;
				}
				return Get (key);
			}
		}

		public override U Remove (object key)
		{
			lock (table) {
				U old = Get ((T)key);
				Dictionary<T, U> newTable = new Dictionary<T, U> (table);
				newTable.Remove ((T)key);
				table = newTable;
				return old;
			}
		}

		public bool Remove (object key, object value)
		{
			lock (table) {
				if (ContainsKey (key) && value.Equals (Get (key))) {
					Dictionary<T, U> newTable = new Dictionary<T, U> (table);
					newTable.Remove ((T)key);
					table = newTable;
					return true;
				}
				return false;
			}
		}

		public bool Replace (T key, U oldValue, U newValue)
		{
			lock (table) {
				if (ContainsKey (key) && oldValue.Equals (Get (key))) {
					Dictionary<T, U> newTable = new Dictionary<T, U> (table);
					newTable[key] = newValue;
					table = newTable;
					return true;
				}
				return false;
			}
		}

		public override IEnumerable<T> Keys {
			get { return table.Keys; }
		}

		public override IEnumerable<U> Values {
			get { return table.Values; }
		}
	}
}
