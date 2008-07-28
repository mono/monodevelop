// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 1634 $</version>
// </file>

using System;
using System.IO;
using ICSharpCode.NRefactory.Parser;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Tests.Lexer.VB
{
	[TestFixture]
	public class LexerPositionTests
	{
		ILexer GenerateLexer(string s)
		{
			return ParserFactory.CreateLexer(SupportedLanguage.VBNet, new StringReader(s));
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
		public void TestReturn()
		{
			ILexer l = GenerateLexer("public\nstatic");
			Token t = l.NextToken();
			t = l.NextToken();
			t = l.NextToken();
			Assert.AreEqual(new Location(1, 2), t.Location);
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
		public void TestFloationPointNumber()
		{
			ILexer l = GenerateLexer("0.142 public");
			Token t = l.NextToken();
			Assert.AreEqual(new Location(1, 1), t.Location);
			t = l.NextToken();
			Assert.AreEqual(new Location(7, 1), t.Location);
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
	}
}
