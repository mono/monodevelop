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
	public class ForeachStatementTests
	{
		#region C#
		[Test]
		public void CSharpForeachStatementTest()
		{
			ForeachStatement foreachStmt = ParseUtilCSharp.ParseStatement<ForeachStatement>("foreach (int i in myColl) {} ");
			// TODO : Extend test.
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetForeachStatementTest()
		{
			ForeachStatement foreachStmt = ParseUtilVBNet.ParseStatement<ForeachStatement>("For Each i As Integer In myColl : Next");
			// TODO : Extend test.
		}
		#endregion
		
	}
}
