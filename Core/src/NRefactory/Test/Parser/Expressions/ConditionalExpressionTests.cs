// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1167 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class ConditionalExpressionTests
	{
		#region C#
		[Test]
		public void CSharpConditionalExpressionTest()
		{
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a == b ? a() : a.B");
			
			Assert.IsTrue(ce.Condition is BinaryOperatorExpression);
			Assert.IsTrue(ce.TrueExpression is InvocationExpression);
			Assert.IsTrue(ce.FalseExpression is FieldReferenceExpression);
		}
		
		[Test]
		public void CSharpConditionalIsExpressionTest()
		{
			// (as is b?) ERROR (conflict with nullables, SD2-419)
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a is b ? a() : a.B");
			
			Assert.IsTrue(ce.Condition is TypeOfIsExpression);
			Assert.IsTrue(ce.TrueExpression is InvocationExpression);
			Assert.IsTrue(ce.FalseExpression is FieldReferenceExpression);
		}
		
		[Test]
		public void CSharpConditionalIsExpressionTest2()
		{
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a is b ? (a()) : a.B");
			
			Assert.IsTrue(ce.Condition is TypeOfIsExpression);
			Assert.IsTrue(ce.TrueExpression is ParenthesizedExpression);
			Assert.IsTrue(ce.FalseExpression is FieldReferenceExpression);
		}
		#endregion
		
		#region VB.NET
		// No VB.NET representation
		#endregion
	}
}
