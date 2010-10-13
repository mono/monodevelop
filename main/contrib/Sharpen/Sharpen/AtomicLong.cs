namespace Sharpen
{
	using System;
	using System.Threading;

	internal class AtomicLong
	{
		private long val;

		public AtomicLong ()
		{
		}

		public AtomicLong (long val)
		{
			this.val = val;
		}

		public long AddAndGet (long addval)
		{
			return Interlocked.Add (ref val, addval);
		}

		public bool CompareAndSet (long expect, long update)
		{
			return (Interlocked.CompareExchange (ref val, update, expect) == expect);
		}

		public long DecrementAndGet ()
		{
			return Interlocked.Decrement (ref val);
		}

		public long Get ()
		{
			return val;
		}

		public long IncrementAndGet ()
		{
			return Interlocked.Increment (ref val);
		}
	}
}
