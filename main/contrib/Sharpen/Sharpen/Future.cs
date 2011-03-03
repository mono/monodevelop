namespace Sharpen
{
	using System;

	internal interface Future<T>
	{
		bool Cancel (bool mayInterruptIfRunning);
		T Get ();
	}
}
