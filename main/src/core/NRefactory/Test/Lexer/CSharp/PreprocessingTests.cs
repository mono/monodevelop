// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.CSharp;
using ICSharpCode.NRefactory.PrettyPrinter;
using NUnit.Framework.SyntaxHelpers;

namespace ICSharpCode.NRefactory.Tests.Lexer.CSharp
{
	[TestFixture]
	public class PreprocessingTests
	{
		ILexer GenerateLexer(string text)
		{
			ILexer lexer = ParserFactory.CreateLexer(SupportedLanguage.CSharp, new StringReader(text));
			lexer.EvaluateConditionalCompilation = true;
			lexer.ConditionalCompilationSymbols["TEST"] = null;
			return lexer;
		}
		
		int[] GetTokenKinds(string text)
		{
			List<int> list = new List<int>();
			ILexer lexer = GenerateLexer(text);
			Token token;
			while ((token = lexer.NextToken()) != null) {
				list.Add(token.kind);
				if (token.kind == Tokens.EOF)
					break;
			}
			Assert.AreEqual("", lexer.Errors.ErrorOutput);
			return list.ToArray();
		}
		
		[Test]
		public void TestEmptyIfdef()
		{
			Assert.AreEqual(new int[] { Tokens.Int, Tokens.EOF }, GetTokenKinds("#if true\n#endif\nint"));
			Assert.AreEqual(new int[] { Tokens.Int, Tokens.EOF }, GetTokenKinds("#if false\n#endif\nint"));
		}
		
		[Test]
		public void TestBooleanPrimitives()
		{
			Assert.AreEqual(new int[] { Tokens.True, Tokens.EOF }, GetTokenKinds("#if true \n true \n #else \n false \n #endif"));
			Assert.AreEqual(new int[] { Tokens.False, Tokens.EOF }, GetTokenKinds("#if false \n true \n #else \n false \n #endif"));
		}
		
		[Test]
		public void TestDefinedSymbols()
		{
			Assert.AreEqual(new int[] { Tokens.True, Tokens.EOF }, GetTokenKinds("#if TEST \n true \n #else \n false \n #endif"));
			Assert.AreEqual(new int[] { Tokens.False, Tokens.EOF }, GetTokenKinds("#if DEBUG \n true \n #else \n false \n #endif"));
		}
		
		[Test]
		public void TestDefineUndefineSymbol()
		{
			Assert.AreEqual(new int[] { Tokens.False, Tokens.EOF }, GetTokenKinds("#undef TEST \n #if TEST \n true \n #else \n false \n #endif"));
			Assert.AreEqual(new int[] { Tokens.True, Tokens.EOF }, GetTokenKinds("#define DEBUG \n #if DEBUG \n true \n #else \n false \n #endif"));
			Assert.AreEqual(new int[] { Tokens.True, Tokens.EOF }, GetTokenKinds("#define DEBUG // comment \n #if DEBUG \n true \n #else \n false \n #endif"));
		}
		
		[Test]
		public void TestNestedIfDef()
		{
			string program = @"
				#if A
					public
					#if B
						abstract
					#elif C
						virtual
					#endif
					void
				#elif B
					protected
					#if C // this is a comment
						sealed
					#endif
					string
				#else
					class
				#endif
			";
			Assert.AreEqual(new int[] { Tokens.Class, Tokens.EOF }, GetTokenKinds(program));
			Assert.AreEqual(new int[] { Tokens.Public, Tokens.Void, Tokens.EOF }, GetTokenKinds("#define A\n" + program));
			Assert.AreEqual(new int[] { Tokens.Public, Tokens.Abstract, Tokens.Void, Tokens.EOF },
			                GetTokenKinds("#define A\n#define B\n" + program));
			Assert.AreEqual(new int[] { Tokens.Public, Tokens.Virtual, Tokens.Void, Tokens.EOF },
			                GetTokenKinds("#define A\n#define C\n" + program));
			Assert.AreEqual(new int[] { Tokens.Public, Tokens.Abstract, Tokens.Void, Tokens.EOF },
			                GetTokenKinds("#define A\n#define B\n#define C\n" + program));
			Assert.AreEqual(new int[] { Tokens.Protected, Tokens.String, Tokens.EOF },
			                GetTokenKinds("#define B\n" + program));
			Assert.AreEqual(new int[] { Tokens.Protected, Tokens.Sealed, Tokens.String, Tokens.EOF },
			                GetTokenKinds("#define B\n#define C\n" + program));
		}
		
		[Test]
		public void TestDefineInIfDef()
		{
			string program = @"
				#if !A
					#define B
					class
				#else
					int
				#endif
				#if B
					struct
				#endif
			";
			Assert.AreEqual(new int[] { Tokens.Class, Tokens.Struct, Tokens.EOF }, GetTokenKinds(program));
			Assert.AreEqual(new int[] { Tokens.Int, Tokens.EOF }, GetTokenKinds("#define A\n" + program));
		}
		
		[Test]
		public void TestMultilineCommentStartInIfDef()
		{
			string program = @"
			#if X
				struct
				/*
			#else
				/* */ class
			#endif
			";
			Assert.AreEqual(new int[] { Tokens.Class, Tokens.EOF }, GetTokenKinds(program));
			Assert.AreEqual(new int[] { Tokens.Struct, Tokens.Class, Tokens.EOF }, GetTokenKinds("#define X\n" + program));
		}
		
		[Test]
		public void Region()
		{
			string program = @"
	#region Region Title
	;
	#endregion
	,";
			Assert.AreEqual(new int[] { Tokens.Semicolon, Tokens.Comma, Tokens.EOF }, GetTokenKinds(program));
			ILexer lexer = GenerateLexer(program);
			while (lexer.NextToken().kind != Tokens.EOF);
			List<ISpecial> specials = lexer.SpecialTracker.RetrieveSpecials();
			
			Assert.IsTrue(specials[0] is BlankLine);
			Assert.AreEqual(new Location(2, 1), specials[0].StartPosition);
			Assert.AreEqual(new Location(2, 1), specials[0].EndPosition);
			
			Assert.AreEqual("#region", (specials[1] as PreprocessingDirective).Cmd);
			Assert.AreEqual("Region Title", (specials[1] as PreprocessingDirective).Arg);
			Assert.AreEqual(new Location(2, 2), specials[1].StartPosition);
			Assert.AreEqual(new Location(22, 2), specials[1].EndPosition);
			
			Assert.AreEqual("#endregion", (specials[2] as PreprocessingDirective).Cmd);
			Assert.AreEqual(new Location(2, 4), specials[2].StartPosition);
			Assert.AreEqual(new Location(12, 4), specials[2].EndPosition);
		}
	}
}
