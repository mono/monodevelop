//
// DockItem.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using Gtk;
using Mono.Unix;

namespace MonoDevelop.Components.Docking
{
	public class DockItem
	{
		IDockItemBackend backend;
		Control content;

		string defaultLocation;
		bool defaultVisible = true;
		DockItemStatus defaultStatus = DockItemStatus.Dockable;
		string id;
		DockFrame frame;
		int defaultWidth = -1;
		int defaultHeight = -1;
		string label;
		Xwt.Drawing.Image icon;
		bool expand;
		DockItemBehavior behavior;
		bool lastVisibleStatus;
		bool lastContentVisibleStatus;
		bool gettingContent;
		bool isPositionMarker;
		bool stickyVisible;
		IDockItemLabelProvider dockLabelProvider;
		DockItemToolbar toolbarTop;
		DockItemToolbar toolbarBottom;
		DockItemToolbar toolbarLeft;
		DockItemToolbar toolbarRight;

		DockVisualStyle regionStyle;
		DockVisualStyle itemStyle;
		DockVisualStyle currentVisualStyle;

		public event EventHandler VisibleChanged;
		public event EventHandler ContentVisibleChanged;
		public event EventHandler ContentRequired;
		
		internal DockItem (DockFrame frame, string id)
		{
			this.frame = frame;
			this.id = id;
			currentVisualStyle = regionStyle = frame.GetRegionStyleForItem (this);
		}

		internal void Init (IDockItemBackend backend)
		{
			this.backend = backend;
			backend.SetStyle (currentVisualStyle);
		}

		internal IDockItemBackend Backend {
			get { return backend; }
		}
		
		public string Id {
			get { return id; }
		}

		internal bool StickyVisible {
			get { return stickyVisible; }
			set { stickyVisible = value; }
		}
		
		public string Label {
			get { return label ?? string.Empty; }
			set {
				label = value; 
				backend.SetLabel (label);
			}
		}

		public bool Visible {
			get {
				return frame.GetVisible (this); 
			}
			set {
				stickyVisible = value;
				frame.SetVisible (this, value);
				UpdateVisibleStatus ();
			}
		}
		
		public bool VisibleInLayout (string layout)
		{
			return frame.GetVisible (this, layout); 
		}
		
		public DockItemStatus Status {
			get {
				return frame.GetStatus (this); 
			}
			set {
				frame.SetStatus (this, value);
			}
		}
		
		public IDockItemLabelProvider DockLabelProvider {
			get { return this.dockLabelProvider; }
			set { this.dockLabelProvider = value; }
		}
		
		public DockVisualStyle VisualStyle {
			get { return itemStyle; }
			set { itemStyle = value; UpdateStyle (); }
		}

		internal void SetRegionStyle (DockVisualStyle style)
		{
			regionStyle = style;
			UpdateStyle ();
		}

		void UpdateStyle ()
		{
			var s = itemStyle != null ? itemStyle : regionStyle;
			if (s != currentVisualStyle) {
				currentVisualStyle = s;
				backend.SetStyle (s);
			}
		}

		internal void RequestContent ()
		{
			if (ContentRequired != null)
				ContentRequired (this, EventArgs.Empty);
		}

		public Control Content {
			get {
				return content;
			}
			set {
				content = value;
				backend.SetContent (content);
			}
		}
		
		public DockItemToolbar GetToolbar (DockPositionType position)
		{
			switch (position) {
			case DockPositionType.Top:
				if (toolbarTop == null)
					toolbarTop = backend.CreateToolbar (DockPositionType.Top);
				return toolbarTop;
			case DockPositionType.Bottom:
				if (toolbarBottom == null)
					toolbarBottom = backend.CreateToolbar (DockPositionType.Bottom);
				return toolbarBottom;
			case DockPositionType.Left:
				if (toolbarLeft == null)
					toolbarLeft = backend.CreateToolbar (DockPositionType.Left);
				return toolbarLeft;
			case DockPositionType.Right:
				if (toolbarRight == null)
					toolbarRight = backend.CreateToolbar (DockPositionType.Right);
				return toolbarRight;
			default: throw new ArgumentException ();
			}
		}
		
		public string DefaultLocation {
			get { return defaultLocation; }
			set { defaultLocation = value; }
		}

		public bool DefaultVisible {
			get { return defaultVisible; }
			set { defaultVisible = value; }
		}

		public DockItemStatus DefaultStatus {
			get { return defaultStatus; }
			set { defaultStatus = value; }
		}

		public int DefaultWidth {
			get {
				return defaultWidth;
			}
			set {
				defaultWidth = value;
			}
		}

		public int DefaultHeight {
			get {
				return defaultHeight;
			}
			set {
				defaultHeight = value;
			}
		}

		public Xwt.Drawing.Image Icon {
			get {
				return icon;
			}
			set {
				icon = value;
				backend.SetIcon (icon);
			}
		}

		public DockItemBehavior Behavior {
			get {
				return behavior;
			}
			set {
				behavior = value;
				backend.SetBehavior (behavior);
			}
		}

		public bool Expand {
			get {
				return expand;
			}
			set {
				expand = value;
			}
		}

		public void Present (bool giveFocus)
		{
			backend.Present (giveFocus);
		}

		public bool ContentVisible {
			get {
				return backend.ContentVisible;
			}
		}
		
		public void SetDockLocation (string location)
		{
			frame.SetDockLocation (this, location);
		}

		internal void UpdateVisibleStatus ()
		{
			bool vis = frame.GetVisible (this);
			if (vis != lastVisibleStatus) {
				lastVisibleStatus = vis;
				if (VisibleChanged != null)
					VisibleChanged (this, EventArgs.Empty);
			}
			UpdateContentVisibleStatus ();
		}
		
		internal void UpdateContentVisibleStatus ()
		{
			bool vis = ContentVisible;
			if (vis != lastContentVisibleStatus) {
				lastContentVisibleStatus = vis;
				if (ContentVisibleChanged != null)
					ContentVisibleChanged (this, EventArgs.Empty);
			}
		}
		
		internal void ShowWidget ()
		{
			backend.ShowWidget ();
		}
		
		internal void HideWidget ()
		{
			backend.HideWidget ();
		}

		internal void SetFloatMode (Xwt.Rectangle rect)
		{
			SetRegionStyle (frame.GetRegionStyleForItem (this));
			backend.SetFloatMode (rect);
		}
		
		internal Xwt.Rectangle FloatingPosition {
			get {
				return backend.FloatingPosition;
			}
		}

		internal void SetAutoHideMode (DockPositionType pos, int size)
		{
			backend.SetAutoHideMode (pos, size);
			SetRegionStyle (frame.GetRegionStyleForItem (this));
		}
		
		internal int AutoHideSize {
			get {
				return backend.AutoHideSize;
			}
		}

		internal void Minimize ()
		{
			backend.Minimize ();
		}

		internal bool IsPositionMarker {
			get {
				return isPositionMarker;
			}
			set {
				isPositionMarker = value;
			}
		}

		public Xwt.Rectangle GetAllocation ()
		{
			return backend.GetAllocation ();
		}
	}

	public interface IDockItemLabelProvider
	{
		Gtk.Widget CreateLabel (Orientation orientation);
	}
}
