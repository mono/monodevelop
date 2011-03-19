using System;

namespace Sharpen
{
	public class WeakReference<T>: WeakReference
	{
		public WeakReference (T t): base (t)
		{
		}
		
		public T Get ()
		{
			return (T) Target;
		}
	}
}

