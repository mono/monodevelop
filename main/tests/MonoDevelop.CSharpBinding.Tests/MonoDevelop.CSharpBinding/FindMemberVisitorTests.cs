////
//// FindMemberVisitorTests.cs
////
//// Author:
////   Mike Krüger <mkrueger@novell.com>
////
//// Copyright (C)  2009  Novell, Inc (http://www.novell.com)
////
//// Permission is hereby granted, free of charge, to any person obtaining
//// a copy of this software and associated documentation files (the
//// "Software"), to deal in the Software without restriction, including
//// without limitation the rights to use, copy, modify, merge, publish,
//// distribute, sublicense, and/or sell copies of the Software, and to
//// permit persons to whom the Software is furnished to do so, subject to
//// the following conditions:
//// 
//// The above copyright notice and this permission notice shall be
//// included in all copies or substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////
//
//using System;
//using System.Collections.Generic;
//using System.Text;
//
//using NUnit.Framework;
//using Mono.TextEditor;
//using MonoDevelop.Core;
//using MonoDevelop.Projects;
//using MonoDevelop.Projects.Dom;
//using MonoDevelop.Projects.Dom.Parser;
//using MonoDevelop.CSharpBinding;
//using MonoDevelop.Projects.CodeGeneration;
//using MonoDevelop.Projects.Text;
//using ICSharpCode.OldNRefactory.Visitors;
//using MonoDevelop.CSharp.Parser;
//using MonoDevelop.CSharp.Resolver;
//using MonoDevelop.CSharp.Refactoring;
//
//namespace MonoDevelop.CSharpBinding.Tests
//{
//	[TestFixture]
//	public class FindMemberVisitorTests : UnitTests.TestBase
//	{
//		#region TestHelper
//		static McsParser parser = new McsParser ();
//		
//		void RunTest (string test)
//		{
//			RunTest (test, null);
//		}
//		
//		void RunTest (string test, LocalVariable localVariable)
//		{
//			StringBuilder     testText           = new StringBuilder ();
//			List<DomLocation> expectedReferences = new List<DomLocation> ();
//			DomLocation memberLocation = DomLocation.Empty;
//			int line = 1, col = 1;
//			foreach (char ch in test) {
//				switch (ch) {
//				case '$':
//					memberLocation = new DomLocation (line, col);
//					break;
//				case '@':
//					expectedReferences.Add (new DomLocation (line, col));
//					break;
//				default:
//					col++;
//					if (ch == '\n') {
//						col = 1;
//						line++;
//					}
//					testText.Append (ch);
//					break;
//				}
//			}
//			DotNetProject project = new DotNetAssemblyProject ("C#");
//			project.FileName = "/tmp/a.csproj";
//			
//			SimpleProjectDom dom = new SimpleProjectDom ();
//			dom.Project = project;
//			ProjectDomService.RegisterDom (dom, "Project:" + project.FileName);
//			
//			ParsedDocument parsedDocument = parser.Parse (null, "a.cs", testText.ToString ());
//			dom.Add (parsedDocument.CompilationUnit);
//			
//			TestViewContent testViewContent = new TestViewContent ();
//			testViewContent.Name = "a.cs";
//			testViewContent.Text = testText.ToString ();
//		//	RefactorerContext ctx = new RefactorerContext (dom, new DumbTextFileProvider(testViewContent), null);
//			NRefactoryResolver resolver = new NRefactoryResolver (dom, 
//			                                                      parsedDocument.CompilationUnit, 
//			                                                      testViewContent.Data, 
//			                                                      "a.cs");
//			SearchMemberVisitor smv = new SearchMemberVisitor (memberLocation.Line);
//			if (localVariable != null) {
//				((LocalVariable)localVariable).DeclaringMember = parsedDocument.CompilationUnit.GetMemberAt (expectedReferences[0]);
//				smv.FoundMember = localVariable;
//			} else {
//				smv.Visit (parsedDocument.CompilationUnit, null);
//				if (smv.FoundMember == null) {
//					ResolveResult resolveResult = resolver.ResolveIdentifier ("a", memberLocation);
//					if (resolveResult is LocalVariableResolveResult)
//						smv.FoundMember = ((LocalVariableResolveResult)resolveResult).LocalVariable;
//				}
//			}
//			
//			Assert.IsNotNull (smv.FoundMember, "Member to search not found.");
//			if (smv.FoundMember is IType) {
//				smv.FoundMember = dom.GetType (((IType)smv.FoundMember).FullName, 
//				                               ((IType)smv.FoundMember).TypeParameters.Count,
//				                               true);
//			}
//			FindMemberAstVisitor astVisitor = new FindMemberAstVisitor (testViewContent.GetTextEditorData ().Document, smv.FoundMember);
//			astVisitor.RunVisitor (resolver);
//			
//			int i = 0, j = 0;
//			StringBuilder errorText = new StringBuilder ();
//			Document doc = new Document ();
//			doc.Text = testViewContent.Text;
//			while (i < expectedReferences.Count && j < astVisitor.FoundReferences.Count) {
//				
//				if (expectedReferences[i].Line != astVisitor.FoundReferences[j].Line || expectedReferences[i].Column != astVisitor.FoundReferences[j].Column) {
//					if (expectedReferences[i].Line < astVisitor.FoundReferences[j].Line) {
//						errorText.Append ("Reference at  line " + expectedReferences[i].Line + " not found.");
//						errorText.AppendLine ();
//						errorText.Append (doc.GetTextAt (doc.GetLine (expectedReferences[i].Line)).Replace ('\t', ' '));
//						errorText.AppendLine ();
//						errorText.Append (new string (' ', expectedReferences[i].Column));errorText.Append ('^');
//						errorText.AppendLine ();
//						i++;
//						continue;
//					}
//					if (expectedReferences[i].Line > astVisitor.FoundReferences[j].Line) {
//						errorText.Append ("Found unexpected Reference at line " + astVisitor.FoundReferences[j].Line);
//						errorText.AppendLine ();
//						errorText.Append (doc.GetTextAt (doc.GetLine (astVisitor.FoundReferences[j].Line)).Replace ('\t', ' '));
//						errorText.AppendLine ();
//						errorText.Append (new string (' ', astVisitor.FoundReferences[j].Column));errorText.Append ('^');
//						errorText.AppendLine ();
//						j++;
//						continue;
//					}
//					
//					errorText.Append ("Column mismatch at line " + astVisitor.FoundReferences[j].Line + " was: " + astVisitor.FoundReferences[j].Column + " should be:" + expectedReferences[i].Column);
//					errorText.AppendLine ();
//					errorText.Append (doc.GetTextAt (doc.GetLine (astVisitor.FoundReferences[j].Line)).Replace ('\t', ' '));
//					errorText.Append (new string (' ', expectedReferences[i].Column));errorText.Append ('^');
//					errorText.AppendLine ();
//					errorText.Append (new string (' ', astVisitor.FoundReferences[j].Column));errorText.Append ('^');
//					errorText.AppendLine ();
//				}
//				i++;j++;
//			}
//			while (i < expectedReferences.Count) {
//				errorText.Append ("Reference at  line " + expectedReferences[i].Line + " not found.");
//				errorText.AppendLine ();
//				errorText.Append (doc.GetTextAt (doc.GetLine (expectedReferences[i].Line)).Replace ('\t', ' '));
//				errorText.AppendLine ();
//				errorText.Append (new string (' ', expectedReferences[j].Column));errorText.Append ('^');
//				errorText.AppendLine ();
//				i++;
//			}
//			while (j < astVisitor.FoundReferences.Count) {
//				errorText.Append ("Found unexpected Reference at line " + astVisitor.FoundReferences[j].Line);
//				errorText.AppendLine ();
//				errorText.Append (doc.GetTextAt (doc.GetLine (astVisitor.FoundReferences[j].Line)).Replace ('\t', ' '));
//				errorText.AppendLine ();
//				errorText.Append (new string (' ', astVisitor.FoundReferences[i].Column));errorText.Append ('^');
//				errorText.AppendLine ();
//				j++;
//			}
//			if (errorText.Length > 0)
//				Assert.Fail ("Member to find:" + smv.FoundMember + Environment.NewLine + errorText.ToString () + Environment.NewLine + "found : " + astVisitor.FoundReferences.Count + " expected:" + expectedReferences.Count);
//		}
//		
//		class DumbTextFileProvider : ITextFileProvider
//		{
//			IEditableTextFile file;
//			public DumbTextFileProvider (IEditableTextFile file)
//			{
//				this.file = file;
//			}
//			public IEditableTextFile GetEditableTextFile (FilePath filePath)
//			{
//				return file;
//			}
//		}
//		
//		class SearchMemberVisitor : AbstractDomVisitor<object, object>
//		{
//			public INode FoundMember {
//				get;
//				set;
//			}
//			int lineNumber;
//			public SearchMemberVisitor (int lineNumber)
//			{
//				this.lineNumber = lineNumber;
//			}
//			
//			void Check (IMember member)
//			{
//				if (member.Location.Line == lineNumber) {
//					FoundMember = member;
//				}
//			}
//			
//			public override object Visit (IType type, object data)
//			{
//				Check (type);
//				foreach (IMember member in type.Members) {
//					member.AcceptVisitor (this, data);
//				}
//				return base.Visit (type, data);
//			}
//		
//			public override object Visit (IField field, object data)
//			{
//				Check (field);
//				return base.Visit (field, data);
//			}
//			
//			public override object Visit (IMethod method, object data)
//			{
//				Check (method);
//				return base.Visit (method, data);
//			}
//			
//			public override object Visit (MonoDevelop.Projects.Dom.IParameter parameter, object data)
//			{
//				if (parameter.Location.Line == lineNumber)
//					FoundMember = parameter;
//				return base.Visit (parameter, data);
//			}
//
//			public override object Visit (IProperty property, object data)
//			{
//				Check (property);
//				return base.Visit (property, data);
//			}
//			
//			public override object Visit (IEvent evt, object data)
//			{
//				Check (evt);
//				return base.Visit (evt, data);
//			}
//			
//			public override object Visit (LocalVariable var, object data)
//			{
//				if (var.Region.Start.Line == lineNumber)
//					FoundMember = var;
//				return base.Visit (var, data);
//			}
//		}
//		#endregion
//		
//		[Test()]
//		public void FindClassReferences ()
//		{
//			RunTest (
//@"class $@Test {
//	@Test (@Test t)
//	{
//	}
//	~@Test ()
//	{}
//
//	void TestMe(@Test p)
//	{
//		@Test i;
//	}
//}
//
//delegate @Test TestDelegate (@Test test);
//
//class OuterTest : @Test
//{
//	@Test testField;
//	@Test TestProperty { get { } }
//	
//	event @Test TestEvent;
//	public @Test this[int i] { get { } }
//	public int this[@Test t] { get { } }
//	
//	public OuterTest (object t) : base ((@Test)t)
//	{
//	}
//
//	@Test Outer (object o)
//	{
//		return ((@Test)o);
//	}
//}
//namespace SomethingDifferent 
//{
//	class Test
//	{
//	}
//}
//");
//		}
//		
//		[Test()]
//		public void FindFieldReferences ()
//		{
//			RunTest (
//@"class TestClass {
//	protected int $@testField;
//
//	TestClass ()
//	{
//		@testField = 5;
//	}
//	
//	void TestMe(int f)
//	{
//		this.@testField = f;
//	}
//}
//
//
//class OuterTest : TestClass
//{
//	int TestProperty { get { return base.@testField; } }
//}
//
//namespace SomethingDifferent
//{
//	class Test
//	{
//		int testField;
//	}
//}
//");
//		}
//		
//		[Test()]
//		public void FindEventReferences ()
//		{
//			RunTest (
//@"class TestClass {
//	delegate void TestEventDelegate ();
//
//	public event TestEventDelegate $@MyEvent;
//
//	TestClass ()
//	{
//		@MyEvent += TestMe;
//	}
//	
//	void TestMe()
//	{
//	}
//}
//
//
//class OuterTest : TestClass
//{
//	void Test ()
//	{
//		@MyEvent -= TestMe;
//	}
//}
//
//namespace SomethingDifferent
//{
//	class Test
//	{
//		public event TestClass.TestEventDelegate MyEvent;
//	}
//}
//");
//		}
//		
//		[Test()]
//		public void FindMethodReferences ()
//		{
//			RunTest (
//@"class TestClass {
//	public void $@TestMethod (
//int a, int b)
//	{
//	}
//	public void TestMethod (int a)
//	{
//		@TestMethod (a, 6);
//	}
//	public void TestMethod ()
//	{
//		TestMethod (4);
//	}
//}
//
//
//class OuterTest : TestClass
//{
//	void A ()
//	{
//		@TestMethod (5, 4);
//		TestMethod (5);
//	}
//}
//
//namespace SomethingOuter
//{
//	class Test
//	{
//		void Bla (TestClass t)
//		{
//			t.@TestMethod (5, 4);
//		}
//	}
//}
//");
//		}
//		
//		[Test()]
//		public void FindPropertyReferences ()
//		{
//			RunTest (
//@"class TestClass {
//	public int $@MyProperty { get {} set {}}
//	
//	public void TestMethod (int a)
//	{
//		@MyProperty = a;
//	}
//	public void TestMethod ()
//	{
//		WriteLine (this.@MyProperty);
//	}
//}
//
//
//class OuterTest : TestClass
//{
//	void A ()
//	{
//		@MyProperty = 5;
//		WriteLine (base.@MyProperty);
//		WriteLine (this.@MyProperty);
//	}
//}
//
//namespace SomethingOuter
//{
//	class Test
//	{
//		void Bla (TestClass t)
//		{
//			t.@MyProperty = 5;
//		}
//	}
//}
//");
//		}
//
//		[Test()]
//		public void FindParameterReferences ()
//		{
//			RunTest (
//@"class TestClass {
//	public void TestMethod (
//int $@a, 
//int b)
//	{
//		WriteLine (@a);
//		@a--;
//		b = @a;
//		@a = b;
//	}
//}
//");
//		}
//
//		[Test()]
//		public void FindLocalVariableReferences ()
//		{
//			RunTest (
//@"class TestClass {
//	public void TestMethod ()
//	{
//		int $@a;
//		int b = 5;
//		WriteLine (@a);
//		@a--;
//		b = @a;
//		@a = b;
//	}
//
//	public void TestMethod2 ()
//	{
//		int a;
//		WriteLine (a);
//		a--;
//		int b = a;
//	}
//}
//");
//		}
//		
//		/// <summary>
//		/// Bug 480492 - Find field references returns incorrect references
//		/// </summary>
//		[Test()]
//		public void TestBug480492 ()
//		{
//			RunTest (
//@"class BaseClass
//{
//}
//
//class A : BaseClass
//{
//	BaseClass $@myField;
//}
//
//class B : BaseClass
//{
//	BaseClass myField;
//	void TestMe ()
//	{
//		myField = null; // this should not be found.
//		this.myField = null; // this should not be found.
//	}
//}");
//		}
//			
//		/// <summary>
//		/// Bug 493202 - List References on private constructor yields nothing
//		/// </summary>
//		[Test()]
//		public void TestBug493202 ()
//		{
//			RunTest (
//@"public class Foo {
//   $@Foo () //right click on Foo and list constructor references
//   {}
//
//   public Foo Instance()
//   {
//     return new @Foo ();
//   }
//}
//");
//		}
//		
//		
//				
//		/// <summary>
//		/// Bug 531525 - Refactoring + Renaming fails for delegate
//		/// </summary>
//		[Test()]
//		public void TestBug531525 ()
//		{
//			RunTest (
//@"public class Foo {
//	void $@IdeAppWorkspaceSolutionLoaded (
//object sender, 
//SolutionEventArgs e)
//	{}
//
//	public void Main()
//	{
//		IdeApp.Workspace.SolutionLoaded += @IdeAppWorkspaceSolutionLoaded;
//	}
//}
//");
//		}
//		
//		/// <summary>
//		/// Bug 545361 - Method -> Rename doesn't update instance in delegate constructor
//		/// </summary>
//		[Test()]
//		public void TestBug545361 ()
//		{
//			RunTest (
//@"class TestClass {
//	public void $@FlyZoomInCallback ()
//	{
//	}
//	public void TestMethod (int a)
//	{
//		GLib.Idle.Add(new GLib.IdleHandler(@FlyZoomInCallback));
//	}
//}
//");
//		}
//		
//		
//		/// <summary>
//		/// Bug 547949 - Rename partial classes does not rename both classes
//		/// </summary>
//		[Test()]
//		public void TestBug547949 ()
//		{
//			RunTest (
//@"partial class $@MyTest
//{
//	void Test1 ()
//	{
//		@MyTest test;
//	}
//	
//	public @MyTest (int a)
//	{
//	}
//}
//
//partial class @MyTest
//{
//	public @MyTest ()
//	{
//	}
//}
//");
//		}
//
//		/// <summary>
//		/// Bug 549858 - Refactoring does not change properties in lambda expressions
//		/// </summary>
//		[Test()]
//		public void TestBug549858 ()
//		{
//			RunTest (
//@"public delegate S MyFunc<T, S> (T t);
//
//public static class TypeManager
//{
//	public static object GetProperty<TType> (MyFunc<TType, object> expression)
//	{
//		return null;
//	}
//}
//
//class TestClass
//{
//	public string $@Value { get; set; }
//	
//	static object ValueProperty = TypeManager.GetProperty<TestClass> (x => x.@Value);
//}
//");
//		}
//		
//		/// <summary>
//		/// Bug 585454 - Lacking one reference to an enum type
//		/// </summary>
//		[Test()]
//		public void TestBug585454 ()
//		{
//			RunTest (
//@"
//internal enum $@EnumTest {
//	Value1,
//	Value2
//}
//class DemoMain
//{
//	@EnumTest innerEnum;
//
//	public DemoMain (@EnumTest theEnum) {
//		innerEnum = theEnum;
//
//		if (innerEnum == @EnumTest.Value1)
//			throw new Exception ();
//	}
//}
//");
//		}
//		
//		/// <summary>
//		/// Bug 587071 - Find references shows a lot of methods with the same name but not from the correct class
//		/// </summary>
//		[Test()]
//		public void TestBug587071 ()
//		{
//			RunTest (
//@"
//
//class Base {
//}
//
//class A : Base 
//{
//	public virtual void $@FooBar () {}
//}
//
//
//class B : Base 
//{
//	public virtual void FooBar () {}
//}
//");
//		}
//
//		/// <summary>
//		/// Bug 587530 – for/foreach rename refactoring ignores the scope
//		/// </summary>
//		[Test()]
//		public void TestBug587530 ()
//		{
//			LocalVariable localVariable = new LocalVariable (null,
//			                                  "t",
//			                                  DomReturnType.Int32,
//			                                  new DomRegion (12, 8, 13, 1));
//			RunTest (
//@"using System;
//
//class C
//{
//	static void Main ()
//	{
//		for (int t = 0; 
//t < 10;
//++t)
//		Console.WriteLine (t);
//
//		for (int $@t = 0; 
//@t < 10;
//++@t)
//			Console.WriteLine (@t);
//	}
//}
//", localVariable);
//		}
//
//		/// <summary>
//		/// Bug 605104 - Highlighter fails to find an instance of my method
//		/// </summary>
//		[Test()]
//		public void TestBug605104 ()
//		{
//			RunTest (
//@"class TestClass
//{
//	bool $@RemoveFromFiltered (
//object item)
//	{
//		return item != null;
//	}
//
//	void RemoveFromFilteredAndGroup (object item)
//	{
//		if (@RemoveFromFiltered (item) && item != null)
//			;
//	}
//}
//");
//		}
//
//		/// <summary>
//		/// Bug 615702 - In-place variable renaming can't rename foreach loop variables
//		/// </summary>
//		[Test()]
//		public void TestBug615702 ()
//		{
//			LocalVariable localVariable = new LocalVariable (null,
//			                                  "obj",
//			                                  DomReturnType.Int32,
//			                                  new DomRegion (6, 3, 8, 3));
//			RunTest (
//@"class FooBar
//{
//	public static void Main (string[] args)
//	{
//		foreach (object $@obj in new object[3])
//		{
//			Console.WriteLine (@obj.GetType());
//		}
//	}
//}", localVariable);
//		}
//		
//		
//		/// <summary>
//		/// Bug 615983 - Refactoring does not include object initializers
//		/// </summary>
//		[Test()]
//		public void TestBug615983 ()
//		{
//			RunTest (
//@"class test
//{
//	public string $@property { get; set; }
//
//	void Test ()
//	{
//		test product = new test {
//			@property = ""asdf""
//		};
//		product.@property = ""asdf"";
//	}
//}");
//		}
//		
//		/// <summary>
//		/// Bug 693228 - Rename in body of foreach loop doesn't change declaration instance
//		/// </summary>
//		[Test()]
//		public void TestBug693228 ()
//		{
//			LocalVariable localVariable = new LocalVariable (null,
//			                                  "arg",
//			                                  DomReturnType.String,
//			                                  new DomRegion (4, 29, 6, 9));
//			RunTest (
//@"class TestClass {
//	public static void Main (string[] args)
//	{
//		foreach (var $@arg in args) {
//			Console.WriteLine (@arg);
//		}
//		
//	}
//}
//", localVariable);
//		}
//		
//		/*
//		[Test()]
//		public void FindInterfaceMethodReferences ()
//		{
//			RunTest (
//@"
//public interface ITest {
//    void $@doSomething(double par);
//}
//
//public abstract class AbstractTest: ITest {
//    public abstract void @doSomething(double par); // Not renamed!!
//}
//
//public class ConcreteTest: AbstractTest {
//    public override void @doSomething(double par)
//    {
//		base.@doSomething(par);
//    }
//}
//");
//		}
//		
//		[Test()]
//		public void FindOverridenMethodReferences ()
//		{
//			RunTest (
//@"
//public class BaseTest
//{
//	public virtual void $@MyMethod()
//	{
//		@MyMethod ();
//	}
//}
//
//public class Test : BaseTest
//{
//	public override void @MyMethod()
//	{
//		@MyMethod ();
//	}
//}
//");
//		}*/
//
//	}
//}
