namespace Sharpen
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public abstract class AbstractCollection<T> : Iterable<T>, IEnumerable, ICollection<T>, IEnumerable<T>
	{
		protected AbstractCollection ()
		{
		}

		public virtual bool AddItem (T element)
		{
			throw new NotSupportedException ();
		}

		public virtual void Clear ()
		{
			Iterator iterator = Iterator ();
			while (iterator.HasNext ()) {
				iterator.Next ();
				iterator.Remove ();
			}
		}

		public virtual bool Contains (object item)
		{
			foreach (var t in this) {
				if (object.ReferenceEquals (t, item) || t.Equals (item))
					return true;
			}
			return false;
		}

		public virtual bool ContainsAll (ICollection<object> c)
		{
			foreach (var t in c) {
				if (!Contains (t))
					return false;
			}
			return true;
		}

		public bool ContainsAll (ICollection<T> c)
		{
			List<object> list = new List<object> (c.Count);
			foreach (var t in c)
				list.Add (t);
			return ContainsAll ((ICollection<object>)list);
		}

		public virtual bool IsEmpty ()
		{
			return (this.Count == 0);
		}

		public virtual bool Remove (object element)
		{
			Iterator iterator = Iterator ();
			while (iterator.HasNext ()) {
				if (iterator.Next ().Equals (element)) {
					iterator.Remove ();
					return true;
				}
			}
			return false;
		}

		void ICollection<T>.Add (T element)
		{
			AddItem (element);
		}

		bool ICollection<T>.Contains (T item)
		{
			return Contains (item);
		}

		void ICollection<T>.CopyTo (T[] array, int arrayIndex)
		{
			foreach (T t in this)
				array[arrayIndex++] = t;
		}

		bool ICollection<T>.Remove (T item)
		{
			return Remove (item);
		}

		public abstract int Count { get; }

		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}
	}
}
