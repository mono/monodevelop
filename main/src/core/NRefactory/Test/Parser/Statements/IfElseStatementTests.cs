// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 2604 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class IfElseStatementTests
	{
		#region C#
		[Test]
		public void CSharpSimpleIfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilCSharp.ParseStatement<IfElseStatement>("if (true) { }");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 0, "false count != 0:" + ifElseStatement.FalseStatement.Count);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is BlockStatement);
		}
		
		[Test]
		public void CSharpSimpleIfElseStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilCSharp.ParseStatement<IfElseStatement>("if (true) { } else { }");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 1, "false count != 1:" + ifElseStatement.FalseStatement.Count);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is BlockStatement, "Statement was: " + ifElseStatement.TrueStatement[0]);
			Assert.IsTrue(ifElseStatement.FalseStatement[0] is BlockStatement, "Statement was: " + ifElseStatement.FalseStatement[0]);
		}
		
		
		[Test]
		public void CSharpIfElseIfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilCSharp.ParseStatement<IfElseStatement>("if (1) { } else if (2) { } else if (3) { } else { }");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.ElseIfSections.Count == 2, "elseif section count != 2:" + ifElseStatement.ElseIfSections.Count);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 1, "false count != 1:" + ifElseStatement.FalseStatement.Count);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is BlockStatement, "Statement was: " + ifElseStatement.TrueStatement[0]);
			Assert.IsTrue(ifElseStatement.FalseStatement[0] is BlockStatement, "Statement was: " + ifElseStatement.FalseStatement[0]);
			Assert.IsTrue(ifElseStatement.ElseIfSections[0].EmbeddedStatement is BlockStatement, "Statement was: " + ifElseStatement.ElseIfSections[0].EmbeddedStatement);
			Assert.IsTrue(ifElseStatement.ElseIfSections[1].EmbeddedStatement is BlockStatement, "Statement was: " + ifElseStatement.ElseIfSections[1].EmbeddedStatement);
			Assert.AreEqual(2, (ifElseStatement.ElseIfSections[0].Condition as PrimitiveExpression).Value);
			Assert.AreEqual(3, (ifElseStatement.ElseIfSections[1].Condition as PrimitiveExpression).Value);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetSimpleIfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilVBNet.ParseStatement<IfElseStatement>("If True THEN END");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 0, "false count != 0:" + ifElseStatement.FalseStatement.Count);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is EndStatement, "Statement was: " + ifElseStatement.TrueStatement[0]);
		}
		[Test]
		public void VBNetSimpleIfStatementTest2()
		{
			IfElseStatement ifElseStatement = ParseUtilVBNet.ParseStatement<IfElseStatement>("If True THEN\n END\n END IF");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 0, "false count != 0:" + ifElseStatement.FalseStatement.Count);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is BlockStatement, "Statement was: " + ifElseStatement.TrueStatement[0]);
		}
		
		// test for SD2-1201
		[Test]
		public void VBNetIfStatementLocationTest()
		{
			IfElseStatement ifElseStatement = ParseUtilVBNet.ParseStatement<IfElseStatement>("If True THEN\n" +
			                                                                                 "DoIt()\n" +
			                                                                                 "ElseIf False Then\n" +
			                                                                                 "DoIt()\n" +
			                                                                                 "End If");
			Assert.AreEqual(3, (ifElseStatement.StartLocation).Y);
			Assert.AreEqual(7, (ifElseStatement.EndLocation).Y);
			Assert.AreEqual(5, (ifElseStatement.ElseIfSections[0].StartLocation).Y);
			Assert.AreEqual(6, (ifElseStatement.ElseIfSections[0].EndLocation).Y);
			Assert.IsNotNull(ifElseStatement.ElseIfSections[0].Parent);
			
		}
		
		[Test]
		public void VBNetElseIfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilVBNet.ParseStatement<IfElseStatement>("If True THEN\n" +
			                                                                                 "END\n" +
			                                                                                 "ElseIf False Then\n" +
			                                                                                 "Stop\n" +
			                                                                                 "End If");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 0, "false count != 0:" + ifElseStatement.FalseStatement.Count);
			Assert.IsFalse((bool)(ifElseStatement.ElseIfSections[0].Condition as PrimitiveExpression).Value);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is BlockStatement, "Statement was: " + ifElseStatement.TrueStatement[0]);
			Assert.IsTrue(ifElseStatement.ElseIfSections[0].EmbeddedStatement.Children[0] is StopStatement, "Statement was: " + ifElseStatement.ElseIfSections[0].EmbeddedStatement.Children[0]);
		}
		[Test]
		public void VBNetElse_IfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilVBNet.ParseStatement<IfElseStatement>("If True THEN\n" +
			                                                                                 "END\n" +
			                                                                                 "Else If False Then\n" +
			                                                                                 "Stop\n" +
			                                                                                 "End If");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.IsTrue(ifElseStatement.TrueStatement.Count == 1, "true count != 1:" + ifElseStatement.TrueStatement.Count);
			Assert.IsTrue(ifElseStatement.FalseStatement.Count == 0, "false count != 0:" + ifElseStatement.FalseStatement.Count);
			Assert.IsFalse((bool)(ifElseStatement.ElseIfSections[0].Condition as PrimitiveExpression).Value);
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is BlockStatement, "Statement was: " + ifElseStatement.TrueStatement[0]);
			Assert.IsTrue(ifElseStatement.ElseIfSections[0].EmbeddedStatement.Children[0] is StopStatement, "Statement was: " + ifElseStatement.ElseIfSections[0].EmbeddedStatement.Children[0]);
		}
		[Test]
		public void VBNetMultiStatementIfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilVBNet.ParseStatement<IfElseStatement>("If True THEN Stop : b");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.AreEqual(2, ifElseStatement.TrueStatement.Count, "true count");
			Assert.AreEqual(0, ifElseStatement.FalseStatement.Count, "false count");
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is StopStatement);
			Assert.IsTrue(ifElseStatement.TrueStatement[1] is ExpressionStatement);
		}
		[Test]
		public void VBNetMultiStatementIfStatementWithEndStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilVBNet.ParseStatement<IfElseStatement>("If True THEN Stop : End : b");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.AreEqual(3, ifElseStatement.TrueStatement.Count, "true count");
			Assert.AreEqual(0, ifElseStatement.FalseStatement.Count, "false count");
			
			Assert.IsTrue(ifElseStatement.TrueStatement[0] is StopStatement);
			Assert.IsTrue(ifElseStatement.TrueStatement[1] is EndStatement);
			Assert.IsTrue(ifElseStatement.TrueStatement[2] is ExpressionStatement);
		}
		
		[Test]
		public void VBNetIfWithEmptyElseTest()
		{
			IfElseStatement ifElseStatement = ParseUtilVBNet.ParseStatement<IfElseStatement>("If True THEN a Else");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.AreEqual(1, ifElseStatement.TrueStatement.Count, "true count");
			Assert.AreEqual(0, ifElseStatement.FalseStatement.Count, "false count");
		}
		
		[Test]
		public void VBNetIfWithMultipleColons()
		{
			IfElseStatement ifElseStatement = ParseUtilVBNet.ParseStatement<IfElseStatement>("If True THEN a : : b");
			Assert.IsFalse(ifElseStatement.Condition.IsNull);
			Assert.AreEqual(2, ifElseStatement.TrueStatement.Count, "true count");
			Assert.AreEqual(0, ifElseStatement.FalseStatement.Count, "false count");
		}
		#endregion
	}
}
