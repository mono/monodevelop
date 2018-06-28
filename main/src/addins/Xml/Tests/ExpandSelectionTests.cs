//
// ExpandSelectionTests.cs
//
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2018 Microsoft Corp
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

using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Xml.Editor;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests
{
	[TestFixture]
	public class ExpandSelectionTests : TextEditorExtensionTestBase
	{
		public static EditorExtensionTestData XmlContentData = new EditorExtensionTestData (
			fileName: "/a.xml",
			language: "C#",
			mimeType: "application/xml",
			projectFileName: "test.csproj"
		);

		const string Document = @"<!-- this is
a comment-->
<foo hello=""hi"" goodbye=""bye"">
	this is some text
	<bar><baz thing=""done"" />
		<!--another comment-->
	</bar>
</foo>
";
		const string CommentDoc = @"<!-- this is
a comment-->";

		const string ElementFoo = @"<foo hello=""hi"" goodbye=""bye"">";

		const string ElementWithBodyFoo = @"<foo hello=""hi"" goodbye=""bye"">
	this is some text
	<bar><baz thing=""done"" />
		<!--another comment-->
	</bar>
</foo>";

		const string AttributesFoo = @"hello=""hi"" goodbye=""bye""";

		const string AttributeHello = @"hello=""hi""";

		const string AttributeGoodbye = @"goodbye=""bye""";

		const string BodyFoo = @"
	this is some text
	<bar><baz thing=""done"" />
		<!--another comment-->
	</bar>
";
		const string ElementBar = @"<bar>";

		const string ElementWithBodyBar = @"<bar><baz thing=""done"" />
		<!--another comment-->
	</bar>";

		const string BodyBar = @"<baz thing=""done"" />
		<!--another comment-->
	";

		const string ElementBaz= @"<baz thing=""done"" />";

		const string AttributeThing = @"thing=""done""";

		const string CommentBar = @"<!--another comment-->";

		protected override EditorExtensionTestData GetContentData () => XmlContentData;

		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			yield return new XmlTextEditorExtension ();
		}

		//args are line, col, then the expected sequence of expansions
		[Test]
		[TestCase (1, 2, CommentDoc)]
		[TestCase (3, 2, "foo", ElementFoo, ElementWithBodyFoo)]
		[TestCase (3, 3, "foo", ElementFoo, ElementWithBodyFoo)]
		[TestCase (3, 15, "hi", AttributeHello, AttributesFoo, ElementFoo, ElementWithBodyFoo)]
		[TestCase (3, 7, "hello", AttributeHello, AttributesFoo, ElementFoo, ElementWithBodyFoo)]
		[TestCase (4, 7, BodyFoo, ElementWithBodyFoo)]
		[TestCase (5, 22, "done", AttributeThing, ElementBaz, BodyBar, ElementWithBodyBar, BodyFoo, ElementWithBodyFoo)]
		[TestCase (6, 12, CommentBar, BodyBar, ElementWithBodyBar, BodyFoo, ElementWithBodyFoo)]
		public async Task TestExpandShrink (object[] args)
		{
			var loc = new DocumentLocation ((int)args [0], (int)args[1]);
			using (var testCase = await SetupTestCase (Document)) {
				var doc = testCase.Document;
				doc.Editor.SetCaretLocation (loc);
				var ext = doc.GetContent<BaseXmlEditorExtension> ();

				//check initial state 
				Assert.IsFalse (doc.Editor.IsSomethingSelected);
				Assert.AreEqual (loc, doc.Editor.CaretLocation);

				//check expanding causes correct selections
				for (int i = 2; i < args.Length; i++) {
					ext.ExpandSelection ();
					Assert.AreEqual (args [i], doc.Editor.SelectedText);
				}

				//check entire doc is selected
				ext.ExpandSelection ();
				var sel = doc.Editor.SelectionRange;
				Assert.AreEqual (0, sel.Offset);
				Assert.AreEqual (Document.Length, sel.Length);

				//check expanding again does not change it
				ext.ExpandSelection ();
				Assert.AreEqual (0, sel.Offset);
				Assert.AreEqual (Document.Length, sel.Length);

				//check shrinking causes correct selections
				for (int i = args.Length - 1; i >= 2; i--) {
					ext.ShrinkSelection ();
					Assert.AreEqual (args [i], doc.Editor.SelectedText);
				}

				//final shrink back to a caret 
				ext.ShrinkSelection ();
				Assert.IsFalse (doc.Editor.IsSomethingSelected);
				Assert.AreEqual (loc, doc.Editor.CaretLocation);

				//check shrinking again does not change it
				ext.ShrinkSelection ();
				Assert.IsFalse (doc.Editor.IsSomethingSelected);
				Assert.AreEqual (loc, doc.Editor.CaretLocation);
			}
		}
	}
}
