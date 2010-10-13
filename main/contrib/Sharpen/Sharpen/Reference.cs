namespace Sharpen
{
	using System;

	internal abstract class Reference<T>
	{
		protected Reference ()
		{
		}

		public abstract T Get ();
	}
}
