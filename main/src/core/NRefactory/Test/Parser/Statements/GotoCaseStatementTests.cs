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
	public class GotoCaseStatementTests
	{
		#region C#
		[Test]
		public void CSharpGotoCaseDefaltStatementTest()
		{
			GotoCaseStatement gotoCaseStmt = ParseUtilCSharp.ParseStatement<GotoCaseStatement>("goto default;");
			Assert.IsTrue(gotoCaseStmt.IsDefaultCase);
		}
		
		[Test]
		public void CSharpGotoCaseStatementTest()
		{
			GotoCaseStatement gotoCaseStmt = ParseUtilCSharp.ParseStatement<GotoCaseStatement>("goto case 6;");
			Assert.IsFalse(gotoCaseStmt.IsDefaultCase);
			Assert.IsTrue(gotoCaseStmt.Expression is PrimitiveExpression);
		}
		#endregion
		
		#region VB.NET
			// No VB.NET representation
		#endregion
	}
}
