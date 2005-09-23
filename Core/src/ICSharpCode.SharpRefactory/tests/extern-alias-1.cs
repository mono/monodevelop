extern alias SystemRef;
extern alias FooBar;

using System;

class T
{
	static void Main ()
	{
		SystemRef::Console.WriteLine ("hello");
		global::Console.WriteLine ("hello");
		FooBar::Console.WriteLine ("hello");
	}
}

class Console
{
	void WriteLine (string line)
	{
	}
}
