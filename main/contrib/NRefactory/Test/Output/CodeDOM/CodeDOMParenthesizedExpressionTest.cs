// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1609 $</version>
// </file>

using System;
using System.CodeDom;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace ICSharpCode.NRefactory.Tests.Output.CodeDOM.Tests
{
	[TestFixture]
	public class CodeDOMParenthesizedExpressionTest
	{
		[Test]
		public void TestParenthesizedExpression()
		{
			object output = new ParenthesizedExpression(new PrimitiveExpression(5, "5")).AcceptVisitor(new CodeDomVisitor(), null);
			Assert.IsTrue(output is CodePrimitiveExpression);
		}
	}
}
