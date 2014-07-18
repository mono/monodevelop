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
using MonoDevelop.Ide.Gui;
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
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.CSharp
{
	class PathedDocumentTextEditorExtension : TextEditorExtension, IPathedDocument
	{
		public override void Dispose ()
		{
			Editor.CaretPositionChanged -= UpdatePath;
			IdeApp.Workspace.FileAddedToProject -= HandleProjectChanged;
			IdeApp.Workspace.FileRemovedFromProject -= HandleProjectChanged;


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
		List<DotNetProject> ownerProjects;

		protected override void Initialize ()
		{
			CurrentPath = new PathEntry[] { new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = null } };
			isPathSet = false;
			UpdateOwnerProjects ();
			UpdatePath (null, null);

			Editor.CaretPositionChanged += UpdatePath;
			ext = DocumentContext.GetContent<CSharpCompletionTextEditorExtension> ();
			ext.TypeSegmentTreeUpdated += HandleTypeSegmentTreeUpdated;

			IdeApp.Workspace.FileAddedToProject += HandleProjectChanged;
			IdeApp.Workspace.FileRemovedFromProject += HandleProjectChanged;
		}

		void HandleProjectChanged (object sender, EventArgs e)
		{
			UpdateOwnerProjects ();
			UpdatePath (null, null);
		}

		void HandleTypeSegmentTreeUpdated (object sender, EventArgs e)
		{
			UpdatePath (null, null);
		}

		void UpdateOwnerProjects ()
		{
			var projects = new HashSet<DotNetProject> (IdeApp.Workspace.GetAllSolutionItems<DotNetProject> ().Where (p => p.IsFileInProject (DocumentContext.Name)));
			if (ownerProjects == null || !projects.SetEquals (ownerProjects)) {
				ownerProjects = projects.OrderBy (p => p.Name).ToList ();
				var dnp = DocumentContext.Project as DotNetProject;
				if (ownerProjects.Count > 0 && (dnp == null || !ownerProjects.Contains (dnp))) {
					// If the project for the document is not a DotNetProject but there is a project containing this file
					// in the current solution, then use that project
					var pp = DocumentContext.Project != null ? ownerProjects.FirstOrDefault (p => p.ParentSolution == DocumentContext.Project.ParentSolution) : null;
					if (pp != null)
						DocumentContext.AttachToProject (pp);
				}
			}
			if (DocumentContext.Project == null && ownerProjects.Count > 0)
				DocumentContext.AttachToProject (ownerProjects[0]);
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

			void AddTypeToMemberList (TypeDeclarationSyntax type)
			{
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
					memberList.AddRange (unit.GetRoot ().DescendantNodes ().OfType<TypeDeclarationSyntax> ());
				} else if (tag is TypeDeclarationSyntax) {
					AddTypeToMemberList ((TypeDeclarationSyntax)tag);
				} else if (tag is AccessorDeclarationSyntax) {
					var acc = (AccessorDeclarationSyntax)tag;
					var parent = (MemberDeclarationSyntax)acc.Parent;
					memberList.AddRange (parent.ChildNodes ().OfType<AccessorDeclarationSyntax> ());
				} else if (tag is MemberDeclarationSyntax) {
					var entity = (MemberDeclarationSyntax)tag;
					var type = entity.Parent as TypeDeclarationSyntax;
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
					var type = node as TypeDeclarationSyntax;
					if (type != null) {
						var sb = new StringBuilder ();
						sb.Append (type.Identifier.ToString ());
						while (type.Parent is TypeDeclarationSyntax) {
							type = type.Parent as TypeDeclarationSyntax;
							sb.Insert (0, type.Identifier + ".");
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
				
				var accessor = node as AccessorDeclarationSyntax;
				if (accessor != null) {
					if (accessor.CSharpKind () == SyntaxKind.GetAccessorDeclaration)
						return "get";
					if (accessor.CSharpKind () == SyntaxKind.SetAccessorDeclaration)
						return "set";
					if (accessor.CSharpKind () == SyntaxKind.AddAccessorDeclaration)
						return "add";
					if (accessor.CSharpKind () == SyntaxKind.RemoveAccessorDeclaration)
						return "remove";
					return node.ToString ();
				}
				if (node is OperatorDeclarationSyntax)
					return "operator";

				if (node is MemberDeclarationSyntax)
					return ((MemberDeclarationSyntax)node).ToString ();
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
						var sb = new StringBuilder ();
						sb.Append (ext.GetEntityMarkup (type));
						while (type.Parent is TypeDeclarationSyntax) {
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
					if (node is MemberDeclarationSyntax) {
						icon = ((MemberDeclarationSyntax)node).GetStockIcon ();
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
						extEditor.SetCaretLocation (line, col, true);
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
				return GLib.Markup.EscapeText (DocumentContext.ParsedDocument.UserRegions.ElementAt (n).Name);
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
				return DocumentContext.ParsedDocument.UserRegions.ElementAt (n);
			}

			public void ActivateItem (int n)
			{
				var reg = DocumentContext.ParsedDocument.UserRegions.ElementAt (n);
				var extEditor = editor;
				if (extEditor != null) {
					extEditor.SetCaretLocation(Math.Max (1, reg.Region.BeginLine), reg.Region.BeginColumn, true);
				}
			}

			public int IconCount {
				get {
					if (DocumentContext.ParsedDocument == null)
						return 0;
					return DocumentContext.ParsedDocument.UserRegions.Count ();
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

		static PathEntry GetRegionEntry (ParsedDocument unit, DocumentLocation loc)
		{
			PathEntry entry;
			if (unit == null || !unit.UserRegions.Any ())
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

		SyntaxNode lastType;
		string lastTypeMarkup;
		SyntaxNode lastMember;
		string lastMemberMarkup;
		MonoDevelop.Projects.Project lastProject;
		AstAmbience amb;

		string GetEntityMarkup (SyntaxNode node)
		{
			if (amb == null || node == null)
				return "";
			return amb.GetEntityMarkup (node);
		}


		async void UpdatePath (object sender, Mono.TextEditor.DocumentLocationEventArgs e)
		{
			var analysisDocument = Document.AnalysisDocument;
			if (analysisDocument == null)
				return;
			var caretOffset = Editor.CaretOffset;
			var unit = await analysisDocument.GetSyntaxTreeAsync ();
			if (unit == null)
				return;
			amb = new AstAmbience (RoslynTypeSystemService.Workspace.Options);
			
			var loc = Document.Editor.Caret.Location;
			var compExt = Document.GetContent<CSharpCompletionTextEditorExtension> ();

			var segType = compExt.GetTypeAt (caretOffset);

			var root = unit.GetRoot ();
			var token = root.FindNode (TextSpan.FromBounds (caretOffset, caretOffset));

			var curMember = token.AncestorsAndSelf ().FirstOrDefault (m => m is MemberDeclarationSyntax && !(m is NamespaceDeclarationSyntax));
			var curType = token.AncestorsAndSelf ().FirstOrDefault (m => m is TypeDeclarationSyntax || m is DelegateDeclarationSyntax);

			var curProject = ownerProjects.Count > 1 ? Document.Project : null;

			if (curType == curMember || curType is DelegateDeclarationSyntax)
				curMember = null;
			if (isPathSet && curType == lastType && curMember == lastMember && curProject == lastProject)
				return;

			var curTypeMakeup = GetEntityMarkup (curType);
			var curMemberMarkup = GetEntityMarkup (curMember);
			if (isPathSet && curType != null && lastType != null && curTypeMakeup == lastTypeMarkup &&
				curMember != null && lastMember != null && curMemberMarkup == lastMemberMarkup && curProject == lastProject)
				return;

			lastType = curType;
			lastTypeMarkup = curTypeMakeup;

			lastMember = curMember;
			lastMemberMarkup = curMemberMarkup;

			lastProject = curProject;

			var result = new List<PathEntry> ();

			if (ownerProjects.Count > 1) {
				// Current project if there is more than one
				result.Add (new PathEntry (ImageService.GetIcon (DocumentContext.Project.StockIcon, Gtk.IconSize.Menu), GLib.Markup.EscapeText (DocumentContext.Project.Name)) { Tag = DocumentContext.Project });
			}

			if (curType == null) {
				if (CurrentPath != null && CurrentPath.Length == 1 && CurrentPath [0].Tag is CSharpSyntaxTree)
					return;
				if (CurrentPath != null && CurrentPath.Length == 2 && CurrentPath [1].Tag is CSharpSyntaxTree)
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
					if (!(type is TypeDeclarationSyntax))
						break;
					result.Insert (pos, new PathEntry (ImageService.GetIcon (type.GetStockIcon (), Gtk.IconSize.Menu), GetEntityMarkup (type)) { Tag = (object)type ?? unit });
					type = type.Parent;
				}
			}
				
			if (curMember != null) {
				result.Add (new PathEntry (ImageService.GetIcon (curMember.GetStockIcon (), Gtk.IconSize.Menu), curMemberMarkup) { Tag = curMember });
				if (curMember.CSharpKind () == SyntaxKind.GetAccessorDeclaration ||
					curMember.CSharpKind () == SyntaxKind.SetAccessorDeclaration ||
					curMember.CSharpKind () == SyntaxKind.AddAccessorDeclaration ||
					curMember.CSharpKind () == SyntaxKind.RemoveAccessorDeclaration) {
					var parent = curMember.Parent;
					if (parent != null)
						result.Insert (result.Count - 1, new PathEntry (ImageService.GetIcon (parent.GetStockIcon (), Gtk.IconSize.Menu), GetEntityMarkup (parent)) { Tag = parent });
				}
			}
				
			var entry = GetRegionEntry (document.ParsedDocument, loc);
			if (entry != null)
				result.Add (entry);
				
			PathEntry noSelection = null;
			if (curType == null) {
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit };
			} else if (curMember == null && !(curType is DelegateDeclarationSyntax)) { 
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
