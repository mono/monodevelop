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

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide;
using MonoDevelop.Components;


namespace MonoDevelop.DesignerSupport
{


	public class ClassOutlineTextEditorExtension : TextEditorExtension, IOutlinedDocument
	{
		ParsedDocument lastCU = null;
		MonoDevelop.Ide.Gui.Components.PadTreeView outlineTreeView;
		TreeStore outlineTreeStore;
		bool refreshingOutline;
		bool disposed;

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

		Widget MonoDevelop.DesignerSupport.IOutlinedDocument.GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;

			outlineTreeStore = new TreeStore (typeof(object));
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
				object o = outlineTreeStore.GetValue (iter, 0);
				
				IdeApp.ProjectOperations.JumpToDeclaration (o as INode);
			};

			this.lastCU = Document.ParsedDocument;
			
			outlineTreeView.Realized += delegate { RefillOutlineStore (); };

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
