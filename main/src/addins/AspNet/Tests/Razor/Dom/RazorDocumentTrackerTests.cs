//
// RazorDocumentTrackerTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.AspNet.Razor.Dom;
using MonoDevelop.AspNet.Razor.Parser;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Xml.Parser;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.AspNet.Tests.Razor.Dom
{
	[TestFixture]
	public class RazorDocumentTrackerTests : TestBase
	{
		Func<ITextDocument> originalGetActiveDocument;
		TextEditor editor;

		[SetUp]
		public void SetUp ()
		{
			originalGetActiveDocument = RazorWorkbenchService.GetActiveDocument;
			editor = TextEditorFactory.CreateNewEditor ();
			editor.MimeType = "text/x-cshtml";
			RazorWorkbenchService.GetActiveDocument = () => {
				return editor;
			};
		}

		[TearDown]
		public override void TearDown ()
		{
			RazorWorkbenchService.GetActiveDocument = originalGetActiveDocument;
			base.TearDown ();
		}

		[Test]
		public void StateShouldBeRazorRootStateAfterCodeBlock ()
		{
			editor.Text = 
@"@{
}

";
			var parser = new XmlParser (new RazorRootState (), false);
			var tracker = new DocumentStateTracker<XmlParser> (parser, editor);
			editor.CaretLine = 3;
			tracker.UpdateEngine ();

			Assert.IsInstanceOf<RazorRootState> (tracker.Engine.CurrentState);
		}
	}
}

