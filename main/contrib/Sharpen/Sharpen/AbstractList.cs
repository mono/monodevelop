namespace Sharpen
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Reflection;

	public abstract class AbstractList<T> : AbstractCollection<T>, IEnumerable, ICollection<T>, IEnumerable<T>, IList<T>
	{
		protected AbstractList ()
		{
		}

		public override bool AddItem (T element)
		{
			Add (Count, element);
			return true;
		}

		public virtual void Add (int index, T element)
		{
			throw new NotSupportedException ();
		}

		public virtual bool AddAll<Q>(ICollection<Q> c) where Q:T
		{
			foreach (var q in c)
				AddItem (q);
			return true;
		}

		public override void Clear ()
		{
			RemoveRange (0, Count);
		}

		public abstract T Get (int index);
		
		public override Iterator<T> Iterator ()
		{
			return new SimpleIterator (this);
		}

		public virtual T Remove (int index)
		{
			if (index < 0) {
				throw new IndexOutOfRangeException ();
			}
			int num = 0;
			object item = null;
			Sharpen.Iterator iterator = this.Iterator ();
			while (num <= index) {
				if (!iterator.HasNext ()) {
					throw new IndexOutOfRangeException ();
				}
				item = iterator.Next ();
				num++;
			}
			iterator.Remove ();
			return (T)item;
		}

		public virtual void RemoveRange (int index, int toIndex)
		{
			int num = 0;
			Sharpen.Iterator iterator = this.Iterator ();
			while (num <= index) {
				if (!iterator.HasNext ()) {
					throw new IndexOutOfRangeException ();
				}
				iterator.Next ();
				num++;
			}
			if (index < toIndex) {
				iterator.Remove ();
			}
			for (num = index + 1; num < toIndex; num++) {
				if (!iterator.HasNext ()) {
					throw new IndexOutOfRangeException ();
				}
				iterator.Next ();
				iterator.Remove ();
			}
		}

		public virtual T Set (int index, T element)
		{
			throw new NotSupportedException ();
		}
		
		public override bool Equals (object obj)
		{
			if (obj == this)
				return true;
			IList list = obj as IList;
			if (list == null)
				return false;
			if (list.Count != Count)
				return false;
			for (int n=0; n<list.Count; n++) {
				if (!object.Equals (Get(n), list[n]))
					return false;
			}
			return true;
		}
		
		public override int GetHashCode ()
		{
			int h = 0;
			foreach (object o in this)
				if (o != null)
					h += o.GetHashCode ();
			return h;
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

		public T this[int n] {
			get { return Get (n); }
			set { Set (n, value); }
		}

		private class SimpleIterator : Iterator<T>
		{
			private int current;
			private AbstractList<T> list;

			public SimpleIterator (AbstractList<T> list)
			{
				this.current = 0;
				this.list = list;
			}

			public override bool HasNext ()
			{
				return (current < list.Count);
			}

			public override T Next ()
			{
				return list.Get (current++);
			}

			public override void Remove ()
			{
				list.Remove (--current);
			}
		}
	}
}
