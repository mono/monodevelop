//
// Copyright (c) Microsoft. All rights reserved.
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.TextEditor;

namespace MonoDevelop.CSharp
{
	sealed partial class CSharpPathedDocumentExtension : IPathedDocument, IDisposable
	{
		readonly ITextView textView;
		readonly Microsoft.VisualStudio.Text.Operations.IEditorOperations editorOperations;
		readonly List<DotNetProject> ownerProjects = new List<DotNetProject> ();
		bool disposed;

		public CSharpPathedDocumentExtension (ITextView view, Microsoft.VisualStudio.Text.Operations.IEditorOperations editorOperations)
		{
			textView = view;
			this.editorOperations = editorOperations;

			//FIXME track the active owner project(s)
			var document = view.TryGetParentDocument ();
			var project = document?.Project;
			if (project is DotNetProject dnp) {
				ownerProjects.Add (dnp);
			}

			CurrentPath = new PathEntry [] { new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = null } };

			view.Caret.PositionChanged += CaretPositionChanged;
			view.TextBuffer.Changed += TextBufferChanged;

			//FIXME: this currently doesn't work as it gets initialized before roslyn is informed about the document being opened
			var roslynDocument = view.TextBuffer.CurrentSnapshot.AsText ().GetOpenDocumentInCurrentContextWithChanges ();
			if (roslynDocument != null) {
				Update (roslynDocument, textView.Caret.Position.BufferPosition);
			}
		}

		public void Dispose ()
		{
			if (disposed) {
				return;
			}
			disposed = true;

			textView.Caret.PositionChanged -= CaretPositionChanged;
			textView.TextBuffer.Changed -= TextBufferChanged;
		}

		void TextBufferChanged (object sender, TextContentChangedEventArgs e)
		{
			Update (textView.Caret.Position);
		}

		void CaretPositionChanged (object sender, CaretPositionChangedEventArgs e)
		{
			Update (e.NewPosition);
		}

		ITextSnapshot lastSnapshot;
		int lastOffset;

		void Update (CaretPosition position)
		{
			var snapshot = position.BufferPosition.Snapshot;
			int offset = position.BufferPosition.Position;

			if (lastSnapshot == snapshot && lastOffset == offset) {
				return;
			}
			lastSnapshot = snapshot;
			lastOffset = offset;

			var roslynDocument = snapshot.AsText ().GetOpenDocumentInCurrentContextWithChanges ();
			if (roslynDocument != null) {
				Update (roslynDocument, position.BufferPosition.Position);
			}
		}

		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;

		void OnPathChanged (DocumentPathChangedEventArgs e) => PathChanged?.Invoke (this, e);

		public Control CreatePathWidget (int index)
		{
			PathEntry [] path = CurrentPath;
			if (path == null || index < 0 || index >= path.Length)
				return null;
			var tag = path [index].Tag;
			//FIXME
			//var window = new DropDownBoxListWindow (tag == null ? (DropDownBoxListWindow.IListDataProvider)new CompilationUnitDataProvider (Editor, DocumentContext) : new DataProvider (this, tag));
			var window = new DropDownBoxListWindow (new DataProvider (this, tag) {
				EditorOperations = editorOperations
			}) {
				FixedRowHeight = 22,
				MaxVisibleRows = 14
			};
			window.SelectItem (path [index].Tag);
			return window;
		}

		PathEntry [] currentPath;
		bool isPathSet;

		public PathEntry [] CurrentPath {
			get {
				return currentPath;
			}
			private set {
				currentPath = value;
				isPathSet = true;
			}
		}

		AstAmbience? amb;

		string GetEntityMarkup (SyntaxNode node)
		{
			if (amb == null || node == null)
				return "";
			return amb.Value.GetEntityMarkup (node);
		}

		static bool IsType (SyntaxNode m)
		{
			return m is BaseTypeDeclarationSyntax || m is DelegateDeclarationSyntax;
		}

		/*
		bool isPathSet;
		CSharpCompletionTextEditorExtension ext;

		List<DotNetProject> ownerProjects = new List<DotNetProject> ();

		protected override void Initialize ()
		{
			// Delay the execution of UpdateOwnerProjects since it may end calling DocumentContext.AttachToProject,
			// which shouldn't be called while the extension chain is being initialized.
			Gtk.Application.Invoke ((o, args) => {
				UpdateOwnerProjects ();
				Editor_CaretPositionChanged (null, null);
			});

			Editor.TextChanging += Editor_TextChanging;
			DocumentContext.DocumentParsed += DocumentContext_DocumentParsed; 
			ext = DocumentContext.GetContent<CSharpCompletionTextEditorExtension> ();
			ext.TypeSegmentTreeUpdated += HandleTypeSegmentTreeUpdated;

			IdeApp.Workspace.FileAddedToProject += HandleProjectChanged;
			IdeApp.Workspace.FileRemovedFromProject += HandleProjectChanged;
			IdeApp.Workspace.WorkspaceItemUnloaded += HandleWorkspaceItemUnloaded;
			IdeApp.Workspace.WorkspaceItemLoaded += HandleWorkspaceItemLoaded;
			IdeApp.Workspace.ItemAddedToSolution += HandleProjectChanged;
			IdeApp.Workspace.ActiveConfigurationChanged += HandleActiveConfigurationChanged;
			SubscribeCaretPositionChange ();
		}

		

		CancellationTokenSource documentParsedCancellationTokenSource = new CancellationTokenSource ();

		void DocumentContext_DocumentParsed (object sender, EventArgs e)
		{
			SubscribeCaretPositionChange ();

			// Fixes a potential memory leak see: https://bugzilla.xamarin.com/show_bug.cgi?id=38041
			if (ownerProjects?.Count > 1) {
				var currentOwners = ownerProjects.Where (p => p != DocumentContext.Project).Select (TypeSystemService.GetCodeAnalysisProject).ToList ();
				CancelDocumentParsedUpdate ();
				var token = documentParsedCancellationTokenSource.Token;
				Task.Run (async delegate {
					foreach (var otherProject in currentOwners) {
						if (otherProject == null)
							continue;
						await otherProject.GetCompilationAsync (token).ConfigureAwait (false);
					}
				});
			}
		}

		void HandleActiveConfigurationChanged (object sender, EventArgs e)
		{
			// If the current configuration changes and the project to which this document is bound is disabled in the
			// new configuration, try to find another project
			if (DocumentContext.Project != null && DocumentContext.Project.ParentSolution == IdeApp.ProjectOperations.CurrentSelectedSolution) {
				var conf = DocumentContext.Project.ParentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
				if (conf != null && !conf.BuildEnabledForItem (DocumentContext.Project))
					ResetOwnerProject ();
			}
		}

		void HandleWorkspaceItemLoaded (object sender, WorkspaceItemEventArgs e)
		{
			if (ownerProjects != null)
				return;
			UpdateOwnerProjects (e.Item.GetAllItems<DotNetProject> ());
		}

		void HandleWorkspaceItemUnloaded (object sender, WorkspaceItemEventArgs e)
		{
			if (ownerProjects == null)
				return;
			foreach (var p in e.Item.GetAllItems<DotNetProject> ()) {
				RemoveOwnerProject (p);
			}
			if (ownerProjects.Count == 0) {
				ownerProjects = null;
				DocumentContext.AttachToProject (null);
			}
		}

		void HandleProjectChanged (object sender, EventArgs e)
		{
			UpdateOwnerProjects ();
			Editor_CaretPositionChanged (null, null);
		}

		void UpdateOwnerProjects (IEnumerable<DotNetProject> allProjects)
		{
			Editor.RunWhenRealized (() => {
				if (DocumentContext == null) {
					return;//This can happen if this object is disposed
				}
				var projects = new HashSet<DotNetProject> (allProjects.Where (p => p.IsFileInProject (DocumentContext.Name)));
				if (ownerProjects == null || !projects.SetEquals (ownerProjects)) {
					SetOwnerProjects (projects.OrderBy (p => p.Name).ToList ());
					var dnp = DocumentContext.Project as DotNetProject;
					if (ownerProjects.Count > 0 && (dnp == null || !ownerProjects.Contains (dnp))) {
						// If the project for the document is not a DotNetProject but there is a project containing this file
						// in the current solution, then use that project
						var pp = DocumentContext.Project != null ? FindBestDefaultProject (DocumentContext.Project.ParentSolution) : null;
						if (pp != null)
							DocumentContext.AttachToProject (pp);
					}
				}
			});
		}

		void UpdateOwnerProjects ()
		{
			UpdateOwnerProjects (IdeApp.Workspace.GetAllItems<DotNetProject> ());
			if (DocumentContext != null && DocumentContext.Project == null)
				ResetOwnerProject ();
		}

		void ResetOwnerProject ()
		{
			if (ownerProjects == null)
				return;
			if (ownerProjects.Count > 0)
				DocumentContext.AttachToProject (FindBestDefaultProject ());
		}

		DotNetProject FindBestDefaultProject (Projects.Solution solution = null)
		{
			// The best candidate to be selected as default project for this document is the startup project.
			// If the startup project is not an owner, pick any project that is not disabled in the current configuration.
			DotNetProject best = null;
			if (solution == null)
				solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			foreach (var p in ownerProjects) {
				if (p.ParentSolution != solution)
					continue;
				var solConf = p.ParentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
				if (solConf == null || !solConf.BuildEnabledForItem (p))
					continue;
				if (p == p.ParentSolution.StartupItem)
					return p;
				if (best == null)
					best = p;
			}
			return best ?? ownerProjects.FirstOrDefault (pr => pr.ParentSolution == solution) ?? ownerProjects.FirstOrDefault ();
		}

		void SetOwnerProjects (List<DotNetProject> projects)
		{
			UntrackStartupProjectChanges ();
			ownerProjects = projects;
			TrackStartupProjectChanges ();
		}

		void RemoveOwnerProject (DotNetProject project)
		{
			UntrackStartupProjectChanges ();
			ownerProjects.Remove (project);
			TrackStartupProjectChanges ();
		}

		void TrackStartupProjectChanges ()
		{
			if (ownerProjects != null) {
				foreach (var sol in ownerProjects.Select (p => p.ParentSolution).Distinct ())
					sol.StartupItemChanged += HandleStartupProjectChanged;
			}
		}

		void UntrackStartupProjectChanges ()
		{
			if (ownerProjects != null) {
				foreach (var sol in ownerProjects.Select (p => p.ParentSolution).Distinct ()) {
					if (sol != null)
						sol.StartupItemChanged -= HandleStartupProjectChanged;
				}
			}
		}

		void HandleStartupProjectChanged (object sender, EventArgs e)
		{
			// If the startup project changes, and the new startup project is an owner of this document,
			// then attach the document to that project

			var sol = (Projects.Solution) sender;
			var p = sol.StartupItem as DotNetProject;
			if (p != null && ownerProjects.Contains (p))
				DocumentContext?.AttachToProject (p);
		}

		#region IPathedDocument implementation

		class CompilationUnitDataProvider : DropDownBoxListWindow.IListDataProvider
		{
			TextEditor editor;

			DocumentContext DocumentContext {
				get;
				set;
			}

			public CompilationUnitDataProvider (TextEditor editor, DocumentContext documentContext)
			{
				this.editor = editor;
				this.DocumentContext = documentContext;
			}

			#region IListDataProvider implementation

			public void Reset ()
			{
			}

			public string GetMarkup (int n)
			{
				return GLib.Markup.EscapeText (DocumentContext.ParsedDocument.GetUserRegionsAsync().Result.ElementAt (n).Name);
			}
			
			internal static Xwt.Drawing.Image Pixbuf {
				get {
					return ImageService.GetIcon (Ide.Gui.Stock.Region, Gtk.IconSize.Menu);
				}
			}
			
			public Xwt.Drawing.Image GetIcon (int n)
			{
				return Pixbuf;
			}

			public object GetTag (int n)
			{
				return DocumentContext.ParsedDocument.GetUserRegionsAsync().Result.ElementAt (n);
			}

			public void ActivateItem (int n)
			{
				var reg = DocumentContext.ParsedDocument.GetUserRegionsAsync().Result.ElementAt (n);
				var extEditor = editor;
				if (extEditor != null) {
					extEditor.SetCaretLocation(Math.Max (1, reg.Region.BeginLine), reg.Region.BeginColumn, true);
				}
			}

			public int IconCount {
				get {
					if (DocumentContext.ParsedDocument == null)
						return 0;
					return DocumentContext.ParsedDocument.GetUserRegionsAsync().Result.Count ();
				}
			}

			#endregion

		}

		async static Task<PathEntry> GetRegionEntry (ParsedDocument unit, DocumentLocation loc)
		{
			PathEntry entry;
			FoldingRegion reg = null;
			try {
				if (unit != null) {
					var regions = await unit.GetUserRegionsAsync ().ConfigureAwait (false);
					if (!regions.Any ())
						return null;
					reg = regions.LastOrDefault (r => r.Region.Contains (loc));
				}
			} catch (AggregateException) {
				return null;
			} catch (OperationCanceledException) {
				return null;
			}
			if (reg == null) {
				entry = new PathEntry (GettextCatalog.GetString ("No region"));
			} else {
				var pixbuf = await Runtime.RunInMainThread (() => CompilationUnitDataProvider.Pixbuf).ConfigureAwait (false);
				entry = new PathEntry (pixbuf, GLib.Markup.EscapeText (reg.Name));
			}
			entry.Position = EntryPosition.Right;
			return entry;
		}

		bool caretPositionChangedSubscribed;
		*/

		Projects.Project lastProject;
		SyntaxNode lastType;
		string lastTypeMarkup;
		SyntaxNode lastMember;
		string lastMemberMarkup;

		CancellationTokenSource src = new CancellationTokenSource ();

		async void Update(Document document, int caretOffset)
		{
			var oldToken = src.Token;
			var model = await document.GetSemanticModelAsync (oldToken).ConfigureAwait (false);
			if (model == null || oldToken.IsCancellationRequested)
				return;

			CancelUpdatePath ();
			var cancellationToken = src.Token;

			amb = new AstAmbience(TypeSystemService.Workspace.Options);

			var unit = model.SyntaxTree;
			SyntaxNode root;
			SyntaxNode node;
			try {
				root = await unit.GetRootAsync(cancellationToken).ConfigureAwait(false);
				if (root.FullSpan.Length <= caretOffset) {
					var prevPath = CurrentPath;
					CurrentPath = new PathEntry [] { new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = null } };
					isPathSet = false;
					await Runtime.RunInMainThread (delegate {
						OnPathChanged (new DocumentPathChangedEventArgs (prevPath));
					});
					return;
				}
				node = root.FindNode(TextSpan.FromBounds(caretOffset, caretOffset));
				if (node.SpanStart != caretOffset)
					node = root.SyntaxTree.FindTokenOnLeftOfPosition(caretOffset, cancellationToken).Parent;
			} catch (Exception ex ) {
				LoggingService.LogError ("Error updating C# breadcrumbs", ex);
				return;
			}

			var curMember = node?.AncestorsAndSelf ().FirstOrDefault (m => m is VariableDeclaratorSyntax && m.Parent != null && !(m.Parent.Parent is LocalDeclarationStatementSyntax) || (m is MemberDeclarationSyntax && !(m is NamespaceDeclarationSyntax)));
			var curType = node != null ? node.AncestorsAndSelf ().FirstOrDefault (IsType) : null;

			var curProject = ownerProjects != null && ownerProjects.Count > 1 ? ownerProjects[0] : null;
			if (curType == curMember || curType is DelegateDeclarationSyntax)
				curMember = null;
			if (isPathSet && curType == lastType && curMember == lastMember && curProject == lastProject) {
				return;
			}
			var curTypeMakeup = GetEntityMarkup(curType);
			var curMemberMarkup = GetEntityMarkup(curMember);
			if (isPathSet && curType != null && lastType != null && curTypeMakeup == lastTypeMarkup &&
				curMember != null && lastMember != null && curMemberMarkup == lastMemberMarkup && curProject == lastProject) {
				return;
			}

//			var regionEntry = await GetRegionEntry (DocumentContext.ParsedDocument, loc).ConfigureAwait (false);

			await Runtime.RunInMainThread (() => {
				var result = new List<PathEntry>();

				if (curProject != null) {
					// Current project if there is more than one 
					result.Add (new PathEntry (ImageService.GetIcon (curProject.StockIcon, Gtk.IconSize.Menu), GLib.Markup.EscapeText (curProject.Name)) { Tag = curProject });
				}

				if (curType == null) {
					if (CurrentPath != null && CurrentPath.Length == 1 && CurrentPath [0]?.Tag is CSharpSyntaxTree)
						return;
					if (CurrentPath != null && CurrentPath.Length == 2 && CurrentPath [1]?.Tag is CSharpSyntaxTree)
						return;
					var prevPath = CurrentPath;
					result.Add (new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit });
					if (cancellationToken.IsCancellationRequested)
						return;

					CurrentPath = result.ToArray ();
					lastType = curType;
					lastTypeMarkup = curTypeMakeup;

					lastMember = curMember;
					lastMemberMarkup = curMemberMarkup;

					lastProject = curProject;
					OnPathChanged (new DocumentPathChangedEventArgs (prevPath));
					return;
				}

				if (curType != null) {
					var type = curType;
					var pos = result.Count;
					while (type != null) {
						if (!(type is BaseTypeDeclarationSyntax))
							break;
						var tag = (object)type.Ancestors ().FirstOrDefault (IsType) ?? unit;
						result.Insert (pos, new PathEntry (ImageService.GetIcon (type.GetStockIcon (), Gtk.IconSize.Menu), GetEntityMarkup (type)) { Tag = tag });
						type = type.Parent;
					}
				}
				if (curMember != null) {
					result.Add (new PathEntry (ImageService.GetIcon (curMember.GetStockIcon (), Gtk.IconSize.Menu), curMemberMarkup) { Tag = curMember });
					if (curMember.Kind () == SyntaxKind.GetAccessorDeclaration ||
					curMember.Kind () == SyntaxKind.SetAccessorDeclaration ||
					curMember.Kind () == SyntaxKind.AddAccessorDeclaration ||
					curMember.Kind () == SyntaxKind.RemoveAccessorDeclaration) {
						var parent = curMember.Parent;
						if (parent != null)
							result.Insert (result.Count - 1, new PathEntry (ImageService.GetIcon (parent.GetStockIcon (), Gtk.IconSize.Menu), GetEntityMarkup (parent)) { Tag = parent });
					}
				}

//				if (regionEntry != null)
//					result.Add(regionEntry);

				PathEntry noSelection = null;
				if (curType == null) {
					noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit };
				} else if (curMember == null && !(curType is DelegateDeclarationSyntax)) {
					noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = curType };
				}

				if (noSelection != null)
					result.Add(noSelection);
				var prev = CurrentPath;
				if (prev != null && prev.Length == result.Count) {
					bool equals = true;
					for (int i = 0; i < prev.Length; i++) {
						if (prev [i].Markup != result [i].Markup) {
							equals = false;
							break;
						}
					}
					if (equals)
						return;
				}
				if (cancellationToken.IsCancellationRequested)
					return;
				CurrentPath = result.ToArray();
				lastType = curType;
				lastTypeMarkup = curTypeMakeup;

				lastMember = curMember;
				lastMemberMarkup = curMemberMarkup;

				lastProject = curProject;

				OnPathChanged (new DocumentPathChangedEventArgs(prev));
			});
		}

		void CancelUpdatePath ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}
	}
}
