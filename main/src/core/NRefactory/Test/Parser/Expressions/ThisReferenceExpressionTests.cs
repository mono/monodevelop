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
	public class ThisReferenceExpressionTests
	{
		#region C#
		[Test]
		public void CSharpThisReferenceExpressionTest1()
		{
			ThisReferenceExpression tre = ParseUtilCSharp.ParseExpression<ThisReferenceExpression>("this");
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetThisReferenceExpressionTest1()
		{
			ThisReferenceExpression ie = ParseUtilVBNet.ParseExpression<ThisReferenceExpression>("Me");
		}
		#endregion
	}
}
