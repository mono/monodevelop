using System.Collections.Generic;

namespace Sharpen
{
	using System;

	public abstract class AbstractSet<T> : AbstractCollection<T>
	{
		protected AbstractSet ()
		{
		}
		
		public override bool Equals (object obj)
		{
			if (obj == this)
				return true;
			ICollection<T> c = obj as ICollection<T>;
			if (c != null) {
				if (c.Count != Count)
					return false;
				return ContainsAll (c);
			}
			ICollection<object> co = obj as ICollection<object>;
			if (co != null) {
				if (co.Count != Count)
					return false;
				return ContainsAll (co);
			}
			return false;
		}
		
		public override int GetHashCode ()
		{
			int t = 0;
			foreach (object o in this)
				if (o != null)
					t += o.GetHashCode ();
			return t;
		}
	}
}
