// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 3660 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class MemberReferenceExpressionTests
	{
		#region C#
		[Test]
		public void CSharpSimpleFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("myTargetObject.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is IdentifierExpression);
			Assert.AreEqual("myTargetObject", ((IdentifierExpression)fre.TargetObject).Identifier);
		}
		
		[Test]
		public void CSharpGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("SomeClass<string>.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpFullNamespaceGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("Namespace.Subnamespace.SomeClass<string>.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("Namespace.Subnamespace.SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpGlobalFullNamespaceGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("global::Namespace.Subnamespace.SomeClass<string>.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.IsFalse(tr is InnerClassTypeReference);
			Assert.AreEqual("Namespace.Subnamespace.SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].Type);
			Assert.IsTrue(tr.IsGlobal);
		}
		
		[Test]
		public void CSharpNestedGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("MyType<string>.InnerClass<int>.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			InnerClassTypeReference ic = (InnerClassTypeReference)((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("InnerClass", ic.Type);
			Assert.AreEqual(1, ic.GenericTypes.Count);
			Assert.AreEqual("System.Int32", ic.GenericTypes[0].Type);
			Assert.AreEqual("MyType", ic.BaseType.Type);
			Assert.AreEqual(1, ic.BaseType.GenericTypes.Count);
			Assert.AreEqual("System.String", ic.BaseType.GenericTypes[0].Type);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetSimpleFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilVBNet.ParseExpression<MemberReferenceExpression>("myTargetObject.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is IdentifierExpression);
			Assert.AreEqual("myTargetObject", ((IdentifierExpression)fre.TargetObject).Identifier);
		}
		
		[Test]
		public void VBNetFieldReferenceExpressionWithoutTargetTest()
		{
			MemberReferenceExpression fre = ParseUtilVBNet.ParseExpression<MemberReferenceExpression>(".myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject.IsNull);
		}
		
		[Test]
		public void VBNetGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilVBNet.ParseExpression<MemberReferenceExpression>("SomeClass(of string).myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsInstanceOfType(typeof(IdentifierExpression), fre.TargetObject);
			TypeReference tr = ((IdentifierExpression)fre.TargetObject).TypeArguments[0];
			Assert.AreEqual("System.String", tr.Type);
		}
		
		[Test]
		public void VBNetFullNamespaceGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilVBNet.ParseExpression<MemberReferenceExpression>("System.Subnamespace.SomeClass(of string).myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsInstanceOfType(typeof(MemberReferenceExpression), fre.TargetObject);
			
			MemberReferenceExpression inner = (MemberReferenceExpression)fre.TargetObject;
			Assert.AreEqual("SomeClass", inner.MemberName);
			Assert.AreEqual(1, inner.TypeArguments.Count);
			Assert.AreEqual("System.String", inner.TypeArguments[0].Type);
		}
		
		[Test]
		public void VBNetGlobalFullNamespaceGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilVBNet.ParseExpression<MemberReferenceExpression>("Global.System.Subnamespace.SomeClass(of string).myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsInstanceOfType(typeof(MemberReferenceExpression), fre.TargetObject);
			MemberReferenceExpression inner = (MemberReferenceExpression)fre.TargetObject;
			
			Assert.AreEqual("SomeClass", inner.MemberName);
			Assert.AreEqual(1, inner.TypeArguments.Count);
			Assert.AreEqual("System.String", inner.TypeArguments[0].Type);
		}
		
		[Test]
		public void VBNetNestedGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilVBNet.ParseExpression<MemberReferenceExpression>("MyType(of string).InnerClass(of integer).myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsInstanceOfType(typeof(MemberReferenceExpression), fre.TargetObject);
			
			MemberReferenceExpression inner = (MemberReferenceExpression)fre.TargetObject;
			Assert.AreEqual("InnerClass", inner.MemberName);
		}
		
		#endregion
	}
}
