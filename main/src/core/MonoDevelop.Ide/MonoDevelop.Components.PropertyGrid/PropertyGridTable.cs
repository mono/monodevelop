// 
// PropertyGridTable.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc
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
using Gtk;
using Gdk;
using System.ComponentModel;
using System.Collections.Generic;
using Cairo;
using System.Linq;

namespace MonoDevelop.Components.PropertyGrid
{
	class PropertyGridTable: Gtk.EventBox
	{
		EditorManager editorManager;
		List<TableRow> rows = new List<TableRow> ();
		Dictionary<Gtk.Widget, Gdk.Rectangle> children = new Dictionary<Gtk.Widget, Gdk.Rectangle> ();
		EditSession editSession;
		Gtk.Widget currentEditor;
		TableRow currentEditorRow;
		bool draggingDivider;
		Gdk.Pixbuf discloseDown;
		Gdk.Pixbuf discloseUp;
		bool heightMeasured;

		const int CategoryTopBottomPadding = 6;
		const int CategoryLeftPadding = 8;
		const int PropertyTopBottomPadding = 5;
		const int PropertyLeftPadding = 8;
		const int PropertyContentLeftPadding = 8;
		const int PropertyIndent = 8;
		static readonly Cairo.Color LabelBackgroundColor = new Cairo.Color (250d/255d, 250d/255d, 250d/255d);
		static readonly Cairo.Color DividerColor = new Cairo.Color (217d/255d, 217d/255d, 217d/255d);
		static readonly Cairo.Color CategoryLabelColor = new Cairo.Color (128d/255d, 128d/255d, 128d/255d);

		const uint animationTimeSpan = 10;
		const int animationStepSize = 20;

		double dividerPosition = 0.5;

		Gdk.Cursor resizeCursor;
		Gdk.Cursor handCursor;

		class TableRow
		{
			public bool IsCategory;
			public string Label;
			public object Instace;
			public PropertyDescriptor Property;
			public List<TableRow> ChildRows;
			public bool Expanded;
			public bool Enabled = true;
			public Gdk.Rectangle EditorBounds;
			public bool AnimatingExpand;
			public int AnimationHeight;
			public int ChildrenHeight;
			public uint AnimationHandle;
		}

		public PropertyGridTable (EditorManager editorManager, PropertyGrid parentGrid)
		{
			Mono.TextEditor.GtkWorkarounds.FixContainerLeak (this);

			this.editorManager = editorManager;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			Events |= Gdk.EventMask.PointerMotionMask;
			CanFocus = true;
			resizeCursor = new Cursor (CursorType.SbHDoubleArrow);
			handCursor = new Cursor (CursorType.Hand1);
			discloseDown = Gdk.Pixbuf.LoadFromResource ("disclose-arrow-down.png");
			discloseUp = Gdk.Pixbuf.LoadFromResource ("disclose-arrow-up.png");
		}

		protected override void OnDestroyed ()
		{
			StopAllAnimations ();
			base.OnDestroyed ();
			resizeCursor.Dispose ();
			handCursor.Dispose ();
		}

		public event EventHandler Changed;

		public PropertySort PropertySort { get; set; }

		public ShadowType ShadowType { get; set; }

		public void CommitChanges ()
		{
			EndEditing ();
		}

		HashSet<string> expandedStatus;

		public void SaveStatus ()
		{
			expandedStatus = new HashSet<string> ();
			foreach (var r in rows.Where (r => r.IsCategory))
				if (!r.Expanded)
					expandedStatus.Add (r.Label);
		}

		public void RestoreStatus ()
		{
			if (expandedStatus == null)
				return;

			foreach (var row in rows.Where (r => r.IsCategory))
				row.Expanded = !expandedStatus.Contains (row.Label);

			expandedStatus = null;

			QueueDraw ();
			QueueResize ();
		}

		public virtual void Clear ()
		{
			heightMeasured = false;
			StopAllAnimations ();
			EndEditing ();
			rows.Clear ();
			QueueDraw ();
			QueueResize ();
		}

		internal void Populate (PropertyDescriptorCollection properties, object instance)
		{
			bool categorised = PropertySort == PropertySort.Categorized;
		
			//transcribe browsable properties
			var sorted = new List<PropertyDescriptor>();

			foreach (PropertyDescriptor descriptor in properties)
				if (descriptor.IsBrowsable)
					sorted.Add (descriptor);
			
			if (sorted.Count == 0)
				return;
			
			if (!categorised) {
				if (PropertySort != PropertySort.NoSort)
					sorted.Sort ((a,b) => a.DisplayName.CompareTo (b.DisplayName));
				foreach (PropertyDescriptor pd in sorted)
					AppendProperty (rows, pd, instance);
			}
			else {
				sorted.Sort ((a,b) => {
					var c = a.Category.CompareTo (b.Category);
					return c != 0 ? c : a.DisplayName.CompareTo (b.DisplayName);
				});
				TableRow lastCat = null;
				List<TableRow> rowList = rows;

				foreach (PropertyDescriptor pd in sorted) {
					if (!string.IsNullOrEmpty (pd.Category) && (lastCat == null || pd.Category != lastCat.Label)) {
						TableRow row = new TableRow ();
						row.IsCategory = true;
						row.Expanded = true;
						row.Label = pd.Category;
						row.ChildRows = new List<TableRow> ();
						rows.Add (row);
						lastCat = row;
						rowList = row.ChildRows;
					}
					AppendProperty (rowList, pd, instance);
				}
			}
			QueueDraw ();
			QueueResize ();
		}
		
		internal void Update (PropertyDescriptorCollection properties, object instance)
		{
			foreach (PropertyDescriptor pd in properties)
				UpdateProperty (pd, instance, rows);
			QueueDraw ();
			QueueResize ();
		}
		
		bool UpdateProperty (PropertyDescriptor pd, object instance, IEnumerable<TableRow> rowList)
		{
			foreach (var row in rowList) {
				if (row.Property != null && row.Property.Name == pd.Name && row.Instace == instance) {
					row.Property = pd;
					return true;
				}
				if (row.ChildRows != null) {
					if (UpdateProperty (pd, instance, row.ChildRows))
						return true;
				}
			}
			return false;
		}
	
		void AppendProperty (PropertyDescriptor prop, object instance)
		{
			AppendProperty (rows, prop, new InstanceData (instance));
		}
		
		void AppendProperty (List<TableRow> rowList, PropertyDescriptor prop, object instance)
		{
			TableRow row = new TableRow () {
				IsCategory = false,
				Property = prop,
				Label = prop.DisplayName,
				Instace = instance
			};
			rowList.Add (row);

			TypeConverter tc = prop.Converter;
			if (typeof (ExpandableObjectConverter).IsAssignableFrom (tc.GetType ())) {
				object cob = prop.GetValue (instance);
				row.ChildRows = new List<TableRow> ();
				foreach (PropertyDescriptor cprop in TypeDescriptor.GetProperties (cob))
					AppendProperty (row.ChildRows, cprop, cob);
			}
		}

		PropertyEditorCell GetCell (TableRow row)
		{
			var e = editorManager.GetEditor (row.Property);
			e.Initialize (this, editorManager, row.Property, row.Instace);
			return e;
		}
		
		protected override void ForAll (bool includeInternals, Gtk.Callback callback)
		{
			base.ForAll (includeInternals, callback);
			foreach (var c in children.Keys.ToArray ())
				callback (c);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Width = 20;

			int dx = (int)((double)Allocation.Width * dividerPosition) - PropertyContentLeftPadding;
			if (dx < 0) dx = 0;
			int y = 0;
			MeasureHeight (rows, ref y);
			requisition.Height = y;

			foreach (var c in children)
				c.Key.SizeRequest ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			int y = 0;
			MeasureHeight (rows, ref y);
			if (currentEditorRow != null)
				children [currentEditor] = currentEditorRow.EditorBounds;
			foreach (var cr in children) {
				var r = cr.Value;
				cr.Key.SizeAllocate (new Gdk.Rectangle (r.X, r.Y, r.Width, r.Height));
			}
		}

		public void SetAllocation (Gtk.Widget w, Gdk.Rectangle rect)
		{
			children [w] = rect;
			QueueResize ();
		}
		
		protected override void OnAdded (Gtk.Widget widget)
		{
			children.Add (widget, new Gdk.Rectangle (0,0,0,0));
			widget.Parent = this;
			QueueResize ();
		}
		
		protected override void OnRemoved (Gtk.Widget widget)
		{
			children.Remove (widget);
			widget.Unparent ();
		}

		void MeasureHeight (IEnumerable<TableRow> rowList, ref int y)
		{
			heightMeasured = true;
			Pango.Layout layout = new Pango.Layout (PangoContext);
			foreach (var r in rowList) {
				layout.SetText (r.Label);
				int w,h;
				layout.GetPixelSize (out w, out h);
				if (r.IsCategory) {
					r.EditorBounds = new Gdk.Rectangle (0, y, Allocation.Width, h + CategoryTopBottomPadding * 2);
					y += h + CategoryTopBottomPadding * 2;
				}
				else {
					int eh;
					int dividerX = (int)((double)Allocation.Width * dividerPosition);
					var cell = GetCell (r);
					cell.GetSize (Allocation.Width - dividerX, out w, out eh);
					eh = Math.Max (h + PropertyTopBottomPadding * 2, eh);
					r.EditorBounds = new Gdk.Rectangle (dividerX + PropertyContentLeftPadding, y, Allocation.Width - dividerX - PropertyContentLeftPadding, eh);
					y += eh;
				}
				if (r.ChildRows != null && (r.Expanded || r.AnimatingExpand)) {
					int py = y;
					MeasureHeight (r.ChildRows, ref y);
					r.ChildrenHeight = y - py;
					if (r.AnimatingExpand)
						y = py + r.AnimationHeight;
				}
			}
			layout.Dispose ();
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (Cairo.Context ctx = CairoHelper.Create (evnt.Window)) {
				int dx = (int)((double)Allocation.Width * dividerPosition);
				ctx.LineWidth = 1;
				ctx.Rectangle (0, 0, dx, Allocation.Height);
				ctx.Color = LabelBackgroundColor;
				ctx.Fill ();
				ctx.Rectangle (dx, 0, Allocation.Width - dx, Allocation.Height);
				ctx.Color = new Cairo.Color (1, 1, 1);
				ctx.Fill ();
				ctx.MoveTo (dx + 0.5, 0);
				ctx.RelLineTo (0, Allocation.Height);
				ctx.Color = DividerColor;
				ctx.Stroke ();
	
				int y = 0;
				Draw (ctx, rows, dx, PropertyLeftPadding, ref y);
			}
			return base.OnExposeEvent (evnt);
		}

		void Draw (Cairo.Context ctx, List<TableRow> rowList, int dividerX, int x, ref int y)
		{
			if (!heightMeasured)
				return;

			Pango.Layout layout = new Pango.Layout (PangoContext);
			TableRow lastCategory = null;

			foreach (var r in rowList) {
				int w,h;
				layout.SetText (r.Label);
				layout.GetPixelSize (out w, out h);
				int indent = 0;

				if (r.IsCategory) {
					var rh = h + CategoryTopBottomPadding*2;
					ctx.Rectangle (0, y, Allocation.Width, rh);
					Cairo.LinearGradient gr = new LinearGradient (0, y, 0, rh);
					gr.AddColorStop (0, new Cairo.Color (248d/255d, 248d/255d, 248d/255d));
					gr.AddColorStop (1, new Cairo.Color (240d/255d, 240d/255d, 240d/255d));
					ctx.Pattern = gr;
					ctx.Fill ();

					if (lastCategory == null || lastCategory.Expanded || lastCategory.AnimatingExpand) {
						ctx.MoveTo (0, y + 0.5);
						ctx.LineTo (Allocation.Width, y + 0.5);
					}
					ctx.MoveTo (0, y + rh - 0.5);
					ctx.LineTo (Allocation.Width, y + rh - 0.5);
					ctx.Color = DividerColor;
					ctx.Stroke ();

					ctx.MoveTo (x, y + CategoryTopBottomPadding);
					ctx.Color = CategoryLabelColor;
					Pango.CairoHelper.ShowLayout (ctx, layout);

					var img = r.Expanded ? discloseUp : discloseDown;
					CairoHelper.SetSourcePixbuf (ctx, img, Allocation.Width - img.Width - CategoryTopBottomPadding, y + (rh - img.Height) / 2);
					ctx.Paint ();

					y += rh;
					lastCategory = r;
				}
				else {
					var cell = GetCell (r);
					r.Enabled = !r.Property.IsReadOnly || cell.EditsReadOnlyObject;
					var state = r.Enabled ? State : Gtk.StateType.Insensitive;
					ctx.Save ();
					ctx.Rectangle (0, y, dividerX, h + PropertyTopBottomPadding*2);
					ctx.Clip ();
					ctx.MoveTo (x, y + PropertyTopBottomPadding);
					ctx.Color = Style.Text (state).ToCairoColor ();
					Pango.CairoHelper.ShowLayout (ctx, layout);
					ctx.Restore ();

					if (r != currentEditorRow)
						cell.Render (GdkWindow, r.EditorBounds, state);

					y += r.EditorBounds.Height;
					indent = PropertyIndent;
				}

				if (r.ChildRows != null && r.ChildRows.Count > 0 && (r.Expanded || r.AnimatingExpand)) {
					int py = y;
					if (r.AnimatingExpand) {
						ctx.Save ();
						ctx.Rectangle (0, y, Allocation.Width, r.AnimationHeight);
						ctx.Clip ();
					}

					Draw (ctx, r.ChildRows, dividerX, x + indent, ref y);

					if (r.AnimatingExpand) {
						ctx.Restore ();
						y = py + r.AnimationHeight;
						// Repaing the background because the cairo clip doesn't work for gdk primitives
						int dx = (int)((double)Allocation.Width * dividerPosition);
						ctx.Rectangle (0, y, dx, Allocation.Height - y);
						ctx.Color = LabelBackgroundColor;
						ctx.Fill ();
						ctx.Rectangle (dx + 1, y, Allocation.Width - dx - 1, Allocation.Height - y);
						ctx.Color = new Cairo.Color (1, 1, 1);
						ctx.Fill ();
					}
				}
			}
		}

		IEnumerable<TableRow> GetAllRows (bool onlyVisible)
		{
			return GetAllRows (rows, onlyVisible);
		}

		IEnumerable<TableRow> GetAllRows (IEnumerable<TableRow> rows, bool onlyVisible)
		{
			foreach (var r in rows) {
				yield return r;
				if (r.ChildRows != null && (!onlyVisible || r.Expanded || r.AnimatingExpand)) {
					foreach (var cr in GetAllRows (r.ChildRows, onlyVisible))
						yield return cr;
				}
			}
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Type != EventType.ButtonPress)
				return base.OnButtonPressEvent (evnt);

			var cat = rows.FirstOrDefault (r => r.IsCategory && r.EditorBounds.Contains ((int)evnt.X, (int)evnt.Y));
			if (cat != null) {
				cat.Expanded = !cat.Expanded;
				if (cat.Expanded)
					StartExpandAnimation (cat);
				else
					StartCollapseAnimation (cat);
				QueueResize ();
				return true;
			}

			int dx = (int)((double)Allocation.Width * dividerPosition);
			if (Math.Abs (dx - evnt.X) < 4) {
				draggingDivider = true;
				GdkWindow.Cursor = resizeCursor;
				return true;
			}

			TableRow clickedEditor = null;
			foreach (var r in GetAllRows (true).Where (r => !r.IsCategory)) {
				if (r.EditorBounds.Contains ((int)evnt.X, (int)evnt.Y)) {
					clickedEditor = r;
					break;
				}
			}
			if (clickedEditor != null && clickedEditor.Enabled)
				StartEditing (clickedEditor);
			else {
				EndEditing ();
				GrabFocus ();
			}

			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (draggingDivider) {
				draggingDivider = false;
				QueueResize ();
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if (draggingDivider) {
				var px = evnt.X;
				if (px < 10)
					px = 10;
				else if (px > Allocation.Width - 10)
					px = Allocation.Width - 10;
				dividerPosition = px / (double) Allocation.Width;
				QueueResize ();
				return true;
			}

			var cat = rows.FirstOrDefault (r => r.IsCategory && r.EditorBounds.Contains ((int)evnt.X, (int)evnt.Y));
			if (cat != null) {
				GdkWindow.Cursor = handCursor;
				return true;
			}

			int dx = (int)((double)Allocation.Width * dividerPosition);
			if (Math.Abs (dx - evnt.X) < 4) {
				GdkWindow.Cursor = resizeCursor;
				return true;
			}
			ShowTooltip (evnt);
			GdkWindow.Cursor = null;
			return base.OnMotionNotifyEvent (evnt);
		}

		uint tooltipTimeout;
		TooltipPopoverWindow tooltipWindow;

		void ShowTooltip (EventMotion evnt)
		{
			HideTooltip ();
			tooltipTimeout = GLib.Timeout.Add (500, delegate {
				ShowTooltipWindow ((int)evnt.X, (int)evnt.Y);
				return false;
			});
		}

		void HideTooltip ()
		{
			if (tooltipTimeout != 0) {
				GLib.Source.Remove (tooltipTimeout);
				tooltipTimeout = 0;
			}
			if (tooltipWindow != null) {
				tooltipWindow.Destroy ();
				tooltipWindow = null;
			}
		}

		void ShowTooltipWindow (int x, int y)
		{
			tooltipTimeout = 0;
			int dx = (int)((double)Allocation.Width * dividerPosition);
			if (x >= dx)
				return;
			var row = GetAllRows (true).FirstOrDefault (r => !r.IsCategory && y >= r.EditorBounds.Y && y <= r.EditorBounds.Bottom);
			if (row != null) {
				tooltipWindow = new TooltipPopoverWindow ();
				tooltipWindow.ShowArrow = true;
				var s = "<b>" + row.Property.DisplayName + "</b>\n\n";
				s += GLib.Markup.EscapeText (row.Property.Description);
				tooltipWindow.Markup = s;
				tooltipWindow.ShowPopup (this, new Gdk.Rectangle (0, row.EditorBounds.Y, Allocation.Width, row.EditorBounds.Height), PopupPosition.Right);
			}
		}

		protected override void OnUnrealized ()
		{
			HideTooltip ();
			base.OnUnrealized ();
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			HideTooltip ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		void StartExpandAnimation (TableRow row)
		{
			EndEditing ();
			if (row.AnimatingExpand) {
				GLib.Source.Remove (row.AnimationHandle);
			} else
				row.AnimationHeight = 0;

			row.AnimatingExpand = true;
			row.AnimationHandle = GLib.Timeout.Add (animationTimeSpan, delegate {
				row.AnimationHeight += animationStepSize;
				QueueResize ();
				if (row.AnimationHeight >= row.ChildrenHeight) {
					row.AnimatingExpand = false;
					return false;
				}
				return true;
			});
		}

		void StartCollapseAnimation (TableRow row)
		{
			EndEditing ();
			if (row.AnimatingExpand) {
				GLib.Source.Remove (row.AnimationHandle);
			} else {
				row.AnimationHeight = row.ChildrenHeight;
			}
			row.AnimatingExpand = true;
			row.AnimationHandle = GLib.Timeout.Add (animationTimeSpan, delegate {
				row.AnimationHeight -= animationStepSize;
				QueueResize ();
				if (row.AnimationHeight <= 0) {
					row.AnimatingExpand = false;
					return false;
				}
				return true;
			});
		}

		void StopAllAnimations ()
		{
			foreach (var r in GetAllRows (false)) {
				if (r.AnimatingExpand) {
					GLib.Source.Remove (r.AnimationHandle);
					r.AnimatingExpand = false;
				}
			}
		}

		protected override void OnDragLeave (DragContext context, uint time_)
		{
			if (!draggingDivider)
				GdkWindow.Cursor = null;
			base.OnDragLeave (context, time_);
		}

		void EndEditing ()
		{
			if (editSession != null) {
				Remove (currentEditor);
				currentEditor.Destroy ();
				editSession.Dispose ();
				editSession = null;
				currentEditorRow = null;
				QueueDraw ();
			}
		}

		void StartEditing (TableRow row)
		{
			EndEditing ();
			currentEditorRow = row;
			var cell = GetCell (row);
			editSession = cell.StartEditing (row.EditorBounds, State);
			currentEditor = (Gtk.Widget) editSession.Editor;
			Add (currentEditor);
			SetAllocation (currentEditor, row.EditorBounds);
			currentEditor.Show ();
			currentEditor.CanFocus = true;
			currentEditor.GrabFocus ();
			ConnectTabEvent (currentEditor);
			editSession.Changed += delegate {
				if (Changed != null)
					Changed (this, EventArgs.Empty);
			};
			var vs = ((Gtk.Viewport)Parent).Vadjustment;
			if (row.EditorBounds.Top < vs.Value)
				vs.Value = row.EditorBounds.Top;
			else if (row.EditorBounds.Bottom > vs.Value + vs.PageSize)
				vs.Value = row.EditorBounds.Bottom - vs.PageSize;
			QueueDraw ();
		}

		void ConnectTabEvent (Gtk.Widget w)
		{
			w.KeyPressEvent += HandleKeyPressEvent;
			if (w is Gtk.Container) {
				foreach (var c in ((Gtk.Container)w).Children)
					ConnectTabEvent (c);
			}
		}

		[GLib.ConnectBefore]
		void HandleKeyPressEvent (object o, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Tab || args.Event.Key == Gdk.Key.ISO_Left_Tab || args.Event.Key == Gdk.Key.KP_Tab) {
				var r = args.Event.State == ModifierType.ShiftMask ? GetPreviousRow (currentEditorRow) : GetNextRow (currentEditorRow);
				if (r != null) {
					Gtk.Application.Invoke (delegate {
						StartEditing (r);
					});
					args.RetVal = true;
				}
			}
		}

		TableRow GetNextRow (TableRow row)
		{
			bool found = false;
			foreach (var r in GetAllRows (true)) {
				if (r.IsCategory || !r.Enabled)
					continue;
				if (found)
					return r;
				if (r == row)
					found = true;
			}
			return null;
		}

		TableRow GetPreviousRow (TableRow row)
		{
			TableRow prev = null;
			foreach (var r in GetAllRows (true)) {
				if (r.IsCategory || !r.Enabled)
					continue;
				if (r == row)
					return prev;
				prev = r;
			}
			return null;
		}
	}

	class InstanceData 
	{
		public InstanceData (object instance) 
		{
			Instance = instance;
		}
		
		public object Instance;
	}
}

