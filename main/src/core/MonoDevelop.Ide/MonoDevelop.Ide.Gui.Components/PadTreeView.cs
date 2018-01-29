// 
// PadTreeView.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using Gtk;

namespace MonoDevelop.Ide.Gui.Components
{
	public class PadTreeView : MonoDevelop.Components.ContextMenuTreeView
	{
		PadFontChanger changer;
		CellRendererText textRenderer = new CellRendererText ();
		readonly List<CellRendererText> renderers = new List<CellRendererText> ();

		static void ResetSearchColumn (object sender, EventArgs args)
		{
			// The Gtk documentation is incorrect for SearchColumn.
			// SearchColumn does not get reset to -1 when the model changes,
			// it gets reset to the first string column which then reenables the ctrl+f search feature
			//
			// This is probably not the behaviour expected so we work around it here.

			var tree = sender as TreeView;
			tree.SearchColumn = -1;
		}

		public PadTreeView ()
		{
			Init ();
		}
		
		public PadTreeView (TreeModel model) : base (model)
		{
			Init ();
		}

		void Init ()
		{
			changer = new PadFontChanger (this,
				delegate (Pango.FontDescription desc) {
					textRenderer.FontDesc = desc;
					foreach (var renderer in renderers)
						renderer.FontDesc = desc;
				}, ColumnsAutosize);
			MonoDevelop.Components.GtkUtil.EnableAutoTooltips (this);

			EnableSearch = false;
			SearchColumn = -1;

			AddNotification ("model", ResetSearchColumn);
		}

		public CellRendererText TextRenderer {
			get { return textRenderer; }
		}

		protected override void OnDestroyed ()
		{
			if (changer != null) {
				changer.Dispose ();
				changer = null;
			}
			renderers.Clear ();
			base.OnDestroyed ();
		}

		// Workaround for Bug 1698 - Error list scroll position doesn't reset when list changes, hides items
		// If the store of a pad treeview is modified while the pad is unrealized (autohidden), the treeview
		// doesn't update its internal vertical offset. This can lead to items becoming offset outside the 
		// visible area and therefore becoming unreachable. The only way to force the treeview to recalculate
		// this offset is by setting the Vadjustment.Value, but it ignores values the same as the current value.
		// Therefore we simply set it to something slightly different then back again.
		
		bool forceInternalOffsetUpdate;
		
		protected override void OnUnrealized ()
		{
			base.OnUnrealized ();
			forceInternalOffsetUpdate = true;
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (forceInternalOffsetUpdate && IsRealized) {
				forceInternalOffsetUpdate = false;
				var v = Vadjustment.Value;
				int delta = v > 2? 0 : 1;
				Vadjustment.Value = v + delta;
				Vadjustment.Value = v;
			}
		}

		internal void RegisterRenderForFontChanges (CellRendererText renderer)
		{
			renderer.FontDesc = IdeApp.Preferences.CustomPadFont;
			renderers.Add (renderer);
		}
	}
}