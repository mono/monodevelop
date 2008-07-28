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
	public class SwitchStatementTests
	{
		#region C#
		[Test]
		public void CSharpSwitchStatementTest()
		{
			SwitchStatement switchStmt = ParseUtilCSharp.ParseStatement<SwitchStatement>("switch (a) { case 4: case 5: break; case 6: break; default: break; }");
			Assert.AreEqual("a", ((IdentifierExpression)switchStmt.SwitchExpression).Identifier);
			// TODO: Extend test
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBSwitchStatementTest()
		{
			SwitchStatement switchStmt = ParseUtilVBNet.ParseStatement<SwitchStatement>("Select Case a\n Case 4, 5\n Case 6\n Case Else\n End Select");
			Assert.AreEqual("a", ((IdentifierExpression)switchStmt.SwitchExpression).Identifier);
			// TODO: Extend test
		}
		
		[Test]
		public void InvalidVBSwitchStatementTest()
		{
			SwitchStatement switchStmt = ParseUtilVBNet.ParseStatement<SwitchStatement>("Select Case a\n Case \n End Select", true);
			Assert.AreEqual("a", ((IdentifierExpression)switchStmt.SwitchExpression).Identifier);
			SwitchSection sec = switchStmt.SwitchSections[0];
			Assert.AreEqual(0, sec.SwitchLabels.Count);
		}
		#endregion
	}
}
