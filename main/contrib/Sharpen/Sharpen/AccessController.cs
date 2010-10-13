namespace Sharpen
{
	using System;

	internal class AccessController
	{
		public static T DoPrivileged<T> (PrivilegedAction<T> action)
		{
			return action.Run ();
		}
	}
}
