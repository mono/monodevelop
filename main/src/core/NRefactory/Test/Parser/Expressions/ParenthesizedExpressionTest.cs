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
	public class ParenthesizedExpressionTests
	{
		#region C#
		[Test]
		public void CSharpPrimitiveParenthesizedExpression()
		{
			ParenthesizedExpression p = ParseUtilCSharp.ParseExpression<ParenthesizedExpression>("((1))");
			Assert.IsTrue(p.Expression is ParenthesizedExpression);
			p = p.Expression as ParenthesizedExpression;;
			Assert.IsTrue(p.Expression is PrimitiveExpression);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetPrimitiveParenthesizedExpression()
		{
			ParenthesizedExpression p = ParseUtilVBNet.ParseExpression<ParenthesizedExpression>("((1))");
			Assert.IsTrue(p.Expression is ParenthesizedExpression);
			p = p.Expression as ParenthesizedExpression;;
			Assert.IsTrue(p.Expression is PrimitiveExpression);
		}
		#endregion
		
	}
}
