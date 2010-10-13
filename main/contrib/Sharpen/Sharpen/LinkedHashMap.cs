namespace Sharpen
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;

	internal class LinkedHashMap<T, U> : AbstractMap<T, U>
	{
		private List<KeyValuePair<T, U>> list;
		private Dictionary<T, U> table;

		public LinkedHashMap ()
		{
			this.table = new Dictionary<T, U> ();
			this.list = new List<KeyValuePair<T, U>> ();
		}

		public override void Clear ()
		{
			table.Clear ();
			list.Clear ();
		}
		
		public override int Count {
			get {
				return list.Count;
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
			return list.GetEnumerator ();
		}

		public override bool IsEmpty ()
		{
			return (table.Count == 0);
		}

		public override U Put (T key, U value)
		{
			U old;
			if (table.TryGetValue (key, out old)) {
				int index = list.FindIndex (p => p.Key.Equals (key));
				if (index != -1)
					list.RemoveAt (index);
			}
			table[key] = value;
			list.Add (new KeyValuePair<T, U> (key, value));
			return old;
		}

		public override U Remove (object key)
		{
			U local = default(U);
			if (table.TryGetValue ((T)key, out local)) {
				int index = list.FindIndex (p => p.Key.Equals (key));
				if (index != -1)
					list.RemoveAt (index);
				table.Remove ((T)key);
			}
			return local;
		}

		public override IEnumerable<T> Keys {
			get { return list.Select (p => p.Key); }
		}

		public override IEnumerable<U> Values {
			get { return list.Select (p => p.Value); }
		}
	}
}
