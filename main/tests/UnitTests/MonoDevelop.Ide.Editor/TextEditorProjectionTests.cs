//
// TextEditorProjectionTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
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
using NUnit.Framework;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Projection;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharpBinding;
using UnitTests;
using MonoDevelop.CSharpBinding.Tests;
using System.Linq;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public class TextEditorProjectionTests : TestBase
	{
		[Test]
		public void TestProjectionUpdate ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			editor.Text = "1234567890";

			var projectedDocument = TextEditorFactory.CreateNewDocument (
				new StringTextSource ("__12__34__56__78__90"), 
				"a"
			);

			var segments = new List<ProjectedSegment> ();
			for (int i = 0; i < 5; i++) {
				segments.Add (new ProjectedSegment (i * 2, 2 + i * 4, 2));
			}
			var projection = new Projection.Projection (projectedDocument, segments);
			var tww = new TestWorkbenchWindow ();
			var content = new TestViewContent ();
			tww.ViewContent = content;

			var originalContext = new Document (tww);
			var projectedEditor = projection.CreateProjectedEditor (originalContext);
			editor.SetOrUpdateProjections (originalContext, new [] { projection }, TypeSystem.DisabledProjectionFeatures.All);
			editor.InsertText (1, "foo");
			Assert.AreEqual ("__1foo2__34__56__78__90", projectedEditor.Text);

			Assert.AreEqual (2 , projection.ProjectedSegments.ElementAt (0).ProjectedOffset);
			Assert.AreEqual (2 + "foo".Length, projection.ProjectedSegments.ElementAt (0).Length);
			for (int i = 1; i < 5; i++) {
				Assert.AreEqual (2 + i * 4 + "foo".Length, projection.ProjectedSegments.ElementAt (i).ProjectedOffset);
				Assert.AreEqual (2, projection.ProjectedSegments.ElementAt (i).Length);
			}

		}
	}
}

