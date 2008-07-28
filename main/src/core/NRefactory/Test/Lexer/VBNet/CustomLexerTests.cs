// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 2658$</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.VB;
namespace ICSharpCode.NRefactory.Tests.Lexer.VB
{
	[TestFixture]
	public class CustomLexerTests
	{
		ILexer GenerateLexer(StringReader sr)
		{
			return ParserFactory.CreateLexer(SupportedLanguage.VBNet, sr);
		}
		
		[Test]
		public void TestSingleEOLForMulitpleLines()
		{
			ILexer lexer = GenerateLexer(new StringReader("Stop\n\n\nEnd"));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Stop));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.End));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void TestSingleEOLForMulitpleLinesWithContinuation()
		{
			ILexer lexer = GenerateLexer(new StringReader("Stop\n _\n\nEnd"));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Stop));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.End));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void EscapedIdentifier()
		{
			ILexer lexer = GenerateLexer(new StringReader("[Stop]"));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void IdentifierWithTypeCharacter()
		{
			ILexer lexer = GenerateLexer(new StringReader("Stop$"));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void ExclamationMarkIsTypeCharacter()
		{
			ILexer lexer = GenerateLexer(new StringReader("a!=b"));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Assign));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void ExclamationMarkIsTypeCharacter2()
		{
			ILexer lexer = GenerateLexer(new StringReader("a! b"));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void ExclamationMarkIsIdentifier()
		{
			ILexer lexer = GenerateLexer(new StringReader("a!b"));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.ExclamationMark));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void ExclamationMarkIsIdentifier2()
		{
			ILexer lexer = GenerateLexer(new StringReader("a![b]"));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.ExclamationMark));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void RemCommentTest()
		{
			ILexer lexer = GenerateLexer(new StringReader("a rem b"));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void RemCommentTest2()
		{
			ILexer lexer = GenerateLexer(new StringReader("REM c"));
			Assert.That(lexer.NextToken().kind, Is.EqualTo(Tokens.EOF));
		}
	}
}
