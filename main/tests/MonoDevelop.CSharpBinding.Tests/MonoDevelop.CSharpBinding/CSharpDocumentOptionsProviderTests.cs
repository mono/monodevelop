//
// CSharpDocumentOptionsProviderTests.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using NUnit.Framework;
using MonoDevelop.Debugger;
using Mono.Debugging.Client;
using System.Threading;
using MonoDevelop.Ide.Editor;
using MonoDevelop.SourceEditor;
using Gtk;
using MonoDevelop.Projects.Policies;
using MonoDevelop.CSharp.Formatting;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class CSharpDocumentOptionsProviderTests : TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharpWithReferences;

		[Test]
		public async Task TestIssue5925 ()
		{
			using (var testCase = await SetupTestCase ("", 0)) {

				var doc = testCase.Document;
				var policy = new CSharpFormattingPolicy ();
				policy.SpaceAfterMethodCallName = true;
				doc.Project.Policies.Set (policy, "text/x-csharp");
				var option = await doc.AnalysisDocument.GetOptionsAsync ();
				var spaceAfterMethodCall = option.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterMethodCallName);
				Assert.IsTrue (spaceAfterMethodCall);
			}
		}
	}
}
