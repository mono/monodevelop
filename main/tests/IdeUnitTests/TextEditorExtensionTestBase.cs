//
// TextEditorExtensionTestBase.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide
{
	public class EditorExtensionTestData
	{
		public string FileName { get; }
		public string MimeType { get; }
		public string Language { get; }
		public string ProjectFileName { get; }
		public string [] References { get; private set; }

		public EditorExtensionTestData (string fileName, string language, string mimeType, string projectFileName, string[] references = null)
		{
			FileName = fileName;
			Language = language;
			MimeType = mimeType;
			ProjectFileName = projectFileName;
			References = references ?? new string [0];
		}

		EditorExtensionTestData (EditorExtensionTestData other) :
			this (other.FileName, other.Language, other.MimeType, other.ProjectFileName, other.References)
		{
		}

		public static EditorExtensionTestData CSharp = new EditorExtensionTestData (
			fileName: "/a.cs",
			language: "C#",
			mimeType: "text/x-csharp",
			projectFileName: "test.csproj"
		);

		public static EditorExtensionTestData CSharpWithReferences = CSharp.WithReferences(new string[] {
				"mscorlib",
				"System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
				"System.Core",
			}
		);

		public EditorExtensionTestData WithReferences (string[] references)
		{
			return new EditorExtensionTestData (this) {
				References = references
			};
		}
	}

	public class TextEditorExtensionTestCase : IDisposable
	{
		public Document Document { get; }
		public Project Project => Document.Project;
		public Solution Solution => Document.Project.ParentSolution;
		public TestViewContent Content { get; }
		public TestWorkbenchWindow Window { get; }
		public EditorExtensionTestData TestData { get; }
		bool Wrap { get; }

		public TextEditorExtensionTestCase (Document doc, TestViewContent content, TestWorkbenchWindow window, EditorExtensionTestData data, bool wrap)
		{
			Document = doc;
			Content = content;
			Window = window;
			TestData = data;
			Wrap = wrap;
		}

		public void Dispose ()
		{
			using (var solution = Document.Project?.ParentSolution)
				TypeSystemService.Unload (solution);
			Window.CloseWindowSync ();
			if (!Wrap)
				Document.DisposeDocument ();
		}

		public T GetContent<T> () where T:class => Content.GetContent<T> ();
	}

	public abstract class TextEditorExtensionTestBase : IdeTestBase
	{
		protected abstract EditorExtensionTestData GetContentData ();

		protected virtual IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			yield break;
		}

		protected async Task<TextEditorExtensionTestCase> SetupTestCase (string input, int cursorPosition = -1, bool wrap = false)
		{
			await Composition.CompositionManager.InitializeAsync ();

			var data = GetContentData ();

			var content = new TestViewContent {
				ContentName = data.FileName,
				Text = input,
			};
			content.Data.MimeType = data.MimeType;
			if (cursorPosition != -1)
				content.CursorPosition = cursorPosition;

			var tww = new TestWorkbenchWindow {
				ViewContent = content,
			};

			var project = Services.ProjectService.CreateDotNetProject (data.Language);
			project.Name = Path.GetFileNameWithoutExtension (data.ProjectFileName);
			project.FileName = data.ProjectFileName;
			project.Files.Add (new ProjectFile (content.ContentName, BuildAction.Compile));
			foreach (var reference in data.References)
				project.References.Add (ProjectReference.CreateAssemblyReference (reference));

			var solution = new Solution ();
			solution.AddConfiguration ("", true);
			solution.DefaultSolutionFolder.AddItem (project);

			content.Project = project;

			if (wrap && !IdeApp.IsInitialized)
				IdeApp.Initialize (new ProgressMonitor ());
			Document doc = wrap ? IdeApp.Workbench.WrapDocument (tww) : new Document (tww);

			doc.SetProject (project);

			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);

			foreach (var ext in GetEditorExtensions ()) {
				ext.Initialize (doc.Editor, doc);
				content.Contents.Add (ext);
			}
			await doc.UpdateParseDocument ();
			return new TextEditorExtensionTestCase (doc, content, tww, data, wrap);
		}
	}
}
