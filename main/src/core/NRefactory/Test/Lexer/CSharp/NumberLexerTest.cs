// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 1017 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.CSharp;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.NRefactory.Tests.Lexer.CSharp
{
	[TestFixture]
	public sealed class NumberLexerTests
	{
		ILexer GenerateLexer(StringReader sr)
		{
			return ParserFactory.CreateLexer(SupportedLanguage.CSharp, sr);
		}
		
		Token GetSingleToken(string text)
		{
			ILexer lexer = GenerateLexer(new StringReader(text));
			Token t = lexer.NextToken();
			Assert.AreEqual(Tokens.EOF, lexer.NextToken().kind, "Tokens.EOF");
			Assert.AreEqual("", lexer.Errors.ErrorOutput);
			return t;
		}
		
		void CheckToken(string text, object val)
		{
			Token t = GetSingleToken(text);
			Assert.AreEqual(Tokens.Literal, t.kind, "Tokens.Literal");
			Assert.AreEqual(text, t.val, "value");
			Assert.IsNotNull(t.literalValue, "literalValue is null");
			Assert.AreEqual(val, t.literalValue, "literalValue");
		}
		
		[Test]
		public void TestSingleDigit()
		{
			CheckToken("5", 5);
		}
		
		[Test]
		public void TestZero()
		{
			CheckToken("0", 0);
		}
		
		[Test]
		public void TestInteger()
		{
			CheckToken("66", 66);
		}
		
		[Test]
		public void TestOctalInteger()
		{
			CheckToken("077", 077);
			CheckToken("056", 056);
		}
		
		[Test]
		public void TestHexadecimalInteger()
		{
			CheckToken("0x99F", 0x99F);
			CheckToken("0xAB1f", 0xAB1f);
		}
		
		[Test]
		public void InvalidHexadecimalInteger()
		{
			// don't check result, just make sure there is no exception
			GenerateLexer(new StringReader("0x2GF")).NextToken();
			GenerateLexer(new StringReader("0xG2F")).NextToken();
			// SD2-457
			GenerateLexer(new StringReader("0x")).NextToken();
		}
		
		[Test]
		public void TestLongHexadecimalInteger()
		{
			CheckToken("0x4244636f446c6d58", 0x4244636f446c6d58);
		}
		
		[Test]
		public void TestDouble()
		{
			CheckToken("1.0", 1.0);
			CheckToken("1.1", 1.1);
			CheckToken("1.1e-2", 1.1e-2);
		}
		
		[Test]
		public void TestFloat()
		{
			CheckToken("1f", 1f);
			CheckToken("1.0f", 1.0f);
			CheckToken("1.1f", 1.1f);
			CheckToken("1.1e-2f", 1.1e-2f);
		}
		
		[Test]
		public void TestDecimal()
		{
			CheckToken("1m", 1m);
			CheckToken("1.0m", 1.0m);
			CheckToken("1.1m", 1.1m);
			CheckToken("1.1e-2m", 1.1e-2m);
			CheckToken("2.0e-5m", 2.0e-5m);
		}
		
		[Test]
		public void TestString()
		{
			CheckToken(@"@""-->""""<--""", @"-->""<--");
			CheckToken(@"""-->\""<--""", "-->\"<--");
		}
	}
}
