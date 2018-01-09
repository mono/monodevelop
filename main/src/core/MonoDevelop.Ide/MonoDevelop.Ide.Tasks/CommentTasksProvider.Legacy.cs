//
// CommentTasksProvider.Legacy.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft, Inc
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Tasks
{
	static partial class CommentTasksProvider
	{
		internal static class Legacy
		{
			internal static void Initialize ()
			{
				IdeApp.Workspace.LastWorkspaceItemClosed += LastWorkspaceItemClosed;
				IdeApp.Workspace.WorkspaceItemUnloaded += OnWorkspaceItemUnloaded;
				IdeApp.Workbench.DocumentOpened += WorkbenchDocumentOpened;
				IdeApp.Workbench.DocumentClosed += WorkbenchDocumentClosed;
			}

			static void LastWorkspaceItemClosed (object sender, EventArgs e)
			{
				projectTags.Clear ();
			}

			static void OnWorkspaceItemUnloaded (object sender, WorkspaceItemEventArgs e)
			{
				if (e.Item is Solution solution) {
					foreach (var proj in solution.GetAllProjects ())
						projectTags.Remove (proj);
				}
			}

			static Dictionary<Project, ProjectCommentTags> projectTags = new Dictionary<Project, ProjectCommentTags> ();
			static CancellationTokenSource src = new CancellationTokenSource ();
			static void UpdateCommentTagsForProject (Project project, CancellationToken token)
			{
				if (token.IsCancellationRequested)
					return;
				ProjectCommentTags tags;
				if (!projectTags.TryGetValue (project, out tags)) {
					tags = new ProjectCommentTags ();
					projectTags [project] = tags;
				}

				var files = project.Files.Where (x => {
					var mt = DesktopService.GetMimeTypeForUri (x.FilePath);
					// FIXME: Handle all language services.

					// Discard files with known IToDoCommentService implementations
					return mt != "text/x-csharp";
				}).ToArray ();

				Task.Run (async () => {
					try {
						await tags.UpdateAsync (project, files, token);
					} catch (TaskCanceledException) {
					} catch (AggregateException ae) {
						ae.Flatten ().Handle (x => x is TaskCanceledException);
					} catch (Exception e) {
						LoggingService.LogError ("Error while updating comment tags.", e);
					}
				});
			}

			public static string MimeTypeToLanguage (string mimeType)
			{
				switch (mimeType) {
				case "text/x-csharp":
					return Microsoft.CodeAnalysis.LanguageNames.CSharp;
				}
				return null;
			}

			internal static void LoadSolutionContents (Solution sln)
			{
				src.Cancel ();
				src = new CancellationTokenSource ();
				var token = src.Token;

				Task.Run (delegate {
					sln.SolutionItemAdded += delegate (object sender, SolutionItemChangeEventArgs e) {
						var newProject = e.SolutionItem as Project;
						if (newProject == null)
							return;
						UpdateCommentTagsForProject (newProject, token);
					};

					// Load all tags that are stored in pidb files
					foreach (Project p in sln.GetAllProjects ()) {
						UpdateCommentTagsForProject (p, token);
					}
				});
			}

			static void WorkbenchDocumentClosed (object sender, DocumentEventArgs e)
			{
				e.Document.DocumentParsed -= HandleDocumentParsed;
			}

			static void WorkbenchDocumentOpened (object sender, DocumentEventArgs e)
			{
				e.Document.DocumentParsed += HandleDocumentParsed;
			}

			static void HandleDocumentParsed (object sender, EventArgs e)
			{
				var doc = (Document)sender;

				// C# uses the roslyn-provided todo comments system.
				if (doc.Editor.MimeType == "text/x-csharp")
					return;

				var pd = doc.ParsedDocument;
				var project = doc.Project;
				if (pd == null || project == null)
					return;
				ProjectCommentTags tags;
				if (!projectTags.TryGetValue (project, out tags))
					return;
				var token = src.Token;
				var file = doc.FileName;
				Task.Run (async () => {
					try {
						tags.UpdateTags (project, file, await pd.GetTagCommentsAsync (token).ConfigureAwait (false));
					} catch (TaskCanceledException) {
					} catch (AggregateException ae) {
						ae.Flatten ().Handle (x => x is TaskCanceledException);
					}
				});
			}
		}
	}
}
