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
	public class FieldReferenceExpressionTests
	{
		#region C#
		[Test]
		public void CSharpSimpleFieldReferenceExpressionTest()
		{
			FieldReferenceExpression fre = ParseUtilCSharp.ParseExpression<FieldReferenceExpression>("myTargetObject.myField");
			Assert.AreEqual("myField", fre.FieldName);
			Assert.IsTrue(fre.TargetObject is IdentifierExpression);
			Assert.AreEqual("myTargetObject", ((IdentifierExpression)fre.TargetObject).Identifier);
		}
		
		[Test]
		public void CSharpGenericFieldReferenceExpressionTest()
		{
			FieldReferenceExpression fre = ParseUtilCSharp.ParseExpression<FieldReferenceExpression>("SomeClass<string>.myField");
			Assert.AreEqual("myField", fre.FieldName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].SystemType);
		}
		
		[Test]
		public void CSharpFullNamespaceGenericFieldReferenceExpressionTest()
		{
			FieldReferenceExpression fre = ParseUtilCSharp.ParseExpression<FieldReferenceExpression>("Namespace.Subnamespace.SomeClass<string>.myField");
			Assert.AreEqual("myField", fre.FieldName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("Namespace.Subnamespace.SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].SystemType);
		}
		
		[Test]
		public void CSharpGlobalFullNamespaceGenericFieldReferenceExpressionTest()
		{
			FieldReferenceExpression fre = ParseUtilCSharp.ParseExpression<FieldReferenceExpression>("global::Namespace.Subnamespace.SomeClass<string>.myField");
			Assert.AreEqual("myField", fre.FieldName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.IsFalse(tr is InnerClassTypeReference);
			Assert.AreEqual("Namespace.Subnamespace.SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].SystemType);
			Assert.IsTrue(tr.IsGlobal);
		}
		
		[Test]
		public void CSharpNestedGenericFieldReferenceExpressionTest()
		{
			FieldReferenceExpression fre = ParseUtilCSharp.ParseExpression<FieldReferenceExpression>("MyType<string>.InnerClass<int>.myField");
			Assert.AreEqual("myField", fre.FieldName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			InnerClassTypeReference ic = (InnerClassTypeReference)((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("InnerClass", ic.Type);
			Assert.AreEqual(1, ic.GenericTypes.Count);
			Assert.AreEqual("System.Int32", ic.GenericTypes[0].SystemType);
			Assert.AreEqual("MyType", ic.BaseType.Type);
			Assert.AreEqual(1, ic.BaseType.GenericTypes.Count);
			Assert.AreEqual("System.String", ic.BaseType.GenericTypes[0].SystemType);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetSimpleFieldReferenceExpressionTest()
		{
			FieldReferenceExpression fre = ParseUtilVBNet.ParseExpression<FieldReferenceExpression>("myTargetObject.myField");
			Assert.AreEqual("myField", fre.FieldName);
			Assert.IsTrue(fre.TargetObject is IdentifierExpression);
			Assert.AreEqual("myTargetObject", ((IdentifierExpression)fre.TargetObject).Identifier);
		}
		
		
		[Test]
		public void VBNetGenericFieldReferenceExpressionTest()
		{
			FieldReferenceExpression fre = ParseUtilVBNet.ParseExpression<FieldReferenceExpression>("SomeClass(of string).myField");
			Assert.AreEqual("myField", fre.FieldName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].SystemType);
		}
		
		[Test]
		public void VBNetFullNamespaceGenericFieldReferenceExpressionTest()
		{
			FieldReferenceExpression fre = ParseUtilVBNet.ParseExpression<FieldReferenceExpression>("System.Subnamespace.SomeClass(of string).myField");
			Assert.AreEqual("myField", fre.FieldName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("System.Subnamespace.SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].SystemType);
		}
		
		[Test]
		public void VBNetGlobalFullNamespaceGenericFieldReferenceExpressionTest()
		{
			FieldReferenceExpression fre = ParseUtilVBNet.ParseExpression<FieldReferenceExpression>("Global.System.Subnamespace.SomeClass(of string).myField");
			Assert.AreEqual("myField", fre.FieldName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.IsFalse(tr is InnerClassTypeReference);
			Assert.AreEqual("System.Subnamespace.SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].SystemType);
			Assert.IsTrue(tr.IsGlobal);
		}
		
		[Test]
		public void VBNetNestedGenericFieldReferenceExpressionTest()
		{
			FieldReferenceExpression fre = ParseUtilVBNet.ParseExpression<FieldReferenceExpression>("MyType(of string).InnerClass(of integer).myField");
			Assert.AreEqual("myField", fre.FieldName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			InnerClassTypeReference ic = (InnerClassTypeReference)((TypeReferenceExpression)fre.TargetObject).TypeReference;
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
