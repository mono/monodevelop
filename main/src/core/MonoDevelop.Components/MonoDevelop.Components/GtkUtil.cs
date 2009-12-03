// GtkUtil.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using Gtk;

namespace MonoDevelop.Components
{
	public static class GtkUtil
	{
		static Dictionary<TreeView, TreeViewTooltipsData> treeData = new Dictionary<TreeView, TreeViewTooltipsData> ();
		
		public static void EnableAutoTooltips (this Gtk.TreeView tree)
		{
			TreeViewTooltipsData data = new TreeViewTooltipsData ();
			treeData [tree] = data;
			tree.MotionNotifyEvent += HandleMotionNotifyEvent;
			tree.LeaveNotifyEvent += HandleLeaveNotifyEvent;;
			tree.Destroyed += delegate {
				treeData.Remove (tree);
			};
		}

		[GLib.ConnectBeforeAttribute]
		static void HandleLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
		{
			GLib.Timeout.Add (50, delegate {
				Gtk.TreeView tree = (Gtk.TreeView) o;
				TreeViewTooltipsData data = treeData [tree];
				if (data != null && data.Tooltip != null && data.Tooltip.MouseIsOver)
					return false;
				HideTooltip (tree);
				return false;
			});
		}

		internal static void HideTooltip (TreeView tree)
		{
			TreeViewTooltipsData data = treeData [tree];
			if (data.ShowingTooltip) {
				GLib.Source.Remove (data.Timer);
				data.ShowingTooltip = false;
				return;
			}
			if (data.Tooltip != null) {
				data.Tooltip.Destroy ();
				data.Tooltip = null;
			}
		}

		[GLib.ConnectBeforeAttribute]
		static void HandleMotionNotifyEvent(object o, MotionNotifyEventArgs args)
		{
			TreeView tree = (TreeView) o;
			TreeViewTooltipsData data = treeData [tree];

			HideTooltip (tree);

			int cx, cy;
			TreePath path;
			TreeViewColumn col;
			if (!tree.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path, out col, out cx, out cy))
				return;
			data.Path = path;
			data.ShowingTooltip = true;
			data.Timer = GLib.Timeout.Add (300, delegate {
				data.ShowingTooltip = false;
				int ox, oy;
				tree.GdkWindow.GetOrigin (out ox, out oy);
				Gdk.Rectangle rect = tree.GetCellArea (path, col);
				data.Tooltip = new CellTooltipWindow (tree, col, path);
				if (rect.X + data.Tooltip.SizeRequest ().Width > tree.Allocation.Width) {
					data.Tooltip.Move (ox + rect.X - 1, oy + rect.Y);
					data.Tooltip.ShowAll ();
				} else {
					data.Tooltip.Destroy ();
					data.Tooltip = null;
				}
				return false;
			});
		}
	}

	class TreeViewTooltipsData
	{
		public bool ShowingTooltip;
		public uint Timer;
		public TreePath Path;
		public CellTooltipWindow Tooltip;
	}

	class CellTooltipWindow: TooltipWindow
	{
		TreeViewColumn col;
		TreeView tree;
		TreePath path;
		TreeIter iter;
		
		public bool MouseIsOver;
		
		public CellTooltipWindow (TreeView tree, TreeViewColumn col, TreePath path)
		{
			this.tree = tree;
			this.col = col;
			this.path = path;
			
			NudgeHorizontal = true;
			
			Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
			
			Gdk.Rectangle rect = tree.GetCellArea (path, col);
			rect.Inflate (2, 2);
			
			if (!tree.Model.GetIter (out iter, path)) {
				Hide ();
				return;
			}

			col.CellSetCellData (tree.Model, iter, false, false);
			
			int x = 0;
			int th = 0;
			CellRenderer[] renderers = col.CellRenderers;
			foreach (CellRenderer cr in renderers) {
				int sp, wi, he, xo, yo;
				col.CellGetPosition (cr, out sp, out wi);
				Gdk.Rectangle crect = new Gdk.Rectangle (x, rect.Y, wi, rect.Height);
				cr.GetSize (tree, ref crect, out xo, out yo, out wi, out he);
				if (cr != renderers [renderers.Length - 1])
					x += crect.Width + col.Spacing + 1;
				else
					x += wi + 1;
				if (he > th) th = he;
			}
			SetSizeRequest (x, th + 2);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			base.OnExposeEvent (evnt);
			Gdk.Rectangle rect = Allocation;
			col.CellSetCellData (tree.Model, iter, false, false);
			int x = 1;
			foreach (CellRenderer cr in col.CellRenderers) {
				int sp, wi, he, xo, yo;
				col.CellGetPosition (cr, out sp, out wi);
				Gdk.Rectangle colcrect = new Gdk.Rectangle (x, rect.Y, wi, rect.Height - 2);
				cr.GetSize (tree, ref colcrect, out xo, out yo, out wi, out he);
				int leftMargin = (int) ((colcrect.Width - wi) * cr.Xalign);
				int rightMargin = (int) ((colcrect.Height - he) * cr.Yalign);
				Gdk.Rectangle crect = new Gdk.Rectangle (colcrect.X + leftMargin, colcrect.Y + rightMargin + 1, wi, he);
				cr.Render (this.GdkWindow, tree, colcrect, crect, rect, CellRendererState.Focused);
				x += colcrect.Width + col.Spacing + 1;
			}
			return true;
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			Gdk.ModifierType mtype;
			bool ctrlKey = Gtk.Global.GetCurrentEventState (out mtype) && (mtype & Gdk.ModifierType.ControlMask) != 0;
			bool shiftKey = Gtk.Global.GetCurrentEventState (out mtype) && (mtype & Gdk.ModifierType.ShiftMask) != 0;
			GtkUtil.HideTooltip (tree);
			TreePath[] paths = tree.Selection.GetSelectedRows ();
			if (ctrlKey) {
				foreach (TreePath p in paths)
					tree.Selection.SelectPath (p);
				tree.Selection.SelectPath (path);
			}
			else if (shiftKey && paths.Length > 0) {
				tree.Selection.SelectRange (path, paths[0]);
			} else {
				tree.Selection.SelectPath (path);
				tree.SetCursor (path, col, false);
			}
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			MouseIsOver = false;
			GtkUtil.HideTooltip (tree);
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			MouseIsOver = true;
			return base.OnEnterNotifyEvent (evnt);
		}
	}
}
