namespace Sharpen
{
	using System;
	using System.Threading;

	internal class AtomicInteger
	{
		private int val;

		public AtomicInteger ()
		{
		}

		public AtomicInteger (int val)
		{
			this.val = val;
		}

		public int AddAndGet (int addval)
		{
			return Interlocked.Add (ref val, addval);
		}

		public bool CompareAndSet (int expect, int update)
		{
			return (Interlocked.CompareExchange (ref val, update, expect) == expect);
		}

		public int DecrementAndGet ()
		{
			return Interlocked.Decrement (ref val);
		}

		public int Get ()
		{
			return this.val;
		}

		public int IncrementAndGet ()
		{
			return Interlocked.Increment (ref val);
		}
	}
}
