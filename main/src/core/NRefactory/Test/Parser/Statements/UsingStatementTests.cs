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
	public class UsingStatementTests
	{
		#region C#
		[Test]
		public void CSharpUsingStatementTest()
		{
			UsingStatement usingStmt = ParseUtilCSharp.ParseStatement<UsingStatement>("using (MyVar var = new MyVar()) { } ");
			// TODO : Extend test.
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetUsingStatementTest()
		{
			string usingText = @"
Using nf As New System.Drawing.Font(""Arial"", 12.0F, FontStyle.Bold)
        c.Font = nf
        c.Text = ""This is 12-point Arial bold""
End Using";
			UsingStatement usingStmt = ParseUtilVBNet.ParseStatement<UsingStatement>(usingText);
			// TODO : Extend test.
		}
		[Test]
		public void VBNetUsingStatementTest2()
		{
			string usingText = @"
Using nf As Font = New Font()
	Bla(nf)
End Using";
			UsingStatement usingStmt = ParseUtilVBNet.ParseStatement<UsingStatement>(usingText);
			// TODO : Extend test.
		}
		#endregion
	}
}
