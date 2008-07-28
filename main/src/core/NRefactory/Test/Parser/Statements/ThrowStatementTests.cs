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
	public class ThrowStatementTests
	{
		#region C#
		[Test]
		public void CSharpEmptyThrowStatementTest()
		{
			ThrowStatement throwStmt = ParseUtilCSharp.ParseStatement<ThrowStatement>("throw;");
			Assert.IsTrue(throwStmt.Expression.IsNull);
		}
		
		[Test]
		public void CSharpThrowStatementTest()
		{
			ThrowStatement throwStmt = ParseUtilCSharp.ParseStatement<ThrowStatement>("throw new Exception();");
			Assert.IsTrue(throwStmt.Expression is ObjectCreateExpression);
		}
		#endregion
		
		#region VB.NET
			// TODO
		#endregion 
	}
}
