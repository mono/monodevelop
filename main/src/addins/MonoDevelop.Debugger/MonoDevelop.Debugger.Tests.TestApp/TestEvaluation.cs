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
using System.Dynamic;
using System.Threading.Tasks;

namespace MonoDevelop.Debugger.Tests.TestApp
{
	class TestEvaluationParent
	{
		public int TestMethodBase ()
		{
			float c = 4;
			return 1;
		}

		protected string ProtectedStringProperty{ get; set; }

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
			obj.Test ("testString", 55);
		}

		public void Test (string stringParam, int intParam = 22, int intParam2 = 66)
		{
			int intZero = 0, intOne = 1;
			int n = 32;
			decimal dec = 123.456m;
			var stringList = new List<string> ();
			stringList.Add ("aaa");
			stringList.Add ("bbb");
			stringList.Add ("ccc");

			var alist = new ArrayList ();
			alist.Add (1);
			alist.Add ("two");
			alist.Add (3);
			string modifyInLamda = "";

			var debugDisplayMethodTest = new DebuggerDisplayMethodTest ();

			A c = new C ();
			A b = new B ();
			A a = new A ();

			var withDisplayString = new WithDisplayString ();
			var withProxy = new WithProxy ();
			var withToString = new WithToString ();

			var numbersArrays = new int [2][];
			numbersArrays [0] = new int [10];
			numbersArrays [0] [7] = 24;
			var numbersMulti = new int [3, 4, 5];

			var ops1 = new BinaryOperatorOverrides (1);
			var ops2 = new BinaryOperatorOverrides (2);
			var ops3 = new BinaryOperatorOverrides (2);

			var dict = new Dictionary<int, string[]> ();
			dict.Add (5, new string[]{ "a", "b" });
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

			dynamic dynObj = new ExpandoObject ();
			dynObj.someInt = 53;
			dynObj.someString = "Hello dynamic objects!";

			var objWithMethodA = new ClassWithMethodA ();

			bool? nullableBool = null;
			nullableBool = true;

			var richObject = new RichClass ();
			byte[] nulledByteArray = null;

			var arrayWithLowerBounds = Array.CreateInstance (typeof(int), new int[] { 3, 4, 5 }, new int[] { 5, 4, 3 });
			int m = 100;
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 4; j++) {
					for (int k = 0; k < 5; k++) {
						numbersMulti.SetValue (m, i, j, k);
						arrayWithLowerBounds.SetValue (m++, i + 5, j + 4, k + 3);
					}
				}
			}

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

		class NestedGenericClass<T1,T2>
		{

		}
	}

	public class SomeClassInNamespace
	{

	}
}

class RichClass
{
	public int publicInt1 = 1;
	public int publicInt2 = 2;
	public int publicInt3 = 3;

	public string publicStringA = "stringA";
	public string publicStringB = "stringB";
	public string publicStringC = "stringC";

	private int privateInt1 = 1;
	private int privateInt2 = 2;
	private int privateInt3 = 3;

	private string privateStringA = "stringA";
	private string privateStringB = "stringB";
	private string privateStringC = "stringC";

	public int publicPropInt1  { get; set; }

	public int publicPropInt2  { get; set; }

	public int publicPropInt3  { get; set; }

	public string publicPropStringA  { get; set; }

	public string publicPropStringB { get; set; }

	public string publicPropStringC  { get; set; }

	private int privatePropInt1  { get; set; }

	private int privatePropInt2 { get; set; }

	private int privatePropInt3 { get; set; }

	private string privatePropStringA { get; set; }

	private string privatePropStringB { get; set; }

	private string privatePropStringC { get; set; }

	public RichClass ()
	{
		publicPropInt1 = 1;
		publicPropInt2 = 2;
		publicPropInt3 = 3;

		publicPropStringA = "stringA";
		publicPropStringB = "stringB";
		publicPropStringC = "stringC";

		privatePropInt1 = 1;
		privatePropInt2 = 2;
		privatePropInt3 = 3;

		privatePropStringA = "stringA";
		privatePropStringB = "stringB";
		privatePropStringC = "stringC";
	}
}

interface IInterfaceWithMethodA
{
	string MethodA ();
}

abstract class AbstractClassWithMethodA
{
	public abstract string MethodA ();
}

class ClassWithMethodA : AbstractClassWithMethodA, IInterfaceWithMethodA
{
	public override string MethodA ()
	{
		return "AbstractImplementation";
	}

	string IInterfaceWithMethodA.MethodA ()
	{
		return "InterfaceImplementation";
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

[DebuggerDisplay ("{GetDebuggerDisplay(), nq}")]
class DebuggerDisplayMethodTest
{
	int someInt = 32;
	int someInt2 = 43;

	string GetDebuggerDisplay ()
	{
		return "First Int:" + someInt + " Second Int:" + someInt2;
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

class ClassWithCompilerGeneratedNestedClass
{
	async Task TestMethodAsync()
	{
		await Task.Delay (1);
	}

	public class NestedClass
	{

	}
}

class BinaryOperatorOverrides
{
	int value;

	public BinaryOperatorOverrides (int num)
	{
		value = num;
	}

	public override string ToString ()
	{
		return string.Format ("[BinaryOperatorOverrides {0}]", value);
	}

	public static bool operator== (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return ops1.value == ops2.value;
	}

	public static bool operator!= (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return ops1.value != ops2.value;
	}

	public static bool operator>= (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return ops1.value >= ops2.value;
	}

	public static bool operator> (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return ops1.value > ops2.value;
	}

	public static bool operator<= (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return ops1.value <= ops2.value;
	}

	public static bool operator< (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return ops1.value < ops2.value;
	}

	public static BinaryOperatorOverrides operator+ (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return new BinaryOperatorOverrides (ops1.value + ops2.value);
	}

	public static BinaryOperatorOverrides operator- (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return new BinaryOperatorOverrides (ops1.value - ops2.value);
	}

	public static BinaryOperatorOverrides operator* (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return new BinaryOperatorOverrides (ops1.value * ops2.value);
	}

	public static BinaryOperatorOverrides operator/ (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return new BinaryOperatorOverrides (ops1.value / ops2.value);
	}

	public static BinaryOperatorOverrides operator% (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return new BinaryOperatorOverrides (ops1.value % ops2.value);
	}

	public static BinaryOperatorOverrides operator& (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return new BinaryOperatorOverrides (ops1.value & ops2.value);
	}

	public static BinaryOperatorOverrides operator| (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return new BinaryOperatorOverrides (ops1.value | ops2.value);
	}

	public static BinaryOperatorOverrides operator^ (BinaryOperatorOverrides ops1, BinaryOperatorOverrides ops2)
	{
		return new BinaryOperatorOverrides (ops1.value ^ ops2.value);
	}

	public static BinaryOperatorOverrides operator<< (BinaryOperatorOverrides ops1, int shift)
	{
		return new BinaryOperatorOverrides (ops1.value << shift);
	}

	public static BinaryOperatorOverrides operator>> (BinaryOperatorOverrides ops1, int shift)
	{
		return new BinaryOperatorOverrides (ops1.value >> shift);
	}
}