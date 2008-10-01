using System;
using System.Collections.Generic;
using System.Text;

namespace ClassLibrary1
{
	public class Class1
	{
		public static void Foo ()
		{
			ClassLibrary2.Class2.Foo ();
			ClassLibrary3.Class3.Foo ();
		}
	}
}
