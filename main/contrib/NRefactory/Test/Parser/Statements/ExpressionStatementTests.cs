// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 2676 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class ExpressionStatementTests
	{
		#region C#
		[Test]
		public void CSharpStatementExpressionTest()
		{
			ExpressionStatement stmtExprStmt = ParseUtilCSharp.ParseStatement<ExpressionStatement>("my.Obj.PropCall;");
			Assert.IsTrue(stmtExprStmt.Expression is MemberReferenceExpression);
		}
		[Test]
		public void CSharpStatementExpressionTest1()
		{
			ExpressionStatement stmtExprStmt = ParseUtilCSharp.ParseStatement<ExpressionStatement>("yield.yield;");
			Assert.IsTrue(stmtExprStmt.Expression is MemberReferenceExpression);
		}
		#endregion
		
		#region VB.NET
			// TODO
		#endregion 
	}
}
