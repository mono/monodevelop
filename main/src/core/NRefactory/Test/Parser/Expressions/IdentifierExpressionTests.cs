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
	public class IdentifierExpressionTests
	{
		#region C#
		[Test]
		public void CSharpIdentifierExpressionTest1()
		{
			IdentifierExpression ident = ParseUtilCSharp.ParseExpression<IdentifierExpression>("MyIdentifier");
			Assert.AreEqual("MyIdentifier", ident.Identifier);
		}
		
		[Test]
		public void CSharpIdentifierExpressionTest2()
		{
			IdentifierExpression ident = ParseUtilCSharp.ParseExpression<IdentifierExpression>("@public");
			Assert.AreEqual("public", ident.Identifier);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetIdentifierExpressionTest1()
		{
			IdentifierExpression ie = ParseUtilVBNet.ParseExpression<IdentifierExpression>("MyIdentifier");
			Assert.AreEqual("MyIdentifier", ie.Identifier);
		}
		
		[Test]
		public void VBNetIdentifierExpressionTest2()
		{
			IdentifierExpression ie = ParseUtilVBNet.ParseExpression<IdentifierExpression>("[Public]");
			Assert.AreEqual("Public", ie.Identifier);
		}
		
		[Test]
		public void VBNetAssemblyIdentifierExpressionTest()
		{
			IdentifierExpression ie = ParseUtilVBNet.ParseExpression<IdentifierExpression>("Assembly");
			Assert.AreEqual("Assembly", ie.Identifier);
		}
		#endregion
		
	}
}
