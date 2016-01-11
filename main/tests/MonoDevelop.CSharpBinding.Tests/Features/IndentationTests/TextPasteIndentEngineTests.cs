//
// TextPasteIndentEngineTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp;
using System.Text;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.IndentationTests
{
	[TestFixture]
	public class TextPasteIndentEngineTests
	{
		public static CacheIndentEngine CreateEngine(string text, out SourceText sourceText, OptionSet options = null)
		{
			if (options == null) {
				options = FormattingOptionsFactory.CreateMono();
			//	options.AlignToFirstIndexerArgument = formatOptions.AlignToFirstMethodCallArgument = true;
			}
			
			var sb = new StringBuilder();
			int offset = 0;
			for (int i = 0; i < text.Length; i++) {
				var ch = text [i];
				if (ch == '$') {
					offset = i;
					continue;
				}
				sb.Append(ch);
			}


			sourceText = SourceText.From(sb.ToString());

			var result = new CacheIndentEngine(new CSharpIndentEngine(options));
			result.Update(sourceText, offset);
			return result;
		}

		static OptionSet CreateInvariantOptions()
		{
			return null;
		}

		[Test]
		public void TestSimplePaste()
		{
			SourceText sourceText;
			var indent = CreateEngine(@"
class Foo
{
	void Bar ()
	{
		System.Console.WriteLine ($);
	}
}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			var text = handler.FormatPlainText(sourceText, indent.Offset, "Foo", null);
			Assert.AreEqual("Foo", text);
		}

		[Test]
		public void TestMultiLinePaste()
		{
			SourceText sourceText;
			var indent = CreateEngine(@"
namespace FooBar
{
	class Foo
	{
		void Bar ()
		{
			System.Console.WriteLine ();
		}
		$
	}
}
", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			
			var text = handler.FormatPlainText(sourceText, indent.Offset, "void Bar ()\n{\nSystem.Console.WriteLine ();\n}", null);
			Assert.AreEqual("void Bar ()\n\t\t{\n\t\t\tSystem.Console.WriteLine ();\n\t\t}", text);
		}

		[Test]
		public void TestMultiplePastes()
		{
			SourceText sourceText;
			var indent = CreateEngine(@"
class Foo
{
	void Bar ()
	{
		System.Console.WriteLine ();
	}
	$
}


", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			
			for (int i = 0; i < 2; i++) {
				var text = handler.FormatPlainText(sourceText, indent.Offset, "void Bar ()\n{\nSystem.Console.WriteLine ();\n}", null);
				Assert.AreEqual("void Bar ()\n\t{\n\t\tSystem.Console.WriteLine ();\n\t}", text);
			}
		}
		

		[Test]
		public void TestPasteNewLine()
		{
			SourceText sourceText;
			var indent = CreateEngine(@"
class Foo
{
	$void Bar ()
	{
	}
}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			var text = handler.FormatPlainText(sourceText, indent.Offset, "int i;\n", null);
			Assert.AreEqual("int i;\n\t", text);
		}

		[Test]
		public void TestPasteNewLineCase2()
		{
			SourceText sourceText;
			var indent = CreateEngine(@"
class Foo
{
$	void Bar ()
	{
	}
}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			var text = handler.FormatPlainText(sourceText, indent.Offset, "int i;\n", null);
			Assert.AreEqual("\tint i;\n", text);
		}

		[Test]
		public void PasteVerbatimString()
		{
			SourceText sourceText;
			var indent = CreateEngine(@"
class Foo
{
void Bar ()
{
	
}
}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			var str = "string str = @\"\n1\n\t2 \n\t\t3\n\";";
			var text = handler.FormatPlainText(sourceText, indent.Offset, str, null);
			Assert.AreEqual(str, text);
		}

		[Test]
		public void TestWindowsLineEnding()
		{
			SourceText sourceText;
			var indent = CreateEngine("\r\nclass Foo\r\n{\r\n\tvoid Bar ()\r\n\t{\r\n\t\t$\r\n\t}\r\n}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			var text = handler.FormatPlainText(sourceText, indent.Offset, "Foo();\r\nBar();\r\nTest();", null);
			Assert.AreEqual("Foo();\n\t\tBar();\n\t\tTest();", text);
		}

		[Test]
		public void TestPasteBlankLines()
		{
			SourceText sourceText;
			var indent = CreateEngine("class Foo\n{\n\tvoid Bar ()\n\t{\n\t\tSystem.Console.WriteLine ($);\n\t}\n}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			var text = handler.FormatPlainText(sourceText, indent.Offset, "\n\n\n", null);
			Assert.AreEqual("\n\n\n\t\t\t", text);
		}

		[Ignore("This option isn't part of the roslyn option set")]
		[Test]
		public void TestPasteBlankLinesAndIndent()
		{
			SourceText sourceText;
			var indent = CreateEngine("class Foo\n{\n\tvoid Bar ()\n\t{\n\t\tSystem.Console.WriteLine ($);\n\t}\n}", out sourceText);
			var options = FormattingOptionsFactory.CreateMono();
//			options.EmptyLineFormatting = EmptyLineFormatting.Indent;
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, options);
			var text = handler.FormatPlainText(sourceText, indent.Offset, "\n\n\n", null);
			Assert.AreEqual("\n\t\t\t\n\t\t\t\n\t\t\t", text);
		}

		[Test]
		public void TestWindowsLineEndingCase2()
		{
			var options = FormattingOptionsFactory.CreateMono();
			options = options.WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\r\n");
			SourceText sourceText;
			var indent = CreateEngine("\r\nclass Foo\r\n{\r\n\tvoid Bar ()\r\n\t{\r\n\t\t$\r\n\t}\r\n}", out sourceText, options);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, options);
			var text = handler.FormatPlainText(sourceText, indent.Offset, "if (true)\r\nBar();\r\nTest();", null);
			Assert.AreEqual("if (true)\r\n\t\t\tBar();\r\n\t\tTest();", text);
		}

		[Test]
		public void PasteVerbatimStringBug1()
		{
			var textEditorOptions = FormattingOptionsFactory.CreateMono();
			textEditorOptions = textEditorOptions.WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\r\n");
			SourceText sourceText;
			var indent = CreateEngine("\r\nclass Foo\r\n{\r\n\tvoid Bar ()\r\n\t{\r\n\t\t$\r\n\t}\r\n}", out sourceText, textEditorOptions);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, textEditorOptions);
			var text = handler.FormatPlainText(sourceText, indent.Offset, "Console.WriteLine (@\"Hello World!\", out sourceText);\n", null);
			Assert.AreEqual("Console.WriteLine (@\"Hello World!\", out sourceText);\r\n\t\t", text);
		}

		[Test]
		public void PasteVerbatimStringBug2()
		{
			SourceText sourceText;
			var indent = CreateEngine("\nclass Foo\n{\n\tvoid Bar ()\n\t{\n\t\t$\n\t}\n}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			var text = handler.FormatPlainText(sourceText, indent.Offset, "if (true)\nConsole.WriteLine (@\"Hello\n World!\", out sourceText);\n", null);
			Assert.AreEqual("if (true)\n\t\t\tConsole.WriteLine (@\"Hello\n World!\", out sourceText);\n\t\t", text);
		}

		[Test]
		public void PasteVerbatimStringBug3()
		{
			SourceText sourceText;
			var indent = CreateEngine("\nclass Foo\n{\n\tvoid Bar ()\n\t{\n$\n\t}\n}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());

			var text = handler.FormatPlainText(sourceText, indent.Offset, "\t\tSystem.Console.WriteLine(@\"<evlevlle>\", out sourceText);\n", null);
			Assert.AreEqual("\t\tSystem.Console.WriteLine(@\"<evlevlle>\", out sourceText);\n\t\t", text);
		}

		[Test]
		public void PasteVerbatimStringBug4()
		{
			SourceText sourceText;
			var indent = CreateEngine("\nclass Foo\n{\n\tvoid Bar ()\n\t{\n$\n\t}\n}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());

			var text = handler.FormatPlainText(sourceText, indent.Offset, "var str1 = \n@\"hello\";", null);
			Assert.AreEqual("\t\tvar str1 = \n\t\t\t@\"hello\";", text);
		}

		[Test]
		public void TestPasteComments()
		{
			SourceText sourceText;
			var indent = CreateEngine(@"
class Foo
{
	$
}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			var text = handler.FormatPlainText(sourceText, indent.Offset, "// Foo\n\t// Foo 2\n\t// Foo 3", null);
			Assert.AreEqual("// Foo\n\t// Foo 2\n\t// Foo 3", text);
		}

		[Test]
		public void PastemultilineAtFirstColumnCorrection()
		{
			SourceText sourceText;
			var indent = CreateEngine("class Foo\n{\n$\n}", out sourceText);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, FormattingOptionsFactory.CreateMono());
			var text = handler.FormatPlainText(sourceText, indent.Offset, "void Bar ()\n{\n\tSystem.Console.WriteLine ();\n}", null);
			Assert.AreEqual("\tvoid Bar ()\n\t{\n\t\tSystem.Console.WriteLine ();\n\t}", text);
		}

		[Test]
		public void TestPasteToWindowsEol()
		{
			SourceText sourceText;
			var indent = CreateEngine("$", out sourceText);
			var options = FormattingOptionsFactory.CreateMono();
			options = options.WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\r\n");
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, options);
			var text = handler.FormatPlainText(sourceText, indent.Offset, "namespace Foo\n{\n\tpublic static class FooExtensions\n\t{\n\t\tpublic static int ObjectExtension (this object value)\n\t\t{\n\t\t\treturn 0;\n\t\t}\n\n\t\tpublic static int IntExtension (this int value)\n\t\t{\n\t\t\treturn 0;\n\t\t}\n\t}\n\n\tpublic class Client\n\t{\n\t\tpublic void Method ()\n\t\t{\n\t\t\t0.ToString ();\n\t\t}\n\t}\n}", null);
			Assert.AreEqual("namespace Foo\r\n{\r\n\tpublic static class FooExtensions\r\n\t{\r\n\t\tpublic static int ObjectExtension (this object value)\r\n\t\t{\r\n\t\t\treturn 0;\r\n\t\t}\r\n\r\n\t\tpublic static int IntExtension (this int value)\r\n\t\t{\r\n\t\t\treturn 0;\r\n\t\t}\r\n\t}\r\n\r\n\tpublic class Client\r\n\t{\r\n\t\tpublic void Method ()\r\n\t\t{\r\n\t\t\t0.ToString ();\r\n\t\t}\r\n\t}\r\n}", text);
		}

		[Ignore("This option isn't part of the roslyn option set")]
		[Test]
		public void PastePreProcessorDirectivesNoIndent()
		{
			var opt = FormattingOptionsFactory.CreateMono();
//			opt.IndentPreprocessorDirectives = false;

			SourceText sourceText;
			var indent = CreateEngine(@"
class Foo
{
$
}", out sourceText, opt);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, opt);
			var text = handler.FormatPlainText(sourceText, indent.Offset, "#if DEBUG\n\tvoid Foo()\n\t{\n\t}\n#endif", null);
			Assert.AreEqual("#if DEBUG\n\tvoid Foo()\n\t{\n\t}\n#endif", text);
		}

		[Test]
		public void PasteInUnterminatedString ()
		{
			var opt = FormattingOptionsFactory.CreateMono();
		//	opt.IndentPreprocessorDirectives = false;

			SourceText sourceText;
			var indent = CreateEngine(@"
var foo = ""hello$
", out sourceText, opt);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, opt);
			var text = handler.FormatPlainText(sourceText, indent.Offset, "Hi \" + username;", null);
			Assert.AreEqual("Hi \" + username;", text);
		}

		[Test]
		public void PasteInTerminatedString ()
		{
			var opt = FormattingOptionsFactory.CreateMono();
			//opt.IndentPreprocessorDirectives = false;

			SourceText sourceText;
			var indent = CreateEngine(@"
var foo = ""hello$"";
", out sourceText, opt);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, opt);
			var text = handler.FormatPlainText(sourceText, indent.Offset, "Hi \" + username;", null);
			Assert.AreEqual("Hi \\\" + username;", text);
		}

		[Test]
		public void PasteInUnterminatedVerbatimString ()
		{
			var opt = FormattingOptionsFactory.CreateMono();
			//opt.IndentPreprocessorDirectives = false;

			SourceText sourceText;
			var indent = CreateEngine(@"
var foo = @""hello$
", out sourceText,opt);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, opt);
			var text = handler.FormatPlainText(sourceText, indent.Offset, "Hi \" + username;", null);
			Assert.AreEqual("Hi \" + username;", text);
		}

		[Test]
		public void PasteInTerminatedVerbatimString ()
		{
			var opt = FormattingOptionsFactory.CreateMono();
			//opt.IndentPreprocessorDirectives = false;

			SourceText sourceText;
			var indent = CreateEngine(@"
var foo = @""hello$"";
", out sourceText,opt);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, opt);
			var text = handler.FormatPlainText(sourceText, indent.Offset, "Hi \" + username;", null);
			Assert.AreEqual("Hi \"\" + username;", text);
		}


		/// <summary>
		/// Bug 16415 - Formatter - Copy paste comments 
		/// </summary>
		[Test]
		public void TestBug16415 ()
		{
			SourceText sourceText;
			var opt = FormattingOptionsFactory.CreateMono();
			var indent = CreateEngine("class Foo\n{\n\tpublic static void Main (string[] args)\n\t{\n\t\tConsole.WriteLine ();$\n\t}\n}\n", out sourceText, opt);
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, opt);
			var text = handler.FormatPlainText(sourceText, indent.Offset, "// Line 1\n// Line 2\n// Line 3", null);
			Assert.AreEqual("// Line 1\n\t\t// Line 2\n\t\t// Line 3", text);
		}
	}
}

