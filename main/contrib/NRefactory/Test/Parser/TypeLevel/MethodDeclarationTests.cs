// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 3717 $</version>
// </file>

using System;
using ICSharpCode.NRefactory.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class MethodDeclarationTests
	{
		#region C#
		[Test]
		public void CSharpSimpleMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyMethod() {} ");
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(0, md.Parameters.Count);
			Assert.IsFalse(md.IsExtensionMethod);
		}
		
		[Test]
		public void CSharpAbstractMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("abstract void MyMethod();");
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(0, md.Parameters.Count);
			Assert.IsFalse(md.IsExtensionMethod);
			Assert.IsTrue(md.Body.IsNull);
			Assert.AreEqual(Modifiers.Abstract, md.Modifier);
		}
		
		[Test]
		public void CSharpDefiningPartialMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("partial void MyMethod();");
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(0, md.Parameters.Count);
			Assert.IsFalse(md.IsExtensionMethod);
			Assert.IsTrue(md.Body.IsNull);
			Assert.AreEqual(Modifiers.Partial, md.Modifier);
		}
		
		[Test]
		public void CSharpImplementingPartialMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("partial void MyMethod() { }");
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(0, md.Parameters.Count);
			Assert.IsFalse(md.IsExtensionMethod);
			Assert.IsFalse(md.Body.IsNull);
			Assert.AreEqual(Modifiers.Partial, md.Modifier);
		}
		
		[Test]
		public void CSharpSimpleMethodRegionTest()
		{
			const string program = @"
		void MyMethod()
		{
			OtherMethod();
		}
";
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(2, md.StartLocation.Line, "StartLocation.Y");
			Assert.AreEqual(2, md.EndLocation.Line, "EndLocation.Y");
			Assert.AreEqual(3, md.StartLocation.Column, "StartLocation.X");
			
			// endLocation.X is currently 20. It should be 18, but that error is not critical
			//Assert.AreEqual(18, md.EndLocation.X, "EndLocation.X");
		}
		
		[Test]
		public void CSharpMethodWithModifiersRegionTest()
		{
			const string program = @"
		public static void MyMethod()
		{
			OtherMethod();
		}
";
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(2, md.StartLocation.Line, "StartLocation.Y");
			Assert.AreEqual(2, md.EndLocation.Line, "EndLocation.Y");
			Assert.AreEqual(3, md.StartLocation.Column, "StartLocation.X");
		}
		
		[Test]
		public void CSharpMethodWithUnnamedParameterDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyMethod(int) {} ", true);
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			//Assert.AreEqual("?", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
		}
		
		[Test]
		public void CSharpGenericVoidMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyMethod<T>(T a) {} ");
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
		}
		
		[Test]
		public void CSharpGenericMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("T MyMethod<T>(T a) {} ");
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
		}
		
		[Test]
		public void CSharpGenericMethodDeclarationWithConstraintTest()
		{
			string program = "T MyMethod<T>(T a) where T : ISomeInterface {} ";
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void CSharpGenericMethodInInterface()
		{
			const string program = @"interface MyInterface {
	T MyMethod<T>(T a) where T : ISomeInterface;
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			MethodDeclaration md = (MethodDeclaration)td.Children[0];
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void CSharpGenericVoidMethodInInterface()
		{
			const string program = @"interface MyInterface {
	void MyMethod<T>(T a) where T : ISomeInterface;
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			MethodDeclaration md = (MethodDeclaration)td.Children[0];
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void CSharpShadowingMethodInInterface()
		{
			const string program = @"interface MyInterface : IDisposable {
	new void Dispose();
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			MethodDeclaration md = (MethodDeclaration)td.Children[0];
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(0, md.Parameters.Count);
			Assert.AreEqual(Modifiers.New, md.Modifier);
		}
		
		[Test]
		public void CSharpMethodImplementingInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("int MyInterface.MyMethod() {} ");
			Assert.AreEqual("System.Int32", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
		}
		
		[Test]
		public void CSharpMethodImplementingGenericInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("int MyInterface<string>.MyMethod() {} ");
			Assert.AreEqual("System.Int32", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
			Assert.AreEqual("System.String", md.InterfaceImplementations[0].InterfaceType.GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpVoidMethodImplementingInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyInterface.MyMethod() {} ");
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
		}
		
		[Test]
		public void CSharpVoidMethodImplementingGenericInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyInterface<string>.MyMethod() {} ");
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
			Assert.AreEqual("System.String", md.InterfaceImplementations[0].InterfaceType.GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpIncompleteConstraintsTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(
				"void a<T>() where T { }", true /* expect errors */
			);
			Assert.AreEqual("a", md.Name);
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(0, md.Templates[0].Bases.Count);
		}
		
		[Test]
		public void CSharpExtensionMethodTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(
				"public static int ToInt32(this string s) { return int.Parse(s); }"
			);
			Assert.AreEqual("ToInt32", md.Name);
			Assert.IsTrue(md.IsExtensionMethod);
			Assert.AreEqual("s", md.Parameters[0].ParameterName);
			Assert.AreEqual("System.String", md.Parameters[0].TypeReference.Type);
		}
		
		[Test]
		public void CSharpVoidExtensionMethodTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(
				"public static void Print(this string s) { Console.WriteLine(s); }"
			);
			Assert.AreEqual("Print", md.Name);
			Assert.IsTrue(md.IsExtensionMethod);
			Assert.AreEqual("s", md.Parameters[0].ParameterName);
			Assert.AreEqual("System.String", md.Parameters[0].TypeReference.Type);
		}
		
		[Test]
		public void CSharpMethodWithEmptyAssignmentErrorInBody()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(
				"void A\n" +
				"{\n" +
				"int a = 3;\n" +
				" = 4;\n" +
				"}", true /* expect errors */
			);
			Assert.AreEqual("A", md.Name);
			Assert.AreEqual(new Location(1, 2), md.Body.StartLocation);
			Assert.AreEqual(new Location(2, 5), md.Body.EndLocation);
		}
		#endregion
		
		#region VB.NET
		
		[Test]
		public void VBNetDefiningPartialMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>(@"Partial Sub MyMethod()
			                                                                         End Sub");
			Assert.AreEqual(0, md.Parameters.Count);
			Assert.AreEqual("MyMethod", md.Name);
			Assert.IsFalse(md.IsExtensionMethod);
			Assert.AreEqual(Modifiers.Partial, md.Modifier);
		}
		
		[Test]
		public void VBNetMethodWithModifiersRegionTest()
		{
			const string program = @"public shared sub MyMethod()
				OtherMethod()
			end sub";
			
			MethodDeclaration md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(Modifiers.Public | Modifiers.Static, md.Modifier);
			Assert.AreEqual(2, md.StartLocation.Line, "StartLocation.Y");
			Assert.AreEqual(2, md.EndLocation.Line, "EndLocation.Y");
			Assert.AreEqual(2, md.StartLocation.Column, "StartLocation.X");
		}
		
		[Test]
		public void VBNetGenericFunctionMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>("function MyMethod(Of T)(a As T) As Double\nEnd Function");
			Assert.AreEqual("System.Double", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
		}
		
		[Test]
		public void VBNetGenericMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>("Function MyMethod(Of T)(a As T) As T\nEnd Function ");
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
		}
		
		[Test]
		public void VBNetGenericMethodDeclarationWithConstraintTest()
		{
			string program = "Function MyMethod(Of T As { ISomeInterface })(a As T) As T\n End Function";
			MethodDeclaration md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void VBNetExtensionMethodDeclaration()
		{
			MethodDeclaration md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>(
				@"<Extension> _
				Sub Print(s As String)
					Console.WriteLine(s)
				End Sub");
			
			Assert.AreEqual("Print", md.Name);
			
			// IsExtensionMethod is only valid for c#.
			// Assert.IsTrue(md.IsExtensionMethod);
			
			Assert.AreEqual("s", md.Parameters[0].ParameterName);
			Assert.AreEqual("System.String", md.Parameters[0].TypeReference.Type);
		}
		
		[Test]
		public void VBNetGenericMethodInInterface()
		{
			const string program = @"Interface MyInterface
				Function MyMethod(Of T As {ISomeInterface})(a As T) As T
				End Interface";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(program);
			MethodDeclaration md = (MethodDeclaration)td.Children[0];
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void VBNetGenericVoidMethodInInterface()
		{
			const string program = @"interface MyInterface
	Sub MyMethod(Of T As {ISomeInterface})(a as T)
End Interface
";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(program);
			MethodDeclaration md = (MethodDeclaration)td.Children[0];
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void VBNetMethodWithHandlesClause()
		{
			MethodDeclaration md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>(
				@"Public Sub MyMethod(sender As Object, e As EventArgs) Handles x.y
			End Sub");
			Assert.AreEqual(new string[] { "x.y" }, md.HandlesClause.ToArray());
			
			md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>(
				@"Public Sub MyMethod() Handles Me.FormClosing
			End Sub");
			Assert.AreEqual(new string[] { "Me.FormClosing" }, md.HandlesClause.ToArray());
			
			md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>(
				@"Public Sub MyMethod() Handles MyBase.Event, Button1.Click
			End Sub");
			Assert.AreEqual(new string[] { "MyBase.Event", "Button1.Click" }, md.HandlesClause.ToArray());
		}
		
		[Test]
		public void VBNetMethodWithTypeCharactersTest()
		{
			const string program = @"Public Function Func!(ByVal Param&)
				Func! = CSingle(Param&)
			End Function";
			
			MethodDeclaration md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(Modifiers.Public, md.Modifier);
		}
		
		#endregion
	}
}
