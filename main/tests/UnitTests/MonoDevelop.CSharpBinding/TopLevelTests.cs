//
// ParserTest.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.CodeDom;
using System.Collections.Generic;
using NUnit.Framework;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharpBinding;

namespace MonoDevelop.CSharpBinding.Tests
{
	[TestFixture]
	public class TopLevelTests
	{
		void DoTestUsings (IParser parser)
		{
			ICompilationUnit unit = parser.Parse (null, "a.cs", 
@"using System;
using NUnit.Framework;").CompilationUnit;
			foreach (IUsing u in unit.Usings) {
				foreach (string ns in u.Namespaces) {
					if (ns == "System") {
						Assert.AreEqual (1, u.Region.End.Line);
						Assert.AreEqual (1, u.Region.Start.Line);
					} else if (ns == "NUnit.Framework") {
						Assert.AreEqual (2, u.Region.End.Line);
						Assert.AreEqual (2, u.Region.Start.Line);
					} else {
						Assert.Fail ("Unknown using: " + ns);
					}
				}
			}
		}
		
		[Test]
		public void TestUsings ()
		{
			DoTestUsings (new NRefactoryParser ());
			//DoTestUsings (new DomParser ());
		}
		
		void DoTestEnums (IParser parser)
		{
			ICompilationUnit unit = parser.Parse (null, "a.cs", 
@"enum TestEnum {
	A,
	B,
	C
}").CompilationUnit;
			Assert.AreEqual (1, unit.Types.Count);
			IType type = unit.Types[0];
			Assert.AreEqual (ClassType.Enum, type.ClassType);
			Assert.AreEqual ("TestEnum", type.Name);
			Assert.AreEqual (3, type.FieldCount);
			foreach (IField f in type.Fields) {
				Assert.IsTrue (f.IsConst);
				Assert.IsTrue (f.IsSpecialName);
				Assert.IsTrue (f.IsPublic);
				if (f.Name == "A") {
					Assert.AreEqual (2, f.Location.Line);
				} else if (f.Name == "B") {
					Assert.AreEqual (3, f.Location.Line);
				} else if (f.Name == "C") {
					Assert.AreEqual (4, f.Location.Line);
				} else {
					Assert.Fail ("Unknown field: " + f.Name);
				}
			}
		}
		
		[Test]
		public void TestEnums ()
		{
			DoTestEnums (new NRefactoryParser ());
//			DoTestEnums (new DomParser ());
		}
		
		void DoTestStruct (IParser parser)
		{
			ICompilationUnit unit = parser.Parse (null, "a.cs", @"struct TestStruct { }").CompilationUnit;
			Assert.AreEqual (1, unit.Types.Count);
			IType type = unit.Types[0];
			Assert.AreEqual (ClassType.Struct, type.ClassType);
			Assert.AreEqual ("TestStruct", type.Name);
		}
		
		[Test]
		public void TestStruct ()
		{
			DoTestStruct (new NRefactoryParser ());
//			DoTestStruct (new DomParser ());
		}
		
		void DoTestInterface (IParser parser)
		{
			ICompilationUnit unit = parser.Parse (null, "a.cs", @"interface TestInterface { }").CompilationUnit;
			Assert.AreEqual (1, unit.Types.Count);
			IType type = unit.Types[0];
			Assert.AreEqual (ClassType.Interface, type.ClassType);
			Assert.AreEqual ("TestInterface", type.Name);
		}
		
		[Test]
		public void TestInterface ()
		{
			DoTestInterface (new NRefactoryParser ());
//			DoTestInterface (new DomParser ());
		}
		
		void DoTestDelegate (IParser parser)
		{
			ICompilationUnit unit = parser.Parse (null, "a.cs", @"delegate void TestDelegate (int a, string b);").CompilationUnit;
			Assert.AreEqual (1, unit.Types.Count);
			IType type = unit.Types[0];
			Assert.AreEqual (ClassType.Delegate, type.ClassType);
			Assert.AreEqual ("TestDelegate", type.Name);
			foreach (IMethod method in type.Methods) {
				Assert.AreEqual ("Void", method.ReturnType.Name);
				foreach (IParameter parameter in method.Parameters) {
					if (parameter.Name == "a") {
						Assert.AreEqual ("System.Int32", parameter.ReturnType.FullName);
					} else if (parameter.Name == "b") {
						Assert.AreEqual ("System.String", parameter.ReturnType.FullName);
					} else {
						Assert.Fail ("Unknown parameter: " + parameter.Name);
					}
				}
			}
		}
		
		[Test]
		public void TestDelegate ()
		{
			DoTestDelegate (new NRefactoryParser ());
//			DoTestDelegate (new DomParser ());
		}
		
		void DoTestClass (IParser parser)
		{
			ICompilationUnit unit = parser.Parse (null, "a.cs", @"public partial class TestClass<T, S> : MyBaseClass where T : Constraint { }").CompilationUnit;
			Assert.AreEqual (1, unit.Types.Count);
			IType type = unit.Types[0];
			Assert.AreEqual (ClassType.Class, type.ClassType);
			Assert.AreEqual ("TestClass", type.Name);
			Assert.AreEqual ("MyBaseClass", type.BaseType.Name);
			Assert.AreEqual (Modifiers.Partial | Modifiers.Public, type.Modifiers);
			Assert.AreEqual (2, type.TypeParameters.Count);
			Assert.AreEqual ("T", type.TypeParameters[0].Name);
			Assert.AreEqual ("Constraint", type.TypeParameters[0].Constraints[0].Name);
			Assert.AreEqual ("S", type.TypeParameters[1].Name);
		}
		
		[Test]
		public void TestClass ()
		{
			DoTestClass (new NRefactoryParser ());
//			DoTestClass (new DomParser ());
		}
		
		void DoTestNamespace (IParser parser)
		{
			ICompilationUnit unit = parser.Parse (null, "a.cs", @"namespace Test1.Test2.Test3 { class A { } }").CompilationUnit;
			Assert.AreEqual (3, unit.Usings.Count);
			Assert.AreEqual ("Test1.Test2.Test3", unit.Usings[0].Namespaces[0]);
			Assert.AreEqual ("Test1.Test2", unit.Usings[1].Namespaces[0]);
			Assert.AreEqual ("Test1", unit.Usings[2].Namespaces[0]);
			Assert.AreEqual (1, unit.Types.Count);
			IType type = unit.Types[0];
			Assert.AreEqual ("Test1.Test2.Test3", type.Namespace);
		}
		
		[Test]
		public void TestNamespace ()
		{
			DoTestNamespace (new NRefactoryParser ());
//			DoTestNamespace (new DomParser ());
		}
		
		void DoTestAttributes (IParser parser)
		{
			ICompilationUnit unit = parser.Parse (null, "a.cs", @"[Attr1][Attr2(1,true)][Attr3('c',a=1,b=""hi"")] public class TestClass { }").CompilationUnit;
			Assert.AreEqual (1, unit.Types.Count);
			IType type = unit.Types[0];
			Assert.AreEqual (ClassType.Class, type.ClassType);
			Assert.AreEqual ("TestClass", type.Name);
			IEnumerator<IAttribute> e = type.Attributes.GetEnumerator ();
			
			Assert.IsTrue (e.MoveNext ());
			IAttribute att = e.Current;
			Assert.AreEqual ("Attr1", att.Name);
			Assert.AreEqual (0, att.PositionalArguments.Count);
			Assert.AreEqual (0, att.NamedArguments.Count);
			
			Assert.IsTrue (e.MoveNext ());
			att = e.Current;
			Assert.AreEqual ("Attr2", att.Name);
			Assert.AreEqual (2, att.PositionalArguments.Count);
			Assert.AreEqual (0, att.NamedArguments.Count);
			Assert.IsTrue (att.PositionalArguments [0] is CodePrimitiveExpression);
			CodePrimitiveExpression exp = (CodePrimitiveExpression) att.PositionalArguments [0];
			Assert.AreEqual (1, exp.Value);
			exp = (CodePrimitiveExpression) att.PositionalArguments [1];
			Assert.AreEqual (true, exp.Value);
			
			Assert.IsTrue (e.MoveNext ());
			att = e.Current;
			Assert.AreEqual ("Attr3", att.Name);
			Assert.AreEqual (1, att.PositionalArguments.Count);
			Assert.AreEqual (2, att.NamedArguments.Count);
			Assert.IsTrue (att.PositionalArguments [0] is CodePrimitiveExpression);
			exp = (CodePrimitiveExpression) att.PositionalArguments [0];
			Assert.AreEqual ('c', exp.Value);
			exp = (CodePrimitiveExpression) att.NamedArguments ["a"];
			Assert.AreEqual (1, exp.Value);
			exp = (CodePrimitiveExpression) att.NamedArguments ["b"];
			Assert.AreEqual ("hi", exp.Value);
			
			Assert.IsFalse (e.MoveNext ());
		}
		
		[Test()]
		public void TestAttributes ()
		{
			DoTestAttributes (new NRefactoryParser ());
//			DoTestAttributes (new DomParser ());
		}
	}
}
