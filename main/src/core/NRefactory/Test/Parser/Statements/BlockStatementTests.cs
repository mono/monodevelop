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
	public class BlockStatementTests
	{
		#region C#
		[Test]
		public void CSharpBlockStatementTest()
		{
			BlockStatement blockStmt = ParseUtilCSharp.ParseStatement<BlockStatement>("{}");
		}
		
		[Test]
		public void CSharpComplexBlockStatementPositionTest()
		{
			string code = @"{
	WebClient wc = new WebClient();
	wc.Test();
	wc.UploadStringCompleted += delegate {
		output.BeginInvoke((MethodInvoker)delegate {
		                   	output.Text += newText;
		                   });
	};
}";
			BlockStatement blockStmt = ParseUtilCSharp.ParseStatement<BlockStatement>(code);
			//Assert.AreEqual(1, blockStmt.StartLocation.X); // does not work because ParseStatement inserts special code
			Assert.AreEqual(1, blockStmt.StartLocation.Y);
			Assert.AreEqual(2, blockStmt.EndLocation.X);
			Assert.AreEqual(9, blockStmt.EndLocation.Y);
		}

		#endregion
		
		#region VB.NET
		// TODO
		#endregion
	}
}
