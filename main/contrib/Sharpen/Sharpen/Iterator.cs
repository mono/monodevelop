namespace Sharpen
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public interface Iterator
	{
		bool HasNext ();
		object Next ();
		void Remove ();
	}

	public abstract class Iterator<T> : IEnumerator, IDisposable, IEnumerator<T>, Iterator
	{
		private T lastValue;

		protected Iterator ()
		{
		}

		object Iterator.Next ()
		{
			return Next ();
		}

		public abstract bool HasNext ();
		public abstract T Next ();
		public abstract void Remove ();

		bool IEnumerator.MoveNext ()
		{
			if (HasNext ()) {
				lastValue = Next ();
				return true;
			}
			return false;
		}

		void IEnumerator.Reset ()
		{
			throw new NotImplementedException ();
		}

		void IDisposable.Dispose ()
		{
		}

		T IEnumerator<T>.Current {
			get { return lastValue; }
		}

		object IEnumerator.Current {
			get { return lastValue; }
		}
	}
}
