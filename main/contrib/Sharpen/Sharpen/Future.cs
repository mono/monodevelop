namespace Sharpen
{
	using System;

	internal interface Future<T>
	{
		void Cancel (bool b);
		void Get ();
	}
}
