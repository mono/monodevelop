namespace Sharpen
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Reflection;

	internal class CopyOnWriteArrayList<T> : Iterable<T>, IEnumerable, ICollection<T>, IEnumerable<T>, IList<T>
	{
		private List<T> list;

		public CopyOnWriteArrayList ()
		{
			this.list = new List<T> ();
		}

		public virtual void Add (T element)
		{
			lock (list) {
				List<T> newList = new List<T> (list);
				newList.Add (element);
				list = newList;
			}
		}

		public virtual void Add (int index, T element)
		{
			lock (list) {
				List<T> newList = new List<T> (list);
				newList.Insert (index, element);
				list = newList;
			}
		}

		public virtual void Clear ()
		{
			lock (list) {
				list = new List<T> ();
			}
		}

		public virtual T Get (int index)
		{
			return list[index];
		}

		public override Iterator<T> Iterator ()
		{
			return new EnumeratorWrapper<T> (list, list.GetEnumerator ());
		}

		public virtual T Remove (int index)
		{
			lock (list) {
				T old = list[index];
				List<T> newList = new List<T> (list);
				newList.RemoveAt (index);
				list = newList;
				return old;
			}
		}

		public virtual T Set (int index, T element)
		{
			lock (list) {
				T old = list[index];
				List<T> newList = new List<T> (list);
				newList[index] = element;
				list = newList;
				return old;
			}
		}

		bool ICollection<T>.Contains (T item)
		{
			return list.Contains (item);
		}

		void ICollection<T>.CopyTo (T[] array, int arrayIndex)
		{
			list.CopyTo (array, arrayIndex);
		}

		bool ICollection<T>.Remove (T item)
		{
			lock (list) {
				List<T> newList = new List<T> (list);
				bool removed = newList.Remove (item);
				list = newList;
				return removed;
			}
		}

		int IList<T>.IndexOf (T item)
		{
			int num = 0;
			foreach (T t in this) {
				if (object.ReferenceEquals (t, item) || t.Equals (item))
					return num;
				num++;
			}
			return -1;
		}

		void IList<T>.Insert (int index, T item)
		{
			Add (index, item);
		}

		void IList<T>.RemoveAt (int index)
		{
			Remove (index);
		}

		public virtual int Count {
			get { return list.Count; }
		}

		public T this[int n] {
			get { return Get (n); }
			set { Set (n, value); }
		}

		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}
	}
}
