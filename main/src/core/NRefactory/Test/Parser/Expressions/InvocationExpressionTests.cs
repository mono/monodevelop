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
	public class InvocationExpressionTests
	{
		void CheckSimpleInvoke(InvocationExpression ie)
		{
			Assert.AreEqual(0, ie.Arguments.Count);
			Assert.IsTrue(ie.TargetObject is IdentifierExpression);
			Assert.AreEqual("myMethod", ((IdentifierExpression)ie.TargetObject).Identifier);
		}
		
		void CheckGenericInvoke(InvocationExpression expr)
		{
			Assert.AreEqual(1, expr.Arguments.Count);
			Assert.IsTrue(expr.TargetObject is IdentifierExpression);
			Assert.AreEqual("myMethod", ((IdentifierExpression)expr.TargetObject).Identifier);
			Assert.AreEqual(1, expr.TypeArguments.Count);
			Assert.AreEqual("System.Char", expr.TypeArguments[0].SystemType);
		}
		
		
		#region C#
		[Test]
		public void CSharpSimpleInvocationExpressionTest()
		{
			CheckSimpleInvoke(ParseUtilCSharp.ParseExpression<InvocationExpression>("myMethod()"));
		}
		
		[Test]
		public void CSharpGenericInvocationExpressionTest()
		{
			CheckGenericInvoke(ParseUtilCSharp.ParseExpression<InvocationExpression>("myMethod<char>('a')"));
		}
		
		[Test]
		public void CSharpInvalidNestedInvocationExpressionTest()
		{
			// this test was written because this bug caused the AbstractASTVisitor to crash
			
			InvocationExpression expr = ParseUtilCSharp.ParseExpression<InvocationExpression>("WriteLine(myMethod(,))", true);
			Assert.IsTrue(expr.TargetObject is IdentifierExpression);
			Assert.AreEqual("WriteLine", ((IdentifierExpression)expr.TargetObject).Identifier);
			
			Assert.AreEqual(1, expr.Arguments.Count); // here a second null parameter was added incorrectly
			
			Assert.IsTrue(expr.Arguments[0] is InvocationExpression);
			CheckSimpleInvoke((InvocationExpression)expr.Arguments[0]);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetSimpleInvocationExpressionTest()
		{
			CheckSimpleInvoke(ParseUtilVBNet.ParseExpression<InvocationExpression>("myMethod()"));
		}
		
		[Test]
		public void VBNetGenericInvocationExpressionTest()
		{
			CheckGenericInvoke(ParseUtilVBNet.ParseExpression<InvocationExpression>("myMethod(Of Char)(\"a\"c)"));
		}
		
		[Test]
		public void PrimitiveExpression1Test()
		{
			InvocationExpression ie = ParseUtilVBNet.ParseExpression<InvocationExpression>("546.ToString()");
			Assert.AreEqual(0, ie.Arguments.Count);
		}
		
		#endregion
	}
}
