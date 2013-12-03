// 
// Main.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace MonoDevelop.Debugger.Tests.TestApp
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			MainClass mc = new MainClass ();
			typeof (MainClass).GetMethod (args[0]).Invoke (mc, null);
		}
		
		// Tests

		static string staticString = "some static";
		string someString = "hi";
		string[] numbers = { "one","two","three" };
		
		public void TestEvaluation ()
		{
			int n = 32;
			decimal dec = 123.456m;
			var alist = new ArrayList ();
			alist.Add (1);
			alist.Add ("two");
			alist.Add (3);
			
			A c = new C ();
			A b = new B ();
			A a = new A ();
			
			var withDisplayString = new WithDisplayString ();
			var withProxy = new WithProxy ();
			var withToString = new WithToString ();
			
			var numbersArrays = new int [2][];
			var numbersMulti = new int [3,4,5];
			
			var dict = new Dictionary<int, string[]> ();
			var dictArray = new Dictionary<int, string[]> [2,3];
			var thing = new Thing<string> ();
			var done = new Thing<string>.Done<int> ();
			
			Console.WriteLine (n); /*break*/
		}
		
		public int TestMethod ()
		{
			float c = 4;
			return 1;
		}
		
		public int TestMethod (string a)
		{
			return int.Parse (a) + 1;
		}
		
		public int TestMethod (int a)
		{
			return a + 1;
		}
		
		public static int TestMethod (bool b)
		{
			return b ? 1 : 2;
		}
		
		public string BoxingTestMethod (object a)
		{
			return a.ToString ();
		}
		
		public string EscapedStrings {
			get { return " \" \\ \a \b \f \v \n \r \t"; }
		}
	}
}

class A
{
	public virtual int Prop { get { return 1; } }
	public int PropNoVirt1 { get { return 1; } }
	public virtual int PropNoVirt2 { get { return 1; } }
}

class B: A
{
	public override int Prop { get { return 2; } }
	public new int PropNoVirt1 { get { return 2; } }
	public new int PropNoVirt2 { get { return 2; } }
}

class C: B
{
}

[DebuggerDisplay ("Some {Val1} Value {Val2} End")]
class WithDisplayString
{
	internal string Val1 = "one";
	public int Val2 { get { return 2; } }
}

class WithToString
{
	public override string ToString ()
	{
		return "SomeString";
	}
}

[DebuggerTypeProxy (typeof(TheProxy))]
class WithProxy
{
	public string Val1 {
		get { return "one"; }
	}
}

class TheProxy
{
	WithProxy wp;
	
	public TheProxy (WithProxy wp)
	{
		this.wp = wp;
	}
	
	public string Val1 {
		get { return wp.Val1; } 
	}
}

class Thing<T>
{
	public class Done<U>
	{
	}
	
	public Done<int>[] done = new Done<int> [1];
}

[Flags]
enum SomeEnum
{
	none=0,
	one=1,
	two=2,
	four=4
}
