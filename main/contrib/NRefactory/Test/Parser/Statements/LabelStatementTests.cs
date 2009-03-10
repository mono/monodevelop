// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 3139 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class LabelStatementTests
	{
		#region C#
		[Test]
		public void CSharpLabelStatementTest()
		{
			LabelStatement labelStmt = ParseUtilCSharp.ParseStatement<LabelStatement>("myLabel: ; ");
			Assert.AreEqual("myLabel", labelStmt.Label);
		}
		[Test]
		public void CSharpLabel2StatementTest()
		{
			LabelStatement labelStmt = ParseUtilCSharp.ParseStatement<LabelStatement>("yield: ; ");
			Assert.AreEqual("yield", labelStmt.Label);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetLabelStatementTest()
		{
			MethodDeclaration method = ParseUtilVBNet.ParseTypeMember<MethodDeclaration>("Sub Test \n myLabel: Console.WriteLine() \n End Sub");
			Assert.AreEqual(2, method.Body.Children.Count);
			LabelStatement labelStmt = (LabelStatement)method.Body.Children[0];
			Assert.AreEqual("myLabel", labelStmt.Label);
		}
		#endregion 
	}
}
