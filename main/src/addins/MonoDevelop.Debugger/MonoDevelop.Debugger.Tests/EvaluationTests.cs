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

using Mono.Debugging.Client;
using NUnit.Framework;

namespace MonoDevelop.Debugger.Tests
{
	[TestFixture]
	public abstract class EvaluationTests: DebugTests
	{
		DebuggerSession ds;
		StackFrame frame;
		
		protected EvaluationTests (string de): base (de)
		{
		}

		[TestFixtureSetUp]
		public override void SetUp ()
		{
			base.SetUp ();
			ds = Start ("TestEvaluation");
			if (ds == null)
				Assert.Ignore ("Engine not found: {0}", EngineId);

			frame = ds.ActiveThread.Backtrace.GetFrame (0);
		}

		[TestFixtureTearDown]
		public override void TearDown ()
		{
			base.TearDown ();
			if (ds != null) {
				ds.Exit ();
				ds.Dispose ();
			}
		}


		ObjectValue Eval (string exp)
		{
			return frame.GetExpressionValue (exp, true).Sync ();
		}
		
		[Test]
		public void This ()
		{
			ObjectValue val = Eval ("this");
			Assert.AreEqual ("{MonoDevelop.Debugger.Tests.TestApp.MainClass}", val.Value);
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.MainClass", val.TypeName);
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
			ObjectValue val = Eval ("System.String");
			Assert.AreEqual ("string", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);
			
			val = Eval ("MainClass");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.MainClass", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);
			
			val = Eval ("MonoDevelop.Debugger.Tests.TestApp.MainClass");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.MainClass", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);
		}
		
		[Test]
		public virtual void TypeReferenceGeneric ()
		{
			ObjectValue val = Eval ("System.Collections.Generic.Dictionary<string,int>");
			Assert.AreEqual ("System.Collections.Generic.Dictionary<string,int>", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			Assert.AreEqual (ObjectValueFlags.Type, val.Flags & ObjectValueFlags.OriginMask);
		}
		
		[Test]
		public virtual void Typeof ()
		{
			ObjectValue val = Eval ("typeof(System.Console)");
			Assert.IsTrue (val.TypeName == "System.MonoType" || val.TypeName == "System.RuntimeType", "Incorrect type name: " + val.TypeName);
			Assert.AreEqual ("{System.Console}", val.Value);
		}
		
		[Test]
		public void MethodInvoke ()
		{
			ObjectValue val;
			val = Eval ("TestMethod ()");
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("TestMethod (\"23\")");
			Assert.AreEqual ("24", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("TestMethod (42)");
			Assert.AreEqual ("43", val.Value);
			Assert.AreEqual ("int", val.TypeName);
						 
			val = Eval ("TestMethod (false)");
			Assert.AreEqual ("2", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("this.TestMethod ()");
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("this.TestMethod (\"23\")");
			Assert.AreEqual ("24", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("this.TestMethod (42)");
			Assert.AreEqual ("43", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("System.Int32.Parse (\"67\")");
			Assert.AreEqual ("67", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("this.BoxingTestMethod (43)");
			Assert.AreEqual ("\"43\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
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
			Assert.AreEqual ("'m'", val.Value);
			Assert.AreEqual ("char", val.TypeName);
			
			val = Eval ("alist[0]");
			Assert.AreEqual ("1", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			val = Eval ("alist[1]");
			Assert.AreEqual ("\"two\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("alist[2]");
			Assert.AreEqual ("3", val.Value);
			Assert.AreEqual ("int", val.TypeName);
		}
		
		[Test]
		public void MemberReference ()
		{
			ObjectValue val = Eval ("alist.Count");
			Assert.AreEqual ("3", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			
			Eval ("var tt = this");
			
			val = Eval ("tt.someString");
			Assert.AreEqual ("\"hi\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			
			val = Eval ("MonoDevelop.Debugger.Tests");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests", val.Value);
			Assert.AreEqual ("<namespace>", val.TypeName);
			
			val = Eval ("MonoDevelop.Debugger.Tests.TestApp.MainClass");
			Assert.AreEqual ("MonoDevelop.Debugger.Tests.TestApp.MainClass", val.Value);
			Assert.AreEqual ("<type>", val.TypeName);
			
			val = Eval ("MonoDevelop.Debugger.Tests.TestApp.MainClass.staticString");
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
			Assert.AreEqual ("\"MonoDevelop.Debugger.Tests.TestApp.MainClassa\"", val.Value);
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
			
			// Arithmetic
			
			val = Eval ("2 + 3");
			Assert.AreEqual ("5", val.Value);
			
			val = Eval ("2 + 2 == 4");
			Assert.AreEqual ("true", val.Value);
			Assert.AreEqual ("bool", val.TypeName);
		}
		
		[Test]
		public virtual void Assignment ()
		{
			ObjectValue val;
			Eval ("n = 6");
			val = Eval ("n");
			Assert.AreEqual ("6", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			Eval ("n = 32");
			val = Eval ("n");
			Assert.AreEqual ("32", val.Value);
			
			Eval ("someString = \"test\"");
			val = Eval ("someString");
			Assert.AreEqual ("\"test\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			Eval ("someString = \"hi\"");
			val = Eval ("someString");
			Assert.AreEqual ("\"hi\"", val.Value);
			
			Eval ("numbers[0] = \"test\"");
			val = Eval ("numbers[0]");
			Assert.AreEqual ("\"test\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			Eval ("numbers[0] = \"one\"");
			val = Eval ("numbers[0]");
			Assert.AreEqual ("\"one\"", val.Value);

			Eval ("alist[0] = 6");
			val = Eval ("alist[0]");
			Assert.AreEqual ("6", val.Value);
			Assert.AreEqual ("int", val.TypeName);
			Eval ("alist[0] = 1");
			val = Eval ("alist[0]");
			Assert.AreEqual ("1", val.Value);
		}
		
		[Test]
		public virtual void AssignmentStatic ()
		{
			ObjectValue val;
			
			Eval ("staticString = \"test\"");
			val = Eval ("staticString");
			Assert.AreEqual ("\"test\"", val.Value);
			Assert.AreEqual ("string", val.TypeName);
			Eval ("staticString = \"some static\"");
			val = Eval ("staticString");
			Assert.AreEqual ("\"some static\"", val.Value);
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
		
			ds.Options.EvaluationOptions.AllowTargetInvoke = false;
			val = Eval ("dict");
			ds.Options.EvaluationOptions.AllowTargetInvoke = true;
			Assert.AreEqual ("{System.Collections.Generic.Dictionary<int,string[]>}", val.Value);
			Assert.AreEqual ("System.Collections.Generic.Dictionary<int,string[]>", val.TypeName);
			
			ds.Options.EvaluationOptions.AllowTargetInvoke = false;
			val = Eval ("dictArray");
			ds.Options.EvaluationOptions.AllowTargetInvoke = true;
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
	}
}
