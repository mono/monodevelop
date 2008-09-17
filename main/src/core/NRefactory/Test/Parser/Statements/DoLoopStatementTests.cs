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
	public class DoLoopStatementTests
	{
		#region C#
		[Test]
		public void CSharpWhileStatementTest()
		{
			DoLoopStatement doLoopStmt = ParseUtilCSharp.ParseStatement<DoLoopStatement>("while (true) { }");
			Assert.AreEqual(ConditionPosition.Start, doLoopStmt.ConditionPosition);
			Assert.AreEqual(ConditionType.While, doLoopStmt.ConditionType);
			Assert.IsTrue(doLoopStmt.Condition is PrimitiveExpression);
			Assert.IsTrue(doLoopStmt.EmbeddedStatement is BlockStatement);
		}
		
		[Test]
		public void CSharpDoWhileStatementTest()
		{
			DoLoopStatement doLoopStmt = ParseUtilCSharp.ParseStatement<DoLoopStatement>("do { } while (true);");
			Assert.AreEqual(ConditionPosition.End, doLoopStmt.ConditionPosition);
			Assert.AreEqual(ConditionType.While, doLoopStmt.ConditionType);
			Assert.IsTrue(doLoopStmt.Condition is PrimitiveExpression);
			Assert.IsTrue(doLoopStmt.EmbeddedStatement is BlockStatement);
		}
		#endregion
		
		#region VB.NET
			// TODO
		#endregion 
	}
}
