// 
// PathedDocumentTextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Completion;
using System.Linq;
using MonoDevelop.Ide;
using System.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Projects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CSharp
{
	class PathedDocumentTextEditorExtension : TextEditorExtension, IPathedDocument
	{
		static PathedDocumentTextEditorExtension ()
		{
			MonoDevelopWorkspace.GetInsertionPoints = async delegate (TextEditor editor, int offset) {
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc == null || doc.AnalysisDocument == null)
					return new List<InsertionPoint> ();
				var semanticModel = await doc.AnalysisDocument.GetSemanticModelAsync ();
				var declaringType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol> (offset, default(CancellationToken));
				if (declaringType == null)
					return new List<InsertionPoint> ();
				return MonoDevelop.Refactoring.InsertionPointService.GetInsertionPoints (
					editor,
					semanticModel,
					declaringType,
					offset
				);
			};
			MonoDevelopWorkspace.StartRenameSession = async (TextEditor editor, DocumentContext ctx, Core.Text.ITextSourceVersion version, SyntaxToken? token) => {
				var latestDocument = ctx.AnalysisDocument;
				var cancellationToken = default (CancellationToken);
				var latestModel = await latestDocument.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
				var latestRoot = await latestDocument.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
				await Runtime.RunInMainThread (async delegate {
					try {
						var node = latestRoot.FindNode (token.Value.Parent.Span, false, false);
						if (node == null)
							return;
						var info = latestModel.GetSymbolInfo (node);
						var sym = info.Symbol ?? latestModel.GetDeclaredSymbol (node);
						if (sym != null)
							await new MonoDevelop.Refactoring.Rename.RenameRefactoring ().Rename (sym);
					} catch (Exception ex) {
						LoggingService.LogError ("Error while renaming " + token.Value.Parent, ex);
					}
				});
			};
		}

		public override void Dispose ()
		{
			CancelDocumentParsedUpdate ();
			CancelUpdatePathTimeout ();
			CancelUpdatePath ();
			Editor.TextChanging -= Editor_TextChanging;
			DocumentContext.DocumentParsed -= DocumentContext_DocumentParsed; 
			Editor.CaretPositionChanged -= Editor_CaretPositionChanged;
			UntrackStartupProjectChanges ();

			IdeApp.Workspace.FileAddedToProject -= HandleProjectChanged;
			IdeApp.Workspace.FileRemovedFromProject -= HandleProjectChanged;
			IdeApp.Workspace.WorkspaceItemUnloaded -= HandleWorkspaceItemUnloaded;
			IdeApp.Workspace.WorkspaceItemLoaded -= HandleWorkspaceItemLoaded;
			IdeApp.Workspace.ItemAddedToSolution -= HandleProjectChanged;
			IdeApp.Workspace.ActiveConfigurationChanged -= HandleActiveConfigurationChanged;

			if (ext != null) {
				ext.TypeSegmentTreeUpdated -= HandleTypeSegmentTreeUpdated;
				ext = null;
			}

			currentPath = null;
			lastType = null;
			lastMember = null;
			base.Dispose ();
		}

		bool isPathSet;
		CSharpCompletionTextEditorExtension ext;

		List<DotNetProject> ownerProjects = new List<DotNetProject> ();

		public override bool IsValidInContext (DocumentContext context)
		{
			return context.GetContent<CSharpCompletionTextEditorExtension> () != null;
		}

		protected override void Initialize ()
		{
			CurrentPath = new PathEntry[] { new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = null } };
			isPathSet = false;
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

		void CancelUpdatePathTimeout ()
		{
			if (updatePathTimeoutId == 0)
				return;
			GLib.Source.Remove (updatePathTimeoutId);
			updatePathTimeoutId = 0;
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

		void CancelDocumentParsedUpdate ()
		{
			documentParsedCancellationTokenSource.Cancel ();
			documentParsedCancellationTokenSource = new CancellationTokenSource ();
		}

		void SubscribeCaretPositionChange ()
		{
			if (caretPositionChangedSubscribed)
				return;
			caretPositionChangedSubscribed = true;
			Editor.CaretPositionChanged += Editor_CaretPositionChanged;
		}

		void Editor_TextChanging (object sender, EventArgs e)
		{
			if (!caretPositionChangedSubscribed)
				return;
			caretPositionChangedSubscribed = false;
			Editor.CaretPositionChanged -= Editor_CaretPositionChanged;
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

		void HandleTypeSegmentTreeUpdated (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (() => {
				CancelUpdatePathTimeout ();
				updatePathTimeoutId = GLib.Timeout.Add (updatePathTimeout, delegate {
					Update ();
					updatePathTimeoutId = 0;
					return false;
				});
			});
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

		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;
		protected virtual void OnPathChanged (DocumentPathChangedEventArgs e)
		{
			EventHandler<DocumentPathChangedEventArgs> handler = this.PathChanged;
			if (handler != null)
				handler (this, e);
		}

		class DataProvider : DropDownBoxListWindow.IListDataProvider
		{
			readonly PathedDocumentTextEditorExtension ext;
			object tag;
			List<SyntaxNode> memberList = new List<SyntaxNode> ();

			public DataProvider (PathedDocumentTextEditorExtension ext, object tag)
			{
				if (ext == null)
					throw new ArgumentNullException ("ext");
				this.ext = ext;
				this.tag = tag;
				Reset ();
			}

			#region IListDataProvider implementation

			void AddTypeToMemberList (BaseTypeDeclarationSyntax btype)
			{
				var e = btype as EnumDeclarationSyntax;
				if (e !=null){
					foreach (var member in e.Members) {
						memberList.Add (member);
					}
					return;
				}
				var type = btype as TypeDeclarationSyntax;
				foreach (var member in type.Members) {
					if (member is FieldDeclarationSyntax) {
						foreach (var variable in ((FieldDeclarationSyntax)member).Declaration.Variables)
							memberList.Add (variable);
					} else if (member is EventFieldDeclarationSyntax) {
						foreach (var variable in ((EventFieldDeclarationSyntax)member).Declaration.Variables)
							memberList.Add (variable);
					} else {
						memberList.Add (member);
					}
				}
			}

			public void Reset ()
			{
				memberList.Clear ();
				if (tag is SyntaxTree) {
					var unit = tag as SyntaxTree;
					memberList.AddRange (unit.GetRoot ().DescendantNodes ().Where (IsType));
				} else if (tag is AccessorDeclarationSyntax) {
					var acc = (AccessorDeclarationSyntax)tag;
					var parent = (MemberDeclarationSyntax)acc.Parent;
					memberList.AddRange (parent.ChildNodes ().OfType<AccessorDeclarationSyntax> ());
				} else if (tag is SyntaxNode) {
					var entity = (SyntaxNode)tag;
					var type = entity.AncestorsAndSelf ().OfType<BaseTypeDeclarationSyntax> ().FirstOrDefault ();
					if (type != null) {
						AddTypeToMemberList (type);
					}
				}

				memberList.Sort ((x, y) => {
					var result = String.Compare (GetName (x), GetName (y), StringComparison.OrdinalIgnoreCase);
					if (result == 0)
						result = GetTypeParameters (x).CompareTo (GetTypeParameters (y));
					if (result == 0)
						result = GetParameters (x).CompareTo (GetParameters (y));

					// partial methods without body should come last
					if (result == 0 && x is MethodDeclarationSyntax && y is MethodDeclarationSyntax) {
						var mx = x as MethodDeclarationSyntax;
						var my = y as MethodDeclarationSyntax;
						if (mx.Body == null && my.Body != null)
							return 1;
						if (mx.Body != null && my.Body == null)
							return -1;
					}
					return result;
				});
			}

			static int GetTypeParameters (SyntaxNode x)
			{
				return 0; //x.GetChildrenByRole (Roles.TypeParameter).Count ();
			}

			static int GetParameters (SyntaxNode x)
			{
				return 0; // x.GetChildrenByRole (Roles.Parameter).Count ();
			}

			string GetName (SyntaxNode node)
			{
				if (tag is SyntaxTree) {
					var type = node;
					if (type != null) {
						var sb = StringBuilderCache.Allocate ();
						sb.Append (ext.GetEntityMarkup (type));
						while (type.Parent is BaseTypeDeclarationSyntax) {
							sb.Insert (0, ext.GetEntityMarkup (type.Parent) + ".");
							type = type.Parent;
						}
						return StringBuilderCache.ReturnAndFree (sb);
					}
				}
				var accessor = node as AccessorDeclarationSyntax;
				if (accessor != null) {
					if (accessor.Kind () == SyntaxKind.GetAccessorDeclaration)
						return "get";
					if (accessor.Kind () == SyntaxKind.SetAccessorDeclaration)
						return "set";
					if (accessor.Kind () == SyntaxKind.AddAccessorDeclaration)
						return "add";
					if (accessor.Kind () == SyntaxKind.RemoveAccessorDeclaration)
						return "remove";
					return node.ToString ();
				}
				if (node is OperatorDeclarationSyntax)
					return "operator";
				if (node is PropertyDeclarationSyntax)
					return ((PropertyDeclarationSyntax)node).Identifier.ToString ();
				if (node is MethodDeclarationSyntax)
					return ((MethodDeclarationSyntax)node).Identifier.ToString ();
				if (node is ConstructorDeclarationSyntax)
					return ((ConstructorDeclarationSyntax)node).Identifier.ToString ();
				if (node is DestructorDeclarationSyntax)
					return ((DestructorDeclarationSyntax)node).Identifier.ToString ();
				if (node is BaseTypeDeclarationSyntax)
					return ((BaseTypeDeclarationSyntax)node).Identifier.ToString ();

//				if (node is fixeds) {
//					return ((FixedVariableInitializer)node).Name;
//				}
				if (node is VariableDeclaratorSyntax)
					return ((VariableDeclaratorSyntax)node).Identifier.ToString ();
				return node.ToString ();
			}

			public string GetMarkup (int n)
			{
				if (tag is DotNetProject) {
					return GLib.Markup.EscapeText (ext.ownerProjects [n].Name);
				}

				var node = memberList [n];
				if (tag is SyntaxTree) {
					var type = node;
					if (type != null) {
						var sb = StringBuilderCache.Allocate ();
						sb.Append (ext.GetEntityMarkup (type));
						while (type.Parent is BaseTypeDeclarationSyntax) {
							sb.Insert (0, ext.GetEntityMarkup (type.Parent) + ".");
							type = type.Parent;
						}
						return StringBuilderCache.ReturnAndFree (sb);
					}
				}
				return ext.GetEntityMarkup (node);
			}
			
			public Xwt.Drawing.Image GetIcon (int n)
			{
				string icon;
				if (tag is DotNetProject) {
					icon = ext.ownerProjects [n].StockIcon;
				} else {
					var node = memberList [n];
					if (node is MemberDeclarationSyntax) {
						icon = ((MemberDeclarationSyntax)node).GetStockIcon ();
					} else if (node is VariableDeclaratorSyntax) {
						icon = node.Parent.Parent.GetStockIcon ();
					} else {
						icon = node.Parent.GetStockIcon ();
					}
				}
				return ImageService.GetIcon (icon, Gtk.IconSize.Menu);
			}

			public object GetTag (int n)
			{
				if (tag is DotNetProject)
					return ext.ownerProjects [n];
				else
					return memberList [n];
			}

			public void ActivateItem (int n)
			{
				if (tag is DotNetProject) {
					ext.DocumentContext.AttachToProject (ext.ownerProjects [n]);
				} else {
					var node = memberList [n];
					var extEditor = ext.DocumentContext.GetContent<TextEditor> ();
					if (extEditor != null) {
						int offset;
						if (node is OperatorDeclarationSyntax) { 
							offset = Math.Max (1, ((OperatorDeclarationSyntax)node).OperatorToken.SpanStart);
						} else if (node is MemberDeclarationSyntax && !(node is AccessorDeclarationSyntax)) {
							offset = Math.Max (1, ((MemberDeclarationSyntax)node).SpanStart);
						} else {
							offset = node.SpanStart;
						}
						extEditor.SetCaretLocation (extEditor.OffsetToLocation (offset), true);
					}
				}
			}

			public int IconCount {
				get {
					if (tag is DotNetProject)
						return ext.ownerProjects.Count;
					else
						return memberList.Count;
				}
			}

			#endregion

		}

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

		public Control CreatePathWidget (int index)
		{
			PathEntry[] path = CurrentPath;
			if (path == null || index < 0 || index >= path.Length)
				return null;
			var tag = path [index].Tag;
			var window = new DropDownBoxListWindow (tag == null ? (DropDownBoxListWindow.IListDataProvider)new CompilationUnitDataProvider (Editor, DocumentContext) : new DataProvider (this, tag));
			window.FixedRowHeight = 22;
			window.MaxVisibleRows = 14;
			window.SelectItem (path [index].Tag);
			return window;
		}

		PathEntry[] currentPath;

		public PathEntry[] CurrentPath {
			get {
				return currentPath;
			}
			private set {
				currentPath = value;
				isPathSet = true;
			}
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

		void ClearPath ()
		{
			var prev = CurrentPath;
			CurrentPath = new PathEntry[0];
			OnPathChanged (new DocumentPathChangedEventArgs (prev));	
		}

		SyntaxNode lastType;
		string lastTypeMarkup;
		SyntaxNode lastMember;
		string lastMemberMarkup;
		MonoDevelop.Projects.Project lastProject;
		AstAmbience? amb;
		CancellationTokenSource src = new CancellationTokenSource ();
		bool caretPositionChangedSubscribed;
		uint updatePathTimeoutId;
		uint updatePathTimeout = 147;

		string GetEntityMarkup (SyntaxNode node)
		{
			if (amb == null || node == null)
				return "";
			return amb.Value.GetEntityMarkup (node);
		}


		void Editor_CaretPositionChanged (object sender, EventArgs e)
		{
			CancelUpdatePathTimeout ();
			Update ();
		}

		void Update()
		{
			if (DocumentContext == null)
				return;
			var parsedDocument = DocumentContext.ParsedDocument;
			if (parsedDocument == null)
				return;
			var caretOffset = Editor.CaretOffset;
			var model = parsedDocument.GetAst<SemanticModel>();
			if (model == null)
				return;
			CancelUpdatePath ();
			var cancellationToken = src.Token;
			amb = new AstAmbience(TypeSystemService.Workspace.Options);
			var loc = Editor.CaretLocation;
			Task.Run(async delegate {
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
					Console.WriteLine (ex);
					return;
				}

				var curMember = node != null ? node.AncestorsAndSelf ().FirstOrDefault (m => m is VariableDeclaratorSyntax && m.Parent != null && !(m.Parent.Parent is LocalDeclarationStatementSyntax) || (m is MemberDeclarationSyntax && !(m is NamespaceDeclarationSyntax))) : null;
				var curType = node != null ? node.AncestorsAndSelf ().FirstOrDefault (IsType) : null;

				var curProject = ownerProjects != null && ownerProjects.Count > 1 ? DocumentContext.Project : null;

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

				var regionEntry = await GetRegionEntry (DocumentContext.ParsedDocument, loc).ConfigureAwait (false);

				Gtk.Application.Invoke ((o, args) => {
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

					if (regionEntry != null)
						result.Add(regionEntry);

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
			});
		}

		static bool IsType (SyntaxNode m)
		{
			return m is BaseTypeDeclarationSyntax || m is DelegateDeclarationSyntax;
		}

		void CancelUpdatePath ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}
		#endregion

	}
}
