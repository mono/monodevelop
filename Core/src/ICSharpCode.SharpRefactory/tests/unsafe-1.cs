using System;

unsafe interface IFoo
{
}

unsafe class T
{
	unsafe int* counter;

	unsafe private void Foo ()
	{
	}

	unsafe int Bar {
		get { return 0; }
	}

	unsafe public event EventHandler Notify;
}

unsafe struct Foo
{
}

unsafe delegate void FooBar ();

