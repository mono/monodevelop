// 
// EvaluationTests.cs
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

using Mono.Debugging.Client;
using NUnit.Framework;
using Mono.Debugging.Soft;

namespace MonoDevelop.Debugger.Tests
{
	[TestFixture]
	public abstract class EvaluationTests: DebugTests
	{
		protected EvaluationTests (string de, bool allowTargetInvokes) : base (de)
		{
			AllowTargetInvokes = allowTargetInvokes;
		}

		[TestFixtureSetUp]
		public override void SetUp ()
		{
			base.SetUp ();

			Start ("TestEvaluation");
			Session.TypeResolverHandler = ResolveType;
		}

		static string ResolveType (string identifier, SourceLocation location)
		{
			switch (identifier) {
			case "SomeClassInNamespace":
				return "MonoDevelop.Debugger.Tests.TestApp.SomeClassInNamespace";
			case "ParentNestedClass":
				return "MonoDevelop.Debugger.Tests.TestApp.TestEvaluationParent.ParentNestedClass";
			case "NestedClass":
				return "MonoDevelop.Debugger.Tests.TestApp.TestEvaluation.NestedClass";
			case "TestEvaluation":
				return "MonoDevelop.Debugger.Tests.TestApp.TestEvaluation";
			case "NestedGenericClass`2":
				return "MonoDevelop.Debugger.Tests.TestApp.TestEvaluation.NestedGenericClass";
			case "Dictionary`2":
				return "System.Collections.Generic.Dictionary";
			case "Thing`1":
				return "Thing";
			case "A":
				return "A";
			case "B":
				return "B";
			case "C":
				return "C";
			case "System":
				return "System";
			case "MonoDevelop":
				return "MonoDevelop";
			case "SomeEnum":
				return "SomeEnum";
			}
			return null;
		}

		void EnsureLoaded (ObjectValue val, bool checkIsNotSupported)
		{
			if (!AllowTargetInvokes) {
				var options = Session.Options.EvaluationOptions.Clone ();
				options.AllowTargetInvoke = true;

				if (checkIsNotSupported)
					Assert.IsTrue (val.IsNotSupported);
				val.Refresh (options);
				val = val.Sync ();
			}
		}

		[Test]
		public void This ()
		{
			ObjectValue val = Eval ("this");
			Assert.AreEqual ("{MonoDevelop.Debugger.Tests.TestApp.TestEvaluation}", val.Value);
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluation", val.TypeName);
		}

		[Test]
		public void UnaryOperators ()
		{
			ObjectValue val = Eval ("~1234");
			Assert.AreEqual ((~1234).ToString (), val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("!true");
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("!false");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("-1234");
			Assert.AreEqual ("-1234", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("+1234");
			Assert.AreEqual ("1234", val.Value);
			Assert.AreEqual ("int", val.TypeName);
		}

		[Test]
		public void TypeReference ()
		{
			ObjectValue val;
			val = Eval ("System.String");
			Assert.AreEqual ("string", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);
			
			val = Eval ("TestEvaluation");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluation", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);

			val = Eval ("A");
			Assert.AreEqual ("A", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);

			val = Eval ("NestedClass");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluation.NestedClass", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);

			val = Eval ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluation");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluation", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);

			val = Eval ("NestedClass.DoubleNestedClass");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluation.NestedClass.DoubleNestedClass", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);

			val = Eval ("ParentNestedClass");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluationParent.ParentNestedClass", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);

			val = Eval ("SomeClassInNamespace");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.SomeClassInNamespace", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);
		}

		[Test]
		public virtual void TypeReferenceGeneric ()
		{
			ObjectValue val;
			val = Eval ("System.Collections.Generic.Dictionary<string,int>");
			Assert.AreEqual ("System.Collections.Generic.Dictionary<string,int>", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);

			val = Eval ("Thing<string>.Done<int>");
			Assert.AreEqual ("Thing<string>.Done<int>", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);
		}

		[Test]
		public virtual void Typeof ()
		{
			var val = Eval ("typeof(System.Console)");
			Assert.IsTrue (val.TypeName == "System.MonoType" || val.TypeName == "System.RuntimeType", "Incorrect type name: " + val.TypeName);
			if (!AllowTargetInvokes) {
				var options = Session.Options.EvaluationOptions.Clone ();
				options.AllowTargetInvoke = true;

				Assert.IsTrue (val.Value == "{System.MonoType}" || val.Value == "{System.RuntimeType}");
				val.Refresh (options);
				val = val.Sync ();
			}
			Assert.AreEqual ("{System.Console}", val.Value);
		}

		[Test]
		[Ignore ("TODO: SDB and CorDebugger")]
		public void GetTypeTest ()
		{
			ObjectValue val;
			val = Eval ("a.GetType()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("System.Type", val.TypeName);
			Assert.AreEqual ("A", val.Value);

			val = Eval ("this.GetType()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("System.Type", val.TypeName);
			Assert.AreEqual ("TestEvaluation", val.Value);
		}

		[Test]
		public void MethodInvoke ()
		{
			ObjectValue val;

			val = Eval ("TestMethod ()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("TestMethod (\"23\")");
			EnsureLoaded (val, true);
			Assert.AreEqual ("24", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("TestMethod (42)");
			EnsureLoaded (val, true);
			Assert.AreEqual ("43", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("TestMethod (false)");
			EnsureLoaded (val, true);
			Assert.AreEqual ("2", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("this.TestMethod ()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("this.TestMethod (\"23\")");
			EnsureLoaded (val, true);
			Assert.AreEqual ("24", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("this.TestMethod (42)");
			EnsureLoaded (val, true);
			Assert.AreEqual ("43", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("System.Int32.Parse (\"67\")");
			EnsureLoaded (val, true);
			Assert.AreEqual ("67", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("this.BoxingTestMethod (43)");
			EnsureLoaded (val, true);
			Assert.AreEqual ("\"43\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);

			val = Eval ("objWithMethodA.MethodA()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("\"AbstractImplementation\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);

			// FIXME: failing on CorDebugger
			if (Session is SoftDebuggerSession) {
				val = Eval ("true.ToString()");
				EnsureLoaded (val, true);
				Assert.AreEqual ("\"True\"", val.Value);
				Assert.AreEqual ("string", val.TypeName);
			}
		}

		[Test]
		public void GenericMethodInvoke ()
		{
			if (Session.GetType ().Name == "CorDebuggerSession")
				Assert.Ignore ("TODO: Win32 support generic invokes");

			if (!AllowTargetInvokes)
				return;

			ObjectValue val;
			val = Eval ("done.ReturnInt5()");
			Assert.AreEqual ("5", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("done.ReturnSame(3)");
			Assert.AreEqual ("3", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("ReturnSame(4)");
			Assert.AreEqual ("4", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("ReturnSame(\"someString\")");
			Assert.AreEqual ("\"someString\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);

			val = Eval ("ReturnNew<WithToString>()");
			Assert.AreEqual ("{SomeString}", val.Value);
			Assert.AreEqual ("WithToString", val.TypeName);

			val = Eval ("intZero");
			Assert.AreEqual ("0", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("intOne");
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			// TODO: in, out, and ref params are not currently supported
			//val = Eval ("Swap (ref intZero, ref intOne)");
			//Assert.AreEqual ("", val.Value);

			//val = Eval ("intZero");
			//Assert.AreEqual ("1", val.Value);
			//Assert.AreEqual ("int", val.TypeName);

			//val = Eval ("intOne");
			//Assert.AreEqual ("0", val.Value);
			//Assert.AreEqual ("int", val.TypeName);

			//Lets return in same state as before in case some other test will use
			//val = Eval ("Swap (ref intZero, ref intOne)");
			//Assert.AreEqual ("", val.Value);

			val = Eval ("GenerateList(\"someString\", 5)");
			Assert.AreEqual ("Count=5", val.Value);
			Assert.AreEqual ("System.Collections.Generic.List<string>", val.TypeName);

			val = Eval ("GenerateList(2.0, 6)");
			Assert.AreEqual ("Count=6", val.Value);
			Assert.AreEqual ("System.Collections.Generic.List<double>", val.TypeName);

			val = Eval ("done.GetDefault()");
			Assert.AreEqual ("0", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("done.GetParentDefault()");
			Assert.AreEqual ("(null)", val.Value);
			Assert.AreEqual ("object", val.TypeName);

			val = Eval ("new Dictionary<int,string>()");
			Assert.AreEqual ("Count=0", val.Value);
			Assert.AreEqual ("System.Collections.Generic.Dictionary<int,string>", val.TypeName);

			val = Eval ("done.Property");
			Assert.AreEqual ("54", val.Value);
			Assert.AreEqual ("int", val.TypeName);
		}

		[Test]
		public void Indexers ()
		{
			ObjectValue val = Eval ("numbers[0]");
			Assert.AreEqual ("\"one\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("numbers[1]");
			Assert.AreEqual ("\"two\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("numbers[2]");
			Assert.AreEqual ("\"three\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("staticString[2]");
			EnsureLoaded (val, true);
			Assert.AreEqual ("'m'", val.Value);
			Assert.AreEqual ("char", val.TypeName);
			
			val = Eval ("alist[0]");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("alist[1]");
			EnsureLoaded (val, true);
			Assert.AreEqual ("\"two\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("alist[2]");
			EnsureLoaded (val, true);
			Assert.AreEqual ("3", val.Value);
			Assert.AreEqual ("int", val.TypeName);
		}

		[Test]
		public void MemberReference ()
		{
			ObjectValue val;

			val = Eval ("\"someString\".Length");
			EnsureLoaded (val, true);
			Assert.AreEqual ("10", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("numbers.Length");
			EnsureLoaded (val, true);
			Assert.AreEqual ("3", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("\"someString\".GetHashCode()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("numbers.GetHashCode()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("\"someString\".EndsWith (\"ing\")");
			EnsureLoaded (val, true);
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("alist.Count");
			EnsureLoaded (val, true);
			Assert.AreEqual ("3", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			Eval ("var tt = this");

			// FIXME: this errors out when target invokes are disabled
			if (AllowTargetInvokes) {
				val = Eval ("tt.someString");
				Assert.AreEqual ("\"hi\"", val.Value);
				Assert.AreEqual ("string", val.TypeName);
			}

			val = Eval ("MonoDevelop.Debugger.Tests");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests", val.Value);
			Assert.AreEqual ("<namespace>", val.TypeName);
			
			val = Eval ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluation");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluation", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			
			val = Eval ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluation.staticString");
			Assert.AreEqual ("\"some static\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
		}

		[Test]
		public void ConditionalExpression ()
		{
			ObjectValue val = Eval ("true ? \"yes\" : \"no\"");
			Assert.AreEqual ("\"yes\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("false ? \"yes\" : \"no\"");
			Assert.AreEqual ("\"no\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
		}

		[Test]
		public void Cast ()
		{
			ObjectValue val;
			val = Eval ("(byte)n");
			Assert.AreEqual ("32", val.Value);
			Assert.AreEqual ("byte", val.TypeName);
			
			val = Eval ("(int)n");
			Assert.AreEqual ("32", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("(long)n");
			Assert.AreEqual ("32", val.Value);
			Assert.AreEqual ("long", val.TypeName);
			
			val = Eval ("(float)n");
			Assert.AreEqual ("32", val.Value);
			Assert.AreEqual ("float", val.TypeName);
			
			val = Eval ("(double)n");
			Assert.AreEqual ("32", val.Value);
			Assert.AreEqual ("double", val.TypeName);
			
			val = Eval ("(string)staticString");
			Assert.AreEqual ("\"some static\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("(int)numbers");
			Assert.IsTrue (val.IsError);
			
			val = Eval ("(int)this");
			Assert.IsTrue (val.IsError);
			
			val = Eval ("(C)a");
			Assert.IsTrue (val.IsError);
			
			val = Eval ("(C)b");
			Assert.IsTrue (val.IsError);
			
			val = Eval ("(C)c");
			Assert.AreEqual ("{C}", val.Value);
			Assert.AreEqual ("C", val.TypeName);
			
			val = Eval ("(B)a");
			Assert.IsTrue (val.IsError);
			
			val = Eval ("(B)b");
			Assert.AreEqual ("{B}", val.Value);
			Assert.AreEqual ("B", val.TypeName);
			
			val = Eval ("(B)c");
			Assert.AreEqual ("{C}", val.Value);
			Assert.AreEqual ("C", val.TypeName);
			
			val = Eval ("(A)a");
			Assert.AreEqual ("{A}", val.Value);
			Assert.AreEqual ("A", val.TypeName);
			
			val = Eval ("(A)b");
			Assert.AreEqual ("{B}", val.Value);
			Assert.AreEqual ("B", val.TypeName);
			
			val = Eval ("(A)c");
			Assert.AreEqual ("{C}", val.Value);
			Assert.AreEqual ("C", val.TypeName);
			
			// Try cast
			
			val = Eval ("c as A");
			Assert.AreEqual ("{C}", val.Value);
			Assert.AreEqual ("C", val.TypeName);
			
			val = Eval ("c as B");
			Assert.AreEqual ("{C}", val.Value);
			Assert.AreEqual ("C", val.TypeName);
			
			val = Eval ("c as C");
			Assert.AreEqual ("{C}", val.Value);
			Assert.AreEqual ("C", val.TypeName);
			
			val = Eval ("b as A");
			Assert.AreEqual ("{B}", val.Value);
			Assert.AreEqual ("B", val.TypeName);
			
			val = Eval ("b as B");
			Assert.AreEqual ("{B}", val.Value);
			Assert.AreEqual ("B", val.TypeName);
			
			val = Eval ("b as C");
			Assert.AreEqual ("null", val.Value);
			Assert.AreEqual ("C", val.TypeName);
			
			val = Eval ("a as A");
			Assert.AreEqual ("{A}", val.Value);
			Assert.AreEqual ("A", val.TypeName);
			
			val = Eval ("a as B");
			Assert.AreEqual ("null", val.Value);
			Assert.AreEqual ("B", val.TypeName);
			
			val = Eval ("a as C");
			Assert.AreEqual ("null", val.Value);
			Assert.AreEqual ("C", val.TypeName);
			
			// Enum cast
			
			val = Eval ("(int)SomeEnum.two");
			Assert.AreEqual ("2", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("(long)SomeEnum.two");
			Assert.AreEqual ("2", val.Value);
			Assert.AreEqual ("long", val.TypeName);
			
			val = Eval ("(SomeEnum)2");
			Assert.AreEqual ("SomeEnum.two", val.Value);
			Assert.AreEqual ("two", val.DisplayValue);
			Assert.AreEqual ("SomeEnum", val.TypeName);
			
			val = Eval ("(SomeEnum)3");
			Assert.AreEqual ("SomeEnum.one|SomeEnum.two", val.Value);
			Assert.AreEqual ("one|two", val.DisplayValue);
			Assert.AreEqual ("SomeEnum", val.TypeName);
		}

		[Test]
		public void BinaryOperators ()
		{
			ObjectValue val;
			
			// Boolean
			
			val = Eval ("true && true");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("true && false");
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("false && true");
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("false && false");
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("false || false");
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("false || true");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("true || false");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("true || true");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("false || 1");
			Assert.IsTrue (val.IsError);
			
			val = Eval ("1 || true");
			Assert.IsTrue (val.IsError);
			
			val = Eval ("true && 1");
			Assert.IsTrue (val.IsError);
			
			val = Eval ("1 && true");
			Assert.IsTrue (val.IsError);
			
			// Concat string
			
			val = Eval ("\"a\" + \"b\"");
			Assert.AreEqual ("\"ab\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("\"a\" + 2");
			Assert.AreEqual ("\"a2\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("2 + \"a\"");
			Assert.AreEqual ("\"2a\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("this + \"a\"");
			Assert.AreEqual ("\"MonoDevelop.Debugger.Tests.TestApp.TestEvaluationa\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			// Equality
			
			val = Eval ("2 == 2");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("2 == 3");
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			val = Eval ("(long)2 == (int)2");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("SomeEnum.two == SomeEnum.one");
			EnsureLoaded (val, false);
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("SomeEnum.one != SomeEnum.one");
			EnsureLoaded (val, false);
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
			
			// Arithmetic
			
			val = Eval ("2 + 3");
			Assert.AreEqual ("5", val.Value);
			
			val = Eval ("2 + 2 == 4");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
		}

		[Test]
		public void BinaryOperatorOverrides ()
		{
			ObjectValue val;

			if (!AllowTargetInvokes)
				return;

			val = Eval ("ops1 == ops3");
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("ops1 != ops3");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("ops2 == ops3");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("ops2 != ops3");
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("ops1 <= ops2");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("ops1 < ops2");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("ops1 >= ops2");
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("ops1 > ops2");
			Assert.AreEqual ("false", val.Value);
			Assert.AreEqual ("bool", val.TypeName);

			val = Eval ("ops1 + ops2");
			Assert.AreEqual ("{[BinaryOperatorOverrides 3]}", val.Value);
			Assert.AreEqual ("BinaryOperatorOverrides", val.TypeName);

			val = Eval ("ops1 - ops2");
			Assert.AreEqual ("{[BinaryOperatorOverrides -1]}", val.Value);
			Assert.AreEqual ("BinaryOperatorOverrides", val.TypeName);

			val = Eval ("ops1 * ops2");
			Assert.AreEqual ("{[BinaryOperatorOverrides 2]}", val.Value);
			Assert.AreEqual ("BinaryOperatorOverrides", val.TypeName);

			val = Eval ("ops2 / ops1");
			Assert.AreEqual ("{[BinaryOperatorOverrides 2]}", val.Value);
			Assert.AreEqual ("BinaryOperatorOverrides", val.TypeName);

			val = Eval ("ops1 % ops2");
			Assert.AreEqual ("{[BinaryOperatorOverrides 1]}", val.Value);
			Assert.AreEqual ("BinaryOperatorOverrides", val.TypeName);

			val = Eval ("ops1 & ops2");
			Assert.AreEqual ("{[BinaryOperatorOverrides 0]}", val.Value);
			Assert.AreEqual ("BinaryOperatorOverrides", val.TypeName);

			val = Eval ("ops1 | ops2");
			Assert.AreEqual ("{[BinaryOperatorOverrides 3]}", val.Value);
			Assert.AreEqual ("BinaryOperatorOverrides", val.TypeName);

			val = Eval ("ops1 ^ ops2");
			Assert.AreEqual ("{[BinaryOperatorOverrides 3]}", val.Value);
			Assert.AreEqual ("BinaryOperatorOverrides", val.TypeName);

			val = Eval ("ops1 << 1");
			Assert.AreEqual ("{[BinaryOperatorOverrides 2]}", val.Value);
			Assert.AreEqual ("BinaryOperatorOverrides", val.TypeName);

			val = Eval ("ops2 >> 1");
			Assert.AreEqual ("{[BinaryOperatorOverrides 1]}", val.Value);
			Assert.AreEqual ("BinaryOperatorOverrides", val.TypeName);
		}

		void AssertAssignment (string assignment, string variable, string value, string type)
		{
			ObjectValue val;

			val = Eval (assignment);
			EnsureLoaded (val, true);

			Assert.AreEqual (value, val.Value);
			Assert.AreEqual (type, val.TypeName);

			val = Eval (variable);
			if (!AllowTargetInvokes && val.IsNotSupported) {
				var options = Session.Options.EvaluationOptions.Clone ();
				options.AllowTargetInvoke = true;

				val.Refresh (options);
				val = val.Sync ();
			}

			Assert.AreEqual (value, val.Value);
			Assert.AreEqual (type, val.TypeName);
		}

		[Test]
		public virtual void Assignment ()
		{
			AssertAssignment ("n = 6", "n", "6", "int");
			AssertAssignment ("n = 32", "n", "32", "int");

			AssertAssignment ("someString = \"test\"", "someString", "\"test\"", "string");
			AssertAssignment ("someString = \"hi\"", "someString", "\"hi\"", "string");

			AssertAssignment ("numbers[0] = \"test\"", "numbers[0]", "\"test\"", "string");
			AssertAssignment ("numbers[0] = \"one\"", "numbers[0]", "\"one\"", "string");

			AssertAssignment ("alist[0] = 6", "alist[0]", "6", "int");
			AssertAssignment ("alist[0] = 1", "alist[0]", "1", "int");
		}

		[Test]
		public virtual void AssignmentStatic ()
		{
			AssertAssignment ("staticString = \"test\"", "staticString", "\"test\"", "string");
			AssertAssignment ("staticString = \"some static\"", "staticString", "\"some static\"", "string");
		}

		[Test]
		public void FormatBool ()
		{
			ObjectValue val;

			val = Eval ("true");
			Assert.AreEqual ("true", val.Value);

			val = Eval ("false");
			Assert.AreEqual ("false", val.Value);
		}

		[Test]
		public void FormatNumber ()
		{
			ObjectValue val;
			val = Eval ("(int)123");
			Assert.AreEqual ("123", val.Value);
			val = Eval ("(int)-123");
			Assert.AreEqual ("-123", val.Value);
			
			val = Eval ("(long)123");
			Assert.AreEqual ("123", val.Value);
			val = Eval ("(long)-123");
			Assert.AreEqual ("-123", val.Value);
			
			val = Eval ("(byte)123");
			Assert.AreEqual ("123", val.Value);
			
			val = Eval ("(uint)123");
			Assert.AreEqual ("123", val.Value);
			
			val = Eval ("(ulong)123");
			Assert.AreEqual ("123", val.Value);
			
			val = Eval ("dec");
			Assert.AreEqual ("123.456", val.Value);
		}

		[Test]
		public void FormatString ()
		{
			ObjectValue val;
			val = Eval ("\"hi\"");
			Assert.AreEqual ("\"hi\"", val.Value);
			
			val = Eval ("EscapedStrings");
			EnsureLoaded (val, true);
			Assert.AreEqual ("\" \\\" \\\\ \\a \\b \\f \\v \\n \\r \\t\"", val.Value);
			
			val = Eval ("\" \\\" \\\\ \\a \\b \\f \\v \\n \\r \\t\"");
			Assert.AreEqual ("\" \\\" \\\\ \\a \\b \\f \\v \\n \\r \\t\"", val.Value);
		}

		[Test]
		public void FormatChar ()
		{
			ObjectValue val;
			val = Eval ("'A'");
			Assert.AreEqual ("'A'", val.Value);
			Assert.AreEqual ("65 'A'", val.DisplayValue);
			
			val = Eval ("'\\0'");
			Assert.AreEqual ("'\\0'", val.Value);
			Assert.AreEqual ("0 '\\0'", val.DisplayValue);
			
			val = Eval ("'\"'");
			Assert.AreEqual ("'\"'", val.Value);
			Assert.AreEqual ("34 '\"'", val.DisplayValue);
			
			val = Eval ("'\\''");
			Assert.AreEqual ("'\\''", val.Value);
			Assert.AreEqual ("39 '\\''", val.DisplayValue);
			
			val = Eval ("'\\\\'");
			Assert.AreEqual ("'\\\\'", val.Value);
			Assert.AreEqual ("92 '\\\\'", val.DisplayValue);
			
			val = Eval ("'\\a'");
			Assert.AreEqual ("'\\a'", val.Value);
			Assert.AreEqual ("7 '\\a'", val.DisplayValue);
			
			val = Eval ("'\\b'");
			Assert.AreEqual ("'\\b'", val.Value);
			Assert.AreEqual ("8 '\\b'", val.DisplayValue);
			
			val = Eval ("'\\f'");
			Assert.AreEqual ("'\\f'", val.Value);
			Assert.AreEqual ("12 '\\f'", val.DisplayValue);
			
			val = Eval ("'\\v'");
			Assert.AreEqual ("'\\v'", val.Value);
			Assert.AreEqual ("11 '\\v'", val.DisplayValue);
			
			val = Eval ("'\\n'");
			Assert.AreEqual ("'\\n'", val.Value);
			Assert.AreEqual ("10 '\\n'", val.DisplayValue);
			
			val = Eval ("'\\r'");
			Assert.AreEqual ("'\\r'", val.Value);
			Assert.AreEqual ("13 '\\r'", val.DisplayValue);
			
			val = Eval ("'\\t'");
			Assert.AreEqual ("'\\t'", val.Value);
			Assert.AreEqual ("9 '\\t'", val.DisplayValue);
		}

		[Test]
		public void FormatObject ()
		{
			ObjectValue val;
			
			val = Eval ("c");
			Assert.AreEqual ("{C}", val.Value);
			Assert.AreEqual ("C", val.TypeName);
			
			val = Eval ("withDisplayString");
			if (!AllowTargetInvokes) {
				var options = Session.Options.EvaluationOptions.Clone ();
				options.AllowTargetInvoke = true;

				Assert.AreEqual ("{WithDisplayString}", val.Value);
				val.Refresh (options);
				val = val.Sync ();
			}
			Assert.AreEqual ("Some one Value 2 End", val.Value);
			Assert.AreEqual ("WithDisplayString", val.TypeName);
			
			val = Eval ("withProxy");
			Assert.AreEqual ("{WithProxy}", val.Value);
			Assert.AreEqual ("WithProxy", val.TypeName);
			
/*			val = Eval ("withToString");
			Assert.AreEqual ("{SomeString}", val.Value);
			Assert.AreEqual ("WithToString", val.TypeName);*/
		}

		[Test]
		public void FormatArray ()
		{
			ObjectValue val;
			
			val = Eval ("numbers");
			Assert.AreEqual ("{string[3]}", val.Value);
			Assert.AreEqual ("string[]", val.TypeName);
			
			val = Eval ("numbersArrays");
			Assert.AreEqual ("{int[2][]}", val.Value);
			Assert.AreEqual ("int[][]", val.TypeName);
			
			val = Eval ("numbersMulti");
			Assert.AreEqual ("{int[3,4,5]}", val.Value);
			Assert.AreEqual ("int[,,]", val.TypeName);
		}

		[Test]
		public void FormatGeneric ()
		{
			ObjectValue val;

			try {
				Session.Options.EvaluationOptions.AllowTargetInvoke = false;
				val = Eval ("dict");
			} finally {
				Session.Options.EvaluationOptions.AllowTargetInvoke = AllowTargetInvokes;
			}
			Assert.AreEqual ("{System.Collections.Generic.Dictionary<int,string[]>}", val.Value);
			Assert.AreEqual ("System.Collections.Generic.Dictionary<int,string[]>", val.TypeName);

			try {
				Session.Options.EvaluationOptions.AllowTargetInvoke = false;
				val = Eval ("dictArray");
			} finally {
				Session.Options.EvaluationOptions.AllowTargetInvoke = AllowTargetInvokes;
			}
			Assert.AreEqual ("{System.Collections.Generic.Dictionary<int,string[]>[2,3]}", val.Value);
			Assert.AreEqual ("System.Collections.Generic.Dictionary<int,string[]>[,]", val.TypeName);
			
			val = Eval ("thing.done");
			Assert.AreEqual ("{Thing<string>.Done<int>[1]}", val.Value);
			Assert.AreEqual ("Thing<string>.Done<int>[]", val.TypeName);
			
			val = Eval ("done");
			Assert.AreEqual ("{Thing<string>.Done<int>}", val.Value);
			Assert.AreEqual ("Thing<string>.Done<int>", val.TypeName);
		}

		[Test]
		public void FormatEnum ()
		{
			ObjectValue val;
			
			val = Eval ("SomeEnum.one");
			Assert.AreEqual ("SomeEnum.one", val.Value);
			Assert.AreEqual ("one", val.DisplayValue);
			
			val = Eval ("SomeEnum.two");
			Assert.AreEqual ("SomeEnum.two", val.Value);
			Assert.AreEqual ("two", val.DisplayValue);
			
			val = Eval ("SomeEnum.one | SomeEnum.two");
			Assert.AreEqual ("SomeEnum.one|SomeEnum.two", val.Value);
			Assert.AreEqual ("one|two", val.DisplayValue);
		}

		[Test]
		public void LambdaInvoke ()
		{
			ObjectValue val;

			val = Eval ("action");
			Assert.AreEqual ("{System.Action}", val.Value);
			Assert.AreEqual ("System.Action", val.TypeName);

			val = Eval ("modifyInLamda");
			Assert.AreEqual ("\"modified\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);

		}

		[Test]
		public void Structures ()
		{
			ObjectValue val;

			val = Eval ("simpleStruct");
			if (!AllowTargetInvokes) {
				var options = Session.Options.EvaluationOptions.Clone ();
				options.AllowTargetInvoke = true;

				Assert.AreEqual ("{SimpleStruct}", val.Value);
				val.Refresh (options);
				val = val.Sync ();
			}
			Assert.AreEqual ("{str 45 }", val.Value);
			Assert.AreEqual ("SimpleStruct", val.TypeName);

			val = Eval ("nulledSimpleStruct");
			Assert.AreEqual ("null", val.Value);
			Assert.AreEqual ("SimpleStruct?", val.TypeName);
		}

		[Test]
		[Ignore ("TODO")]
		public void SdbFailingTests ()
		{
			ObjectValue val;

			//When fixed put into Inheriting test
			val = Eval ("b.TestMethod ()");
			Assert.AreEqual ("2", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			//When fixed put into Inheriting test
			val = Eval ("b.TestMethod (\"23\")");
			Assert.AreEqual ("25", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			//When fixed put into Inheriting test
			val = Eval ("b.TestMethod (42)");
			Assert.AreEqual ("44", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			//When fixed put into Inheriting test
			val = Eval ("base.TestMethodBase ()");
			Assert.AreEqual ("2", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("base.TestMethodBase (\"23\")");
			Assert.AreEqual ("25", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("base.TestMethodBase (42)");
			Assert.AreEqual ("44", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("base.TestMethodBaseNotOverrided ()");
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			//When fixed put into MemberReference
			val = Eval ("numbers.GetLength(0)");
			EnsureLoaded (val, true);
			Assert.AreEqual ("3", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			//When fixed put into TypeReferenceGeneric
			val = Eval ("Dictionary<string,NestedClass>");
			Assert.AreEqual ("System.Collections.Generic.Dictionary<string,MonoDevelop.Debugger.Tests.TestApp.TestEvaluation.NestedClass>", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);

			//When fixed put into TypeReferenceGeneric
			val = Eval ("NestedGenericClass<int,string>");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.TestEvaluation.NestedGenericClass<int,string>", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);

			//When fixed put into MethodInvoke(failing also on CorDebugger)
			val = Eval ("((IInterfaceWithMethodA)objWithMethodA).MethodA()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("\"InterfaceImplementation\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
		}

		[Test]
		public void ObjectCreation ()
		{
			ObjectValue val;

			val = Eval ("new A().ConstructedBy");
			EnsureLoaded (val, true);
			Assert.AreEqual ("\"NoArg\"", val.Value);

			val = Eval ("new A(7).ConstructedBy");
			EnsureLoaded (val, true);
			Assert.AreEqual ("\"IntArg\"", val.Value);

			val = Eval ("new A(\"someString\").ConstructedBy");
			EnsureLoaded (val, true);
			Assert.AreEqual ("\"StringArg\"", val.Value);
		}

		[Test]
		public void StructCreation ()
		{
			if (!AllowTargetInvokes)
				return;

			var soft = Session as Mono.Debugging.Soft.SoftDebuggerSession;

			if (soft == null)
				Assert.Ignore ("TODO: Win32 support generic invokes");

			if (soft.ProtocolVersion < new Version (2, 31))
				Assert.Inconclusive ("Mono SDB protocol version >= 2.31 required for this test.");

			ObjectValue val;

			val = Eval ("new SimpleStruct()");
			Assert.AreEqual ("SimpleStruct", val.TypeName);
		}

		[Test]
		public void Inheriting ()
		{
			ObjectValue val;

			val = Eval ("a.Prop");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("a.PropNoVirt1");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("a.PropNoVirt2");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("a.IntField");
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("a.TestMethod ()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("a.TestMethod (\"23\")");
			EnsureLoaded (val, true);
			Assert.AreEqual ("24", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("a.TestMethod (42)");
			EnsureLoaded (val, true);
			Assert.AreEqual ("43", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("b.Prop");
			EnsureLoaded (val, true);
			Assert.AreEqual ("2", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("b.PropNoVirt1");
			EnsureLoaded (val, true);
			Assert.AreEqual ("2", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("b.PropNoVirt2");
			EnsureLoaded (val, true);
			Assert.AreEqual ("2", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("b.IntField");
			Assert.AreEqual ("2", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("this.TestMethodBase ()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("this.TestMethodBase (\"23\")");
			EnsureLoaded (val, true);
			Assert.AreEqual ("24", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("this.TestMethodBase (42)");
			EnsureLoaded (val, true);
			Assert.AreEqual ("43", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("this.TestMethodBaseNotOverrided ()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("TestMethodBase ()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("TestMethodBase (\"23\")");
			EnsureLoaded (val, true);
			Assert.AreEqual ("24", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("TestMethodBase (42)");
			EnsureLoaded (val, true);
			Assert.AreEqual ("43", val.Value);
			Assert.AreEqual ("int", val.TypeName);

			val = Eval ("TestMethodBaseNotOverrided ()");
			EnsureLoaded (val, true);
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);
		}

		[Test]
		public void Lists ()
		{
			ObjectValue[] children;
			ObjectValue val;

			val = Eval ("dict");
			children = val.GetAllChildren ();

			if (AllowTargetInvokes) {
				// AllowTargetInvokes also allows debugger proxies
				Assert.AreEqual (2, children.Length);
				Assert.AreEqual ("[0]", children [0].Name);
				Assert.AreEqual ("{[5, System.String[]]}", children [0].Value);
				Assert.AreEqual ("Raw View", children [1].Name);

				children = children [0].GetAllChildren ();
				Assert.AreEqual ("Key", children [0].Name);
				Assert.AreEqual ("5", children [0].Value);
				Assert.AreEqual ("int", children [0].TypeName);
				Assert.AreEqual ("Value", children [1].Name);
				Assert.AreEqual ("{string[2]}", children [1].Value);

				children = children [1].GetAllChildren ();
				Assert.AreEqual ("\"a\"", children [0].Value);
				Assert.AreEqual ("string", children [0].TypeName);
				Assert.AreEqual ("\"b\"", children [1].Value);
			} else {
				// when AllowTargetInvokes is disabled, it also disables debugger proxies
				Assert.AreEqual (6, children.Length);
				Assert.AreEqual ("Comparer", children [0].Name);
				Assert.AreEqual ("Count", children [1].Name);
				Assert.AreEqual ("Keys", children [2].Name);
				Assert.AreEqual ("Values", children [3].Name);
				Assert.AreEqual ("Static members", children [4].Name);
				Assert.AreEqual ("Non-public members", children [5].Name);
			}

			val = Eval ("stringList");
			children = val.GetAllChildren ();

			if (AllowTargetInvokes) {
				// AllowTargetInvokes also allows debugger proxies
				Assert.AreEqual (4, children.Length);
				Assert.AreEqual ("[0]", children [0].Name);
				Assert.AreEqual ("[1]", children [1].Name);
				Assert.AreEqual ("[2]", children [2].Name);
				Assert.AreEqual ("Raw View", children [3].Name);
				Assert.AreEqual ("\"aaa\"", children [0].Value);
				Assert.AreEqual ("\"bbb\"", children [1].Value);
				Assert.AreEqual ("\"ccc\"", children [2].Value);
			} else {
				// when AllowTargetInvokes is disabled, it also disables debugger proxies
				Assert.AreEqual (4, children.Length);
				Assert.AreEqual ("Capacity", children [0].Name);
				Assert.AreEqual ("Count", children [1].Name);
				Assert.AreEqual ("Static members", children [2].Name);
				Assert.AreEqual ("Non-public members", children [3].Name);
			}

			val = Eval ("alist");
			children = val.GetAllChildren ();

			if (AllowTargetInvokes) {
				// AllowTargetInvokes also allows debugger proxies
				Assert.AreEqual (4, children.Length);
				Assert.AreEqual ("[0]", children [0].Name);
				Assert.AreEqual ("[1]", children [1].Name);
				Assert.AreEqual ("[2]", children [2].Name);
				Assert.AreEqual ("Raw View", children [3].Name);
				Assert.AreEqual ("1", children [0].Value);
				Assert.AreEqual ("\"two\"", children [1].Value);
				Assert.AreEqual ("3", children [2].Value);
			} else {
				// when AllowTargetInvokes is disabled, it also disables debugger proxies
				Assert.AreEqual (8, children.Length);
				Assert.AreEqual ("Capacity", children [0].Name);
				Assert.AreEqual ("Count", children [1].Name);
				Assert.AreEqual ("IsFixedSize", children [2].Name);
				Assert.AreEqual ("IsReadOnly", children [3].Name);
				Assert.AreEqual ("IsSynchronized", children [4].Name);
				Assert.AreEqual ("SyncRoot", children [5].Name);
				Assert.AreEqual ("Static members", children [6].Name);
				Assert.AreEqual ("Non-public members", children [7].Name);
			}
		}

		[Test]
		[Ignore ("TODO: Evaluating dynamic objects")]
		public void DynamicObjects ()
		{
			ObjectValue val;
			val = Eval ("dynObj.someInt");
			Assert.AreEqual ("dynamic {int}", val.TypeName);
			Assert.AreEqual ("53", val.Value);

			val = Eval ("dynObj.someString");
			Assert.AreEqual ("dynamic {string}", val.TypeName);
			Assert.AreEqual ("\"Hello dynamic objects!\"", val.Value);
		}
	}
}
