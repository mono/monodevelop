using System;
using LibA;
using LibB;

namespace LibC
{
	public class ClassFromLibC
	{
		public string Id { get; set; } = "LibC";

		public ClassFromLibC ()
		{
			A = new ClassFromLibA ();
			B = new ClassFromLibB ();
		}

		public ClassFromLibA A { get; set; }
		public ClassFromLibB B { get; set; }
	}
}
