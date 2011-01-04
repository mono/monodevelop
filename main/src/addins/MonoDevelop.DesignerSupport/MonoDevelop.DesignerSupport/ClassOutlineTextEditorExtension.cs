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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Components.Docking;


namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Displays a types and members outline of the current document.
	/// </summary>
	/// <remarks>
	/// Document types and members are displayed in a tree view.
	/// The displayed nodes can be sorted by pressing the buttons on the toolbar.
	/// Sort behaviour can be configured in the MonoDevelopProperties.xml by tweaking
	/// the sortKey* elements of type byte. Nodes with lower sortKey value will be sorted
	/// before nodes with higher value. Nodes with equal sortKey will be sorted by string
	/// comparison of the name of the nodes. The string comparison ignores symbols
	/// (e.g. sort 'Foo()' next to '~Foo()').
	/// </remarks>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineNodeComparer"/>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineSortingProperties"/>
	public class ClassOutlineTextEditorExtension : TextEditorExtension, IOutlinedDocument
	{
		/// <summary>
		/// Currently fires whenever clicking on the sort toggle buttons or preferences are changed.
		/// </summary>
		public static event EventHandler EventSortingPropertiesChanged;

		/// <summary>
		/// Name of the sorting property entry in MonoDevelopProperties.xml.
		/// </summary>
		public const string                        SORTING_PROPERTY = "MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtension.Sorting";

		ParsedDocument                             lastCU = null;

		MonoDevelop.Ide.Gui.Components.PadTreeView outlineTreeView;

		TreeStore                                  outlineTreeStore;

		TreeModelSort                              outlineTreeModelSort;
		ToggleButton                               groupToggleButton;
		ToggleButton                               sortAlphabeticallyToggleButton;
		DockToolButton                             preferencesButton;

		ClassOutlineNodeComparer                   comparer;

		bool                                       refreshingOutline;
		bool                                       disposed;

		/// <summary>
		/// Call to notify listeners of sorting property changes.
		/// </summary>
		/// <param name="o">
		/// The object that changed the properties.
		/// </param>
		/// <param name="e">
		/// Empty event arguments.
		/// </param>
		public static void SortingPropertiesChanged (object o, EventArgs e)
		{
			if (EventSortingPropertiesChanged != null) {
				EventSortingPropertiesChanged (o, e);
			}
		}

		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			MonoDevelop.Projects.IDotNetLanguageBinding binding = MonoDevelop.Projects.LanguageBindingService.GetBindingPerFileName (doc.Name) as MonoDevelop.Projects.IDotNetLanguageBinding;
			return binding != null;
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
			base.Dispose ();
		}

		void UpdateSorting ()
		{
			if (IsSorting ()) {

				/*
				 * Sort the model, sort keys may have changed.
				 *
				 * Only setting the column again does not re-sort so we set
				 * the function instead.
				 */

				outlineTreeModelSort.SetSortFunc (0, comparer.CompareNodes);

				outlineTreeView.Model = outlineTreeModelSort;
			} else {
				outlineTreeView.Model = outlineTreeStore;
			}

			/*
			 * Currently the general tree expansion concept is suboptimal, e.g. tree expands
			 * on all changes to the source file regardless of sort settings.
			 * Because sorting the tree by setting the sort function also collapses
			 * the tree view we just expand the whole tree for now.
			 */

			outlineTreeView.ExpandAll ();
		}

		/*
		 * Called when the group button changes state.
		 * Based on buttons state the tree view is directly linked to the tree model or
		 * channeled through the sorted model.
		 * The change is also stored into the properties and a change event is fired.
		 */

		void OnButtonGroupClicked (object sender, EventArgs e)
		{
			// Set properties to button state

			ClassOutlineSortingProperties properties = PropertyService.Get<ClassOutlineSortingProperties> (SORTING_PROPERTY);

			properties.IsGrouping = groupToggleButton.Active;

			UpdateSorting ();

			// Notifiy listeners on properties changes (this includes us as a side effect)

			SortingPropertiesChanged (this, EventArgs.Empty);
		}

		/*
		 * Called when the sorting button changes state.
		 * Based on buttons state the tree view is directly linked to the tree model or
		 * channeled through the sorted model.
		 * The change is also stored into the properties and a change event is fired.
		 */

		void OnButtonSortAlphabeticallyClicked (object sender, EventArgs e)
		{
			// Set properties to button state

			ClassOutlineSortingProperties properties = PropertyService.Get<ClassOutlineSortingProperties> (SORTING_PROPERTY);

			properties.IsSortingAlphabetically = sortAlphabeticallyToggleButton.Active;

			UpdateSorting ();

			// Notifiy listeners on properties changes (this includes us as a side effect)

			SortingPropertiesChanged (this, EventArgs.Empty);
		}

		/*
		 * Called when the properties change.
		 */

		void OnPropertiesChanged (object sender, EventArgs e)
		{
			// If the event does not come from us (e.g. handleButtonToggle)

			if (sender != this) {

				/*
				 * This class never changes the property object reference, only the object's members.
				 * If the change originates from somewhere else the object reference could have been changed
				 * to refer to a different object so we query the PropertyService again.
				 */

				ClassOutlineSortingProperties properties = PropertyService.Get<ClassOutlineSortingProperties> (SORTING_PROPERTY);

				// Update button state to reflect properties

				groupToggleButton.Active              = properties.IsGrouping;
				sortAlphabeticallyToggleButton.Active = properties.IsSortingAlphabetically;

				// Let the comparer know about the changes

				comparer.Properties = properties;

				UpdateSorting ();
			}
		}

		/*
		 * Called when the preferences button is clicked.
		 */

		void OnButtonPreferencesClicked (object sender, EventArgs e)
		{
			ClassOutlineTextEditorExtensionPreferencesDialog dialog;
			dialog = new ClassOutlineTextEditorExtensionPreferencesDialog ();

			dialog.Run ();

			dialog.Destroy ();
		}

		bool IsSorting ()
		{
			return groupToggleButton.Active || sortAlphabeticallyToggleButton.Active;
		}

		Widget MonoDevelop.DesignerSupport.IOutlinedDocument.GetOutlineWidget (IPadWindow window)
		{
			if (outlineTreeView != null)
				return outlineTreeView;

			outlineTreeStore = new TreeStore (typeof(object));

			ClassOutlineSortingProperties properties = PropertyService.Get (
				SORTING_PROPERTY,
				ClassOutlineSortingProperties.GetDefaultInstance ());

			// Initialize sorted tree model

			outlineTreeModelSort = new TreeModelSort (outlineTreeStore);

			comparer = new ClassOutlineNodeComparer (GetAmbience (), properties, outlineTreeModelSort);

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

				IdeApp.ProjectOperations.JumpToDeclaration (o as INode);
			};

			this.lastCU = Document.ParsedDocument;

			outlineTreeView.Realized += delegate { RefillOutlineStore (); };

			// Initialize grouping toggle button

			groupToggleButton = new ToggleButton ();
			groupToggleButton.Image = new Image (ImageService.GetPixbuf ("md-design-categorise", IconSize.Menu));
			groupToggleButton.Toggled += new EventHandler (OnButtonGroupClicked);
			groupToggleButton.TooltipText = GettextCatalog.GetString ("Group");

			// Initialize alphabetically sorting toggle button

			sortAlphabeticallyToggleButton = new ToggleButton ();
			sortAlphabeticallyToggleButton.Image = new Image (Gtk.Stock.SortAscending, IconSize.Menu);
			sortAlphabeticallyToggleButton.Toggled += new EventHandler (OnButtonSortAlphabeticallyClicked);
			sortAlphabeticallyToggleButton.TooltipText = GettextCatalog.GetString ("Sort alphabetically");

			// Initialize preferences button

			preferencesButton = new DockToolButton (Gtk.Stock.Preferences);
			preferencesButton.Image = new Image (Gtk.Stock.Preferences, IconSize.Menu);
			preferencesButton.Clicked += new EventHandler (OnButtonPreferencesClicked);
			preferencesButton.TooltipText = GettextCatalog.GetString ("Preferences");

			// Initialize toolbar

			DockItemToolbar toolbar = window.GetToolbar (PositionType.Top);
			toolbar.Add (groupToggleButton);
			toolbar.Add (sortAlphabeticallyToggleButton);
			toolbar.Add (preferencesButton);
			toolbar.ShowAll ();

			// Listen for property changes (e.g. preferences dialog)

			EventSortingPropertiesChanged += OnPropertiesChanged;

			// Restore property state

			groupToggleButton.Active              = properties.IsGrouping;
			sortAlphabeticallyToggleButton.Active = properties.IsSortingAlphabetically;

			var sw = new CompactScrolledWindow ();
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}

		void OutlineTreeIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var pixRenderer = (CellRendererPixbuf)cell;
			object o = model.GetValue (iter, 0);
			if (o is IMember) {
				pixRenderer.Pixbuf = ImageService.GetPixbuf (((IMember)o).StockIcon, IconSize.Menu);
			} else if (o is FoldingRegion) {
				pixRenderer.Pixbuf = ImageService.GetPixbuf ("gtk-add", IconSize.Menu);
			}
		}

		void OutlineTreeTextFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText txtRenderer = (CellRendererText)cell;
			object o = model.GetValue (iter, 0);
			Ambience am = GetAmbience ();
			if (o is IMember) {
				txtRenderer.Text = am.GetString ((IMember)o, OutputFlags.ClassBrowserEntries);
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
			ScrolledWindow w = (ScrolledWindow)outlineTreeView.Parent;
			w.Destroy ();
			outlineTreeStore.Dispose ();
			outlineTreeStore = null;
			outlineTreeView = null;
			groupToggleButton = null;
			sortAlphabeticallyToggleButton = null;
			preferencesButton = null;

			EventSortingPropertiesChanged -= OnPropertiesChanged;
		}

		void UpdateDocumentOutline (object sender, EventArgs args)
		{
			lastCU = Document.ParsedDocument;
			//limit update rate to 3s
			if (!refreshingOutline) {
				refreshingOutline = true;
				GLib.Timeout.Add (3000, new GLib.TimeoutHandler (RefillOutlineStore));
			}
		}

		bool RefillOutlineStore ()
		{
			DispatchService.AssertGuiThread ();
			Gdk.Threads.Enter ();
			refreshingOutline = false;
			if (outlineTreeStore == null || !outlineTreeView.IsRealized)
				return false;

			outlineTreeStore.Clear ();
			if (lastCU != null) {
				BuildTreeChildren (outlineTreeStore, TreeIter.Zero, lastCU);
				outlineTreeView.ExpandAll ();
			}

			Gdk.Threads.Leave ();

			//stop timeout handler
			return false;
		}

		static void BuildTreeChildren (TreeStore store, TreeIter parent, ParsedDocument parsedDocument)
		{
			if (parsedDocument.CompilationUnit == null)
				return;
			foreach (IType cls in parsedDocument.CompilationUnit.Types) {
				TreeIter childIter;
				if (!parent.Equals (TreeIter.Zero))
					childIter = store.AppendValues (parent, cls);
				else
					childIter = store.AppendValues (cls);

				AddTreeClassContents (store, childIter, parsedDocument, cls);
			}
		}

		static void AddTreeClassContents (TreeStore store, TreeIter parent, ParsedDocument parsedDocument, IType cls)
		{
			List<object> items = new List<object> ();
			foreach (object o in cls.Members)
				items.Add (o);

			items.Sort (delegate(object x, object y) {
				DomRegion r1 = GetRegion (x), r2 = GetRegion (y);
				return r1.CompareTo (r2);
			});

			List<FoldingRegion> regions = new List<FoldingRegion> ();
			foreach (FoldingRegion fr in parsedDocument.UserRegions)
				//check regions inside class
				if (cls.BodyRegion.Contains (fr.Region))
					regions.Add (fr);
			regions.Sort (delegate(FoldingRegion x, FoldingRegion y) { return x.Region.CompareTo (y.Region); });

			IEnumerator<FoldingRegion> regionEnumerator = regions.GetEnumerator ();
			if (!regionEnumerator.MoveNext ())
				regionEnumerator = null;

			FoldingRegion currentRegion = null;
			TreeIter currentParent = parent;
			foreach (object item in items) {

				//no regions left; quick exit
				if (regionEnumerator != null) {
					DomRegion itemRegion = GetRegion (item);

					//advance to a region that could potentially contain this member
					while (regionEnumerator != null && !OuterEndsAfterInner (regionEnumerator.Current.Region, itemRegion))
						if (!regionEnumerator.MoveNext ())
							regionEnumerator = null;

					//if member is within region, make sure it's the current parent.
					//If not, move target iter back to class parent
					if (regionEnumerator != null && regionEnumerator.Current.Region.Contains (itemRegion)) {
						if (currentRegion != regionEnumerator.Current) {
							currentParent = store.AppendValues (parent, regionEnumerator.Current);
							currentRegion = regionEnumerator.Current;
						}
					} else {
						currentParent = parent;
					}
				}


				TreeIter childIter = store.AppendValues (currentParent, item);
				if (item is IType)
					AddTreeClassContents (store, childIter, parsedDocument, (IType)item);
			}
		}

		static DomRegion GetRegion (object o)
		{
			if (o is IField) {
				var f = (IField)o;
				return new DomRegion (f.Location, f.Location);
			}
			
			if (o is IMember)
				return ((IMember)o).BodyRegion;
			throw new InvalidOperationException (o.GetType ().ToString ());
		}

		static bool OuterEndsAfterInner (DomRegion outer, DomRegion inner)
		{
			return ((outer.End.Line > 1 && outer.End.Line > inner.End.Line) || (outer.End.Line == inner.End.Line && outer.End.Column > inner.End.Column));
		}
	}
}
