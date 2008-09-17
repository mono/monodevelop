// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 2676 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class PointerReferenceExpressionTests
	{
		#region C#
		[Test]
		public void CSharpPointerReferenceExpressionTest()
		{
			PointerReferenceExpression pre = ParseUtilCSharp.ParseExpression<PointerReferenceExpression>("myObj.field->b");
			Assert.IsTrue(pre.TargetObject is MemberReferenceExpression);
			Assert.AreEqual("b", pre.Identifier);
		}
		#endregion
		
		#region VB.NET
			// No VB.NET representation
		#endregion
	}
}
