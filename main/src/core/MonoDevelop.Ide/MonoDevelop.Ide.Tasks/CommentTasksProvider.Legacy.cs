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
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.Ide.Tasks
{
	partial class CommentTasksProvider
	{
		internal static class Legacy
		{
			static Dictionary<Project, ProjectCommentTags> projectTags = new Dictionary<Project, ProjectCommentTags> ();
			static CancellationTokenSource src = new CancellationTokenSource ();

			internal static void Initialize ()
			{
				Runtime.ServiceProvider.WhenServiceInitialized<RootWorkspace> (s => {
					s.LastWorkspaceItemClosed += LastWorkspaceItemClosed;
					s.WorkspaceItemUnloaded += OnWorkspaceItemUnloaded;
				});
			}

			public static Dictionary<Project, ProjectCommentTags> ProjectTags => projectTags;

			public static CancellationToken CancellationToken => src.Token;

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
					var mt = IdeServices.DesktopService.GetMimeTypeForUri (x.FilePath);
					// FIXME: Handle all language services.

					// Discard files with known IToDoCommentService implementations
					return mt != "text/x-csharp";
				}).ToArray ();

				Task.Run (async () => {
					try {
						await tags.UpdateAsync (project, files, token);
					} catch (OperationCanceledException) {
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
					sln.SolutionItemAdded += (sender, e) => {
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
		}
	}

	[ExportDocumentControllerExtension (MimeType = "*")]
	class LegacyDocumentExtension: DocumentControllerExtension
	{
		DocumentContext context;

		public override async Task<bool> SupportsController (DocumentController controller)
		{
			if (!await base.SupportsController (controller) || !(controller is FileDocumentController))
				return false;

			if (controller is FileDocumentController file) {
				// C# uses the roslyn-provided todo comments system.
				if (file.MimeType == "text/x-csharp")
					return false;
			}
			var s = controller.GetContent<DocumentContext> () != null;
			return s;
		}

		public override Task Initialize (Properties status)
		{
			context = Controller.GetContent<DocumentContext> ();
			context.DocumentParsed += Context_DocumentParsed;
			return base.Initialize (status);
		}

		public override void Dispose ()
		{
			if (context != null)
				context.DocumentParsed -= Context_DocumentParsed;
			base.Dispose ();
		}

		void Context_DocumentParsed (object sender, EventArgs e)
		{
			var pd = context.ParsedDocument;
			var project = Controller.Owner as Project;
			if (project == null || !(Controller is FileDocumentController fileController))
				return;
			ProjectCommentTags tags;
			if (!CommentTasksProvider.Legacy.ProjectTags.TryGetValue (project, out tags))
				return;
			var token = CommentTasksProvider.Legacy.CancellationToken;
			var file = fileController.FilePath;
			Task.Run (async () => {
				try {
					tags.UpdateTags (project, file, await pd.GetTagCommentsAsync (token).ConfigureAwait (false));
				} catch (OperationCanceledException) {
				}
			});
		}

	}
}
