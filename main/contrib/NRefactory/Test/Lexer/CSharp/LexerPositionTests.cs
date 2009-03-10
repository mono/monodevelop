// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
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
	public class LexerPositionTests
	{
		ILexer GenerateLexer(string s)
		{
			return ParserFactory.CreateLexer(SupportedLanguage.CSharp, new StringReader(s));
		}
		
		[Test]
		public void Test1()
		{
			ILexer l = GenerateLexer("public");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
		}
		
		[Test]
		public void Test2()
		{
			ILexer l = GenerateLexer("public static");
			Token t = l.NextToken();
			t = l.NextToken();
			Assert.AreEqual(new Location(8, 1), t.Location);
		}
		
		[Test]
		public void TestNewLine()
		{
			ILexer l = GenerateLexer("public\nstatic");
			Token t = l.NextToken();
			Assert.AreEqual(Tokens.Public, t.Kind);
			Assert.AreEqual(new Location(1, 1), t.Location);
			Assert.AreEqual(new Location(7, 1), t.EndLocation);
			t = l.NextToken();
			Assert.AreEqual(Tokens.Static, t.Kind);
			Assert.AreEqual(new Location(1, 2), t.Location);
			Assert.AreEqual(new Location(7, 2), t.EndLocation);
		}
		
		[Test]
		public void TestCarriageReturnNewLine()
		{
			ILexer l = GenerateLexer("public\r\nstatic");
			Token t = l.NextToken();
			Assert.AreEqual(Tokens.Public, t.Kind);
			Assert.AreEqual(new Location(1, 1), t.Location);
			Assert.AreEqual(new Location(7, 1), t.EndLocation);
			t = l.NextToken();
			Assert.AreEqual(Tokens.Static, t.Kind);
			Assert.AreEqual(new Location(1, 2), t.Location);
			Assert.AreEqual(new Location(7, 2), t.EndLocation);
		}
		
		[Test]
		public void TestSpace()
		{
			ILexer l = GenerateLexer("  public");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(3, 1), t.Location);
		}
		
		[Test]
		public void TestOctNumber()
		{
			ILexer l = GenerateLexer("0142");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
		}
		
		[Test]
		public void TestHexNumber()
		{
			ILexer l = GenerateLexer("0x142 public");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
			t = l.NextToken();
			Assert.AreEqual(new Location(7, 1), t.Location);
		}
		
		[Test]
		public void TestHexNumberChar()
		{
			ILexer l = GenerateLexer("\'\\x224\' public");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
			t = l.NextToken();
			Assert.AreEqual(new Location(9, 1), t.Location);
		}
		
		[Test]
		public void TestFloationPointNumber()
		{
			ILexer l = GenerateLexer("0.142 public");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
			t = l.NextToken();
			Assert.AreEqual(new Location(7, 1), t.Location);
		}
		
		[Test]
		public void TestVerbatimString()
		{
			ILexer l = GenerateLexer("@\"a\"\"a\" public");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
			t = l.NextToken();
			Assert.AreEqual(new Location(9, 1), t.Location);
		}
		
		[Test]
		public void TestAtIdent()
		{
			ILexer l = GenerateLexer("@public =");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
			t = l.NextToken();
			Assert.AreEqual(new Location(9, 1), t.Location);
		}
		
		[Test]
		public void TestNoFloationPointNumber()
		{
			ILexer l = GenerateLexer("5.a");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
			t = l.NextToken();
			Assert.AreEqual(new Location(2, 1), t.Location);
			t = l.NextToken();
			Assert.AreEqual(new Location(3, 1), t.Location);
		}
		
		[Test]
		public void TestNumber()
		{
			ILexer l = GenerateLexer("142\nstatic");
			Token t = l.NextToken();
			t = l.NextToken();
			Assert.AreEqual(new Location(1, 2), t.Location);
		}
		
		[Test]
		public void TestNumber2()
		{
			ILexer l = GenerateLexer("14 static");
			Token t = l.NextToken();
			t = l.NextToken();
			Assert.AreEqual(new Location(4, 1), t.Location);
		}
		
		[Test]
		public void TestOperator()
		{
			ILexer l = GenerateLexer("<<=");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
			Assert.AreEqual(Tokens.EOF, l.NextToken().Kind);
		}
		
		[Test]
		public void TestPositionLineBreakAfterApostrophe()
		{
			// see SD2-1469
			// the expression finder requires correct positions even when there are syntax errors
			ILexer l = GenerateLexer("'\r\nvoid");
			Token t = l.NextToken();
			// the incomplete char literal should not generate a token
			Assert.AreEqual(Tokens.Void, t.Kind);
			Assert.AreEqual(new Location(1, 2), t.Location);
			Assert.AreEqual(Tokens.EOF, l.NextToken().Kind);
		}
		
		[Test]
		public void TestPositionMissingEndApostrophe()
		{
			// see SD2-1469
			// the expression finder requires correct positions even when there are syntax errors
			ILexer l = GenerateLexer("'a\nvoid");
			Token t = l.NextToken();
			Assert.AreEqual(Tokens.Literal, t.Kind);
			Assert.AreEqual(new Location(1, 1), t.Location);
			t = l.NextToken();
			Assert.AreEqual(Tokens.Void, t.Kind);
			Assert.AreEqual(new Location(1, 2), t.Location);
			Assert.AreEqual(Tokens.EOF, l.NextToken().Kind);
		}
		
		[Test]
		public void TestPositionLineBreakAfterAt()
		{
			// the expression finder requires correct positions even when there are syntax errors
			ILexer l = GenerateLexer("@\nvoid");
			Token t = l.NextToken();
			Assert.AreEqual(Tokens.Void, t.Kind);
			Assert.AreEqual(new Location(1, 2), t.Location);
			Assert.AreEqual(Tokens.EOF, l.NextToken().Kind);
		}
		
		[Test]
		public void TestPositionLineBreakInsideString()
		{
			// the expression finder requires correct positions even when there are syntax errors
			ILexer l = GenerateLexer("\"\nvoid");
			Token t = l.NextToken();
			Assert.AreEqual(Tokens.Literal, t.Kind);
			Assert.AreEqual(new Location(1, 1), t.Location);
			t = l.NextToken();
			Assert.AreEqual(Tokens.Void, t.Kind);
			Assert.AreEqual(new Location(1, 2), t.Location);
			Assert.AreEqual(Tokens.EOF, l.NextToken().Kind);
		}
		
		[Test]
		public void MultilineString()
		{
			ILexer l = GenerateLexer("@\"\r\n\"");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
			Assert.AreEqual(new Location(2, 2), t.EndLocation);
		}
	}
}
