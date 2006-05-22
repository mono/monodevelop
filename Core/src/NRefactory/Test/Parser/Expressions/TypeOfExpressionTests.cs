// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class TypeOfExpressionTests
	{
		#region C#
		[Test]
		public void CSharpSimpleTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyNamespace.N1.MyType)");
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
		}
		
		[Test]
		public void CSharpGlobalTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(global::System.Console)");
			Assert.AreEqual("System.Console", toe.TypeReference.Type);
		}
		
		[Test]
		public void CSharpPrimitiveTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(int)");
			Assert.AreEqual("System.Int32", toe.TypeReference.SystemType);
		}
		
		[Test]
		public void CSharpVoidTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(void)");
			Assert.AreEqual("System.Void", toe.TypeReference.SystemType);
		}
		
		[Test]
		public void CSharpArrayTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyType[])");
			Assert.AreEqual("MyType", toe.TypeReference.Type);
			Assert.AreEqual(new int[] {0}, toe.TypeReference.RankSpecifier);
		}
		
		[Test]
		public void CSharpGenericTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyNamespace.N1.MyType<string>)");
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
			Assert.AreEqual("System.String", toe.TypeReference.GenericTypes[0].SystemType);
		}
		
		[Test]
		public void CSharpNestedGenericTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyType<string>.InnerClass<int>.InnerInnerClass)");
			InnerClassTypeReference ic = (InnerClassTypeReference)toe.TypeReference;
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
		public void CSharpNullableTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyStruct?)");
			Assert.AreEqual("System.Nullable", toe.TypeReference.SystemType);
			Assert.AreEqual("MyStruct", toe.TypeReference.GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpUnboundTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyType<,>)");
			Assert.AreEqual("MyType", toe.TypeReference.Type);
			Assert.IsTrue(toe.TypeReference.GenericTypes[0].IsNull);
			Assert.IsTrue(toe.TypeReference.GenericTypes[1].IsNull);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBSimpleTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilVBNet.ParseExpression<TypeOfExpression>("GetType(MyNamespace.N1.MyType)");
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
		}
		
		
		[Test]
		public void VBGlobalTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilVBNet.ParseExpression<TypeOfExpression>("GetType(Global.System.Console)");
			Assert.AreEqual("System.Console", toe.TypeReference.Type);
		}
		
		[Test]
		public void VBPrimitiveTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilVBNet.ParseExpression<TypeOfExpression>("GetType(integer)");
			Assert.AreEqual("System.Int32", toe.TypeReference.SystemType);
		}
		
		[Test]
		public void VBVoidTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilVBNet.ParseExpression<TypeOfExpression>("GetType(void)");
			Assert.AreEqual("System.Void", toe.TypeReference.SystemType);
		}
		
		[Test]
		public void VBArrayTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilVBNet.ParseExpression<TypeOfExpression>("GetType(MyType())");
			Assert.AreEqual("MyType", toe.TypeReference.Type);
			Assert.AreEqual(new int[] {0}, toe.TypeReference.RankSpecifier);
		}
		
		[Test]
		public void VBGenericTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilVBNet.ParseExpression<TypeOfExpression>("GetType(MyNamespace.N1.MyType(Of string))");
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
			Assert.AreEqual("System.String", toe.TypeReference.GenericTypes[0].SystemType);
		}
		
		[Test]
		public void VBUnboundTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilVBNet.ParseExpression<TypeOfExpression>("GetType(MyType(Of ,))");
			Assert.AreEqual("MyType", toe.TypeReference.Type);
			Assert.IsTrue(toe.TypeReference.GenericTypes[0].IsNull);
			Assert.IsTrue(toe.TypeReference.GenericTypes[1].IsNull);
		}
		
		[Test]
		public void VBNestedGenericTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilVBNet.ParseExpression<TypeOfExpression>("GetType(MyType(Of string).InnerClass(of integer).InnerInnerClass)");
			InnerClassTypeReference ic = (InnerClassTypeReference)toe.TypeReference;
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
