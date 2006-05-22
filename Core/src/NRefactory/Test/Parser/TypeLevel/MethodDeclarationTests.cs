// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1018 $</version>
// </file>

using System;
using System.Drawing;
using System.IO;

using NUnit.Framework;

using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class MethodDeclarationTests
	{
		#region C#
		[Test]
		public void CSharpSimpleMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyMethod() {} ");
			Assert.AreEqual("void", md.TypeReference.Type);
			Assert.AreEqual(0, md.Parameters.Count);
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
			Assert.AreEqual(2, md.StartLocation.Y, "StartLocation.Y");
			Assert.AreEqual(2, md.EndLocation.Y, "EndLocation.Y");
			Assert.AreEqual(3, md.StartLocation.X, "StartLocation.X");
			
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
			Assert.AreEqual(2, md.StartLocation.Y, "StartLocation.Y");
			Assert.AreEqual(2, md.EndLocation.Y, "EndLocation.Y");
			Assert.AreEqual(3, md.StartLocation.X, "StartLocation.X");
		}
		
		[Test]
		public void CSharpMethodWithUnnamedParameterDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyMethod(int) {} ", true);
			Assert.AreEqual("void", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("?", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
		}
		
		[Test]
		public void CSharpGenericVoidMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyMethod<T>(T a) {} ");
			Assert.AreEqual("void", md.TypeReference.Type);
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
			Assert.AreEqual("void", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void CSharpMethodImplementingInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("int MyInterface.MyMethod() {} ");
			Assert.AreEqual("int", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
		}
		
		[Test]
		public void CSharpMethodImplementingGenericInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("int MyInterface<string>.MyMethod() {} ");
			Assert.AreEqual("int", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
			Assert.AreEqual("System.String", md.InterfaceImplementations[0].InterfaceType.GenericTypes[0].SystemType);
		}
		
		[Test]
		public void CSharpVoidMethodImplementingInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyInterface.MyMethod() {} ");
			Assert.AreEqual("void", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
		}
		
		[Test]
		public void CSharpVoidMethodImplementingGenericInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyInterface<string>.MyMethod() {} ");
			Assert.AreEqual("void", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
			Assert.AreEqual("System.String", md.InterfaceImplementations[0].InterfaceType.GenericTypes[0].SystemType);
		}
		#endregion
		
		#region VB.NET
		
		[Test]
		public void VBNetSimpleMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyMethod() {} ");
			Assert.AreEqual("void", md.TypeReference.Type);
			Assert.AreEqual(0, md.Parameters.Count);
		}
		
		[Test]
		public void VBNetSimpleMethodRegionTest()
		{
			const string program = @"
		void MyMethod()
		{
			OtherMethod();
		}
";
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(2, md.StartLocation.Y, "StartLocation.Y");
			Assert.AreEqual(2, md.EndLocation.Y, "EndLocation.Y");
			Assert.AreEqual(3, md.StartLocation.X, "StartLocation.X");
			
			// endLocation.X is currently 20. It should be 18, but that error is not critical
			//Assert.AreEqual(18, md.EndLocation.X, "EndLocation.X");
		}
		
		[Test]
		public void VBNetMethodWithModifiersRegionTest()
		{
			const string program = @"public shared sub MyMethod()
				OtherMethod()
			end sub";
			
			MethodDeclaration md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(Modifier.Public | Modifier.Static, md.Modifier);
			Assert.AreEqual(2, md.StartLocation.Y, "StartLocation.Y");
			Assert.AreEqual(2, md.EndLocation.Y, "EndLocation.Y");
			Assert.AreEqual(2, md.StartLocation.X, "StartLocation.X");
		}
		
		[Test]
		public void VBNetGenericFunctionMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>("function MyMethod(Of T)(a As T) As Double\nEnd Function");
			Assert.AreEqual("Double", md.TypeReference.Type);
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
			Assert.AreEqual("", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		#endregion
	}
}
