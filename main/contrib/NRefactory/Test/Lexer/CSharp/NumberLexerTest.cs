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
			Assert.AreEqual(Tokens.EOF, lexer.NextToken().Kind, "Tokens.EOF");
			Assert.AreEqual("", lexer.Errors.ErrorOutput);
			return t;
		}
		
		void CheckToken(string text, object val)
		{
			Token t = GetSingleToken(text);
			Assert.AreEqual(Tokens.Literal, t.Kind, "Tokens.Literal");
			Assert.AreEqual(text, t.Value, "value");
			Assert.IsNotNull(t.LiteralValue, "literalValue is null");
			Assert.AreEqual(val.GetType(), t.LiteralValue.GetType(), "literalValue.GetType()");
			Assert.AreEqual(val, t.LiteralValue, "literalValue");
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
		public void TestNonOctalInteger()
		{
			// C# does not have octal integers, so 077 should parse to 77
			Assert.IsTrue(077 == 77);
			
			CheckToken("077", 077);
			CheckToken("056", 056);
		}
		
		[Test]
		public void TestHexadecimalInteger()
		{
			CheckToken("0x99F", 0x99F);
			CheckToken("0xAB1f", 0xAB1f);
			CheckToken("0xffffffff", 0xffffffff);
			CheckToken("0xffffffffL", 0xffffffffL);
			CheckToken("0xffffffffuL", 0xffffffffuL);
		}
		
		[Test]
		public void InvalidHexadecimalInteger()
		{
			// don't check result, just make sure there is no exception
			GenerateLexer(new StringReader("0x2GF")).NextToken();
			GenerateLexer(new StringReader("0xG2F")).NextToken();
			// SD2-457
			GenerateLexer(new StringReader("0x")).NextToken();
			// hexadecimal integer >ulong.MaxValue
			GenerateLexer(new StringReader("0xfedcba98765432100")).NextToken();
		}
		
		[Test]
		public void TestLongHexadecimalInteger()
		{
			CheckToken("0x4244636f446c6d58", 0x4244636f446c6d58);
			CheckToken("0xf244636f446c6d58", 0xf244636f446c6d58);
		}
		
		[Test]
		public void TestLongInteger()
		{
			CheckToken("9223372036854775807", 9223372036854775807); // long.MaxValue
			CheckToken("9223372036854775808", 9223372036854775808); // long.MaxValue+1
			CheckToken("18446744073709551615", 18446744073709551615); // ulong.MaxValue
			CheckToken("18446744073709551616f", 18446744073709551616f); // ulong.MaxValue+1 as float
			CheckToken("18446744073709551616d", 18446744073709551616d); // ulong.MaxValue+1 as double
			CheckToken("18446744073709551616m", 18446744073709551616m); // ulong.MaxValue+1 as decimal
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
			
			CheckToken(@"""\U00000041""", "\U00000041");
			CheckToken(@"""\U00010041""", "\U00010041");
		}
		
		[Test]
		public void TestInvalidString()
		{
			// ensure that line numbers are correct after newline in string
			ILexer l = GenerateLexer(new StringReader("\"\n\"\n;"));
			Token t = l.NextToken();
			Assert.AreEqual(Tokens.Literal, t.Kind);
			Assert.AreEqual(new Location(1, 1), t.Location);
			
			t = l.NextToken();
			Assert.AreEqual(Tokens.Literal, t.Kind);
			Assert.AreEqual(new Location(1, 2), t.Location);
			
			t = l.NextToken();
			Assert.AreEqual(Tokens.Semicolon, t.Kind);
			Assert.AreEqual(new Location(1, 3), t.Location);
			
			t = l.NextToken();
			Assert.AreEqual(Tokens.EOF, t.Kind);
		}
		
		[Test]
		public void TestCharLiteral()
		{
			CheckToken(@"'a'", 'a');
			CheckToken(@"'\u0041'", '\u0041');
			CheckToken(@"'\x41'", '\x41');
			CheckToken(@"'\x041'", '\x041');
			CheckToken(@"'\x0041'", '\x0041');
			CheckToken(@"'\U00000041'", '\U00000041');
		}
	}
}
