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
	public class EmptyStatementTests
	{
		#region C#
		[Test]
		public void CSharpEmptyStatementTest()
		{
			EmptyStatement emptyStmt = ParseUtilCSharp.ParseStatement<EmptyStatement>(";");
		}
		#endregion
		
		#region VB.NET
			// No VB.NET representation
		#endregion
		
	}
}
