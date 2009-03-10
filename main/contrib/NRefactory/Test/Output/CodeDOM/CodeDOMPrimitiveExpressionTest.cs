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
	public class CodeDOMPrimitiveExpressionsTests
	{
		[Test]
		public void TestPrimitiveExpression()
		{
			object output = new PrimitiveExpression(5, "5").AcceptVisitor(new CodeDomVisitor(), null);
			Assert.IsTrue(output is CodePrimitiveExpression);
			Assert.AreEqual(((CodePrimitiveExpression)output).Value, 5);
		}
	}
}
