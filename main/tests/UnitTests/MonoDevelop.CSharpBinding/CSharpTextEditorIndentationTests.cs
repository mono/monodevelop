// 
// CSharpTextEditorIndentationTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using NUnit.Framework;

using MonoDevelop.CSharp.Parser;
using Mono.TextEditor;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.CSharp.Formatting;
using UnitTests;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.CSharpBinding
{

	[TestFixture]
	public class CSharpTextEditorIndentationTests : TestBase
	{
		const string eolMarker = "\n";
		TextEditorData Create (string input)
		{
			var data = new TextEditorData ();
			data.Options.DefaultEolMarker = eolMarker;
			data.Options.IndentStyle = IndentStyle.Smart;
			int idx = input.IndexOf ('$');
			if (idx > 0)
				input = input.Substring (0, idx) + input.Substring (idx + 1);
			data.Text = input;
			if (idx > 0)
				data.Caret.Offset = idx;
			return data;
		}

		DocumentStateTracker<CSharpIndentEngine> CreateTracker (TextEditorData data)
		{
			var policy = PolicyService.InvariantPolicies.Get <CSharpFormattingPolicy> ("text/x-csharp");
			var textStylePolicy = PolicyService.InvariantPolicies.Get <TextStylePolicy> ("text/x-csharp");
			var result = new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (policy, textStylePolicy), data);
			result.UpdateEngine ();
			return result;
		}

		void CheckOutput (TextEditorData data, string output)
		{
			CSharpTextEditorIndentation.FixLineStart (data, CreateTracker (data), data.Caret.Line);
			int idx = output.IndexOf ('$');
			if (idx > 0)
				output = output.Substring (0, idx) + output.Substring (idx + 1);
			if (output != data.Text)
				Console.WriteLine (data.Text.Replace ("\t", "\\t").Replace (" ", "."));
			Assert.AreEqual (output, data.Text);
			Assert.AreEqual (idx, data.Caret.Offset, "Caret offset mismatch.");
		}

		[Test]
		public void TestXmlDocumentContinuation ()
		{
			var data = Create (
				"\t\t///" + eolMarker + 
					"\t\t/// Hello$" + eolMarker +
					"\t\tclass Foo {}"
			);

			MiscActions.InsertNewLine (data);

			CheckOutput (data,
				"\t\t///" + eolMarker +
				"\t\t/// Hello" + eolMarker +
				"\t\t/// $" + eolMarker +
				"\t\tclass Foo {}");
		}

		[Test]
		public void TestXmlDocumentContinuationCase2 ()
		{
			var data = Create ("\t\t///" + eolMarker +
"\t\t/// Hel$lo" + eolMarker +
"\t\tclass Foo {}");
			MiscActions.InsertNewLine (data);

			CheckOutput (data, "\t\t///" + eolMarker +
"\t\t/// Hel" + eolMarker +
"\t\t/// $lo" + eolMarker +
				"\t\tclass Foo {}");
		}

		[Test]
		public void TestMultiLineCommentContinuation ()
		{
			var data = Create ("\t\t/*$" + eolMarker + "\t\tclass Foo {}");
			MiscActions.InsertNewLine (data);

			CheckOutput (data, "\t\t/*" + eolMarker + "\t\t * $" + eolMarker + "\t\tclass Foo {}");
		}

		[Test]
		public void TestMultiLineCommentContinuationCase2 ()
		{
			var data = Create (
				"\t\t/*" + eolMarker +
				"\t\t * Hello$" + eolMarker +
				"\t\tclass Foo {}");
			MiscActions.InsertNewLine (data);
			CheckOutput (data, 
			             "\t\t/*" + eolMarker +
			             "\t\t * Hello" + eolMarker +
			             "\t\t * $" + eolMarker +
			             "\t\tclass Foo {}");
		}

		[Test]
		public void TestMultiLineCommentContinuationCase3 ()
		{
			var data = Create ("\t\t/*" + eolMarker +
			             "\t\t * Hel$lo" + eolMarker +
			             "class Foo {}");
			MiscActions.InsertNewLine (data);

			CheckOutput (data,
			             "\t\t/*" + eolMarker +
			             "\t\t * Hel" + eolMarker +
			             "\t\t * $lo" + eolMarker +
			             "class Foo {}");
		}

		[Test]
		public void TestStringContination ()
		{
			var data = Create ("\t\t\"Hello$ World\"");
			MiscActions.InsertNewLine (data);

			CheckOutput (data, "\t\t\"Hello\" +" + eolMarker + "\t\t\"$World\"");
		}

		/// <summary>
		/// Bug 3214 - Unclosed String causes 'Enter' key to produce appended String line.
		/// </summary>
		[Test]
		public void TestBug3214 ()
		{
			var data = Create ("\"Hello\n\t$");
			MiscActions.InsertNewLine (data);

			CheckOutput (data, "\"Hello\n\t" + eolMarker + "\t$");
		}
	}
}

