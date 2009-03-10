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

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class AssignmentExpressionTests
	{
		#region C#
		void CSharpTestAssignmentExpression(string program, AssignmentOperatorType op)
		{
			AssignmentExpression ae = ParseUtilCSharp.ParseExpression<AssignmentExpression>(program);
			
			Assert.AreEqual(op, ae.Op);
			
			Assert.IsTrue(ae.Left is IdentifierExpression);
			Assert.IsTrue(ae.Right is IdentifierExpression);
		}
		
		[Test]
		public void CSharpAssignTest()
		{
			CSharpTestAssignmentExpression("a = b", AssignmentOperatorType.Assign);
		}
		
		[Test]
		public void CSharpAddTest()
		{
			CSharpTestAssignmentExpression("a += b", AssignmentOperatorType.Add);
		}
		
		[Test]
		public void CSharpSubtractTest()
		{
			CSharpTestAssignmentExpression("a -= b", AssignmentOperatorType.Subtract);
		}
		
		[Test]
		public void CSharpMultiplyTest()
		{
			CSharpTestAssignmentExpression("a *= b", AssignmentOperatorType.Multiply);
		}
		
		[Test]
		public void CSharpDivideTest()
		{
			CSharpTestAssignmentExpression("a /= b", AssignmentOperatorType.Divide);
		}
		
		[Test]
		public void CSharpModulusTest()
		{
			CSharpTestAssignmentExpression("a %= b", AssignmentOperatorType.Modulus);
		}
		
		[Test]
		public void CSharpShiftLeftTest()
		{
			CSharpTestAssignmentExpression("a <<= b", AssignmentOperatorType.ShiftLeft);
		}
		
		[Test]
		public void CSharpShiftRightTest()
		{
			CSharpTestAssignmentExpression("a >>= b", AssignmentOperatorType.ShiftRight);
		}
		
		[Test]
		public void CSharpBitwiseAndTest()
		{
			CSharpTestAssignmentExpression("a &= b", AssignmentOperatorType.BitwiseAnd);
		}
		
		[Test]
		public void CSharpBitwiseOrTest()
		{
			CSharpTestAssignmentExpression("a |= b", AssignmentOperatorType.BitwiseOr);
		}
		
		[Test]
		public void CSharpExclusiveOrTest()
		{
			CSharpTestAssignmentExpression("a ^= b", AssignmentOperatorType.ExclusiveOr);
		}
		#endregion
		
		#region VB.NET
		void VBNetTestAssignmentExpression(string program, AssignmentOperatorType op)
		{
			ExpressionStatement se = ParseUtilVBNet.ParseStatement<ExpressionStatement>(program);
			AssignmentExpression ae = se.Expression as AssignmentExpression;
			Assert.AreEqual(op, ae.Op);
			
			Assert.IsTrue(ae.Left is IdentifierExpression);
			Assert.IsTrue(ae.Right is IdentifierExpression);
		}

		[Test]
		public void VBNetAssignTest()
		{
			VBNetTestAssignmentExpression("a = b", AssignmentOperatorType.Assign);
		}
		
		[Test]
		public void VBNetAddTest()
		{
			VBNetTestAssignmentExpression("a += b", AssignmentOperatorType.Add);
		}
		
		[Test]
		public void VBNetSubtractTest()
		{
			VBNetTestAssignmentExpression("a -= b", AssignmentOperatorType.Subtract);
		}
		
		[Test]
		public void VBNetMultiplyTest()
		{
			VBNetTestAssignmentExpression("a *= b", AssignmentOperatorType.Multiply);
		}
		
		[Test]
		public void VBNetDivideTest()
		{
			VBNetTestAssignmentExpression("a /= b", AssignmentOperatorType.Divide);
		}
		
		[Test]
		public void VBNetExclusiveOrTest()
		{
			VBNetTestAssignmentExpression("a ^= b", AssignmentOperatorType.Power);
		}
		
		[Test]
		public void VBNetStringConcatTest()
		{
			VBNetTestAssignmentExpression("a &= b", AssignmentOperatorType.ConcatString);
		}

		[Test]
		public void VBNetModulusTest()
		{
			VBNetTestAssignmentExpression("a \\= b", AssignmentOperatorType.DivideInteger);
		}
		#endregion
	}
}
