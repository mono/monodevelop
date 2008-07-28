// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 3120 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
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
			CSharpTestUnaryOperatorExpressionTest("*a", UnaryOperatorType.Dereference);
		}
		
		[Test]
		public void CSharpBitWiseAndTest()
		{
			CSharpTestUnaryOperatorExpressionTest("&a", UnaryOperatorType.AddressOf);
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
		public void VBNetInEqualsNotTest()
		{
			BinaryOperatorExpression e = ParseUtilVBNet.ParseExpression<BinaryOperatorExpression>("b <> Not a");
			Assert.AreEqual(BinaryOperatorType.InEquality, e.Op);
			UnaryOperatorExpression ue = (UnaryOperatorExpression)e.Right;
			Assert.AreEqual(UnaryOperatorType.Not, ue.Op);
		}
		
		[Test]
		public void VBNetNotEqualTest()
		{
			UnaryOperatorExpression e = ParseUtilVBNet.ParseExpression<UnaryOperatorExpression>("Not a = b");
			Assert.AreEqual(UnaryOperatorType.Not, e.Op);
			BinaryOperatorExpression boe = (BinaryOperatorExpression)e.Expression;
			Assert.AreEqual(BinaryOperatorType.Equality, boe.Op);
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
