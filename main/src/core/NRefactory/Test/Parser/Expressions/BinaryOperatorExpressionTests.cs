// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1609 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class BinaryOperatorExpressionTests
	{
		void OperatorPrecedenceTest(string strongOperator, BinaryOperatorType strongOperatorType,
		                            string weakOperator, BinaryOperatorType weakOperatorType, bool vb)
		{
			string program = "a " + weakOperator + " b " + strongOperator + " c";
			BinaryOperatorExpression boe;
			if (vb)
				boe = ParseUtilVBNet.ParseExpression<BinaryOperatorExpression>(program);
			else
				boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(weakOperatorType, boe.Op);
			Assert.IsTrue(boe.Left is IdentifierExpression);
			boe = (BinaryOperatorExpression)boe.Right;
			Assert.AreEqual(strongOperatorType, boe.Op);
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			
			program = "a " + strongOperator + " b " + weakOperator + " c";
			if (vb)
				boe = ParseUtilVBNet.ParseExpression<BinaryOperatorExpression>(program);
			else
				boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(weakOperatorType, boe.Op);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			boe = (BinaryOperatorExpression)boe.Left;
			Assert.AreEqual(strongOperatorType, boe.Op);
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
		}
		
		void SameOperatorPrecedenceTest(string firstOperator, BinaryOperatorType firstOperatorType,
		                                string secondOperator, BinaryOperatorType secondOperatorType, bool vb)
		{
			string program = "a " + secondOperator + " b " + firstOperator + " c";
			BinaryOperatorExpression boe;
			if (vb)
				boe = ParseUtilVBNet.ParseExpression<BinaryOperatorExpression>(program);
			else
				boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(firstOperatorType, boe.Op);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			boe = (BinaryOperatorExpression)boe.Left;
			Assert.AreEqual(secondOperatorType, boe.Op);
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			
			program = "a " + firstOperator + " b " + secondOperator + " c";
			if (vb)
				boe = ParseUtilVBNet.ParseExpression<BinaryOperatorExpression>(program);
			else
				boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(secondOperatorType, boe.Op);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			boe = (BinaryOperatorExpression)boe.Left;
			Assert.AreEqual(firstOperatorType, boe.Op);
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
		}
		
		#region C#
		void CSharpTestBinaryOperatorExpressionTest(string program, BinaryOperatorType op)
		{
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(op, boe.Op);
			
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			
		}
		
		[Test]
		public void CSharpOperatorPrecedenceTest()
		{
			SameOperatorPrecedenceTest("*", BinaryOperatorType.Multiply, "/", BinaryOperatorType.Divide, false);
			SameOperatorPrecedenceTest("*", BinaryOperatorType.Multiply, "%", BinaryOperatorType.Modulus, false);
			OperatorPrecedenceTest("*", BinaryOperatorType.Multiply, "+", BinaryOperatorType.Add, false);
			SameOperatorPrecedenceTest("-", BinaryOperatorType.Subtract, "+", BinaryOperatorType.Add, false);
			OperatorPrecedenceTest("+", BinaryOperatorType.Add, "<<", BinaryOperatorType.ShiftLeft, false);
			SameOperatorPrecedenceTest(">>", BinaryOperatorType.ShiftRight, "<<", BinaryOperatorType.ShiftLeft, false);
			OperatorPrecedenceTest("<<", BinaryOperatorType.ShiftLeft, "==", BinaryOperatorType.Equality, false);
			SameOperatorPrecedenceTest("!=", BinaryOperatorType.InEquality, "==", BinaryOperatorType.Equality, false);
			OperatorPrecedenceTest("==", BinaryOperatorType.Equality, "&", BinaryOperatorType.BitwiseAnd, false);
			OperatorPrecedenceTest("&", BinaryOperatorType.BitwiseAnd, "^", BinaryOperatorType.ExclusiveOr, false);
			OperatorPrecedenceTest("^", BinaryOperatorType.ExclusiveOr, "|", BinaryOperatorType.BitwiseOr, false);
			OperatorPrecedenceTest("|", BinaryOperatorType.BitwiseOr, "&&", BinaryOperatorType.LogicalAnd, false);
			OperatorPrecedenceTest("&&", BinaryOperatorType.LogicalAnd, "||", BinaryOperatorType.LogicalOr, false);
			OperatorPrecedenceTest("||", BinaryOperatorType.LogicalOr, "??", BinaryOperatorType.NullCoalescing, false);
		}
		
		[Test]
		public void CSharpSubtractionLeftToRight()
		{
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("a - b - c");
			Assert.IsTrue(boe.Right is IdentifierExpression);
			Assert.IsTrue(boe.Left is BinaryOperatorExpression);
		}
		
		[Test]
		public void CSharpNullCoalescingRightToLeft()
		{
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("a ?? b ?? c");
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is BinaryOperatorExpression);
		}
		
		[Test]
		public void CSharpBitwiseAndTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a & b", BinaryOperatorType.BitwiseAnd);
		}
		
		[Test]
		public void CSharpBitwiseOrTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a | b", BinaryOperatorType.BitwiseOr);
		}
		
		[Test]
		public void CSharpLogicalAndTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a && b", BinaryOperatorType.LogicalAnd);
		}
		
		[Test]
		public void CSharpLogicalOrTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a || b", BinaryOperatorType.LogicalOr);
		}
		
		[Test]
		public void CSharpExclusiveOrTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a ^ b", BinaryOperatorType.ExclusiveOr);
		}
		
		
		[Test]
		public void CSharpGreaterThanTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a > b", BinaryOperatorType.GreaterThan);
		}
		
		[Test]
		public void CSharpGreaterThanOrEqualTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a >= b", BinaryOperatorType.GreaterThanOrEqual);
		}
		
		[Test]
		public void CSharpEqualityTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a == b", BinaryOperatorType.Equality);
		}
		
		[Test]
		public void CSharpInEqualityTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a != b", BinaryOperatorType.InEquality);
		}
		
		[Test]
		public void CSharpLessThanTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a < b", BinaryOperatorType.LessThan);
		}
		
		[Test]
		public void CSharpLessThanOrEqualTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a <= b", BinaryOperatorType.LessThanOrEqual);
		}
		
		[Test]
		public void CSharpAddTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a + b", BinaryOperatorType.Add);
		}
		
		[Test]
		public void CSharpSubtractTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a - b", BinaryOperatorType.Subtract);
		}
		
		[Test]
		public void CSharpMultiplyTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a * b", BinaryOperatorType.Multiply);
		}
		
		[Test]
		public void CSharpDivideTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a / b", BinaryOperatorType.Divide);
		}
		
		[Test]
		public void CSharpModulusTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a % b", BinaryOperatorType.Modulus);
		}
		
		[Test]
		public void CSharpShiftLeftTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a << b", BinaryOperatorType.ShiftLeft);
		}
		
		[Test]
		public void CSharpShiftRightTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a >> b", BinaryOperatorType.ShiftRight);
		}
		
		[Test]
		public void CSharpNullCoalescingTest()
		{
			CSharpTestBinaryOperatorExpressionTest("a ?? b", BinaryOperatorType.NullCoalescing);
		}
		
		[Test]
		public void CSharpLessThanOrGreaterTest()
		{
			const string expr = "i1 < 0 || i1 > (Count - 1)";
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(expr);
			Assert.AreEqual(BinaryOperatorType.LogicalOr, boe.Op);
		}
		#endregion
		
		#region VB.NET
		void VBNetTestBinaryOperatorExpressionTest(string program, BinaryOperatorType op)
		{
			BinaryOperatorExpression boe = ParseUtilVBNet.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(op, boe.Op);
			
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			
		}
		
		[Test]
		public void VBOperatorPrecedenceTest()
		{
			OperatorPrecedenceTest("^", BinaryOperatorType.Power, "*", BinaryOperatorType.Multiply, true);
			SameOperatorPrecedenceTest("*", BinaryOperatorType.Multiply, "/", BinaryOperatorType.Divide, true);
			OperatorPrecedenceTest("/", BinaryOperatorType.Divide, "\\", BinaryOperatorType.DivideInteger, true);
			OperatorPrecedenceTest("\\", BinaryOperatorType.DivideInteger, "Mod", BinaryOperatorType.Modulus, true);
			OperatorPrecedenceTest("Mod", BinaryOperatorType.Modulus, "+", BinaryOperatorType.Add, true);
			SameOperatorPrecedenceTest("+", BinaryOperatorType.Add, "-", BinaryOperatorType.Subtract, true);
			OperatorPrecedenceTest("-", BinaryOperatorType.Subtract, "&", BinaryOperatorType.Concat, true);
			OperatorPrecedenceTest("&", BinaryOperatorType.Concat, "<<", BinaryOperatorType.ShiftLeft, true);
			SameOperatorPrecedenceTest("<<", BinaryOperatorType.ShiftLeft, ">>", BinaryOperatorType.ShiftRight, true);
			OperatorPrecedenceTest("<<", BinaryOperatorType.ShiftLeft, "=", BinaryOperatorType.Equality, true);
			SameOperatorPrecedenceTest("<>", BinaryOperatorType.InEquality, "=", BinaryOperatorType.Equality, true);
			SameOperatorPrecedenceTest("<", BinaryOperatorType.LessThan, "=", BinaryOperatorType.Equality, true);
			SameOperatorPrecedenceTest("<=", BinaryOperatorType.LessThanOrEqual, "=", BinaryOperatorType.Equality, true);
			SameOperatorPrecedenceTest(">", BinaryOperatorType.GreaterThan, "=", BinaryOperatorType.Equality, true);
			SameOperatorPrecedenceTest(">=", BinaryOperatorType.GreaterThanOrEqual, "=", BinaryOperatorType.Equality, true);
			SameOperatorPrecedenceTest("Like", BinaryOperatorType.Like, "=", BinaryOperatorType.Equality, true);
			SameOperatorPrecedenceTest("Is", BinaryOperatorType.ReferenceEquality, "=", BinaryOperatorType.Equality, true);
			SameOperatorPrecedenceTest("IsNot", BinaryOperatorType.ReferenceInequality, "=", BinaryOperatorType.Equality, true);
			OperatorPrecedenceTest("=", BinaryOperatorType.Equality, "And", BinaryOperatorType.BitwiseAnd, true);
			SameOperatorPrecedenceTest("And", BinaryOperatorType.BitwiseAnd, "AndAlso", BinaryOperatorType.LogicalAnd, true);
			OperatorPrecedenceTest("And", BinaryOperatorType.BitwiseAnd, "Or", BinaryOperatorType.BitwiseOr, true);
			SameOperatorPrecedenceTest("Or", BinaryOperatorType.BitwiseOr, "OrElse", BinaryOperatorType.LogicalOr, true);
			SameOperatorPrecedenceTest("Or", BinaryOperatorType.BitwiseOr, "Xor", BinaryOperatorType.ExclusiveOr, true);
		}
		
		[Test]
		public void VBNetTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a ^ b", BinaryOperatorType.Power);
		}
		
		[Test]
		public void VBNetPowerTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a ^ b", BinaryOperatorType.Power);
		}
		
		[Test]
		public void VBNetConcatTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a & b", BinaryOperatorType.Concat);
		}
		
		[Test]
		public void VBNetLogicalAndTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a AndAlso b", BinaryOperatorType.LogicalAnd);
		}
		[Test]
		public void VBNetLogicalAndNotLazyTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a And b", BinaryOperatorType.BitwiseAnd);
		}
		
		[Test]
		public void VBNetLogicalOrTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a OrElse b", BinaryOperatorType.LogicalOr);
		}
		[Test]
		public void VBNetLogicalOrNotLazyTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a Or b", BinaryOperatorType.BitwiseOr);
		}
		
		[Test]
		public void VBNetExclusiveOrTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a Xor b", BinaryOperatorType.ExclusiveOr);
		}
		
		
		[Test]
		public void VBNetGreaterThanTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a > b", BinaryOperatorType.GreaterThan);
		}
		
		[Test]
		public void VBNetGreaterThanOrEqualTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a >= b", BinaryOperatorType.GreaterThanOrEqual);
		}
		
		[Test]
		public void VBNetEqualityTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a = b", BinaryOperatorType.Equality);
		}
		
		[Test]
		public void VBNetInEqualityTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a <> b", BinaryOperatorType.InEquality);
		}
		
		[Test]
		public void VBNetLessThanTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a < b", BinaryOperatorType.LessThan);
		}
		
		[Test]
		public void VBNetLessThanOrEqualTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a <= b", BinaryOperatorType.LessThanOrEqual);
		}
		
		[Test]
		public void VBNetAddTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a + b", BinaryOperatorType.Add);
		}
		
		[Test]
		public void VBNetSubtractTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a - b", BinaryOperatorType.Subtract);
		}
		
		[Test]
		public void VBNetMultiplyTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a * b", BinaryOperatorType.Multiply);
		}
		
		[Test]
		public void VBNetDivideTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a / b", BinaryOperatorType.Divide);
		}
		
		[Test]
		public void VBNetDivideIntegerTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a \\ b", BinaryOperatorType.DivideInteger);
		}
		
		[Test]
		public void VBNetModulusTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a Mod b", BinaryOperatorType.Modulus);
		}
		
		[Test]
		public void VBNetShiftLeftTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a << b", BinaryOperatorType.ShiftLeft);
		}
		
		[Test]
		public void VBNetShiftRightTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a >> b", BinaryOperatorType.ShiftRight);
		}
		
		[Test]
		public void VBNetISTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a is b", BinaryOperatorType.ReferenceEquality);
		}
		
		[Test]
		public void VBNetISNotTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a IsNot b", BinaryOperatorType.ReferenceInequality);
		}
		
		[Test]
		public void VBNetLikeTest()
		{
			VBNetTestBinaryOperatorExpressionTest("a Like b", BinaryOperatorType.Like);
		}
		#endregion
		
		
		#region AddIntegerTests
		string AddIntegerToBoe(string input, int number)
		{
			return AddInteger<BinaryOperatorExpression>(input, number);
		}
		
		string AddInteger<T>(string input, int number) where T : Expression
		{
			Expression e = ParseUtilCSharp.ParseExpression<T>(input);
			e = Expression.AddInteger(e, number);
			CSharpOutputVisitor v = new CSharpOutputVisitor();
			e.AcceptVisitor(v, null);
			return v.Text;
		}
		
		[Test]
		public void AddInteger()
		{
			Assert.AreEqual("a + 2", AddIntegerToBoe("a + 1", 1));
			Assert.AreEqual("a + 2", AddIntegerToBoe("a + 3", -1));
			Assert.AreEqual("a + b + c + 2", AddIntegerToBoe("a + b + c + 1", 1));
			Assert.AreEqual("a", AddIntegerToBoe("a + 1", -1));
			Assert.AreEqual("2", AddInteger<PrimitiveExpression>("1", 1));
			Assert.AreEqual("-1", AddInteger<PrimitiveExpression>("1", -2));
			Assert.AreEqual("0", AddInteger<PrimitiveExpression>("1", -1));
			Assert.AreEqual("a + 1", AddInteger<IdentifierExpression>("a", 1));
		}
		
		[Test]
		public void AddIntegerWithNegativeResult()
		{
			Assert.AreEqual("a - 1", AddIntegerToBoe("a + 1", -2));
			Assert.AreEqual("a - 2", AddIntegerToBoe("a - 1", -1));
			Assert.AreEqual("a + b + c - 2", AddIntegerToBoe("a + b + c + 2", -4));
			Assert.AreEqual("a + b + c - 6", AddIntegerToBoe("a + b + c - 2", -4));
			Assert.AreEqual("a + b + c", AddIntegerToBoe("a + b + c + 2", -2));
			Assert.AreEqual("a", AddIntegerToBoe("a - 1", 1));
			Assert.AreEqual("a + 1", AddIntegerToBoe("a - 2", 3));
			Assert.AreEqual("a - 1", AddInteger<IdentifierExpression>("a", -1));
		}
		#endregion
	}
}
