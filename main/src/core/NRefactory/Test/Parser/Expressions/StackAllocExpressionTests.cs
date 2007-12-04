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
	public class StackAllocExpressionTests
	{
		#region C#
		[Test]
		public void CSharpStackAllocExpressionTest()
		{
			string program = "class A { unsafe void A() { int* fib = stackalloc int[100]; } }";
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.CSharp, new StringReader(program));
			parser.Parse();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			
//			Assert.IsTrue(expr is StackAllocExpression);
//			StackAllocExpression sae = (StackAllocExpression)expr;
//			
//			Assert.AreEqual("int", sae.TypeReference.Type);
//			Assert.IsTrue(sae.Expression is PrimitiveExpression);
		}
		#endregion
		
		#region VB.NET
			// No VB.NET representation
		#endregion
	}
}
