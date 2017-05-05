//
// ClassOutlineTextEditorExtension.cs
//
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.DesignerSupport;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CSharp.ClassOutline
{
	/// <summary>
	/// Displays a types and members outline of the current document.
	/// </summary>
	/// <remarks>
	/// Document types and members are displayed in a tree view.
	/// The displayed nodes can be sorted by changing the sorting properties state.
	/// The sort behaviour is serialized into MonoDevelopProperties.xml.
	/// Nodes with lower sortKey value will be sorted before nodes with higher value.
	/// Nodes with equal sortKey will be sorted by string comparison of the name of the nodes.
	/// The string comparison ignores symbols (e.g. sort 'Foo()' next to '~Foo()').
	/// </remarks>
	/// <seealso cref="MonoDevelop.CSharp.ClassOutline.OutlineNodeComparer"/>
	/// <seealso cref="MonoDevelop.CSharp.ClassOutline.OutlineSettings"/>
	class CSharpOutlineTextEditorExtension : TextEditorExtension, IOutlinedDocument
	{
		SemanticModel lastCU = null;

		MonoDevelop.Ide.Gui.Components.PadTreeView outlineTreeView;
		TreeStore outlineTreeStore;
		TreeModelSort outlineTreeModelSort;
		Widget[] toolbarWidgets;

		OutlineNodeComparer comparer;
		OutlineSettings settings;

		bool refreshingOutline;
		bool disposed;
		bool outlineReady;


		public override bool IsValidInContext (DocumentContext context)
		{
			return LanguageBindingService.GetBindingPerFileName (context.Name) != null;
		}

		protected override void Initialize ()
		{
			base.Initialize ();

			if (DocumentContext != null)
				DocumentContext.DocumentParsed += UpdateDocumentOutline;
		}

		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			if (DocumentContext != null)
				DocumentContext.DocumentParsed -= UpdateDocumentOutline;
			RemoveRefillOutlineStoreTimeout ();
			lastCU = null;
			settings = null;
			comparer = null;
			base.Dispose ();
		}

		Widget IOutlinedDocument.GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;

			outlineTreeStore = new TreeStore (typeof(object));
			outlineTreeModelSort = new TreeModelSort (outlineTreeStore);
			
			settings = OutlineSettings.Load ();
			comparer = new OutlineNodeComparer (new AstAmbience (TypeSystemService.Workspace.Options), settings, outlineTreeModelSort);

			outlineTreeModelSort.SetSortFunc (0, comparer.CompareNodes);
			outlineTreeModelSort.SetSortColumnId (0, SortType.Ascending);

			outlineTreeView = new MonoDevelop.Ide.Gui.Components.PadTreeView (outlineTreeStore);

			var pixRenderer = new CellRendererImage ();
			pixRenderer.Xpad = 0;
			pixRenderer.Ypad = 0;

			outlineTreeView.TextRenderer.Xpad = 0;
			outlineTreeView.TextRenderer.Ypad = 0;

			TreeViewColumn treeCol = new TreeViewColumn ();
			treeCol.PackStart (pixRenderer, false);

			treeCol.SetCellDataFunc (pixRenderer, new TreeCellDataFunc (OutlineTreeIconFunc));
			treeCol.PackStart (outlineTreeView.TextRenderer, true);

			treeCol.SetCellDataFunc (outlineTreeView.TextRenderer, new TreeCellDataFunc (OutlineTreeTextFunc));
			outlineTreeView.AppendColumn (treeCol);

			outlineTreeView.HeadersVisible = false;

			outlineTreeView.Selection.Changed += delegate {
				JumpToDeclaration (false);
			};
			
			outlineTreeView.RowActivated += delegate {
				JumpToDeclaration (true);
			};

			var analysisDocument = DocumentContext.ParsedDocument;
			if (analysisDocument != null)
				lastCU = analysisDocument.GetAst<SemanticModel> ();

			outlineTreeView.Realized += delegate { RefillOutlineStore (); };
			UpdateSorting ();

			var sw = new CompactScrolledWindow ();
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}
		
		IEnumerable<Widget> IOutlinedDocument.GetToolbarWidgets ()
		{
			if (toolbarWidgets != null)
				return toolbarWidgets;
			
			var groupToggleButton = new ToggleButton {
				Image = new ImageView (Ide.Gui.Stock.GroupByCategory, IconSize.Menu),
				TooltipText = GettextCatalog.GetString ("Group entries by type"),
				Active = settings.IsGrouped,
			};	
			groupToggleButton.Toggled += delegate {
				if (groupToggleButton.Active == settings.IsGrouped)
					return;
				settings.IsGrouped = groupToggleButton.Active;
				UpdateSorting ();
			};

			var sortAlphabeticallyToggleButton = new ToggleButton {
				Image = new ImageView (Ide.Gui.Stock.SortAlphabetically, IconSize.Menu),
				TooltipText = GettextCatalog.GetString ("Sort entries alphabetically"),
				Active = settings.IsSorted,
			};	
			sortAlphabeticallyToggleButton.Toggled += delegate {
				if (sortAlphabeticallyToggleButton.Active == settings.IsSorted)
					return;
				settings.IsSorted = sortAlphabeticallyToggleButton.Active;
				UpdateSorting ();
			};

			var preferencesButton = new DockToolButton (Ide.Gui.Stock.Options) {
				TooltipText = GettextCatalog.GetString ("Open preferences dialog"),
			};
			preferencesButton.Clicked += delegate {
				using (var dialog = new OutlineSortingPreferencesDialog (settings)) {
					if (MonoDevelop.Ide.MessageService.ShowCustomDialog (dialog) == (int)Gtk.ResponseType.Ok) {
						dialog.SaveSettings ();
						comparer = new OutlineNodeComparer (new AstAmbience (TypeSystemService.Workspace.Options), settings, outlineTreeModelSort);
						UpdateSorting ();
					}
				}
			};
			
			return toolbarWidgets = new Widget[] {
				groupToggleButton,
				sortAlphabeticallyToggleButton,
				new VSeparator (),
				preferencesButton,
			};
		}
		
		void JumpToDeclaration (bool focusEditor)
		{
			if (!outlineReady)
				return;
			TreeIter iter;
			if (!outlineTreeView.Selection.GetSelected (out iter))
				return;

			var o = outlineTreeStore.GetValue (IsSorting () ? outlineTreeModelSort.ConvertIterToChildIter (iter) : iter, 0);

			var syntaxNode = o as SyntaxNode;
			if (syntaxNode != null) {
				Editor.CaretOffset = syntaxNode.SpanStart;
			} else {
				Editor.CaretOffset = ((SyntaxTrivia)o).SpanStart;
			}

			if (focusEditor) {
				GLib.Timeout.Add (10, delegate {
					Editor.GrabFocus ();
					return false;
				});
			}
		}

		static void OutlineTreeIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var pixRenderer = (CellRendererImage)cell;
			object o = model.GetValue (iter, 0);
			if (o is SyntaxNode) {
				pixRenderer.Image = ImageService.GetIcon (((SyntaxNode)o).GetStockIcon (), IconSize.Menu);
			} else if (o is SyntaxTrivia) {
				pixRenderer.Image = ImageService.GetIcon (Ide.Gui.Stock.Region, IconSize.Menu);
			}
		}

		static void OutlineTreeTextFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var astAmbience = new AstAmbience (TypeSystemService.Workspace.Options);
			var txtRenderer = (CellRendererText)cell;
			object o = model.GetValue (iter, 0);
			var syntaxNode = o as SyntaxNode;
			if (syntaxNode != null) {
				txtRenderer.Markup = astAmbience.GetEntityMarkup (syntaxNode);
			} else if (o is SyntaxTrivia) {
				txtRenderer.Text = ((SyntaxTrivia)o).ToString ();
			}
		}

		void IOutlinedDocument.ReleaseOutlineWidget ()
		{
			if (outlineTreeView == null)
				return;
			var w = (ScrolledWindow)outlineTreeView.Parent;
			w.Destroy ();
			outlineTreeView = null;
			RemoveRefillOutlineStoreTimeout ();
			settings = null;
			foreach (var tw in toolbarWidgets)
				tw.Destroy ();
			toolbarWidgets = null;
			comparer = null;
		}

		void RemoveRefillOutlineStoreTimeout ()
		{
			if (refillOutlineStoreId == 0)
				return;
			GLib.Source.Remove (refillOutlineStoreId);
			refillOutlineStoreId = 0;
		}

		uint refillOutlineStoreId;
		void UpdateDocumentOutline (object sender, EventArgs args)
		{
			var analysisDocument = DocumentContext.ParsedDocument;
			if (analysisDocument == null)
				return;
			lastCU = analysisDocument.GetAst<SemanticModel> ();
			//limit update rate to 3s
			if (!refreshingOutline) {
				refreshingOutline = true;
				refillOutlineStoreId = GLib.Timeout.Add (3000, RefillOutlineStore);
			}
		}

		bool RefillOutlineStore ()
		{
			Runtime.AssertMainThread ();
			Gdk.Threads.Enter ();
			refreshingOutline = false;
			if (outlineTreeStore == null || outlineTreeView == null || !outlineTreeView.IsRealized) {
				refillOutlineStoreId = 0;
				return false;
			}
			
			outlineReady = false;
			outlineTreeStore.Clear ();
			if (lastCU != null) {
				BuildTreeChildren (outlineTreeStore, TreeIter.Zero, lastCU);
				TreeIter it;
				if (IsSorting ()) {
					if (outlineTreeModelSort.GetIterFirst (out it))
						outlineTreeView.Selection.SelectIter (it);
				} else {
					if (outlineTreeStore.GetIterFirst (out it))
						outlineTreeView.Selection.SelectIter (it);
				}

				outlineTreeView.ExpandAll ();
			}
			outlineReady = true;

			Gdk.Threads.Leave ();

			//stop timeout handler
			refillOutlineStoreId = 0;
			return false;
		}

		class TreeVisitor : CSharpSyntaxWalker
		{
			TreeStore store;
			TreeIter curIter;

			public TreeVisitor (TreeStore store, TreeIter curIter) : base (SyntaxWalkerDepth.Trivia)
			{
				this.store = store;
				this.curIter = curIter;
			}

			TreeIter Append (object node) 
			{
				if (!curIter.Equals (TreeIter.Zero))
					return store.AppendValues (curIter, node);
				return store.AppendValues (node);
			}

			public override void VisitTrivia (SyntaxTrivia trivia)
			{
				switch (trivia.Kind ()) {
				case SyntaxKind.RegionDirectiveTrivia:
					curIter = Append (trivia);
					break;
				case SyntaxKind.EndRegionDirectiveTrivia:
					TreeIter parent;
					if (store.IterParent (out parent, curIter))
						curIter = parent;
					break;
				}
				base.VisitTrivia (trivia);
			}

			void VisitBody (SyntaxNode node)
			{
				var oldIter = curIter;

				foreach (var syntaxNodeOrToken in node.ChildNodesAndTokens ()) {
					var syntaxNode = syntaxNodeOrToken.AsNode ();
					if (syntaxNode != null) {
						Visit (syntaxNode);
					} else {
						var syntaxToken = syntaxNodeOrToken.AsToken ();
						if (syntaxToken.Kind () == SyntaxKind.OpenBraceToken)
							curIter = Append (node);
						VisitToken (syntaxToken);
					}
				}
				curIter = oldIter;

			}

			public override void VisitNamespaceDeclaration (NamespaceDeclarationSyntax node)
			{
				VisitBody (node);
			}

			public override void VisitClassDeclaration (ClassDeclarationSyntax node)
			{
				VisitBody (node);
			}

			public override void VisitStructDeclaration (StructDeclarationSyntax node)
			{
				VisitBody (node);
			}

			public override void VisitEnumDeclaration (EnumDeclarationSyntax node)
			{
				VisitBody (node);
			}

			public override void VisitInterfaceDeclaration (InterfaceDeclarationSyntax node)
			{
				VisitBody (node);
			}

			public override void VisitDelegateDeclaration (DelegateDeclarationSyntax node)
			{
				base.VisitDelegateDeclaration (node);
				Append (node);
			}

			public override void VisitFieldDeclaration (FieldDeclarationSyntax node)
			{
				base.VisitFieldDeclaration (node);
				foreach (var v in node.Declaration.Variables)
					Append (v);
			}

			public override void VisitPropertyDeclaration (PropertyDeclarationSyntax node)
			{
				base.VisitPropertyDeclaration (node);
				Append (node);
			}

			public override void VisitIndexerDeclaration (IndexerDeclarationSyntax node)
			{
				base.VisitIndexerDeclaration (node);
				Append (node);
			}

			public override void VisitMethodDeclaration (MethodDeclarationSyntax node)
			{
				base.VisitMethodDeclaration (node);
				Append (node);
			}

			public override void VisitOperatorDeclaration (OperatorDeclarationSyntax node)
			{
				base.VisitOperatorDeclaration (node);
				Append (node);
			}

			public override void VisitConstructorDeclaration (ConstructorDeclarationSyntax node)
			{
				base.VisitConstructorDeclaration (node);
				Append (node);
			}

			public override void VisitDestructorDeclaration (DestructorDeclarationSyntax node)
			{
				base.VisitDestructorDeclaration (node);
				Append (node);
			}

			public override void VisitEventDeclaration (EventDeclarationSyntax node)
			{
				base.VisitEventDeclaration (node);
				Append (node);
			}

			public override void VisitEventFieldDeclaration (EventFieldDeclarationSyntax node)
			{
				base.VisitEventFieldDeclaration (node);
				foreach (var v in node.Declaration.Variables)
					Append (v);
			}

			public override void VisitEnumMemberDeclaration (EnumMemberDeclarationSyntax node)
			{
				base.VisitEnumMemberDeclaration (node);
				Append (node);
			}

			public override void VisitBlock (BlockSyntax node)
			{
				// skip
			}
		}


		static void BuildTreeChildren (TreeStore store, TreeIter parent, SemanticModel parsedDocument)
		{
			if (parsedDocument == null)
				return;

			var root = parsedDocument.SyntaxTree.GetRoot ();

			var visitor = new TreeVisitor (store, parent);
			visitor.Visit (root); 
		}

		void UpdateSorting ()
		{
			if (IsSorting ()) {
				// Sort the model, sort keys may have changed.
				// Only setting the column again does not re-sort so we set the function instead.
				outlineTreeModelSort.SetSortFunc (0, comparer.CompareNodes);
				outlineTreeView.Model = outlineTreeModelSort;
			} else {
				outlineTreeView.Model = outlineTreeStore;
			}
			outlineTreeView.SearchColumn = -1; // disable the interactive search

			// Because sorting the tree by setting the sort function also collapses the tree view we expand
			// the whole tree.
			outlineTreeView.ExpandAll ();
		}

		bool IsSorting ()
		{
			return settings.IsGrouped || settings.IsSorted;
		}
	}
}
