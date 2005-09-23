using System;

public partial class Foo
{
	public partial interface IInternal
	{
	}
}

partial interface IFoo
{
}

partial struct VFoo
{
}

class Bar {
	static void Main () {}
}
