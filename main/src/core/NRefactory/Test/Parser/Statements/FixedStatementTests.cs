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
	public class FixedStatementTests
	{
		#region C#
		[Test]
		public void CSharpFixedStatementTest()
		{
			FixedStatement fixedStmt = ParseUtilCSharp.ParseStatement<FixedStatement>("fixed (int* ptr = &myIntArr) { }");
			// TODO : Extend test.
		}
		#endregion
		
		#region VB.NET
			// No VB.NET representation
		#endregion	
	}
}
