//
// GlobalUndoTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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

using System.IO;
using NUnit.Framework;
using UnitTests;
using System.Threading;
using GuiUnit;
using System;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	class GlobalUndoTests : IdeTestBase
	{
		[TestFixtureSetUp]
		public void SetUp ()
		{
			if (!IdeApp.IsInitialized) {
				IdeApp.Initialize (new ProgressMonitor ());
			}
		}

		[Test]
		public void TestGlobalUndo ()
		{
			var doc = IdeApp.Workbench.NewDocument ("/undo.cs", "text/x-csharp", @"class Hello {}");
			using (var transaction = new GlobalUndoService ().OpenGlobalUndoTransaction (null, "test")) {
				var mdTransaction = (IMonoDevelopUndoTransaction)transaction;
				mdTransaction.CommitChange (new GlobalUndoService.TextReplaceChange (mdTransaction, "/undo.cs", 0, "class".Length, "struct"));
				doc.Editor.ReplaceText (0, "class".Length, "struct");
				mdTransaction.Commit ();
			}

			GlobalUndoService.Undo (true);
			Assert.AreEqual ("class Hello {}", doc.Editor.Text);
			GlobalUndoService.Reset ();
		}

		[Test]
		public void TestGlobalRedo ()
		{
			var doc = IdeApp.Workbench.NewDocument ("/redo.cs", "text/x-csharp", @"class Hello {}");
			using (var transaction = new GlobalUndoService ().OpenGlobalUndoTransaction (null, "test")) {
				var mdTransaction = (IMonoDevelopUndoTransaction)transaction;
				mdTransaction.CommitChange (new GlobalUndoService.TextReplaceChange (mdTransaction, "/redo.cs", 0, "class".Length, "struct"));
				doc.Editor.ReplaceText (0, "class".Length, "struct");
				mdTransaction.Commit ();
			}

			GlobalUndoService.Undo (true);
			Assert.AreEqual ("class Hello {}", doc.Editor.Text);
			GlobalUndoService.Redo (true);
			GlobalUndoService.Reset ();
		}
	}
}
