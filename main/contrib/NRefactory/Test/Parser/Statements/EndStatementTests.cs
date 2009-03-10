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
	public class EndStatementTests
	{
		#region C#
		// No C# representation
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetEndStatementTest()
		{
			EndStatement endStatement = ParseUtilVBNet.ParseStatement<EndStatement>("End");
		}
		
		[Test]
		public void VBNetEndStatementInIfThenTest2()
		{
			IfElseStatement endStatement = ParseUtilVBNet.ParseStatement<IfElseStatement>("IF a THEN End");
		}
		#endregion
	}
}
