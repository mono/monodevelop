//
// CodeSnippetTests.cs
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
using UnitTests;
using NUnit.Framework.Internal;
using MonoDevelop.Core.Text;
using System.Text;
using NUnit.Framework;
using System.IO;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Extension;
using System.Threading.Tasks;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public class CodeSnippetTests : IdeTestBase
	{
		//Bug 620177: [Feedback] VS for Mac: code snippet problem
		[Test]
		public async Task TestBug620177 ()
		{
			var snippet = new CodeTemplate ();
			snippet.Code = "class Foo:Test\n{\n    $end$\n}";

			var editor = await RunSnippet (snippet);

			Assert.AreEqual ("class Foo : Test\n{\n\n}", editor.Text);
			Assert.AreEqual ("class Foo : Test\n{\n".Length, editor.CaretOffset);
		}

		static async Task<TextEditor> RunSnippet (CodeTemplate snippet)
		{
			var project = Services.ProjectService.CreateDotNetProject ("C#");
			project.Name = "test";
			project.References.Add (MonoDevelop.Projects.ProjectReference.CreateAssemblyReference ("mscorlib"));
			project.References.Add (MonoDevelop.Projects.ProjectReference.CreateAssemblyReference ("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
			project.References.Add (MonoDevelop.Projects.ProjectReference.CreateAssemblyReference ("System.Core"));

			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile ("/a.cs", BuildAction.Compile));

			var solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true);
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);

			var content = new TestViewContent ();
			content.Project = project;
			content.ContentName = "/a.cs";
			content.Data.MimeType = "text/x-csharp";
			content.Text = "";

			var tww = new TestWorkbenchWindow ();
			tww.ViewContent = content;

			var doc = new TestDocument (tww);
			doc.SetProject (project);

			doc.Editor.IndentationTracker = new TestIndentTracker ("    ");
			doc.Editor.Options = new CustomEditorOptions (doc.Editor.Options) {
				IndentStyle = IndentStyle.Smart,
				RemoveTrailingWhitespaces = true
			};
			await doc.UpdateParseDocument ();
			snippet.Insert (doc);
			TypeSystemService.Unload (solution);
			return doc.Editor;
		}

		class TestIndentTracker : IndentationTracker
		{
			string indentString;
			Dictionary<int, string> definedIndents = new Dictionary<int, string> ();

			public override IndentationTrackerFeatures SupportedFeatures {
				get {
					return IndentationTrackerFeatures.All;
				}
			}

			public TestIndentTracker (string indentString = "\t\t")
			{
				this.indentString = indentString;
			}

			public override string GetIndentationString (int lineNumber)
			{
				if (definedIndents.TryGetValue (lineNumber, out string result))
					return result;
				return indentString;
			}

			public void SetIndent (int lineNumber, string indentationString)
			{
				definedIndents [lineNumber] = indentationString;
			}
		}

	}
}

