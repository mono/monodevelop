//
// SyntaxHighlightingTest.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Microsoft
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

using System;
using Mono.TextEditor;
using MonoDevelop.Ide.Editor.Util;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Ide.Editor.Highlighting;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Text;
using System.Threading;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	class SyntaxHighlightingTest : TestBase
	{
		[Test]
		public void TestMatch ()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source

contexts:
  main:
    - match: \b(foo|bar)\b
      scope: keyword
";
			string test = @"
test foo this bar
^ source
     ^ keyword
         ^ source
              ^ keyword
";
			RunSublimeHighlightingTest (highlighting, test);
		}

		[Test]
		public void TestPushPop ()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source

contexts:
  main:
    - match: \b(foo|bar)\b
      scope: keyword
    - match: '""'
      scope: string.quoted
      push: string

  string:
    - match: '\\.'
      scope: constant.character.escape
    - match: '""'
      pop: true
";
			string test = @"
test foo ""th\tis"" bar test
^ source
     ^ keyword
         ^ string.quoted
            ^ constant.character.escape
                  ^ keyword
                        ^ source
";
			RunSublimeHighlightingTest (highlighting, test);

		}

		[Test]
		public void TestFallbak ()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source

contexts:
  main:
    - match: '""'
      scope: punctuation.definition.string.begin
      push:
        - meta_scope: string.quoted.double
        - match: '""'
          scope: punctuation.definition.string.end
          pop: true
";
			string test = @"
""123"" test
^ punctuation.definition.string.begin
  ^ string.quoted.double
    ^ punctuation.definition.string.end
      ^ source
";
			RunSublimeHighlightingTest (highlighting, test);

		}


		[Test]
		public void TestCaptures ()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source

contexts:
  main:
    - match: ""^\\s*(#)\\s*\\b(region)\\b""
      captures:
        1: meta.preprocessor
        2: keyword.control.region

";
			string test = @"
#region
^ meta.preprocessor
 ^ keyword.control.region
";
			RunSublimeHighlightingTest (highlighting, test);
		}

		[Test]
		public void TestIncludes ()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source

contexts:
  main:
    - match: \b(foo|bar)\b
      scope: keyword
    - include: comments
  comments:
    - match: //
      scope: comment
      push:
        - match: $\n?
          pop: true
";
			string test = @"
test foo // this bar
^ source
     ^ keyword
         ^ comment
              ^ comment
";
			RunSublimeHighlightingTest (highlighting, test);
		}

		[Test]
		public void TestEol ()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source

contexts:
  main:
    - match: \b(foo|bar)\b
      scope: keyword
    - include: comments
  comments:
    - match: //
      scope: comment
      push:
        - match: $\n?
          pop: true
";
			string test = @"
test foo // this
test
^ source
";
			RunSublimeHighlightingTest (highlighting, test);
		}

		[Test]
		public void TestVariables ()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source
variables:
  ident: '[A-Za-z_][A-Za-z_0-9]*'
contexts:
  main:
    - match: '(\b{{ident}}\b)'
      captures:
        1: keyword.control
";
			string test = 
@"test123
^ keyword.control
";
			RunSublimeHighlightingTest (highlighting, test);
		}

		[Test]
		public void TestAdvancedStackUsage ()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source
contexts:
  main:
    - match: \btypedef\b
      scope: keyword.control.c
      set: [typedef_after_typename, typename]
  
  typename:
    - match: \bstruct\b
      set:
        - match: ""{""
          set:
            - match: ""}""
              pop: true
    - match: \b[A-Za-z_][A-Za-z_0-9]*\b
      pop: true
  
  typedef_after_typename:
    - match: \b[A-Za-z_][A-Za-z_0-9]*\b
      scope: entity.name.type
      pop: true
";
			string test = @"
typedef int coordinate_t;
^ keyword.control.c
typedef struct { int x; int y; } point_t;
^ keyword.control.c
                                 ^ entity.name.type
typedef struct
^ keyword.control.c
{
	int x;
	int y;
} point_t;
  ^ entity.name.type
";
			RunSublimeHighlightingTest (highlighting, test);

		}

		internal static void RunSublimeHighlightingTest (string highlightingSrc, string inputText)
		{
			var highlighting = Sublime3Format.ReadHighlighting (new StringReader (highlightingSrc));
			RunHighlightingTest (highlighting, inputText);
		}

		internal static void RunHighlightingTest (SyntaxHighlightingDefinition highlighting, string inputText)
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			var sb = new StringBuilder ();
			int lineNumber = 0;
			var expectedSegments = new List<Tuple<DocumentLocation, string>> ();
			using (var sr = new StringReader (inputText)) {
				while (true) {
					var lineText = sr.ReadLine ();
					if (lineText == null)
						break;
					var idx = lineText.IndexOf ('^');
					if (idx >= 0) {
						expectedSegments.Add (Tuple.Create (new DocumentLocation (lineNumber, idx + 1), lineText.Substring (idx + 1).Trim ()));
					} else {
						lineNumber++;
						sb.AppendLine (lineText);
					}
				}
			}
			editor.Text = sb.ToString ();
			highlighting.PrepareMatches ();
			editor.SyntaxHighlighting = new SyntaxHighlighting (highlighting, editor);

			//var line = editor.GetLine (6); {
			foreach (var line in editor.GetLines ()) {
				var coloredSegments = editor.SyntaxHighlighting.GetHighlightedLineAsync (line, CancellationToken.None).WaitAndGetResult (CancellationToken.None).Segments;
				for (int i = 0; i < expectedSegments.Count; i++) {
					var seg = expectedSegments [i];
					if (seg.Item1.Line == line.LineNumber) {
						var matchedSegment = coloredSegments.FirstOrDefault (s => s.Contains (seg.Item1.Column - 1));
						Assert.NotNull (matchedSegment, "No segment found at : " + seg.Item1);
						foreach (var segi in seg.Item2.Split (new [] { " " }, StringSplitOptions.RemoveEmptyEntries)) {
							Console.WriteLine ("line " + line.LineNumber + " : " + editor.GetTextAt (line));
							Console.WriteLine (segi);
							Console.WriteLine (string.Join (", ", matchedSegment.ScopeStack.ToArray ()));
							string mk = null;
							int d = 0;
							var expr = StackMatchExpression.Parse (segi);
							var matchResult = expr.MatchesStack (matchedSegment.ScopeStack, ref mk);
							Assert.IsTrue (matchResult.Item1, "Wrong color at " + seg.Item1 + " expected " + segi + " was " + string.Join (", ", matchedSegment.ScopeStack.ToArray ()));
						}
						expectedSegments.RemoveAt (i);
						i--;
					}
				}
			}
			Assert.AreEqual (0, expectedSegments.Count, "Not all segments matched.");
		}

		[Test]
		public void TestPOSIXBracketExpressions ()
		{
			Assert.AreEqual ("[\\w\\d]", Sublime3Format.CompileRegex ("[:alnum:]"));
			Assert.AreEqual ("[\\w]", Sublime3Format.CompileRegex ("[:alpha:]"));
			Assert.AreEqual ("[\\t ]", Sublime3Format.CompileRegex ("[:blank:]"));
			Assert.AreEqual ("[\\d]", Sublime3Format.CompileRegex ("[:digit:]"));
			Assert.AreEqual ("[\\S]", Sublime3Format.CompileRegex ("[:graph:]"));
			Assert.AreEqual ("[a-z]", Sublime3Format.CompileRegex ("[:lower:]"));
			Assert.AreEqual ("[\\S ]", Sublime3Format.CompileRegex ("[:print:]"));
			Assert.AreEqual ("[\\s]", Sublime3Format.CompileRegex ("[:space:]"));
			Assert.AreEqual ("[A-Z]", Sublime3Format.CompileRegex ("[:upper:]"));
			Assert.AreEqual ("[\\dA-Fa-f]", Sublime3Format.CompileRegex ("[:xdigit:]"));
		}

		[Test]
		public void TestPosixBracketExpressionsInCharacterClass ()
		{
			Assert.AreEqual ("[\\w\\d_]", Sublime3Format.CompileRegex ("[_[:alnum:]]"));
			Assert.AreEqual ("[\\w_]", Sublime3Format.CompileRegex ("[_[:alpha:]]"));
			Assert.AreEqual ("[\\d_]", Sublime3Format.CompileRegex ("[_[:digit:]]"));
			Assert.AreEqual ("[_a-z]", Sublime3Format.CompileRegex ("[_[:lower:]]"));
			Assert.AreEqual ("[A-Z_]", Sublime3Format.CompileRegex ("[_[:upper:]]"));
			Assert.AreEqual ("[\\dA-F_a-f]", Sublime3Format.CompileRegex ("[_[:xdigit:]]"));
		}

		[Test]
		public void TestNestedCharacterClasses ()
		{
			Assert.AreEqual ("[\\da-z]", Sublime3Format.CompileRegex ("[a-z[0-9]]"));
		}

		[Test]
		public void TestCharConversion ()
		{
			Assert.AreEqual ("[.]", Sublime3Format.CompileRegex ("[.]"));
		}

		[Test]
		public void TestMinusCHar ()
		{
			Assert.AreEqual ("[-:?]", Sublime3Format.CompileRegex ("[?:-]"));
			Assert.AreEqual ("[+-]", Sublime3Format.CompileRegex ("[+-]"));
		}

		[Test]
		public void TestEscapes ()
		{
			Assert.AreEqual ("[\\t]", Sublime3Format.CompileRegex ("[\\t]"));
			Assert.AreEqual ("[,\\[\\]{}]", Sublime3Format.CompileRegex ("[,\\[\\]{},]"));
		}

		[Test]
		public void TestEscapeBug ()
		{
			Assert.AreEqual ("\\[", Sublime3Format.CompileRegex ("\\["));
		}

		[Test]
		public void TestCharacterProperties ()
		{
			Assert.AreEqual ("[0-9a-fA-F]", Sublime3Format.CompileRegex ("\\p{XDigit}"));
		}

		[Test]
		public void TestQuantifierConversion ()
		{
			Assert.AreEqual ("\\w+", Sublime3Format.CompileRegex ("\\w++"));
			Assert.AreEqual ("\\w*", Sublime3Format.CompileRegex ("\\w**"));
			Assert.AreEqual ("[A-Z]*", Sublime3Format.CompileRegex ("[A-Z]*+"));
			Assert.AreEqual ("[A-Z]*", Sublime3Format.CompileRegex ("[A-Z]+*"));
		}

		[Test]
		public void TestCharacterClassBug ()
		{
			Assert.AreEqual ("(<!)(DOCTYPE)\\s+([\\w:_][\\w\\d-.:_]*)", Sublime3Format.CompileRegex ("(<!)(DOCTYPE)\\s+([:a-zA-Z_][:a-zA-Z0-9_.-]*)"));
			Assert.AreEqual ("[\\w\\d-_]+", Sublime3Format.CompileRegex ("[-_a-zA-Z0-9]+"));
			Assert.AreEqual ("\\[(\\\\]|[^\\]])*\\]", Sublime3Format.CompileRegex ("\\[(\\\\]|[^\\]])*\\]"));

			Assert.AreEqual ("[\\p{Lu}]", Sublime3Format.CompileRegex ("[\\p{Lu}]"));
		}

		[Test]
		public void TestComment ()
		{
			Assert.AreEqual ("test ", Sublime3Format.CompileRegex ("test # comment"));
		}

		[Test]
		public void TestGroupReplacement ()
		{
			Assert.AreEqual ("(?<id>[\\w_]*)\\s*\\k<id>", Sublime3Format.CompileRegex ("(?<id>[A-Z_a-z]*)\\s*\\g<id>"));
		}

		[Test]
		public void TestGroupNameCorrection ()
		{
			Assert.AreEqual ("(?<id_id2>[\\w_]*)", Sublime3Format.CompileRegex ("(?<id-id2>[A-Z_a-z]*)"));
		}



		[Test]
		public void TestGroupNameCorrection_Case2 ()
		{
			Assert.AreEqual ("(?<interface_name>\\k<type_name>\\s*\\.\\s*)?", Sublime3Format.CompileRegex ("(?<interface-name>\\g<type-name>\\s*\\.\\s*)?"));
		}

		[Ignore("Fixme")]
		[Test]
		public void TestLookbehindBug ()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source

contexts:
  main:
    - match: '(?=\{)'
      scope: outer.bracket
      push:
        - include: bracket
        - match: '(?<=\>)'
          pop: true
  bracket:
    - match: '\{'
      push:
        - include: bracket
        - match: '\}'
          pop: true
";
			string test = @"
foo
^ source
{ 
  {
    foo  
    ^ outer.bracket
  } 
}
^ outer.bracket
foo
 ^ source
";
			RunSublimeHighlightingTest (highlighting, test);

		}
	}
}
