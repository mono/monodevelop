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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Completion;
using System.Linq;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.CSharp;
using System.Text;
using MonoDevelop.Projects;

namespace MonoDevelop.CSharp
{
	class PathedDocumentTextEditorExtension : TextEditorExtension, IPathedDocument
	{
		public override void Dispose ()
		{
			UntrackStartupProjectChanges ();

			IdeApp.Workspace.FileAddedToProject -= HandleProjectChanged;
			IdeApp.Workspace.FileRemovedFromProject -= HandleProjectChanged;
			IdeApp.Workspace.WorkspaceItemUnloaded -= HandleWorkspaceItemUnloaded;
			IdeApp.Workspace.WorkspaceItemLoaded -= HandleWorkspaceItemLoaded;
			IdeApp.Workspace.ActiveConfigurationChanged -= HandleActiveConfigurationChanged;

			if (caret != null) {
				caret.PositionChanged -= UpdatePath;
				caret = null;
			}
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
		Mono.TextEditor.Caret caret;
		CSharpCompletionTextEditorExtension ext;

		List<DotNetProject> ownerProjects = new List<DotNetProject> ();

		public override void Initialize ()
		{
			CurrentPath = new PathEntry[] { new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = null } };
			isPathSet = false;
			// Delay the execution of UpdateOwnerProjects since it may end calling Document.AttachToProject,
			// which shouldn't be called while the extension chain is being initialized.
			Gtk.Application.Invoke (delegate {
				UpdateOwnerProjects ();
				UpdatePath (null, null);
			});
			caret = Document.Editor.Caret;
			caret.PositionChanged += UpdatePath;
			ext = Document.GetContent<CSharpCompletionTextEditorExtension> ();
			ext.TypeSegmentTreeUpdated += HandleTypeSegmentTreeUpdated;
			IdeApp.Workspace.FileAddedToProject += HandleProjectChanged;
			IdeApp.Workspace.FileRemovedFromProject += HandleProjectChanged;
			IdeApp.Workspace.WorkspaceItemUnloaded += HandleWorkspaceItemUnloaded;
			IdeApp.Workspace.WorkspaceItemLoaded += HandleWorkspaceItemLoaded;
			IdeApp.Workspace.ActiveConfigurationChanged += HandleActiveConfigurationChanged;
		}

		void HandleActiveConfigurationChanged (object sender, EventArgs e)
		{
			// If the current configuration changes and the project to which this document is bound is disabled in the
			// new configuration, try to find another project
			if (Document.Project != null && Document.Project.ParentSolution == IdeApp.ProjectOperations.CurrentSelectedSolution) {
				var conf = Document.Project.ParentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
				if (conf != null && !conf.BuildEnabledForItem (Document.Project))
					ResetOwnerProject ();
			}
		}

		void HandleWorkspaceItemLoaded (object sender, WorkspaceItemEventArgs e)
		{
			if (ownerProjects != null)
				return;
			UpdateOwnerProjects (e.Item.GetAllProjects ().OfType<DotNetProject> ());
		}

		void HandleWorkspaceItemUnloaded (object sender, WorkspaceItemEventArgs e)
		{
			if (ownerProjects == null)
				return;
			foreach (var p in e.Item.GetAllProjects ().OfType<DotNetProject> ()) {
				RemoveOwnerProject (p);
			}
			if (ownerProjects.Count == 0) {
				ownerProjects = null;
				Document.AttachToProject (null);
			}
		}

		void HandleProjectChanged (object sender, ProjectFileEventArgs e)
		{
			UpdateOwnerProjects ();
			UpdatePath (null, null);
		}

		void HandleTypeSegmentTreeUpdated (object sender, EventArgs e)
		{
			UpdatePath (null, null);
		}

		void UpdateOwnerProjects (IEnumerable<DotNetProject> allProjects)
		{
			var projects = new HashSet<DotNetProject> (allProjects.Where (p => p.IsFileInProject (Document.FileName)));
			if (ownerProjects == null || !projects.SetEquals (ownerProjects)) {
				SetOwnerProjects (projects.OrderBy (p => p.Name).ToList ());
				var dnp = Document.Project as DotNetProject;
				if (ownerProjects.Count > 0 && (dnp == null || !ownerProjects.Contains (dnp))) {
					// If the project for the document is not a DotNetProject but there is a project containing this file
					// in the current solution, then use that project
					var pp = Document.Project != null ? FindBestDefaultProject (Document.Project.ParentSolution) : null;
					if (pp != null)
						Document.AttachToProject (pp);
				}
			}
		}

		void UpdateOwnerProjects ()
		{
			UpdateOwnerProjects (IdeApp.Workspace.GetAllSolutionItems<DotNetProject> ());
			if (Document.Project == null)
				ResetOwnerProject ();
		}

		void ResetOwnerProject ()
		{
			if (ownerProjects.Count > 0)
				Document.AttachToProject (FindBestDefaultProject ());
		}

		DotNetProject FindBestDefaultProject (Solution solution = null)
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
				foreach (var sol in ownerProjects.Select (p => p.ParentSolution).Distinct ())
					sol.StartupItemChanged -= HandleStartupProjectChanged;
			}
		}

		void HandleStartupProjectChanged (object sender, EventArgs e)
		{
			// If the startup project changes, and the new startup project is an owner of this document,
			// then attach the document to that project

			var sol = (Solution) sender;
			var p = sol.StartupItem as DotNetProject;
			if (p != null && ownerProjects.Contains (p))
				Document.AttachToProject (p);
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
			List<AstNode> memberList = new List<AstNode> ();

			public DataProvider (PathedDocumentTextEditorExtension ext, object tag)
			{
				if (ext == null)
					throw new ArgumentNullException ("ext");
				this.ext = ext;
				this.tag = tag;
				Reset ();
			}

			#region IListDataProvider implementation

			void AddTypeToMemberList (TypeDeclaration type)
			{
				foreach (var member in type.Members) {
					if (member is FieldDeclaration) {
						foreach (var variable in ((FieldDeclaration)member).Variables)
							memberList.Add (variable);
					} else if (member is FixedFieldDeclaration) {
						foreach (var variable in ((FixedFieldDeclaration)member).Variables)
							memberList.Add (variable);
					} else if (member is EventDeclaration) {
						foreach (var variable in ((EventDeclaration)member).Variables)
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
					memberList.AddRange (unit.GetTypes (true));
				} else if (tag is TypeDeclaration) {
					AddTypeToMemberList ((TypeDeclaration)tag);
				} else if (tag is Accessor) {
					var acc = (Accessor)tag;
					var parent = (EntityDeclaration)acc.Parent;
					memberList.AddRange (parent.Children.OfType<Accessor> ());
				} else if (tag is EntityDeclaration) {
					var entity = (EntityDeclaration)tag;
					var type = entity.Parent as TypeDeclaration;
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
					if (result == 0 && x is MethodDeclaration && y is MethodDeclaration) {
						var mx = x as MethodDeclaration;
						var my = y as MethodDeclaration;
						if (mx.Body.IsNull && !my.Body.IsNull)
							return 1;
						if (!mx.Body.IsNull && my.Body.IsNull)
							return -1;
					}
					return result;
				});
			}

			static int GetTypeParameters (AstNode x)
			{
				return x.GetChildrenByRole (Roles.TypeParameter).Count ();
			}

			static int GetParameters (AstNode x)
			{
				return x.GetChildrenByRole (Roles.Parameter).Count ();
			}

			string GetName (AstNode node)
			{
				if (tag is SyntaxTree) {
					var type = node as TypeDeclaration;
					if (type != null) {
						var sb = new StringBuilder ();
						sb.Append (type.Name);
						while (type.Parent is TypeDeclaration) {
							type = type.Parent as TypeDeclaration;
							sb.Insert (0, type.Name + ".");
						}
						return sb.ToString ();
					}
					var delegateDecl = node as DelegateDeclaration;
					if (delegateDecl != null) {
						var sb = new StringBuilder ();
						sb.Append (delegateDecl.Name);
						var parentType = delegateDecl.Parent as TypeDeclaration;
						while (parentType != null) {
							sb.Insert (0, parentType.Name + ".");
							parentType = parentType.Parent as TypeDeclaration;
						}
						return sb.ToString ();
					}
				}
				
				if (node is Accessor) {
					if (node.Role == PropertyDeclaration.GetterRole)
						return "get";
					if (node.Role == PropertyDeclaration.SetterRole)
						return "set";
					if (node.Role == CustomEventDeclaration.AddAccessorRole)
						return "add";
					if (node.Role == CustomEventDeclaration.RemoveAccessorRole)
						return "remove";
					return node.ToString ();
				}
				if (node is OperatorDeclaration)
					return "operator";

				if (node is EntityDeclaration)
					return ((EntityDeclaration)node).Name;
				if (node is FixedVariableInitializer) {
					return ((FixedVariableInitializer)node).Name;
				}
				return ((VariableInitializer)node).Name;
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
						var sb = new StringBuilder ();
						sb.Append (ext.GetEntityMarkup (type));
						while (type.Parent is TypeDeclaration) {
							sb.Insert (0, ext.GetEntityMarkup (type.Parent) + ".");
							type = type.Parent;
						}
						return sb.ToString ();
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
					if (node is EntityDeclaration) {
						icon = ((EntityDeclaration)node).GetStockIcon ();
					} else {
						icon = ((EntityDeclaration)node.Parent).GetStockIcon ();
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
					ext.Document.AttachToProject (ext.ownerProjects [n]);
				} else {
					var node = memberList [n];
					var extEditor = ext.Document.GetContent<IExtensibleTextEditor> ();
					if (extEditor != null) {
						int line, col;
						if (node is OperatorDeclaration) { 
							line = Math.Max (1, ((OperatorDeclaration)node).OperatorToken.StartLocation.Line);
							col = Math.Max (1, ((OperatorDeclaration)node).OperatorToken.StartLocation.Column);
						} else if (node is IndexerDeclaration) { 
							line = Math.Max (1, ((IndexerDeclaration)node).ThisToken.StartLocation.Line);
							col = Math.Max (1, ((IndexerDeclaration)node).ThisToken.StartLocation.Column);
						} else if (node is EntityDeclaration && !(node is Accessor)) {
							line = Math.Max (1, ((EntityDeclaration)node).NameToken.StartLocation.Line);
							col = Math.Max (1, ((EntityDeclaration)node).NameToken.StartLocation.Column);
						} else {
							line = node.StartLocation.Line;
							col = node.StartLocation.Column;
						}
						extEditor.SetCaretTo (line, col);
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
			Document Document {
				get;
				set;
			}

			public CompilationUnitDataProvider (Document document)
			{
				this.Document = document;
			}

			#region IListDataProvider implementation

			public void Reset ()
			{
			}

			public string GetMarkup (int n)
			{
				return GLib.Markup.EscapeText (Document.ParsedDocument.UserRegions.ElementAt (n).Name);
			}
			
			internal static Xwt.Drawing.Image Pixbuf {
				get {
					return ImageService.GetIcon (Gtk.Stock.Add, Gtk.IconSize.Menu);
				}
			}
			
			public Xwt.Drawing.Image GetIcon (int n)
			{
				return Pixbuf;
			}

			public object GetTag (int n)
			{
				return Document.ParsedDocument.UserRegions.ElementAt (n);
			}

			public void ActivateItem (int n)
			{
				var reg = Document.ParsedDocument.UserRegions.ElementAt (n);
				var extEditor = Document.GetContent<MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor> ();
				if (extEditor != null)
					extEditor.SetCaretTo (Math.Max (1, reg.Region.BeginLine), reg.Region.BeginColumn);
			}

			public int IconCount {
				get {
					if (Document.ParsedDocument == null)
						return 0;
					return Document.ParsedDocument.UserRegions.Count ();
				}
			}

			#endregion

		}

		public Gtk.Widget CreatePathWidget (int index)
		{
			PathEntry[] path = CurrentPath;
			if (path == null || index < 0 || index >= path.Length)
				return null;
			var tag = path [index].Tag;
			var window = new DropDownBoxListWindow (tag == null ? (DropDownBoxListWindow.IListDataProvider)new CompilationUnitDataProvider (Document) : new DataProvider (this, tag));
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

		static PathEntry GetRegionEntry (ParsedDocument unit, Mono.TextEditor.DocumentLocation loc)
		{
			PathEntry entry;
			if (!unit.UserRegions.Any ())
				return null;
			var reg = unit.UserRegions.LastOrDefault (r => r.Region.IsInside (loc));
			if (reg == null) {
				entry = new PathEntry (GettextCatalog.GetString ("No region"));
			} else {
				entry = new PathEntry (CompilationUnitDataProvider.Pixbuf,
				                       GLib.Markup.EscapeText (reg.Name));
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

		EntityDeclaration lastType;
		string lastTypeMarkup;
		EntityDeclaration lastMember;
		string lastMemberMarkup;
		MonoDevelop.Projects.Project lastProject;
		AstAmbience amb;

		string GetEntityMarkup (AstNode node)
		{
			if (amb == null)
				return "";
			return amb.GetEntityMarkup (node);
		}

		void UpdatePath (object sender, Mono.TextEditor.DocumentLocationEventArgs e)
		{
			var parsedDocument = Document.ParsedDocument;
			if (parsedDocument == null || parsedDocument.ParsedFile == null)
				return;
			amb = new AstAmbience (document.GetFormattingOptions ());
			
			var unit = parsedDocument.GetAst<SyntaxTree> ();
			if (unit == null)
				return;

			var loc = Document.Editor.Caret.Location;
			var compExt = Document.GetContent<CSharpCompletionTextEditorExtension> ();
			var caretOffset = Document.Editor.Caret.Offset;
			var segType = compExt.GetTypeAt (caretOffset);
			if (segType != null)
				loc = segType.Region.Begin;

			var curType = (EntityDeclaration)unit.GetNodeAt (loc, n => n is TypeDeclaration || n is DelegateDeclaration);

			var curProject = ownerProjects != null && ownerProjects.Count > 1 ? Document.Project : null;

			var segMember = compExt.GetMemberAt (caretOffset);
			if (segMember != null) {
				loc = segMember.Region.Begin;
			} else {
				loc = Document.Editor.Caret.Location;
			}

			var curMember = unit.GetNodeAt<EntityDeclaration> (loc);
			if (curType == curMember || curType is DelegateDeclaration)
				curMember = null;
			if (isPathSet && curType == lastType && curMember == lastMember && curProject == lastProject)
				return;

			var curTypeMakeup = GetEntityMarkup (curType);
			var curMemberMarkup = GetEntityMarkup (curMember);
			if (isPathSet && curType != null && lastType != null && curType.StartLocation == lastType.StartLocation && curTypeMakeup == lastTypeMarkup &&
				curMember != null && lastMember != null && curMember.StartLocation == lastMember.StartLocation && curMemberMarkup == lastMemberMarkup && curProject == lastProject)
				return;

			lastType = curType;
			lastTypeMarkup = curTypeMakeup;

			lastMember = curMember;
			lastMemberMarkup = curMemberMarkup;

			lastProject = curProject;

			var result = new List<PathEntry> ();

			if (ownerProjects != null && ownerProjects.Count > 1) {
				// Current project if there is more than one
				result.Add (new PathEntry (ImageService.GetIcon (Document.Project.StockIcon, Gtk.IconSize.Menu), GLib.Markup.EscapeText (Document.Project.Name)) { Tag = Document.Project });
			}

			if (curType == null) {
				if (CurrentPath != null && CurrentPath.Length == 1 && CurrentPath [0].Tag is IUnresolvedFile)
					return;
				if (CurrentPath != null && CurrentPath.Length == 2 && CurrentPath [1].Tag is IUnresolvedFile)
					return;
				var prevPath = CurrentPath;
				result.Add (new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit });
				CurrentPath = result.ToArray ();
				OnPathChanged (new DocumentPathChangedEventArgs (prevPath));	
				return;
			}

			if (curType != null) {
				var type = curType;
				var pos = result.Count;
				while (type != null) {
					var declaringType = type.Parent as TypeDeclaration;
					result.Insert (pos, new PathEntry (ImageService.GetIcon (type.GetStockIcon (), Gtk.IconSize.Menu), GetEntityMarkup (type)) { Tag = (AstNode)declaringType ?? unit });
					type = declaringType;
				}
			}
				
			if (curMember != null) {
				result.Add (new PathEntry (ImageService.GetIcon (curMember.GetStockIcon (), Gtk.IconSize.Menu), curMemberMarkup) { Tag = curMember });
				if (curMember is Accessor) {
					var parent = curMember.Parent as EntityDeclaration;
					if (parent != null)
						result.Insert (result.Count - 1, new PathEntry (ImageService.GetIcon (parent.GetStockIcon (), Gtk.IconSize.Menu), GetEntityMarkup (parent)) { Tag = parent });
				}
			}
				
			var entry = GetRegionEntry (parsedDocument, loc);
			if (entry != null)
				result.Add (entry);
				
			PathEntry noSelection = null;
			if (curType == null) {
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit };
			} else if (curMember == null && !(curType is DelegateDeclaration)) { 
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = curType };
			}

			if (noSelection != null)
				result.Add (noSelection);

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
			//		Gtk.Application.Invoke (delegate {
			CurrentPath = result.ToArray ();
			OnPathChanged (new DocumentPathChangedEventArgs (prev));	
			//		});
			//	});
		}

		#endregion

	}
}
