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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Components.Docking;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.DesignerSupport
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
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineNodeComparer"/>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineSortingProperties"/>
	public class ClassOutlineTextEditorExtension : TextEditorExtension, IOutlinedDocument
	{
		ParsedDocument lastCU = null;

		MonoDevelop.Ide.Gui.Components.PadTreeView outlineTreeView;
		TreeStore outlineTreeStore;
		TreeModelSort outlineTreeModelSort;
		Widget[] toolbarWidgets;

		ClassOutlineNodeComparer comparer;
		ClassOutlineSettings settings;

		bool refreshingOutline;
		bool disposed;
		bool outlineReady;

		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			var binding = LanguageBindingService.GetBindingPerFileName (doc.Name);
			return binding != null && binding is IDotNetLanguageBinding;
		}

		public override void Initialize ()
		{
			base.Initialize ();
			if (Document != null)
				Document.DocumentParsed += UpdateDocumentOutline;
		}

		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			if (Document != null)
				Document.DocumentParsed -= UpdateDocumentOutline;
			RemoveRefillOutlineStoreTimeout ();

			base.Dispose ();
		}

		Widget MonoDevelop.DesignerSupport.IOutlinedDocument.GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;

			outlineTreeStore = new TreeStore (typeof(object));
			outlineTreeModelSort = new TreeModelSort (outlineTreeStore);
			
			settings = ClassOutlineSettings.Load ();
			comparer = new ClassOutlineNodeComparer (GetAmbience (), settings, outlineTreeModelSort);

			outlineTreeModelSort.SetSortFunc (0, comparer.CompareNodes);
			outlineTreeModelSort.SetSortColumnId (0, SortType.Ascending);

			outlineTreeView = new MonoDevelop.Ide.Gui.Components.PadTreeView (outlineTreeStore);

			var pixRenderer = new CellRendererPixbuf ();
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

			this.lastCU = Document.ParsedDocument;

			outlineTreeView.Realized += delegate { RefillOutlineStore (); };
			UpdateSorting ();

			var sw = new CompactScrolledWindow ();
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}
		
		IEnumerable<Widget> MonoDevelop.DesignerSupport.IOutlinedDocument.GetToolbarWidgets ()
		{
			if (toolbarWidgets != null)
				return toolbarWidgets;
			
			var groupToggleButton = new ToggleButton () {
				Image = new Image (ImageService.GetPixbuf ("md-design-categorise", IconSize.Menu)),
				TooltipText = GettextCatalog.GetString ("Group entries by type"),
				Active = settings.IsGrouped,
			};	
			groupToggleButton.Toggled += delegate {
				if (groupToggleButton.Active == settings.IsGrouped)
					return;
				settings.IsGrouped = groupToggleButton.Active;
				UpdateSorting ();
			};

			var sortAlphabeticallyToggleButton = new ToggleButton () {
				Image = new Image (Gtk.Stock.SortAscending, IconSize.Menu),
				TooltipText = GettextCatalog.GetString ("Sort entries alphabetically"),
				Active = settings.IsSorted,
			};	
			sortAlphabeticallyToggleButton.Toggled += delegate {
				if (sortAlphabeticallyToggleButton.Active == settings.IsSorted)
					return;
				settings.IsSorted = sortAlphabeticallyToggleButton.Active;
				UpdateSorting ();
			};

			var preferencesButton = new DockToolButton (Gtk.Stock.Preferences) {
				Image = new Image (Gtk.Stock.Preferences, IconSize.Menu),
				TooltipText = GettextCatalog.GetString ("Open preferences dialog"),
			};
			preferencesButton.Clicked += delegate {
				var dialog = new ClassOutlineSortingPreferencesDialog (settings);
				try {
					if (MonoDevelop.Ide.MessageService.ShowCustomDialog (dialog) == (int)Gtk.ResponseType.Ok) {
						dialog.SaveSettings ();
						comparer = new ClassOutlineNodeComparer (GetAmbience (), settings, outlineTreeModelSort);
						UpdateSorting ();
					}
				} finally {
					dialog.Destroy ();
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

			object o;

			if (IsSorting ()) {
				o = outlineTreeStore.GetValue (outlineTreeModelSort.ConvertIterToChildIter (iter), 0);
			}
			else {
				o = outlineTreeStore.GetValue (iter, 0);
			}
				
			IdeApp.ProjectOperations.JumpToDeclaration (o as IEntity);
			if (focusEditor)
				IdeApp.Workbench.ActiveDocument.Select ();
		}

		void OutlineTreeIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var pixRenderer = (CellRendererPixbuf)cell;
			object o = model.GetValue (iter, 0);
			if (o is IEntity) {
				pixRenderer.Pixbuf = ImageService.GetPixbuf (((IEntity)o).GetStockIcon (), IconSize.Menu);
			} else if (o is FoldingRegion) {
				pixRenderer.Pixbuf = ImageService.GetPixbuf ("gtk-add", IconSize.Menu);
			}
		}

		void OutlineTreeTextFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText txtRenderer = (CellRendererText)cell;
			object o = model.GetValue (iter, 0);
			Ambience am = GetAmbience ();
			if (o is IEntity) {
				txtRenderer.Text = am.GetString ((IEntity)o, OutputFlags.ClassBrowserEntries);
			} else if (o is FoldingRegion) {
				string name = ((FoldingRegion)o).Name.Trim ();
				if (string.IsNullOrEmpty (name))
					name = "#region";
				txtRenderer.Text = name;
			}
		}

		void MonoDevelop.DesignerSupport.IOutlinedDocument.ReleaseOutlineWidget ()
		{
			if (outlineTreeView == null)
				return;
			var w = (ScrolledWindow)outlineTreeView.Parent;
			w.Destroy ();
			outlineTreeModelSort.Dispose ();
			outlineTreeModelSort = null;
			outlineTreeStore.Dispose ();
			outlineTreeStore = null;
			outlineTreeView = null;
			settings = null;
			foreach (var tw in toolbarWidgets)
				w.Destroy ();
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
			lastCU = Document.ParsedDocument;
			//limit update rate to 3s
			if (!refreshingOutline) {
				refreshingOutline = true;
				refillOutlineStoreId = GLib.Timeout.Add (3000, RefillOutlineStore);
			}
		}

		bool RefillOutlineStore ()
		{
			DispatchService.AssertGuiThread ();
			Gdk.Threads.Enter ();
			refreshingOutline = false;
			if (outlineTreeStore == null || !outlineTreeView.IsRealized) {
				refillOutlineStoreId = 0;
				return false;
			}
			
			outlineReady = false;
			outlineTreeStore.Clear ();
			if (lastCU != null) {
				BuildTreeChildren (outlineTreeStore, TreeIter.Zero, lastCU);
				TreeIter it;
				if (outlineTreeStore.GetIterFirst (out it))
					outlineTreeView.Selection.SelectIter (it);
				outlineTreeView.ExpandAll ();
			}
			outlineReady = true;

			Gdk.Threads.Leave ();

			//stop timeout handler
			refillOutlineStoreId = 0;
			return false;
		}

		void BuildTreeChildren (TreeStore store, TreeIter parent, ParsedDocument parsedDocument)
		{
			if (parsedDocument == null)
				return;
			
			foreach (var unresolvedCls in parsedDocument.TopLevelTypeDefinitions) {
				var cls = document.Compilation.MainAssembly.GetTypeDefinition (unresolvedCls.FullTypeName);
				if (cls == null)
					continue;
				TreeIter childIter;
				if (!parent.Equals (TreeIter.Zero))
					childIter = store.AppendValues (parent, cls);
				else
					childIter = store.AppendValues (cls);

				AddTreeClassContents (store, childIter, parsedDocument, cls, unresolvedCls);
			}
		}

		static void AddTreeClassContents (TreeStore store, TreeIter parent, ParsedDocument parsedDocument, ITypeDefinition cls, IUnresolvedTypeDefinition part)
		{
			List<object> items = new List<object> ();
			if (cls.Kind != TypeKind.Delegate) {
				foreach (object o in cls.GetMembers (m => part.Region.FileName == m.Region.FileName && part.Region.IsInside (m.Region.Begin))) {
					items.Add (o);
				}
				foreach (object o in cls.GetNestedTypes (m => part.Region.FileName == m.Region.FileName && part.Region.IsInside (m.Region.Begin))) {
					items.Add (o);
				}
				foreach (object o in cls.GetConstructors (m => part.Region.FileName == m.Region.FileName && part.Region.IsInside (m.Region.Begin))) {
					items.Add (o);
				}
			}
			items.Sort (ClassOutlineNodeComparer.CompareRegion);

			List<FoldingRegion> regions = new List<FoldingRegion> ();
			foreach (FoldingRegion fr in parsedDocument.UserRegions)
				//check regions inside class
				if (cls.BodyRegion.IsInside (fr.Region.Begin) && cls.BodyRegion.IsInside (fr.Region.End))
					regions.Add (fr);
			regions.Sort (delegate(FoldingRegion x, FoldingRegion y) { return x.Region.Begin.CompareTo (y.Region.Begin); });
			 
			IEnumerator<FoldingRegion> regionEnumerator = regions.GetEnumerator ();
			if (!regionEnumerator.MoveNext ())
				regionEnumerator = null;
			
			FoldingRegion currentRegion = null;
			TreeIter currentParent = parent;
			foreach (object item in items) {

				//no regions left; quick exit
				if (regionEnumerator != null) {
					DomRegion itemRegion = ClassOutlineNodeComparer.GetRegion (item);

					//advance to a region that could potentially contain this member
					while (regionEnumerator != null && !OuterEndsAfterInner (regionEnumerator.Current.Region, itemRegion))
						if (!regionEnumerator.MoveNext ())
							regionEnumerator = null;

					//if member is within region, make sure it's the current parent.
					//If not, move target iter back to class parent
					if (regionEnumerator != null && regionEnumerator.Current.Region.IsInside (itemRegion.Begin)) {
						if (currentRegion != regionEnumerator.Current) {
							currentParent = store.AppendValues (parent, regionEnumerator.Current);
							currentRegion = regionEnumerator.Current;
						}
					} else {
						currentParent = parent;
					}
				}
				
				TreeIter childIter = store.AppendValues (currentParent, item);
				if (item is ITypeDefinition)
					AddTreeClassContents (store, childIter, parsedDocument, (ITypeDefinition)item, part);
					
			} 
		}

		static DomRegion GetRegion (object o)
		{
			if (o is IEntity) {
				var m = (IEntity)o;
				return m.Region; //m.BodyRegion.IsEmpty ? new DomRegion (m.Location, m.Location) : m.BodyRegion;
			}
			throw new InvalidOperationException (o.GetType ().ToString ());
		}

		static bool OuterEndsAfterInner (DomRegion outer, DomRegion inner)
		{
			return ((outer.End.Line > 1 && outer.End.Line > inner.End.Line)
				|| (outer.End.Line == inner.End.Line && outer.End.Column > inner.End.Column));
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
