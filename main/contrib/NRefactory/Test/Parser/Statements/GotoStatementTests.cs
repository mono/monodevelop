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
	public class GotoStatementTests
	{
		#region C#
		[Test]
		public void CSharpGotoStatementTest()
		{
			GotoStatement gotoStmt = ParseUtilCSharp.ParseStatement<GotoStatement>("goto myLabel;");
			Assert.AreEqual("myLabel", gotoStmt.Label);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetGotoStatementTest()
		{
			GotoStatement gotoStmt = ParseUtilVBNet.ParseStatement<GotoStatement>("GoTo myLabel");
			Assert.AreEqual("myLabel", gotoStmt.Label);
		}
		#endregion
	}
}
