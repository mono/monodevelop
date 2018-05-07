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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide
{
	public class ViewContentData
	{
		public string FileName;
		public string MimeType;
		public string Language;
		public string ProjectFileName;

		public static ViewContentData CSharp = new ViewContentData {
			FileName = "/a.cs",
			Language = "C#",
			MimeType = "text/x-csharp",
			ProjectFileName = "test.csproj",
		};
	}

	public abstract class TextEditorExtensionTestBase : IdeTestBase
	{
		protected abstract ViewContentData GetContentData ();

		protected virtual IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			yield break;
		}

		protected async Task<Document> Setup (string input)
		{
			await Composition.CompositionManager.InitializeAsync ();

			var data = GetContentData ();

			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			TestViewContent content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = data.FileName;
			content.Data.MimeType = data.MimeType;

			var doc = new Document (tww);

			var text = input;
			content.Text = text;

			var project = Services.ProjectService.CreateProject (data.Language);
			project.Name = Path.GetFileNameWithoutExtension (data.ProjectFileName);
			project.FileName = data.ProjectFileName;
			project.Files.Add (new ProjectFile (content.ContentName, BuildAction.Compile));
			var solution = new Solution ();
			solution.AddConfiguration ("", true);
			solution.DefaultSolutionFolder.AddItem (project);
			content.Project = project;
			doc.SetProject (project);

			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);

			foreach (var ext in GetEditorExtensions ()) {
				ext.Initialize (doc.Editor, doc);
				content.Contents.Add (ext);
			}

			return doc;
		}
	}
}
