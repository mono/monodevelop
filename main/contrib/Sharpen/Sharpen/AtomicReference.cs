namespace Sharpen
{
	using System;
	using System.Threading;

	internal class AtomicReference<T> where T : class
	{
		private T val;

		public AtomicReference ()
		{
		}

		public AtomicReference (T val)
		{
			this.val = val;
		}

		public bool CompareAndSet (T expect, T update)
		{
			return (Interlocked.CompareExchange<T> (ref val, update, expect) == expect);
		}

		public T Get ()
		{
			return val;
		}

		public void Set (T t)
		{
			val = t;
		}
	}
}
