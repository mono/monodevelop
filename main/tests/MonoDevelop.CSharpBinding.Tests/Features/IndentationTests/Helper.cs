//
// Helper.cs
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

using ICSharpCode.NRefactory6.CSharp;
using NUnit.Framework;
using System.IO;
using System.Text;
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.IndentationTests
{
	internal static class Helper
	{
		public static IDocumentIndentEngine CreateEngine(string text, OptionSet formatOptions = null, IEnumerable<string> symbols = null)
		{
			var policy = formatOptions;
			if (policy == null) {
					policy = FormattingOptionsFactory.CreateMono();

//				policy.IndentPreprocessorDirectives = false;
//				policy.AlignToFirstMethodCallArgument = policy.AlignToFirstIndexerArgument = true;

			}

			var sb = new StringBuilder();
			int offset = 0;
			for (int i = 0; i < text.Length; i++)
			{
				var ch = text[i];
				if (ch == '$')
				{
					offset = i;
					continue;
				}
				sb.Append(ch);
			}

			var document = SourceText.From(sb.ToString());

			var csi = new CSharpIndentEngine(policy) {
				EnableCustomIndentLevels = true
			};
			if (symbols != null) {
				foreach (var sym in symbols) {
					csi.DefineSymbol(sym);
				}
			}
			var result = new CacheIndentEngine(csi);
			result.Update(document, offset);
			return result;
		}


		public static void RandomTests(string filePath, int count, OptionSet options = null)
		{
			if (File.Exists(filePath))
			{
				var code = File.ReadAllText(filePath);
				var document = SourceText.From(code);
				options = options ?? FormattingOptionsFactory.CreateMono();

				var engine = new CacheIndentEngine(new CSharpIndentEngine(options) { EnableCustomIndentLevels = true });
				Random rnd = new Random();

				for (int i = 0; i < count; i++) {
					int offset = rnd.Next(document.Length);
					engine.Update(document, offset);
					if (engine.CurrentIndent.Length == 0)
						continue;
				}

			}
			else
			{
				Assert.Fail("File " + filePath + " doesn't exist.");
			}
		}

		static string GetIndentString(string indent)
		{
			return indent.Replace(' ', '.').Replace("\t", "\\t");
		}

		public static void ReadAndTest(string filePath, OptionSet options = null)
		{
			if (File.Exists(filePath))
			{
				filePath = Path.GetFullPath(filePath);
				var code = File.ReadAllText(filePath);
				var document = SourceText.From(code);
				if (options == null) {
					options = FormattingOptionsFactory.CreateMono();
					//policy.AlignToFirstIndexerArgument = policy.AlignToFirstMethodCallArgument = true;
				}

				var engine = new CacheIndentEngine(new CSharpIndentEngine(options) { EnableCustomIndentLevels = true });
				int errors = 0;

				var newLine = options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);
				foreach (var ch in code)
				{
					if (newLine [0] == ch)
					{
						if (!(engine.LineBeganInsideMultiLineComment || engine.LineBeganInsideVerbatimString)) {
							if (engine.CurrentIndent.Length > 0) {
								if (engine.NeedsReindent) {
									errors++;
									var line = document.Lines.GetLineFromPosition(engine.Offset);
									Console.WriteLine(string.Format("Indent: {2}, Current indent: {3} in {0}:{1}", filePath, line.LineNumber, GetIndentString (engine.ThisLineIndent), GetIndentString (engine.CurrentIndent)));
								}
							}
						}
					}

					engine.Push(ch);
				}
				Assert.AreEqual(0, errors, "file has errors");

			}
			else
			{
				Assert.Fail("File " + filePath + " doesn't exist.");
			}
		}
	}
}
