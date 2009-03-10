// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 3715 $</version>
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
	public sealed class CustomLexerTests
	{
		ILexer GenerateLexer(StringReader sr)
		{
			return ParserFactory.CreateLexer(SupportedLanguage.CSharp, sr);
		}
		
		[Test]
		public void TestEmptyBlock()
		{
			ILexer lexer = GenerateLexer(new StringReader("{}+"));
			Assert.AreEqual(Tokens.OpenCurlyBrace, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.CloseCurlyBrace, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Plus, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.EOF, lexer.NextToken().Kind);
		}
		
		void CheckIdentifier(string text, string actualIdentifier)
		{
			ILexer lexer = GenerateLexer(new StringReader(text));
			Token t = lexer.NextToken();
			Assert.AreEqual(Tokens.Identifier, t.Kind);
			Assert.AreEqual(actualIdentifier, t.Value);
			t = lexer.NextToken();
			Assert.AreEqual(Tokens.EOF, t.Kind);
			Assert.AreEqual("", lexer.Errors.ErrorOutput);
		}
		
		[Test]
		public void TestYieldAsIdentifier()
		{
			ILexer lexer = GenerateLexer(new StringReader("yield"));
			Token t = lexer.NextToken();
			Assert.AreEqual(Tokens.Yield, t.Kind);
			Assert.IsTrue(Tokens.IdentifierTokens[t.Kind]);
			Assert.AreEqual("yield", t.Value);
		}
		
		[Test]
		public void TestIdentifier()
		{
			CheckIdentifier("a_Bc05", "a_Bc05");
		}
		
		[Test]
		public void TestIdentifierStartingWithUnderscore()
		{
			CheckIdentifier("_Bc05", "_Bc05");
		}
		
		[Test]
		public void TestIdentifierStartingWithEscapeSequence()
		{
			CheckIdentifier(@"\u006cexer", "lexer");
		}
		
		[Test]
		public void TestIdentifierContainingEscapeSequence()
		{
			CheckIdentifier(@"l\U00000065xer", "lexer");
		}
		
		[Test]
		public void TestKeyWordAsIdentifier()
		{
			CheckIdentifier("@int", "int");
		}
		
		[Test]
		public void TestKeywordWithEscapeSequenceIsIdentifier()
		{
			CheckIdentifier(@"i\u006et", "int");
		}
		
		[Test]
		public void TestKeyWordAsIdentifierStartingWithUnderscore()
		{
			CheckIdentifier("@_int", "_int");
		}
		
		[Test]
		public void TestSkippedEmptyBlock()
		{
			ILexer lexer = GenerateLexer(new StringReader("{}+"));
			Assert.AreEqual(Tokens.OpenCurlyBrace, lexer.NextToken().Kind);
			lexer.NextToken();
			lexer.SkipCurrentBlock(Tokens.CloseCurlyBrace);
			Assert.AreEqual(Tokens.CloseCurlyBrace, lexer.LookAhead.Kind);
			Assert.AreEqual(Tokens.Plus, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.EOF, lexer.NextToken().Kind);
		}
		
		[Test]
		public void TestSkippedNonEmptyBlock()
		{
			ILexer lexer = GenerateLexer(new StringReader("{ TestMethod('}'); /* }}} */ while(1) {break;} }+"));
			Assert.AreEqual(Tokens.OpenCurlyBrace, lexer.NextToken().Kind);
			lexer.NextToken();
			lexer.SkipCurrentBlock(Tokens.CloseCurlyBrace);
			Assert.AreEqual(Tokens.CloseCurlyBrace, lexer.LookAhead.Kind);
			Assert.AreEqual(Tokens.Plus, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.EOF, lexer.NextToken().Kind);
		}
		
		[Test]
		public void TestSkippedNonEmptyBlockWithPeek()
		{
			ILexer lexer = GenerateLexer(new StringReader("{ TestMethod(\"}\"); // }}}\n" +
			                                              "while(1) {break;} }+"));
			Assert.AreEqual(Tokens.OpenCurlyBrace, lexer.NextToken().Kind);
			lexer.NextToken();
			lexer.StartPeek();
			lexer.Peek();
			lexer.Peek();
			lexer.Peek();
			lexer.SkipCurrentBlock(Tokens.CloseCurlyBrace);
			Assert.AreEqual(Tokens.CloseCurlyBrace, lexer.LookAhead.Kind);
			Assert.AreEqual(Tokens.Plus, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.EOF, lexer.NextToken().Kind);
		}
		
		[Test]
		public void TestSkippedEmptyBlockWithPeek()
		{
			ILexer lexer = GenerateLexer(new StringReader("{}+"));
			Assert.AreEqual(Tokens.OpenCurlyBrace, lexer.NextToken().Kind);
			lexer.NextToken();
			lexer.StartPeek();
			lexer.Peek();
			lexer.Peek();
			lexer.Peek();
			lexer.SkipCurrentBlock(Tokens.CloseCurlyBrace);
			Assert.AreEqual(Tokens.CloseCurlyBrace, lexer.LookAhead.Kind);
			Assert.AreEqual(Tokens.Plus, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.EOF, lexer.NextToken().Kind);
		}
	}
}
