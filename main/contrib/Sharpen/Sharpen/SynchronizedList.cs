namespace Sharpen
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Reflection;

	internal class SynchronizedList<T> : IEnumerable, ICollection<T>, IList<T>, IEnumerable<T>
	{
		private IList<T> list;

		public SynchronizedList (IList<T> list)
		{
			this.list = list;
		}

		public int IndexOf (T item)
		{
			lock (list) {
				return list.IndexOf (item);
			}
		}

		public void Insert (int index, T item)
		{
			lock (list) {
				list.Insert (index, item);
			}
		}

		public void RemoveAt (int index)
		{
			lock (list) {
				list.RemoveAt (index);
			}
		}

		void ICollection<T>.Add (T item)
		{
			lock (list) {
				list.Add (item);
			}
		}

		void ICollection<T>.Clear ()
		{
			lock (list) {
				list.Clear ();
			}
		}

		bool ICollection<T>.Contains (T item)
		{
			lock (list) {
				return list.Contains (item);
			}
		}

		void ICollection<T>.CopyTo (T[] array, int arrayIndex)
		{
			lock (list) {
				list.CopyTo (array, arrayIndex);
			}
		}

		bool ICollection<T>.Remove (T item)
		{
			lock (list) {
				return list.Remove (item);
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			return list.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return list.GetEnumerator ();
		}

		public T this[int index] {
			get {
				lock (list) {
					return list[index];
				}
			}
			set {
				lock (list) {
					list[index] = value;
				}
			}
		}

		int ICollection<T>.Count {
			get {
				lock (list) {
					return list.Count;
				}
			}
		}

		bool ICollection<T>.IsReadOnly {
			get { return list.IsReadOnly; }
		}
	}
}
