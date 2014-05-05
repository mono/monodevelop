//
// TestEvaluation.cs
// 
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
	class TestEvaluationParent
	{
		public int TestMethodBase ()
		{
			float c = 4;
			return 1;
		}

		public virtual int TestMethodBase (string a)
		{
			return int.Parse (a) + 1;
		}

		protected int TestMethodBase (int a)
		{
			return a + 1;
		}

		public int TestMethodBaseNotOverrided ()
		{
			float c = 4;
			return 1;
		}

		public class ParentNestedClass
		{

		}
	}

	class TestEvaluation : TestEvaluationParent
	{
		static string staticString = "some static";
		string someString = "hi";
		string[] numbers = { "one", "two", "three" };

		public static void RunTest ()
		{
			var obj = new TestEvaluation ();
			obj.Test ();
		}

		public void Test ()
		{
			int intZero = 0, intOne = 1;
			int n = 32;
			decimal dec = 123.456m;
			var alist = new ArrayList ();
			alist.Add (1);
			alist.Add ("two");
			alist.Add (3);
			string modifyInLamda = "";

			A c = new C ();
			A b = new B ();
			A a = new A ();

			var withDisplayString = new WithDisplayString ();
			var withProxy = new WithProxy ();
			var withToString = new WithToString ();

			var numbersArrays = new int [2][];
			var numbersMulti = new int [3, 4, 5];

			var dict = new Dictionary<int, string[]> ();
			var dictArray = new Dictionary<int, string[]> [2, 3];
			var thing = new Thing<string> ();
			var done = new Thing<string>.Done<int> ();
			done.Property = 54;

			SimpleStruct simpleStruct = new SimpleStruct ();
			simpleStruct.IntField = 45;
			simpleStruct.StringField = "str";
			SimpleStruct? nulledSimpleStruct;
			var action = new Action (() => {
				modifyInLamda = "modified";
			});
			action ();
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

		public int TestMethodBase ()
		{
			float c = 4;
			return 1;
		}

		public override int TestMethodBase (string a)
		{
			return int.Parse (a) + 1;
		}

		protected new int TestMethodBase (int a)
		{
			return a + 1;
		}

		public static int TestMethod (bool b)
		{
			return b ? 1 : 2;
		}

		public T ReturnSame<T> (T t)
		{
			return t;
		}

		public T ReturnNew<T> () where T:new()
		{
			return new T ();
		}

		public string BoxingTestMethod (object a)
		{
			return a.ToString ();
		}

		public string EscapedStrings {
			get { return " \" \\ \a \b \f \v \n \r \t"; }
		}

		public static void Swap<T> (ref T a, ref T b)
		{
			T temp = a;
			a = b;
			b = temp;
		}

		public static List<T> GenerateList<T> (T value, int count)
		{
			var list = new List<T> ();
			for (int i = 0; i < count; i++) {
				list.Add (value);
			}
			return list;
		}

		class NestedClass
		{
			public class DoubleNestedClass
			{

			}
		}
	}

	public class SomeClassInNamespace
	{

	}
}

class A
{
	public string ConstructedBy { get; private set; }

	public A ()
	{
		ConstructedBy = "NoArg";
	}

	public A (int i)
	{
		ConstructedBy = "IntArg";
	}

	public A (string str)
	{
		ConstructedBy = "StringArg";
	}

	public virtual int Prop { get { return 1; } }

	public int PropNoVirt1 { get { return 1; } }

	public virtual int PropNoVirt2 { get { return 1; } }

	public int IntField = 1;

	public int TestMethod ()
	{
		float c = 4;
		return 1;
	}

	public virtual int TestMethod (string a)
	{
		return int.Parse (a) + 1;
	}

	public int TestMethod (int a)
	{
		return a + 1;
	}
}

class B: A
{
	public override int Prop { get { return 2; } }

	public new int PropNoVirt1 { get { return 2; } }

	public new int PropNoVirt2 { get { return 2; } }

	public new int IntField = 2;

	public int TestMethod ()
	{
		float c = 4;
		return 2;
	}

	public override int TestMethod (string a)
	{
		return int.Parse (a) + 2;
	}

	public new int TestMethod (int a)
	{
		return a + 2;
	}
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
		private U property;

		public U Property {
			get {
				return property;
			}
			set {
				property = value;
			}
		}


		public int ReturnInt5 ()
		{
			return 5;
		}

		public U ReturnSame (U obj)
		{
			return obj;
		}

		public T ReturnSame (T obj)
		{
			return obj;
		}

		public U GetDefault ()
		{
			return default(U);
		}

		public T GetParentDefault ()
		{
			return default(T);
		}
	}

	public Done<int>[] done = new Done<int> [1];
}

[Flags]
enum SomeEnum
{
	none = 0,
	one = 1,
	two = 2,
	four = 4
}

struct SimpleStruct
{
	public int IntField;
	public string StringField;
	public int? NulledIntField;

	public override string ToString ()
	{
		return StringField + " " + IntField + " " + NulledIntField;
	}
}