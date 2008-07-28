// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 2229 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class LocalVariableDeclarationTests
	{
		#region C#
		
		[Test]
		public void CSharpLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("int a = 5;");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("int", type.Type);
			Assert.AreEqual(5, ((PrimitiveExpression)lvd.Variables[0].Initializer).Value);
		}
		
		[Test]
		public void CSharpComplexGenericLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("Generic<Namespace.Printable, G<Printable[]> > where = new Generic<Namespace.Printable, G<Printable[]>>();");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("where", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("Generic", type.Type);
			Assert.AreEqual(2, type.GenericTypes.Count);
			Assert.AreEqual("Namespace.Printable", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.AreEqual("G", type.GenericTypes[1].Type);
			Assert.AreEqual(1, type.GenericTypes[1].GenericTypes.Count);
			Assert.AreEqual("Printable", type.GenericTypes[1].GenericTypes[0].Type);
			
			// TODO: Check initializer
		}
		
		[Test]
		public void CSharpNestedGenericLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("MyType<string>.InnerClass<int>.InnerInnerClass a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			InnerClassTypeReference ic = (InnerClassTypeReference)lvd.GetTypeForVariable(0);
			Assert.AreEqual("InnerInnerClass", ic.Type);
			Assert.AreEqual(0, ic.GenericTypes.Count);
			ic = (InnerClassTypeReference)ic.BaseType;
			Assert.AreEqual("InnerClass", ic.Type);
			Assert.AreEqual(1, ic.GenericTypes.Count);
			Assert.AreEqual("System.Int32", ic.GenericTypes[0].SystemType);
			Assert.AreEqual("MyType", ic.BaseType.Type);
			Assert.AreEqual(1, ic.BaseType.GenericTypes.Count);
			Assert.AreEqual("System.String", ic.BaseType.GenericTypes[0].SystemType);
		}
		
		[Test]
		public void CSharpGenericWithArrayLocalVariableDeclarationTest1()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("G<int>[] a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("int", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.IsFalse(type.GenericTypes[0].IsArrayType);
			Assert.AreEqual(new int[] {0}, type.RankSpecifier);
		}
		
		[Test]
		public void CSharpGenericWithArrayLocalVariableDeclarationTest2()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("G<int[]> a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("int", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.IsFalse(type.IsArrayType);
			Assert.AreEqual(new int[] {0}, type.GenericTypes[0].RankSpecifier);
		}
		
		[Test]
		public void CSharpGenericLocalVariableDeclarationTest2()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("G<G<int> > a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("G", type.GenericTypes[0].Type);
			Assert.AreEqual(1, type.GenericTypes[0].GenericTypes.Count);
			Assert.AreEqual("int", type.GenericTypes[0].GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpGenericLocalVariableDeclarationTest2WithoutSpace()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("G<G<int>> a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("G", type.GenericTypes[0].Type);
			Assert.AreEqual(1, type.GenericTypes[0].GenericTypes.Count);
			Assert.AreEqual("int", type.GenericTypes[0].GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpGenericLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("G<int> a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("int", type.GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpSimpleLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("MyVar var = new MyVar();");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("var", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("MyVar", type.Type);
			// TODO: Check initializer
		}
		
		[Test]
		public void CSharpSimpleLocalVariableDeclarationTest1()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("yield yield = new yield();");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("yield", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("yield", type.Type);
			// TODO: Check initializer
		}
		
		[Test]
		public void CSharpNullableLocalVariableDeclarationTest1()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("int? a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Nullable", type.SystemType);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].SystemType);
		}
		
		[Test]
		public void CSharpNullableLocalVariableDeclarationTest2()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("DateTime? a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Nullable", type.SystemType);
			Assert.AreEqual("DateTime", type.GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpNullableLocalVariableDeclarationTest3()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("DateTime?[] a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.IsTrue(type.IsArrayType);
			Assert.AreEqual("System.Nullable", type.SystemType);
			Assert.AreEqual("DateTime", type.GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpNullableLocalVariableDeclarationTest4()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("SomeStruct<int?>? a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Nullable", type.SystemType);
			Assert.AreEqual("SomeStruct", type.GenericTypes[0].Type);
			Assert.AreEqual("System.Nullable", type.GenericTypes[0].GenericTypes[0].SystemType);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].GenericTypes[0].GenericTypes[0].SystemType);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a As Integer = 5");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("Integer", type.Type);
			Assert.AreEqual(5, ((PrimitiveExpression)lvd.Variables[0].Initializer).Value);
		}
		
		[Test]
		public void VBNetLocalVariableNamedOverrideDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim override As Integer = 5");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("override", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("Integer", type.Type);
			Assert.AreEqual(5, ((PrimitiveExpression)lvd.Variables[0].Initializer).Value);
		}
		
		[Test]
		public void VBNetLocalArrayDeclarationWithInitializationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a(10) As Integer");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("Integer", type.Type);
			Assert.AreEqual(new int[] { 0 } , type.RankSpecifier);
			ArrayCreateExpression ace = (ArrayCreateExpression)lvd.Variables[0].Initializer;
			Assert.AreEqual(new int[] { 0 } , ace.CreateType.RankSpecifier);
			Assert.AreEqual(1, ace.Arguments.Count);
			Assert.AreEqual(10, ((PrimitiveExpression)ace.Arguments[0]).Value);
		}
		
		[Test]
		public void VBNetLocalArrayDeclarationWithInitializationAndLowerBoundTest()
		{
			// VB.NET allows only "0" as lower bound
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a(0 To 10) As Integer");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("Integer", type.Type);
			Assert.AreEqual(new int[] { 0 } , type.RankSpecifier);
			ArrayCreateExpression ace = (ArrayCreateExpression)lvd.Variables[0].Initializer;
			Assert.AreEqual(new int[] { 0 } , ace.CreateType.RankSpecifier);
			Assert.AreEqual(1, ace.Arguments.Count);
			Assert.AreEqual(10, ((PrimitiveExpression)ace.Arguments[0]).Value);
		}
		
		[Test]
		public void VBNetLocalArrayDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a() As Integer");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("Integer", type.Type);
			Assert.AreEqual(new int[] { 0 } , type.RankSpecifier);
		}
		
		[Test]
		public void VBNetLocalJaggedArrayDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a(10)() As Integer");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("Integer", type.Type);
			Assert.AreEqual(new int[] { 0, 0 } , type.RankSpecifier);
			ArrayCreateExpression ace = (ArrayCreateExpression)lvd.Variables[0].Initializer;
			Assert.AreEqual(new int[] {0, 0}, ace.CreateType.RankSpecifier);
			Assert.AreEqual(1, ace.Arguments.Count);
			Assert.AreEqual(10, ((PrimitiveExpression)ace.Arguments[0]).Value);
		}
		
		[Test]
		public void VBNetComplexGenericLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim where As Generic(Of Printable, G(Of Printable()))");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("where", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("Generic", type.Type);
			Assert.AreEqual(2, type.GenericTypes.Count);
			Assert.AreEqual("Printable", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.AreEqual("G", type.GenericTypes[1].Type);
			Assert.AreEqual(1, type.GenericTypes[1].GenericTypes.Count);
			Assert.AreEqual("Printable", type.GenericTypes[1].GenericTypes[0].Type);
		}
		
		[Test]
		public void VBNetGenericWithArrayLocalVariableDeclarationTest1()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a As G(Of Integer)()");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("Integer", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.IsFalse(type.GenericTypes[0].IsArrayType);
			Assert.AreEqual(new int[] { 0 }, type.RankSpecifier);
		}
		
		[Test]
		public void VBNetGenericWithArrayLocalVariableDeclarationTest2()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a As G(Of Integer())");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("Integer", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.IsFalse(type.IsArrayType);
			Assert.AreEqual(1, type.GenericTypes[0].RankSpecifier.Length);
			Assert.AreEqual(0, type.GenericTypes[0].RankSpecifier[0]);
		}
		
		[Test]
		public void VBNetGenericLocalVariableDeclarationTest2()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a As G(Of G(Of Integer))");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("G", type.GenericTypes[0].Type);
			Assert.AreEqual(1, type.GenericTypes[0].GenericTypes.Count);
			Assert.AreEqual("Integer", type.GenericTypes[0].GenericTypes[0].Type);
		}
		
		[Test]
		public void VBNetGenericLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a As G(Of Integer)");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("Integer", type.GenericTypes[0].Type);
		}
		
		[Test]
		public void VBNetGenericLocalVariableInitializationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a As New G(Of Integer)");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("Integer", type.GenericTypes[0].Type);
			// TODO: Check initializer
		}
		
		[Test]
		public void VBNetNestedGenericLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a as MyType(of string).InnerClass(of integer).InnerInnerClass");
			Assert.AreEqual(1, lvd.Variables.Count);
			InnerClassTypeReference ic = (InnerClassTypeReference)lvd.GetTypeForVariable(0);
			Assert.AreEqual("InnerInnerClass", ic.Type);
			Assert.AreEqual(0, ic.GenericTypes.Count);
			ic = (InnerClassTypeReference)ic.BaseType;
			Assert.AreEqual("InnerClass", ic.Type);
			Assert.AreEqual(1, ic.GenericTypes.Count);
			Assert.AreEqual("System.Int32", ic.GenericTypes[0].SystemType);
			Assert.AreEqual("MyType", ic.BaseType.Type);
			Assert.AreEqual(1, ic.BaseType.GenericTypes.Count);
			Assert.AreEqual("System.String", ic.BaseType.GenericTypes[0].SystemType);
		}
		#endregion
	}
}
