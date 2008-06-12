using System;

namespace MonoDevelop.CodeAnalysis {
	public static class Argument
	{
		public static void NotNull (object value, string name)
		{
			if (value == null)
				throw new ArgumentNullException (name);
		}
	}
}
