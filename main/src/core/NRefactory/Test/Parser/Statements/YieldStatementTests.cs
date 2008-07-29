// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 975 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class YieldStatementTests
	{
		[Test]
		public void YieldReturnStatementTest()
		{
			YieldStatement yieldStmt = ParseUtilCSharp.ParseStatement<YieldStatement>("yield return \"Foo\";");
			Assert.IsTrue(yieldStmt.IsYieldReturn);
			ReturnStatement retStmt = (ReturnStatement)yieldStmt.Statement;
			PrimitiveExpression expr =  (PrimitiveExpression)retStmt.Expression;
			Assert.AreEqual("Foo", expr.Value);
		}
		
		[Test]
		public void YieldBreakStatementTest()
		{
			YieldStatement yieldStmt = ParseUtilCSharp.ParseStatement<YieldStatement>("yield break;");
			Assert.IsTrue(yieldStmt.IsYieldBreak);
		}
		
		[Test]
		public void YieldAsVariableTest()
		{
			StatementExpression se = ParseUtilCSharp.ParseStatement<StatementExpression>("yield = 3;");
			AssignmentExpression ae = se.Expression as AssignmentExpression;
			
			Assert.AreEqual(AssignmentOperatorType.Assign, ae.Op);
			
			Assert.IsTrue(ae.Left is IdentifierExpression);
			Assert.AreEqual("yield", ((IdentifierExpression)ae.Left).Identifier);
			Assert.IsTrue(ae.Right is PrimitiveExpression);
		}
	}
}
