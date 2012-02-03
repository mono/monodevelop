namespace Sharpen
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class TreeSet<T> : AbstractSet<T>
	{
		private SortedDictionary<T, int> dict;

		public TreeSet ()
		{
			this.dict = new SortedDictionary<T, int> ();
		}

		public TreeSet (IEnumerable<T> items)
		{
			this.dict = new SortedDictionary<T, int> ();
			foreach (var i in items)
				AddItem (i);
		}

		public override bool AddItem (T element)
		{
			if (!this.dict.ContainsKey (element)) {
				this.dict[element] = 0;
				return true;
			}
			return false;
		}

		public override void Clear ()
		{
			this.dict.Clear ();
		}

		private int Compare (T a, T b)
		{
			return Comparer<T>.Default.Compare (a, b);
		}

		public override bool Contains (object item)
		{
			return this.dict.ContainsKey ((T)item);
		}

		public T First ()
		{
			if (this.dict.Count == 0) {
				throw new NoSuchMethodException ();
			}
			return this.dict.Keys.First<T> ();
		}

		public ICollection<T> HeadSet (T toElement)
		{
			List<T> list = new List<T> ();
			foreach (T t in this) {
				if (this.Compare (t, toElement) >= 0)
					return list;
				list.Add (t);
			}
			return list;
		}

		public override Sharpen.Iterator<T> Iterator ()
		{
			return new EnumeratorWrapper<T> (this.dict.Keys, this.dict.Keys.GetEnumerator ());
		}

		public override bool Remove (object element)
		{
			return this.dict.Remove ((T)element);
		}

		public override int Count {
			get { return this.dict.Count; }
		}
		
		public override string ToString ()
		{
			return "[" + string.Join (", ", this.Select (d => d.ToString ()).ToArray ()) + "]";
		}
	}
}
