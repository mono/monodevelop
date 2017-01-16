//
// AdvancedEvaluation.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Debugger.Tests.TestApp
{
	public class AdvancedEvaluation
	{
		public static void RunTest ()
		{
			var obj = new AdvancedEvaluation ();
			obj.Test ();
		}

		public static string NextMethodToCall = "";

		public void Test ()
		{
			while (true) {
				Console.Write ("");/*break*/
				try {
					typeof(AdvancedEvaluation).GetMethod (NextMethodToCall).Invoke (this, null);
				} catch {
				}
			}
		}

		public void YieldMethodTest ()
		{
			YieldMethod ().ToList ();
		}

		public IEnumerable<string> YieldMethod ()
		{
			var someVariable = new ArrayList ();
			yield return "1";/*0b1212f8-9035-43dc-bf01-73efd078d680*/
			someField = "das1";
			someVariable.Add (1);
			yield return "2";/*e96b28bb-59bf-445d-b71f-316726ba4c52*/
			someField = "das2";
			someVariable.Add (2);
			yield return "3";/*760feb92-176a-43d7-b5c9-116c4a3c6a6c*/
			yield return "4";/*a9a9aa9d-6b8b-4724-9741-2a3e1fb435e8*/
		}

		public void Bug24998Test ()
		{
			Bug24998 ().Wait ();
		}

		string someField;
		public async Task Bug24998 ()
		{
			someField = "das";
			var someVariable = new ArrayList ();
			var action = new Action (() => someVariable.Add (1));
			await Task.Delay (100);
			action ();
			someVariable.Add (someField);/*cc622137-a162-4b91-a85c-88241e68c3ea*/
		}


		public void InvocationsCountDuringExpandingTest ()
		{
			var mutableFieldClass = new MutableFieldClass ();
			Console.WriteLine("InvocationsCountDuringExpandingTest breakpoint");/*8865cace-6b57-42cc-ad55-68a2c12dd3d7*/
		}

		class MutableFieldClass
		{
			int sharedX = 0;

			public MutableField Prop1
			{
				get { return new MutableField(sharedX++); }
			}

			public MutableField Prop2
			{
				get { return new MutableField(sharedX++); }
			}
		}


		class MutableField
		{
			int x;

			public MutableField(int x)
			{
				this.x = x;
			}
		}

		public void MethodWithTypeGenericArgsEval ()
		{
			var a = new A("Just A");
			var wrappedA = new Wrapper<A>(new A("wrappedA"));
			var genericClass = new GenericClass<A>(new A("Constructor arg A"));
			//genericClass.BaseMethodWithClassTArg (wrappedA);
			//genericClass.RetMethodWithClassTArg (a)
			Console.WriteLine("Break for MethodWithTypeGenericArgsEval");/*ba6350e5-7149-4cc2-a4cf-8a54c635eb38*/
		}

		class A
		{
			public A(string myProp)
			{
				MyProp = myProp;
			}

			public string MyProp { get; set; }

			public override string ToString()
			{
				return MyProp;
			}
		}

		class GenericBaseClass<TBaseClassArg>
		{
			public readonly TBaseClassArg myArg;

			public GenericBaseClass(TBaseClassArg arg)
			{
				myArg = arg;
			}

			public TBaseClassArg BaseMethodWithClassTArg(TBaseClassArg arg)
			{
				return arg;
			}


		}

		class Wrapper<TObj>
		{
			public TObj obj;

			public Wrapper(TObj obj)
			{
				this.obj = obj;
			}

			public override string ToString()
			{
				return string.Format("Wrapper({0})", obj);
			}
		}

		class GenericClass<TOfClass> : GenericBaseClass<Wrapper<TOfClass>>
		{
			public GenericClass(TOfClass arg) : base(new Wrapper<TOfClass>(arg))
			{
			}

			public void VoidMethodWithClassTArg(TOfClass tOfClass) {}
			public TOfClass RetMethodWithClassTArg(TOfClass tOfClass) {return tOfClass;}

			public void VoidMethodWithMethodTArg<TOfMethod>(TOfMethod tOfMethod) { }
			public void VoidMethodWithMethodAndClassTArg<TOfMethod>(TOfMethod tOfMethod, TOfClass tOfClass) { }

		}
	}
}

