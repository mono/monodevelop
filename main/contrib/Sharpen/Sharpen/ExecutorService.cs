namespace Sharpen
{
	using System;

	internal interface ExecutorService : Executor
	{
		bool AwaitTermination (int n, TimeUnit unit);
		void Shutdown ();
		Future<T> Submit<T> (Callable<T> ob);
	}
}
