namespace Sharpen
{
	using System;
	using System.Threading;

	internal class CountDownLatch
	{
		private int count;
		private ManualResetEvent done = new ManualResetEvent (false);

		public CountDownLatch (int count)
		{
			this.count = count;
			if (count == 0) {
				done.Set ();
			}
		}

		public void Await ()
		{
			done.WaitOne ();
		}

		public void CountDown ()
		{
			if (Interlocked.Decrement (ref count) == 0) {
				done.Set ();
			}
		}
	}
}
