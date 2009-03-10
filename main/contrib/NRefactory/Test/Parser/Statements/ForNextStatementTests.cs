// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 3473 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class ForNextStatementTests
	{
		#region C#
		// No C# representation
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetForNextStatementTest()
		{
			ForNextStatement forNextStatement = ParseUtilVBNet.ParseStatement<ForNextStatement>("For i=0 To 10 Step 2 : Next i");
		}
		
		[Test]
		public void VBNetForNextStatementWithComplexExpressionTest()
		{
			ForNextStatement forNextStatement = ParseUtilVBNet.ParseStatement<ForNextStatement>("For SomeMethod().Property = 0 To 10 : Next SomeMethod().Property");
		}
		#endregion
	}
}
