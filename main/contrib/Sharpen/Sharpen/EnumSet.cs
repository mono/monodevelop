namespace Sharpen
{
	using System;
	using System.Collections.Generic;

	public class EnumSet
	{
		public static EnumSet<U> Of<U> (U e)
		{
			EnumSet<U> @set = new EnumSet<U> ();
			@set.AddItem (e);
			return @set;
		}
		
		public static EnumSet<U> Of<U> (params U[] es)
		{
			return CopyOf (es);
		}
		
		public static EnumSet<T> CopyOf<T> (ICollection<T> c)
		{
			EnumSet<T> @set = new EnumSet<T> ();
			foreach (T e in c)
				@set.AddItem (e);
			return @set;
		}
	}

	public class EnumSet<T> : AbstractSet<T>
	{
		// Fields
		private HashSet<T> hset;

		// Methods
		public EnumSet ()
		{
			this.hset = new HashSet<T> ();
		}

		public override bool AddItem (T item)
		{
			return this.hset.Add (item);
		}

		public override void Clear ()
		{
			this.hset.Clear ();
		}
		
		public virtual EnumSet<T> Clone ()
		{
			EnumSet<T> @set = new EnumSet<T> ();
			@set.hset = new HashSet<T> (this.hset);
			return @set;
		}

		public override bool Contains (object o)
		{
			return this.hset.Contains ((T)o);
		}

		public override Iterator<T> Iterator ()
		{
			return new EnumeratorWrapper<T> (this.hset, this.hset.GetEnumerator ());
		}

		public static EnumSet<U> Of<U> (U e)
		{
			EnumSet<U> @set = new EnumSet<U> ();
			@set.AddItem (e);
			return @set;
		}

		public override bool Remove (object item)
		{
			return this.hset.Remove ((T)item);
		}

		// Properties
		public override int Count {
			get { return this.hset.Count; }
		}
	}
}
