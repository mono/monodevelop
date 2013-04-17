//
// CodeIssuePad.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui;
using Xwt;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.Refactoring;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.TypeSystem;
using System.IO;

namespace MonoDevelop.CodeIssues
{
	public class CodeIssuePadControl : VBox
	{
		TreeView view = new TreeView ();
		DataField<string> text = new DataField<string> ();
		DataField<DomRegion> region = new DataField<DomRegion> ();
		Button runButton = new Button ("Run");

		TreeStore store;

		public CodeIssuePadControl ()
		{
			runButton.Clicked += StartAnalyzation;
			PackStart (runButton, BoxMode.None);

			store = new TreeStore (text, region);
			view.DataSource = store;
			view.HeadersVisible = false;

			view.Columns.Add ("Name", text);
			PackStart (view, BoxMode.FillAndExpand);

		}
		

		void StartAnalyzation (object sender, EventArgs e)
		{
			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution == null)
				return;
			runButton.Sensitive = false;
			store.Clear ();
			var rootNode = store.AddNode ();
			rootNode.SetValue (text, "Analyzing...");
			ThreadPool.QueueUserWorkItem (delegate {

				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor ("Analyzing solution", null, false)) {
					int work = 0;
					foreach (var project in solution.GetAllProjects ()) {
						work += project.Files.Count (f => f.BuildAction == BuildAction.Compile);
					}
					monitor.BeginTask ("Analyzing solution", work);
					int allIssues = 0;
					TypeSystemParser parser = null;
					string lastMime = null;
					CodeIssueProvider[] codeIssueProvider = null;
					foreach (var project in solution.GetAllProjects ()) {
						var compilation = TypeSystemService.GetCompilation (project);
						List<CodeIssue> codeIssues = new List<CodeIssue> ();
						Parallel.ForEach (project.Files, file => {
							if (file.BuildAction != BuildAction.Compile)
								return;

							var editor = TextFileProvider.Instance.GetReadOnlyTextEditorData (file.FilePath);

							if (lastMime != editor.MimeType || parser == null)
								parser = TypeSystemService.GetParser (editor.MimeType);
							if (parser == null)
								return;
							var reader = new StreamReader (editor.OpenStream ());
							var document = parser.Parse (true, editor.FileName, reader, project);
							reader.Close ();
							if (document == null) 
								return;

							var resolver = new CSharpAstResolver (compilation, document.GetAst<SyntaxTree> (), document.ParsedFile as CSharpUnresolvedFile);
							var context = document.CreateRefactoringContextWithEditor (editor, resolver, CancellationToken.None);

							if (lastMime != editor.MimeType || codeIssueProvider == null)
								codeIssueProvider = RefactoringService.GetInspectors (editor.MimeType).ToArray ();
							Parallel.ForEach (codeIssueProvider, (provider) => { 
								var severity = provider.GetSeverity ();
								if (severity == Severity.None)
									return;
								try {
									foreach (var r in provider.GetIssues (context, CancellationToken.None)) {
										lock (codeIssues) {
											codeIssues.Add (r);
										}
									}
								} catch (Exception ex) {
									LoggingService.LogError ("Error while running code issue on:"+ editor.FileName, ex);
								}
							});
							lastMime = editor.MimeType;
							monitor.Step (1);
						});
						Application.Invoke (delegate {
							var projectNode = store.AddNode ();
							projectNode.SetValue (text, project.Name + "( " + codeIssues.Count + " issues)");
							foreach (var issue in codeIssues) {
								var child = projectNode.AddChild ();
								child.SetValue (text, issue.Description + " - " + issue.Region);
								child.SetValue (region, issue.Region);
								child.MoveToParent ();
							}
							projectNode.MoveToParent ();
							allIssues += codeIssues.Count;
						});

					}
					Application.Invoke (delegate {
						rootNode.SetValue (text, "Found issues :" + allIssues);
						runButton.Sensitive = true;
					});
					monitor.EndTask ();
				}
			});

		}
	}

	public class CodeIssuePad : AbstractPadContent
	{
		CodeIssuePadControl issueControl;

		public override Gtk.Widget Control {
			get {
				if (issueControl == null)
					issueControl = new CodeIssuePadControl ();
				return (Gtk.Widget)Xwt.Engine.WidgetRegistry.GetNativeWidget (issueControl);
			}
		}
	}
}

