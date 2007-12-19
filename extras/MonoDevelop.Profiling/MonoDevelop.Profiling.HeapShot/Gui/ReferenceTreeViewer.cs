//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

using Gdk;
using Gtk;
using System;
using MonoDevelop.Profiling;

namespace MonoDevelop.Profiling.HeapShot
{
	public delegate void ProgressEventHandler (int current, int max, string message);
	
	public partial class ReferenceTreeViewer : Gtk.Bin
	{
		Gtk.TreeStore store;
		const int ReferenceCol = 0;
		const int ImageCol = 1;
		const int TypeCol = 2;
		const int FilledCol = 3;
		const int SizeCol = 4;
		const int AvgSizeCol = 5;
		const int InstancesCol = 6;
		const int RefsCol = 7;
		const int RootRefsCol = 8;
		const int RootMemCol = 9;
		int TreeColRefs;
		bool reloadRequested;
		bool loading;
		Gtk.Tooltips tips = new Gtk.Tooltips ();
		TipWindow tipWindow;
		bool showTipScheduled;
		uint tipTimeoutId;
		const int TipTimer = 800;
		
		ObjectMapReader file;
		string typeName;
		
		public event ProgressEventHandler ProgressEvent;

		public ReferenceTreeViewer()
		{
			Build ();
			store = new Gtk.TreeStore (typeof(object), typeof(string), typeof(string), typeof(bool), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
			treeview.Model = store;
			treeview.HeadersClickable = true;
			
			Gtk.TreeViewColumn complete_column = new Gtk.TreeViewColumn ();
			complete_column.Title = "Type";
			complete_column.Resizable = true;

			Gtk.CellRendererPixbuf pix_render = new Gtk.CellRendererPixbuf ();
			complete_column.PackStart (pix_render, false);
			complete_column.AddAttribute (pix_render, "stock-id", ImageCol);

			Gtk.CellRendererText text_render = new Gtk.CellRendererText ();
			complete_column.PackStart (text_render, true);
			
			complete_column.AddAttribute (text_render, "text", TypeCol);
			complete_column.Clickable = true;
	
			treeview.AppendColumn (complete_column);
			
			AddColumn ("Instances", InstancesCol, "Number of instances of a type. Only instances that contain references are included.");
			TreeColRefs = treeview.Columns.Length;
			AddColumn ("References", RefsCol, "Number of references to the parent type.");
			AddColumn ("Root Refs", RootRefsCol, "Number of indirect references to instances of the tree root type.");
			AddColumn ("Root Mem", RootMemCol, "Amount of memory of the root instances indirectly referenced.");
			AddColumn ("Memory Size", SizeCol, "Memory allocated by instances of the type.");
			AddColumn ("Avg. Size", AvgSizeCol, "Average size of the instances.");
			
			treeview.TestExpandRow += new Gtk.TestExpandRowHandler (OnTestExpandRow);
			treeview.RowActivated += new Gtk.RowActivatedHandler (OnNodeActivated);
			treeview.AppendColumn (new Gtk.TreeViewColumn());
			
			int nc = 0;
			foreach (TreeViewColumn c in treeview.Columns) {
				store.SetSortFunc (nc, CompareNodes);
				c.SortColumnId = nc++;
			}
			store.SetSortColumnId (1, Gtk.SortType.Descending);
			treeview.RulesHint = true;
			tips.Enable ();
		}
		
		void AddColumn (string title, int ncol, string desc)
		{
			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			Gtk.Label lab = new Gtk.Label (title);
			lab.Xalign = 1;
			EventBox bx = new EventBox ();
			bx.Add (lab);
			bx.ShowAll ();
			col.Widget = bx;
			
			CellRendererText crt = new CellRendererText ();
			crt.Xalign = 1;
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ncol);
			
			treeview.AppendColumn (col);
			tips.SetTip (bx, desc, desc);
		}
		
		public void Clear ()
		{
			entryFilter.Text = "";
			store.Clear ();
		}
		
		public bool InverseReferences {
			get { return checkInverse.Active; }
			set { checkInverse.Active = value; }
		}
		
		public string RootTypeName {
			get { return typeName; }
		}
		
		public string SelectedType {
			get {
				Gtk.TreeModel foo;
				Gtk.TreeIter iter;
				if (!treeview.Selection.GetSelected (out foo, out iter))
					return null;
				ReferenceNode nod = store.GetValue (iter, 0) as ReferenceNode;
				if (nod != null)
					return nod.TypeName;
				else
					return null;
			}
		}
		
		public void FillAllTypes (ObjectMapReader file)
		{
			this.file = file;
			this.typeName = null;
			boxFilter.Visible = true;
			treeview.Columns [TreeColRefs].Visible = InverseReferences;
			treeview.Columns [TreeColRefs+1].Visible = InverseReferences;
			treeview.Columns [TreeColRefs+2].Visible = InverseReferences;
			
			if (loading) {
				// If the tree is already being loaded, notify that loading
				// has to start again, since the file has changed.
				reloadRequested = true;
				return;
			}

			loading = true;
			store.Clear ();
			int n=0;
			foreach (int t in file.GetTypes ()) {
				if (++n == 20) {
					if (ProgressEvent != null) {
						ProgressEvent (n, file.GetTypeCount (), null);
					}
					while (Gtk.Application.EventsPending ())
						Gtk.Application.RunIteration ();
					if (reloadRequested) {
						loading = false;
						reloadRequested = false;
						FillAllTypes (this.file);
						return;
					}
					n = 0;
				}
				if (file.GetObjectCountForType (t) > 0)
					InternalFillType (file, t);
			}
			loading = false;
		}
		
		public void FillType (ObjectMapReader file, string typeName)
		{
			this.typeName = typeName;
			this.file = file;
			store.Clear ();
			boxFilter.Visible = false;
			treeview.Columns [TreeColRefs].Visible = InverseReferences;
			treeview.Columns [TreeColRefs+1].Visible = InverseReferences;
			treeview.Columns [TreeColRefs+2].Visible = InverseReferences;
			TreeIter iter = InternalFillType (file, file.GetTypeFromName (typeName));
			treeview.ExpandRow (store.GetPath (iter), false);
		}
		
		TreeIter InternalFillType (ObjectMapReader file, int type)
		{
			ReferenceNode node = file.GetReferenceTree (type, checkInverse.Active);
			return AddNode (TreeIter.Zero, node);
		}
		
		void Refill ()
		{
			if (typeName != null)
				FillType (file, typeName);
			else
				FillAllTypes (file);
		}
		
		TreeIter AddNode (TreeIter parent, ReferenceNode node)
		{
			if (entryFilter.Text.Length > 0 && node.TypeName.IndexOf (entryFilter.Text) == -1)
				return TreeIter.Zero;
			
			TreeIter iter;
			if (parent.Equals (TreeIter.Zero)) {
				iter = store.AppendValues (node, "md-class", node.TypeName, !node.HasReferences, node.TotalMemory.ToString("n0"), node.AverageSize.ToString("n0"), node.RefCount.ToString ("n0"), "", "", "");
			} else {
				string refs = (InverseReferences ? node.RefsToParent.ToString ("n0") : "");
				string rootRefs = (InverseReferences ? node.RefsToRoot.ToString ("n0") : "");
				string rootMem = (InverseReferences ? node.RootMemory.ToString ("n0") : "");
				iter = store.AppendValues (parent, node, "md-class", node.TypeName, !node.HasReferences, node.TotalMemory.ToString("n0"), node.AverageSize.ToString("n0"), node.RefCount.ToString ("n0"), refs, rootRefs, rootMem);
			}

			if (node.HasReferences) {
				// Add a dummy element to make the expansion icon visible
				store.AppendValues (iter, null, "", "", true, "", "", "", "", "", "");
			}
			return iter;
		}

		TreeIter AddNode (TreeIter parent, FieldReference node)
		{
			if (parent.Equals (TreeIter.Zero))
				return store.AppendValues (node, "md-field", node.FiledName, true, "", "", node.RefCount.ToString ("n0"), "", "");
			else
				return store.AppendValues (parent, node, "md-field", node.FiledName, true, "", "", node.RefCount.ToString ("n0"), "", "");
		}

		private void OnTestExpandRow (object sender, Gtk.TestExpandRowArgs args)
		{
			bool filled = (bool) store.GetValue (args.Iter, FilledCol);
			ReferenceNode parent = (ReferenceNode) store.GetValue (args.Iter, ReferenceCol);
			if (!filled) {
				store.SetValue (args.Iter, FilledCol, true);
				TreeIter iter;
				store.IterChildren (out iter, args.Iter);
				store.Remove (ref iter);
				if (parent.References.Count > 0 || parent.FieldReferences.Count > 0) {
					int nr = 0;
					foreach (ReferenceNode nod in parent.References)
						if (!AddNode (args.Iter, nod).Equals (TreeIter.Zero))
							nr++;
					foreach (FieldReference fref in parent.FieldReferences)
						if (!AddNode (args.Iter, fref).Equals (TreeIter.Zero))
							nr++;
					if (nr == 0)
						args.RetVal = true;
				} else
					args.RetVal = true;
			}
		}

		protected virtual void OnNodeActivated (object sender, Gtk.RowActivatedArgs args)
		{
			if (TypeActivated != null && SelectedType != null)
				TypeActivated (this, EventArgs.Empty);
		}
		
		protected virtual void OnCheckInverseClicked(object sender, System.EventArgs e)
		{
			Refill ();
		}

		protected virtual void OnButtonFilterClicked(object sender, System.EventArgs e)
		{
			Refill ();
		}

		protected virtual void OnEntryFilterActivated(object sender, System.EventArgs e)
		{
			Refill ();
		}
		
		int CompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			int col;
			SortType type;
			store.GetSortColumnId (out col, out type);
			
			object o1 = model.GetValue (a, ReferenceCol);
			object o2 = model.GetValue (b, ReferenceCol);
			
			if (o1 is ReferenceNode && o2 is ReferenceNode) {
				ReferenceNode nod1 = (ReferenceNode) o1;
				ReferenceNode nod2 = (ReferenceNode) o2;
				switch (col) {
					case 0:
						return string.Compare (nod1.TypeName, nod2.TypeName);
					case 1:
						return nod1.RefCount.CompareTo (nod2.RefCount);
					case 2:
						return nod1.RefsToParent.CompareTo (nod2.RefsToParent);
					case 3:
						return nod1.RefsToRoot.CompareTo (nod2.RefsToRoot);
					case 4:
						return nod1.RootMemory.CompareTo (nod2.RootMemory);
					case 5:
						return nod1.TotalMemory.CompareTo (nod2.TotalMemory);
					case 6:
						return nod1.AverageSize.CompareTo (nod2.AverageSize);
					default:
						return 1;
	//					throw new InvalidOperationException ();
				}
			} else if (o1 is FieldReference && o2 is FieldReference) {
				return ((FieldReference)o1).FiledName.CompareTo (((FieldReference)o2).FiledName);
			} else if (o1 is FieldReference) {
				return 1;
			} else {
				return -1;
			}
		}

		[GLib.ConnectBefore]
		protected void OnTreeviewMotionNotifyEvent(object o, Gtk.MotionNotifyEventArgs args)
		{
			UpdateTipWindow ((int)args.Event.X, (int)args.Event.Y);
		}
		
		void UpdateTipWindow (int x, int y)
		{
			if (tipWindow != null) {
				// Tip already being shown. Update it.
				ShowTooltip (x, y);
			}
			else if (showTipScheduled) {
				// Tip already scheduled. Reset the timer.
				GLib.Source.Remove (tipTimeoutId);
				tipTimeoutId = GLib.Timeout.Add (TipTimer, delegate { return ShowTooltip (x,y);} );
			}
			else {
				// Start a timer to show the tip
				showTipScheduled = true;
				tipTimeoutId = GLib.Timeout.Add (TipTimer, delegate { return ShowTooltip (x,y);} );
			}
		}
		
		string lastTipTxt;
		
		bool ShowTooltip (int xloc, int yloc)
		{
			ModifierType mask; // ignored
			int mxloc, myloc;

			showTipScheduled = false;
			
			treeview.GdkWindow.GetPointer (out mxloc, out myloc, out mask);
			
			Gtk.TreePath path;
			Gtk.TreeViewColumn col;
			
			treeview.GetPathAtPos (xloc, yloc, out path, out col);
			if (col == null) {
				HideTipWindow ();
				return false;
			}
			
			Gtk.TreeIter iter;
			if (!store.GetIter (out iter, path)) {
				HideTipWindow ();
				return false;
			}

			object ob = store.GetValue (iter, ReferenceCol);
			string txt = GetTipText (iter, col.SortColumnId, ob);
			if (lastTipTxt != txt) {
				HideTipWindow ();
				tipWindow = new TipWindow (txt);
				tipWindow.ShowAll ();
			}
			lastTipTxt = txt;
			
			int ox, oy;
			treeview.GdkWindow.GetOrigin (out ox, out oy);
			int w = tipWindow.Child.SizeRequest().Width;
			tipWindow.Move (mxloc + ox - (w/2), myloc + oy + 20);
			tipWindow.ShowAll ();

			return false;
		}
		
		string GetTipText (Gtk.TreeIter iter, int col, object ob)
		{
			ReferenceNode node = ob as ReferenceNode;
			if (node != null) {
				switch (col) {
					case 0:
						return "Type " + node.TypeName;
					case 1: {
						string pname = GetParentType (iter);
						if (pname != null) {
							if (InverseReferences)
								return string.Format ("There are <b>{0:n0}</b> instances of type <b>{1}</b> which contain references to objects of type <b>{2}</b>", node.RefCount, GetShortName (node.TypeName), pname);
							else
								return string.Format ("There are <b>{0:n0}</b> instances of type <b>{1}</b> referenced by objects of type <b>{2}</b>", node.RefCount, GetShortName (node.TypeName), pname);
						} else
							return string.Format ("There are <b>{0:n0}</b> instances of type <b>{1}</b>.", node.RefCount, GetShortName (node.TypeName));
					}
					case 2: {
						string pname = GetParentType (iter);
						if (pname != null)
							return string.Format ("There are <b>{0:n0}</b> distinct references from objects of type <b>{1}</b> to objects of type <b>{2}</b>", node.RefsToParent, GetShortName (node.TypeName), pname);
						else
							return "";
					}
					case 3: {
						string rname = GetRootType (iter);
						if (rname != null)
							return string.Format ("There are <b>{0:n0}</b> indirect references from objects of type <b>{1}</b> to objects of type <b>{2}</b>", node.RefsToRoot, GetShortName (node.TypeName), rname);
						else
							return "";
					}
					case 4: {
						string rname = GetRootType (iter);
						if (rname != null)
							return string.Format ("There are <b>{0:n0}</b> bytes of <b>{1}</b> objects indirectly referenced by <b>{2}</b> objects", node.RootMemory, rname, GetShortName (node.TypeName));
						else
							return "";
					}
					case 5: {
						string pname = GetParentType (iter);
						if (pname != null) {
							if (InverseReferences)
								return string.Format ("There are <b>{0:n0}</b> bytes of <b>{1}</b> objects which have references to <b>{2}</b> objects", node.TotalMemory, GetShortName (node.TypeName), pname);
							else
								return string.Format ("There are <b>{0:n0}</b> bytes of <b>{1}</b> objects referenced by <b>{2}</b> objects", node.TotalMemory, GetShortName (node.TypeName), pname);
						} else
							return string.Format ("There are <b>{0:n0}</b> bytes of <b>{1}</b> objects", node.TotalMemory, GetShortName (node.TypeName));
					}
					case 6:
						string pname = GetParentType (iter);
						if (pname != null) {
							if (InverseReferences)
								return string.Format ("Objects of type <b>{0}</b> which have references to <b>{2}</b> objects have an average size of <b>{1:n0}</b> bytes", GetShortName (node.TypeName), node.AverageSize, pname);
							else
								return string.Format ("Objects of type <b>{0}</b> referenced by <b>{2}</b> objects have an average size of <b>{1:n0}</b> bytes", GetShortName (node.TypeName), node.AverageSize, pname);
						} else
							return string.Format ("Objects of type <b>{0}</b> have an average size of <b>{1:n0}</b> bytes", GetShortName (node.TypeName), node.AverageSize);
				}
			} else {
				FieldReference fr = (FieldReference) ob;
				return fr.FiledName;
			}
			
			return "";
		}
		
		string GetShortName (string typeName)
		{
			int i = typeName.LastIndexOf ('.');
			if (i != -1)
				return typeName.Substring (i+1);
			else
				return typeName;
		}
		
		string GetParentType (Gtk.TreeIter it)
		{
			if (store.IterParent (out it, it))
				return GetShortName ((string) store.GetValue (it, TypeCol));
			else
				return null;
		}
		
		string GetRootType (Gtk.TreeIter it)
		{
			Gtk.TreeIter rit;
			while (store.IterParent (out rit, it)) {
				it = rit;
			}
			return GetShortName ((string) store.GetValue (it, TypeCol));
		}
		
		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			HideTipWindow ();
			return base.OnScrollEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)		
		{
			HideTipWindow ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			HideTipWindow ();
			return base.OnButtonPressEvent (e);
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			HideTipWindow ();
			return base.OnKeyPressEvent (evnt);
		}
		
		void HideTipWindow ()
		{
			lastTipTxt = null;
			if (showTipScheduled) {
				GLib.Source.Remove (tipTimeoutId);
				showTipScheduled = false;
			}
			if (tipWindow != null) {
				tipWindow.Destroy ();
				tipWindow = null;
			}
		}

		[GLib.ConnectBefore]
		protected void OnTreeviewLeaveNotifyEvent(object o, Gtk.LeaveNotifyEventArgs args)
		{
			HideTipWindow ();
		}
		
		public event EventHandler TypeActivated;
	}
	
	class TipWindow: Gtk.Window
	{
		public TipWindow (string txt) : base (Gtk.WindowType.Popup)
		{
			Label lab = new Label ();
			lab.Markup = txt;
			lab.Xalign = 0.5f;
			lab.Xpad = 3;
			lab.Ypad = 3;
			lab.Wrap = true;
			Add (lab);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);
			Gtk.Requisition req = SizeRequest ();
			Gtk.Style.PaintFlatBox (this.Style, this.GdkWindow, Gtk.StateType.Normal, Gtk.ShadowType.Out, Gdk.Rectangle.Zero, this, "tooltip", 0, 0, req.Width, req.Height);
			return true;
		}
	}
}
