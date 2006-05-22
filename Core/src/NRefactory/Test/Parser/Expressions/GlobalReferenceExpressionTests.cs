// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class GlobalReferenceExpressionTests
	{
		[Test]
		public void CSharpGlobalReferenceExpressionTest()
		{
			TypeReferenceExpression tre = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("global::System");
			Assert.IsTrue(tre.TypeReference.IsGlobal);
			Assert.AreEqual("System", tre.TypeReference.Type);
		}
		
		[Test]
		public void VBNetGlobalReferenceExpressionTest()
		{
			TypeReferenceExpression tre = ParseUtilVBNet.ParseExpression<TypeReferenceExpression>("Global.System");
			Assert.IsTrue(tre.TypeReference.IsGlobal);
			Assert.AreEqual("System", tre.TypeReference.Type);
		}
		
		[Test]
		public void CSharpGlobalTypeDeclaration()
		{
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("global::System.String a;");
			TypeReference typeRef = lvd.GetTypeForVariable(0);
			Assert.IsTrue(typeRef.IsGlobal);
			Assert.AreEqual("System.String", typeRef.Type);
		}
		
		[Test]
		public void VBNetGlobalTypeDeclaration()
		{
			LocalVariableDeclaration lvd = ParseUtilVBNet.ParseStatement<LocalVariableDeclaration>("Dim a As Global.System.String");
			TypeReference typeRef = lvd.GetTypeForVariable(0);
			Assert.IsTrue(typeRef.IsGlobal);
			Assert.AreEqual("System.String", typeRef.Type);
		}
	}
}
