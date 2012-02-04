using System;

namespace Sharpen
{
	internal class ThreadFactory
	{
		public Thread NewThread (Runnable r)
		{
			Thread t = new Thread (r);
			t.Start ();
			return t;
		}
	}
}
