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
using MonoDevelop.Ide.Gui.Documents;
using IdeUnitTests;
using MonoDevelop.Ide.Composition;
using UnitTests;
using MonoDevelop.Ide.TextEditing;
using MonoDevelop.Ide.Tasks;

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
		DocumentManager documentManager;

		public Document Document { get; private set; }
		public Project Project => Document.Owner as Project;
		public Solution Solution => (Document.Owner as Project)?.ParentSolution;
		public TestViewContent Content { get; private set; }
		public EditorExtensionTestData TestData { get; private set; }
		bool Wrap { get; set; }

		TextEditorExtensionTestCase ()
		{
		}

		public async static Task<TextEditorExtensionTestCase> Create (TestViewContent content, EditorExtensionTestData data, bool wrap)
		{
			var test = new TextEditorExtensionTestCase ();
			await test.Init (content, data, wrap);
			test.Document = await test.DocumentManager.OpenDocument (content);
			return test;
		}

		async Task Init (TestViewContent content, EditorExtensionTestData data, bool wrap)
		{
			//serviceProvider = ServiceHelper.SetupMockShell ();
			documentManager = await Runtime.GetService<DocumentManager> ();

			Content = content;
			TestData = data;
			Wrap = wrap;
		}

		public DocumentManager DocumentManager {
			get {
				return documentManager;
			}
		}

		public void Dispose ()
		{
			if (Solution != null) {
				using (Solution)
					IdeApp.TypeSystemService.Unload (Solution);
			}
			if (!Wrap)
				Document.Close (true).Ignore ();
		}

		public T GetContent<T> () where T:class => Content.GetContent<T> ();
	}

	[RequireService (typeof (TextEditorService))]
	[RequireService (typeof (TaskService))]
	public abstract class TextEditorExtensionTestBase : IdeTestBase
	{
		protected abstract EditorExtensionTestData GetContentData ();

		protected virtual IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			yield break;
		}

		protected async Task<TextEditorExtensionTestCase> SetupTestCase (string input, int cursorPosition = -1, bool wrap = false)
		{
			await Runtime.GetService<CompositionManager> ();

			var data = GetContentData ();

			var content = new TestViewContent ();
			await content.Initialize (new FileDescriptor (data.FileName, null, null));

			content.Text = input;

			content.Editor.MimeType = data.MimeType;
			if (cursorPosition != -1)
				content.CursorPosition = cursorPosition;

			var project = Services.ProjectService.CreateDotNetProject (data.Language);
			project.Name = Path.GetFileNameWithoutExtension (data.ProjectFileName);
			project.FileName = data.ProjectFileName;
			project.Files.Add (new ProjectFile (content.FilePath, BuildAction.Compile));
			foreach (var reference in data.References)
				project.References.Add (ProjectReference.CreateAssemblyReference (reference));

			var solution = new Solution ();
			solution.AddConfiguration ("", true);
			solution.DefaultSolutionFolder.AddItem (project);

			content.Owner = project;

			using (var monitor = new ProgressMonitor ())
				await IdeApp.TypeSystemService.Load (solution, monitor);

			var testCase = await TextEditorExtensionTestCase.Create (content, data, wrap);
			var doc = testCase.Document;

			foreach (var ext in GetEditorExtensions ()) {
				ext.Initialize (doc.Editor, doc.DocumentContext);
				content.AddContent (ext);
			}
			await doc.DocumentContext.UpdateParseDocument ();
			return testCase;
		}
	}
}
