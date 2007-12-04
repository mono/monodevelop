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
	public class UnaryOperatorExpressionTests
	{
		#region C#
		void CSharpTestUnaryOperatorExpressionTest(string program, UnaryOperatorType op)
		{
			UnaryOperatorExpression uoe = ParseUtilCSharp.ParseExpression<UnaryOperatorExpression>(program);
			Assert.AreEqual(op, uoe.Op);
			
			Assert.IsTrue(uoe.Expression is IdentifierExpression);
		}
		
		[Test]
		public void CSharpNotTest()
		{
			CSharpTestUnaryOperatorExpressionTest("!a", UnaryOperatorType.Not);
		}
		
		[Test]
		public void CSharpBitNotTest()
		{
			CSharpTestUnaryOperatorExpressionTest("~a", UnaryOperatorType.BitNot);
		}
		
		[Test]
		public void CSharpMinusTest()
		{
			CSharpTestUnaryOperatorExpressionTest("-a", UnaryOperatorType.Minus);
		}
		
		[Test]
		public void CSharpPlusTest()
		{
			CSharpTestUnaryOperatorExpressionTest("+a", UnaryOperatorType.Plus);
		}
		
		[Test]
		public void CSharpIncrementTest()
		{
			CSharpTestUnaryOperatorExpressionTest("++a", UnaryOperatorType.Increment);
		}
		
		[Test]
		public void CSharpDecrementTest()
		{
			CSharpTestUnaryOperatorExpressionTest("--a", UnaryOperatorType.Decrement);
		}
		
		[Test]
		public void CSharpPostIncrementTest()
		{
			CSharpTestUnaryOperatorExpressionTest("a++", UnaryOperatorType.PostIncrement);
		}
		
		[Test]
		public void CSharpPostDecrementTest()
		{
			CSharpTestUnaryOperatorExpressionTest("a--", UnaryOperatorType.PostDecrement);
		}
		
		[Test]
		public void CSharpStarTest()
		{
			CSharpTestUnaryOperatorExpressionTest("*a", UnaryOperatorType.Star);
		}
		
		[Test]
		public void CSharpBitWiseAndTest()
		{
			CSharpTestUnaryOperatorExpressionTest("&a", UnaryOperatorType.BitWiseAnd);
		}
		#endregion
		
		#region VB.NET
		void VBNetTestUnaryOperatorExpressionTest(string program, UnaryOperatorType op)
		{
			UnaryOperatorExpression uoe = ParseUtilVBNet.ParseExpression<UnaryOperatorExpression>(program);
			Assert.AreEqual(op, uoe.Op);
			
			Assert.IsTrue(uoe.Expression is IdentifierExpression);
		}
		
		[Test]
		public void VBNetNotTest()
		{
			VBNetTestUnaryOperatorExpressionTest("Not a", UnaryOperatorType.Not);
		}
		
		[Test]
		public void VBNetPlusTest()
		{
			VBNetTestUnaryOperatorExpressionTest("+a", UnaryOperatorType.Plus);
		}
		
		[Test]
		public void VBNetMinusTest()
		{
			VBNetTestUnaryOperatorExpressionTest("-a", UnaryOperatorType.Minus);
		}
		#endregion
	}
}
