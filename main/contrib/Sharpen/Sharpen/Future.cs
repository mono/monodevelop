namespace Sharpen
{
	using System;

	internal interface Future<T>
	{
		bool Cancel (bool b);
		T Get ();
	}
}
