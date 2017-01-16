//
// AdvancedEvaluationTests.cs
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
using NUnit.Framework;

namespace MonoDevelop.Debugger.Tests
{
	namespace Soft
	{
		[TestFixture]
		public class SdbAdvancedEvaluationAllowTargetInvokesTests : AdvancedEvaluationTests
		{
			public SdbAdvancedEvaluationAllowTargetInvokesTests () : base ("Mono.Debugger.Soft", true)
			{
			}
		}

		[TestFixture]
		public class SdbAdvancedEvaluationNoTargetInvokesTests : AdvancedEvaluationTests
		{
			public SdbAdvancedEvaluationNoTargetInvokesTests () : base ("Mono.Debugger.Soft", false)
			{
			}
		}
	}

	namespace Win32
	{
		[TestFixture]
		[Platform(Include = "Win")]
		public class CorAdvancedEvaluationAllowTargetInvokesTests : AdvancedEvaluationTests
		{
			public CorAdvancedEvaluationAllowTargetInvokesTests () : base ("MonoDevelop.Debugger.Win32", true)
			{
			}
		}

		[TestFixture]
		[Platform(Include = "Win")]
		public class CorAdvancedEvaluationNoTargetInvokesTests : AdvancedEvaluationTests
		{
			public CorAdvancedEvaluationNoTargetInvokesTests () : base ("MonoDevelop.Debugger.Win32", false)
			{
			}
		}
	}

	[TestFixture]
	public abstract class AdvancedEvaluationTests : DebugTests
	{
		protected AdvancedEvaluationTests (string de, bool allowTargetInvokes) : base (de)
		{
			AllowTargetInvokes = allowTargetInvokes;
		}

		[TestFixtureSetUp]
		public override void SetUp ()
		{
			base.SetUp ();
			Start ("AdvancedEvaluation");
		}

		[Test]
		public void Bug24998 ()
		{
			InitializeTest ();
			AddBreakpoint ("cc622137-a162-4b91-a85c-88241e68c3ea");
			StartTest ("Bug24998Test");
			CheckPosition ("cc622137-a162-4b91-a85c-88241e68c3ea");
			var val = Eval ("someField");
			Assert.AreEqual ("\"das\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);

			val = Eval ("someVariable");
			Assert.AreEqual ("System.Collections.ArrayList", val.TypeName);
			var children = val.GetAllChildrenSync ();
			Assert.AreEqual (2, children.Length);
			Assert.AreEqual ("[0]", children [0].ChildSelector);
			Assert.AreEqual ("1", children [0].Value);
			Assert.AreEqual ("int", children [0].TypeName);

			val = Eval ("action");
			Assert.AreEqual ("System.Action", val.TypeName);
		}


		[Test]
		public void YieldMethodTest ()
		{
			InitializeTest ();
			AddBreakpoint ("0b1212f8-9035-43dc-bf01-73efd078d680");
			StartTest ("YieldMethodTest");
			CheckPosition ("0b1212f8-9035-43dc-bf01-73efd078d680");

			var val = Eval ("someVariable");
			Assert.AreEqual ("System.Collections.ArrayList", val.TypeName);
			Assert.AreEqual (1, val.GetAllChildrenSync ().Length);

			AddBreakpoint ("e96b28bb-59bf-445d-b71f-316726ba4c52");
			Continue ("e96b28bb-59bf-445d-b71f-316726ba4c52");

			val = Eval ("someField");
			Assert.AreEqual ("\"das1\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			val = Eval ("someVariable");
			Assert.AreEqual ("System.Collections.ArrayList", val.TypeName);
			Assert.AreEqual (2, val.GetAllChildrenSync ().Length);

			AddBreakpoint ("760feb92-176a-43d7-b5c9-116c4a3c6a6c");
			Continue ("760feb92-176a-43d7-b5c9-116c4a3c6a6c");

			val = Eval ("someField");
			Assert.AreEqual ("\"das2\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			val = Eval ("someVariable");
			Assert.AreEqual ("System.Collections.ArrayList", val.TypeName);
			Assert.AreEqual (3, val.GetAllChildrenSync ().Length);

			AddBreakpoint ("a9a9aa9d-6b8b-4724-9741-2a3e1fb435e8");
			Continue ("a9a9aa9d-6b8b-4724-9741-2a3e1fb435e8");

			val = Eval ("someField");
			Assert.AreEqual ("\"das2\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			val = Eval ("someVariable");
			Assert.AreEqual ("System.Collections.ArrayList", val.TypeName);
			Assert.AreEqual (3, val.GetAllChildrenSync ().Length);

		}

		[Test]
		public void InvocationsCountDuringExpandingTest ()
		{
			InitializeTest ();
			AddBreakpoint ("8865cace-6b57-42cc-ad55-68a2c12dd3d7");
			StartTest ("InvocationsCountDuringExpandingTest");
			CheckPosition ("8865cace-6b57-42cc-ad55-68a2c12dd3d7");
			var options = Session.EvaluationOptions.Clone ();
			options.GroupPrivateMembers = false; // to access private fields (else there are in Private subgroup)
			var value = Eval ("mutableFieldClass");
			var sharedX = value.GetChildSync ("sharedX", options);
			Assert.NotNull (sharedX);

			var prop1 = value.GetChildSync("Prop1", options);
			Assert.NotNull (prop1);
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.AdvancedEvaluation.MutableField", prop1.TypeName);
			var prop1X = prop1.GetChildSync ("x", options);
			Assert.NotNull (prop1X);
			Assert.AreEqual ("0", prop1X.Value); // before CorValRef optimization this field evaluated to 2,
			// because every value to the root object was recalculated - this was wrong behavior

			var prop2 = value.GetChildSync ("Prop2", options);
			Assert.NotNull (prop2);
			var prop2X = prop2.GetChildSync("x", options);
			Assert.NotNull (prop2X);
			Assert.AreEqual ("1", prop2X.Value);

			Assert.AreEqual ("2", sharedX.Value);
		}

		[Test]
		public void MethodWithTypeGenericArgsEval ()
		{
			InitializeTest ();
			AddBreakpoint ("ba6350e5-7149-4cc2-a4cf-8a54c635eb38");
			StartTest ("MethodWithTypeGenericArgsEval");
			CheckPosition ("ba6350e5-7149-4cc2-a4cf-8a54c635eb38");

			var baseMethodEval = Eval ("genericClass.BaseMethodWithClassTArg (wrappedA)");
			Assert.NotNull (baseMethodEval);
			Assert.AreEqual ("{Wrapper(wrappedA)}", baseMethodEval.Value);

			var thisMethodEval = Eval ("genericClass.RetMethodWithClassTArg (a)");
			Assert.NotNull (thisMethodEval);
			Assert.AreEqual ("{Just A}", thisMethodEval.Value);
		}


	}
}

