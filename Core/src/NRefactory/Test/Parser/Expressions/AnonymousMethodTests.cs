// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1009 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class AnonymousMethodTests
	{
		AnonymousMethodExpression Parse(string program)
		{
			return ParseUtilCSharp.ParseExpression<AnonymousMethodExpression>(program);
		}
		
		[Test]
		public void AnonymousMethodWithoutParameterList()
		{
			AnonymousMethodExpression ame = Parse("delegate {}");
			Assert.AreEqual(0, ame.Parameters.Count);
			Assert.AreEqual(0, ame.Body.Children.Count);
		}
		
		[Test]
		public void AnonymousMethodAfterCast()
		{
			CastExpression c = ParseUtilCSharp.ParseExpression<CastExpression>("(ThreadStart)delegate {}");
			Assert.AreEqual("ThreadStart", c.CastTo.Type);
			AnonymousMethodExpression ame = (AnonymousMethodExpression)c.Expression;
			Assert.AreEqual(0, ame.Parameters.Count);
			Assert.AreEqual(0, ame.Body.Children.Count);
		}
		
		[Test]
		public void EmptyAnonymousMethod()
		{
			AnonymousMethodExpression ame = Parse("delegate() {}");
			Assert.AreEqual(0, ame.Parameters.Count);
			Assert.AreEqual(0, ame.Body.Children.Count);
		}
		
		[Test]
		public void SimpleAnonymousMethod()
		{
			AnonymousMethodExpression ame = Parse("delegate(int a, int b) { return a + b; }");
			Assert.AreEqual(2, ame.Parameters.Count);
			// blocks can't be added without compilation unit -> anonymous method body
			// is always empty when using ParseExpression
			//Assert.AreEqual(1, ame.Body.Children.Count);
			//Assert.IsTrue(ame.Body.Children[0] is ReturnStatement);
		}
	}
}
