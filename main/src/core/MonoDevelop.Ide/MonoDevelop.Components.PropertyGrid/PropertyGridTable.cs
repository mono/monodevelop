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
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

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
		static Xwt.Drawing.Image discloseDown = Xwt.Drawing.Image.FromResource ("disclose-arrow-down-16.png");
		static Xwt.Drawing.Image discloseUp = Xwt.Drawing.Image.FromResource ("disclose-arrow-up-16.png");
		bool heightMeasured;

		const int CategoryTopBottomPadding = 6;
		const int CategoryLeftPadding = 8;
		const int PropertyTopBottomPadding = 5;
		const int PropertyLeftPadding = 8;
		const int PropertyContentLeftPadding = 8;
		const int PropertyIndent = 8;

		const uint animationTimeSpan = 10;
		const int animationStepSize = 20;

		double dividerPosition = 0.5;

		Gdk.Cursor resizeCursor;
		Gdk.Cursor handCursor;

		readonly PropertyGrid parentGrid;

		class TableRow : ITypeDescriptorContext
		{
			readonly PropertyGrid parentGrid;

			public TableRow (PropertyGrid parentGrid)
			{
				this.parentGrid = parentGrid;
				//Accessible = new AccessibilityElementProxy ();
				//Accessible.SetRole (AtkCocoa.Roles.AXRow);
			}

			public bool IsCategory;
			public string Label;
			public object Instance;
			public PropertyDescriptor Property;
			public List<TableRow> ChildRows;
			public bool Expanded;
			public bool Enabled = true;
			public Gdk.Rectangle EditorBounds;
			public Gdk.Rectangle Bounds;
			public bool AnimatingExpand;
			public int AnimationHeight;
			public int ChildrenHeight;
			public uint AnimationHandle;

			//internal IAccessibilityElementProxy Accessible { get; private set; }

			public bool IsExpandable {
				get {
					return IsCategory || (ChildRows != null && ChildRows.Count > 0);
				}
			}

			bool focused;
			public bool Focused {
				get {
					return focused;
				}

				set {
					focused = value;
				}
			}

			bool ITypeDescriptorContext.OnComponentChanging ()
			{
				//TODO ITypeDescriptorContext.OnComponentChanging
				return true;
			}

			void ITypeDescriptorContext.OnComponentChanged ()
			{
				//TODO ITypeDescriptorContext.OnComponentChanged
			}

			IContainer ITypeDescriptorContext.Container {
				get {
					var site = parentGrid.Site;
					return site != null ? site.Container : null;
				}
			}

			object ITypeDescriptorContext.Instance {
				get { return Instance; }
			}

			PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor {
				get { return Property; }
			}

			object IServiceProvider.GetService (Type serviceType)
			{
				var site = parentGrid.Site;
				return site != null ? site.GetService (serviceType) : null;
			}
		}

		Gdk.Cursor cursor;
		Gdk.Cursor Cursor {
			get { return cursor; }
			set {
				if (cursor == value)
					return;
				GdkWindow.Cursor = cursor = value;
			}
		}

		public PropertyGridTable (EditorManager editorManager, PropertyGrid parentGrid)
		{
			GtkWorkarounds.FixContainerLeak (this);

			this.parentGrid = parentGrid;
			this.editorManager = editorManager;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			Events |= Gdk.EventMask.PointerMotionMask;
			CanFocus = true;
			resizeCursor = new Cursor (CursorType.SbHDoubleArrow);
			handCursor = new Cursor (CursorType.Hand1);

			// Accessibility
			Accessible.Role = Atk.Role.TreeTable;
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

		Dictionary<object,List<string>> expandedStatus;
		PropertyDescriptor lastEditedProperty;
		EditSession lastEditSession;

		class ReferenceEqualityComparer<T> : IEqualityComparer<T>
		{
			public bool Equals (T x, T y)
			{
				return object.ReferenceEquals (x, y);
			}
			public int GetHashCode (T obj)
			{
				return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode (obj);
			}
		}

		public void SaveStatus ()
		{
			//when the tree is rebuilt, there isn't a reliable way to match up new nodes to existing nodes
			//since the tree can be built dynamically, and there can be multiple instances of each type.
			//make a best attempt using reference equality to match objects and the name to match their properties.
			expandedStatus = new Dictionary<object,List<string>>(new ReferenceEqualityComparer<object> ());

			foreach (var r in rows) {
				if (!r.IsExpandable)
					continue;
				
				object key;
				string val;
				bool mark;
				if (r.IsCategory) {
					key = this;
					val = r.Label;
					mark = !r.Expanded;
				} else {
					key = r.Instance;
					val = r.Property.Name;
					mark = r.Expanded;
				}
				if (key == null || !mark) {
					continue;
				}
				List<string> list;
				if (!expandedStatus.TryGetValue (key, out list)) {
					expandedStatus [key] = list = new List<string> ();
				}
				list.Add (val);
			}
		}

		public void RestoreStatus ()
		{
			if (expandedStatus == null) {
				return;
			}

			foreach (var r in rows.Where (r => r.IsExpandable)) {
				object key;
				string val;
				if (r.IsCategory) {
					key = this;
					val = r.Label;
				} else {
					key = r.Instance;
					val = r.Property.Name;
				}
				List<string> list;
				var isMarked = expandedStatus.TryGetValue (key, out list) && list.Any (l => string.Equals (l, val));
				//categories are expanded by default, other things are not
				//we mark those that deviate from the defauult
				r.Expanded = r.IsCategory ^ isMarked;
			}

			expandedStatus = null;

			QueueDraw ();
			QueueResize ();
		}

		internal bool IsEditing {
			get {
				return editSession != null;
			}
		}

		internal void SaveEditSession ()
		{
			if (!IsEditing)
				return;

			lastEditedProperty = editSession.Property;
			lastEditSession = editSession;

			// Set the edit session to null explicitly so Clear does not end the edit session.
			editSession = null;
		}

		internal void RestoreEditSession ()
		{
			if (lastEditedProperty == null || lastEditSession == null)
				return;
			
			var newEditRow = FindRow (rows, lastEditedProperty);
			if (newEditRow != null) {
				currentEditorRow = newEditRow;
				editSession = lastEditSession;
			} else {
				editSession = lastEditSession;
				EndEditing ();
			}

			lastEditedProperty = null;
			lastEditSession = null;
		}

		static TableRow FindRow (IEnumerable<TableRow> rows, PropertyDescriptor property)
		{
			if (rows != null) {
				foreach (var row in rows) {
					if (row.Property == property)
						return row;

					var childRes = FindRow (row.ChildRows, property);
					if (childRes != null)
						return childRes;
				}
			}
			return null;
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
						TableRow row = new TableRow (parentGrid);
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
				if (row.Property != null && row.Property.Name == pd.Name && row.Instance == instance) {
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
			TableRow row = new TableRow (parentGrid) {
				IsCategory = false,
				Property = prop,
				Label = prop.DisplayName,
				Instance = instance
			};
			rowList.Add (row);

			TypeConverter tc = prop.Converter;
			if (tc.GetPropertiesSupported (row)) {
				object cob = prop.GetValue (instance);
				row.ChildRows = new List<TableRow> ();
				//TODO: should we support TypeDescriptor extensibility? how to merge with TypeConverter?
				foreach (PropertyDescriptor cprop in tc.GetProperties (row, cob)) {
					if (cprop.IsBrowsable) {
						AppendProperty (row.ChildRows, cprop, cob);
					}
				}
			}
		}

		PropertyEditorCell GetCell (TableRow row)
		{
			if (row.Property == null) {
				return null;
			}

			var e = editorManager.GetEditor (row);
			e.Initialize (this, editorManager, row);
			return e;
		}

		protected override void ForAll (bool includeInternals, Gtk.Callback callback)
		{
			base.ForAll (includeInternals, callback);
			foreach (var c in children.Keys.ToArray ()) {
				if (c.Parent == null) {
					LoggingService.LogError ("Error found unparented child in property grid:" + c.GetType ());
					continue;
				}
				callback (c);
			}
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
			if (currentEditor != null && currentEditorRow != null && children.ContainsKey (currentEditor))
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
				var width = Allocation.Width;
				if (r.IsCategory) {
					r.Bounds = new Gdk.Rectangle (0, y, width, h + CategoryTopBottomPadding * 2);
					y += h + CategoryTopBottomPadding * 2;
				}
				else {
					int eh;
					int dividerX = (int)((double)width * dividerPosition);
					var cell = GetCell (r);
					cell.GetSize (Allocation.Width - dividerX, out w, out eh);
					eh = Math.Max (h + PropertyTopBottomPadding * 2, eh);
					r.Bounds = new Gdk.Rectangle (0, y, Allocation.Width, eh);
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
				ctx.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				ctx.SetSourceColor (Styles.PropertyPadLabelBackgroundColor.ToCairoColor ());
				ctx.Fill ();
				ctx.MoveTo (dx + 0.5, 0);
				ctx.RelLineTo (0, Allocation.Height);
				ctx.SetSourceColor (Styles.PropertyPadDividerColor.ToCairoColor ());
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
					ctx.SetSourceColor (Styles.PadCategoryBackgroundColor.ToCairoColor ());
					ctx.Fill ();

					if (IsFocus && r.Focused) {
						ctx.Rectangle (1, y + 1, Allocation.Width - 2, rh - 3);
						ctx.SetDash (new double[] { 1, 1 }, 0.5);
						ctx.LineWidth = 1.0;
						ctx.SetSourceColor (Styles.BaseForegroundColor.ToCairoColor ());
						ctx.Stroke ();
					}

					if (lastCategory == null || lastCategory.Expanded || lastCategory.AnimatingExpand) {
						ctx.MoveTo (0, y + 0.5);
						ctx.LineTo (Allocation.Width, y + 0.5);
					}
					ctx.MoveTo (0, y + rh - 0.5);
					ctx.LineTo (Allocation.Width, y + rh - 0.5);
					ctx.SetSourceColor (Styles.PadCategoryBorderColor.ToCairoColor ());
					ctx.Stroke ();

					ctx.MoveTo (x, y + CategoryTopBottomPadding);
					ctx.SetSourceColor (Styles.PadCategoryLabelColor.ToCairoColor ());
					Pango.CairoHelper.ShowLayout (ctx, layout);

					var img = r.Expanded ? discloseUp : discloseDown;
					ctx.DrawImage (this, img, Allocation.Width - img.Width - CategoryTopBottomPadding, y + Math.Round ((rh - img.Height) / 2));

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
					ctx.SetSourceColor (Style.Text (state).ToCairoColor ());
					Pango.CairoHelper.ShowLayout (ctx, layout);
					ctx.Restore ();

					if (r != currentEditorRow) {
						var bounds = GetInactiveEditorBounds (r);

						cell.Render (GdkWindow, ctx, bounds, state);

						if (r.IsExpandable) {
							var img = r.Expanded ? discloseUp : discloseDown;
							ctx.DrawImage (
								this, img,
								Allocation.Width - img.Width - PropertyTopBottomPadding,
								y + Math.Round ((h + PropertyTopBottomPadding * 2 - img.Height) / 2)
							);
						}
					}

					y += r.EditorBounds.Height;
					indent = PropertyIndent;
				}

				if (r.ChildRows != null && r.ChildRows.Count > 0 && (r.Expanded || r.AnimatingExpand)) {
					int py = y;

					ctx.Save ();
					if (r.AnimatingExpand)
						ctx.Rectangle (0, y, Allocation.Width, r.AnimationHeight);
					else
						ctx.Rectangle (0, 0, Allocation.Width, Allocation.Height);

					ctx.Clip ();
					Draw (ctx, r.ChildRows, dividerX, x + indent, ref y);
					ctx.Restore ();

					if (r.AnimatingExpand) {
						y = py + r.AnimationHeight;
						// Repaing the background because the cairo clip doesn't work for gdk primitives
						int dx = (int)((double)Allocation.Width * dividerPosition);
						ctx.Rectangle (0, y, dx, Allocation.Height - y);
						ctx.SetSourceColor (Styles.PropertyPadLabelBackgroundColor.ToCairoColor ());
						ctx.Fill ();
						ctx.Rectangle (dx + 1, y, Allocation.Width - dx - 1, Allocation.Height - y);
						ctx.SetSourceColor (Styles.BrowserPadBackground.ToCairoColor());
						ctx.Fill ();
					}
				}
			}
			layout.Dispose ();
		}

		//when inactive, the editor bounds may be shrunk to make room for an expander
		Gdk.Rectangle GetInactiveEditorBounds (TableRow row)
		{
			var bounds = row.EditorBounds;
			if (row.IsExpandable) {
				bounds.Width -= (int) discloseUp.Width + PropertyTopBottomPadding;
			}
			return bounds;
		}

		IEnumerable<TableRow> GetAllRows (bool onlyVisible)
		{
			return GetAllRows (rows, onlyVisible);
		}

		IEnumerable<TableRow> GetAllRows (List<TableRow> rows, bool onlyVisible)
		{
			foreach (var r in rows) {
				yield return r;
				if (r.ChildRows != null && (!onlyVisible || r.Expanded || r.AnimatingExpand)) {
					foreach (var cr in GetAllRows (r.ChildRows, onlyVisible))
						yield return cr;
				}
			}
		}

		TableRow GetRowAtPoint (int x, int y)
		{
			return GetAllRows (true).FirstOrDefault (r => r.Bounds.Contains (x, y));
		}

		void ExpandOrCollapseRow (TableRow row)
		{
			row.Expanded = !row.Expanded;
			if (row.Expanded)
				StartExpandAnimation (row);
			else
				StartCollapseAnimation (row);
			QueueResize ();
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Type != EventType.ButtonPress)
				return base.OnButtonPressEvent (evnt);

			int dx = (int)((double)Allocation.Width * dividerPosition);
			if (Math.Abs (dx - evnt.X) < 4) {
				draggingDivider = true;
				Cursor = resizeCursor;
				return true;
			}

			var row = GetRowAtPoint ((int)evnt.X, (int)evnt.Y);

			if (row != null && editSession == null) {
				var bounds = GetInactiveEditorBounds (row);
				if (!bounds.IsEmpty && bounds.Contains ((int)evnt.X, (int)evnt.Y)) {
					EndEditing ();

					// Ending repopulates the tree, meaning that row no longer points to anything in the tree
					row = GetRowAtPoint ((int)evnt.X, (int)evnt.Y);

					StartEditing (row);
					return true;
				}

				if (row.IsExpandable) {
					ExpandOrCollapseRow (row);
					return true;
				}
			}

			EndEditing ();
			GrabFocus ();

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

			var row = GetAllRows (true).FirstOrDefault (r => r.Bounds.Contains ((int)evnt.X, (int)evnt.Y));
			if (row != null && row.IsExpandable) {
				var bounds = GetInactiveEditorBounds (row);
				if (bounds.IsEmpty || !bounds.Contains ((int)evnt.X, (int)evnt.Y)) {
					Cursor = handCursor;
					return true;
				}
			}

			int dx = (int)((double)Allocation.Width * dividerPosition);
			if (Math.Abs (dx - evnt.X) < 4) {
				Cursor = resizeCursor;
				return true;
			}
			ShowTooltip (evnt);
			Cursor = null;
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
				tooltipWindow.Hide ();
				tooltipWindow.Destroy ();
				tooltipWindow = null;
			}
		}

		void ShowTooltipWindow (int x, int y)
		{
			tooltipTimeout = 0;
			if (x >= Allocation.Width)
				return;
			var row = GetAllRows (true).FirstOrDefault (r => !r.IsCategory && y >= r.EditorBounds.Y && y <= r.EditorBounds.Bottom);
			if (row != null) {
				if (tooltipWindow == null) {
						tooltipWindow = TooltipPopoverWindow.Create ();
						tooltipWindow.ShowArrow = true;
				}
				var s = new System.Text.StringBuilder ("<b>" + row.Property.DisplayName + "</b>");
				s.AppendLine ();
				s.AppendLine ();
				s.Append (GLib.Markup.EscapeText (row.Property.Description));
				if (row.Property.Converter.CanConvertTo (row, typeof(string))) {
					var value = Convert.ToString (row.Property.GetValue (row.Instance));
					if (!string.IsNullOrEmpty (value)) {
						const int chunkLength = 200;
						var multiLineValue = string.Join (Environment.NewLine, Enumerable.Range (0, (int)Math.Ceiling ((double)value.Length / chunkLength)).Select (n => string.Concat (value.Skip (n * chunkLength).Take (chunkLength))));
						s.AppendLine ();
						s.AppendLine ();
						s.Append ("Value: ").Append (multiLineValue);
					}
				}
				tooltipWindow.Markup = s.ToString ();
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
				row.AnimationHandle = 0;
			} else
				row.AnimationHeight = 0;

			row.AnimatingExpand = true;
			row.AnimationHandle = GLib.Timeout.Add (animationTimeSpan, delegate {
				row.AnimationHeight += animationStepSize;
				QueueResize ();
				if (row.AnimationHeight >= row.ChildrenHeight) {
					row.AnimationHandle = 0;
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
				row.AnimationHandle = 0;
			} else {
				row.AnimationHeight = row.ChildrenHeight;
			}
			row.AnimatingExpand = true;
			row.AnimationHandle = GLib.Timeout.Add (animationTimeSpan, delegate {
				row.AnimationHeight -= animationStepSize;
				QueueResize ();
				if (row.AnimationHeight <= 0) {
					row.AnimationHandle = 0;
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
					r.AnimationHandle = 0;
					r.AnimatingExpand = false;
				}
			}
		}

		protected override void OnDragLeave (DragContext context, uint time_)
		{
			if (!draggingDivider)
				Cursor = null;
			base.OnDragLeave (context, time_);
		}

		internal void EndEditing ()
		{
			if (editSession != null) {
				Remove (currentEditor);
				currentEditor.Destroy ();
				currentEditor = null;
				editSession.Dispose ();
				editSession = null;

				parentGrid.Populate (saveEditSession: false);
			}
			QueueDraw ();
		}

		void StartEditing (TableRow row)
		{
			GrabFocus ();

			currentEditorRow = row;
			row.Focused = true;

			var cell = GetCell (row);
			if (cell == null) {
				GrabFocus ();
				QueueDraw ();
				return;
			}

			editSession = cell.StartEditing (row.EditorBounds, State);
			if (editSession == null) {
				return;
			}

			currentEditor = (Gtk.Widget) editSession.Editor;
			Add (currentEditor);
			SetAllocation (currentEditor, row.EditorBounds);
			currentEditor.Show ();
			currentEditor.GrabFocus ();

			var refreshAtt = row.Property.Attributes.OfType<RefreshPropertiesAttribute> ().FirstOrDefault ();
			var refresh = refreshAtt == null ? RefreshProperties.None : refreshAtt.RefreshProperties;
			editSession.Changed += delegate {
				if (refresh == RefreshProperties.Repaint) {
					parentGrid.Refresh ();
				} else if (refresh == RefreshProperties.All) {
					parentGrid.Populate(saveEditSession: true);
				}

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

		protected override bool OnFocused (DirectionType direction)
		{
			TableRow nextFocus = null;
			bool needsRedraw = false;

			if (currentEditorRow != null) {
				needsRedraw = true;
				currentEditorRow.Focused = false;

				// End editing here because it will cause a clear/populate cycle which will make all
				// the rows invalid
				if (!currentEditorRow.IsCategory) {
					EndEditing ();
				}
			}

			switch (direction) {
			case DirectionType.TabForward:
			case DirectionType.Down:
				nextFocus = GetNextRow (currentEditorRow);
				break;

			case DirectionType.TabBackward:
			case DirectionType.Up:
				nextFocus = GetPreviousRow (currentEditorRow);
				break;

			default:
				break;
			}

			currentEditorRow = nextFocus;

			if (currentEditorRow != null) {
				currentEditorRow.Focused = true;
				StartEditing (currentEditorRow);
			}

			if (needsRedraw) {
				QueueDraw ();
			}

			return currentEditorRow != null;
		}

		protected override void OnActivate ()
		{
			if (currentEditorRow != null && currentEditorRow.IsExpandable) {
				ExpandOrCollapseRow (currentEditorRow);
				return;
			}
			base.OnActivate ();
		}

		// When we stop editing a row, the tree gets rebuilt
		// There isn't a reliable method to match old rows with new rows
		// so check Labels and Property.Name
		// see SaveStatus
		bool CompareRows (TableRow r1, TableRow r2)
		{
			if (r1 == null || r2 == null) {
				return false;
			}

			return (r1 == r2) ||
				(r1.IsCategory && r2.IsCategory && r1.Label == r2.Label) ||
				(r1.Property != null && r2.Property != null && r1.Property.Name == r2.Property.Name);
		}

		TableRow GetNextRow (TableRow row)
		{
			bool found = false;
			foreach (var r in GetAllRows (true)) {
				if (!r.Enabled) {
					continue;
				}

				if (row == null || found) {
					return r;
				}

				if (CompareRows (row, r)) {
					found = true;
				}
			}
			return null;
		}

		TableRow GetPreviousRow (TableRow row)
		{
			TableRow prev = null;
			foreach (var r in GetAllRows (true)) {
				if (!r.Enabled)
					continue;

				if (CompareRows (row, r))
					return prev;

				prev = r;
			}

			// Return last row
			return prev;
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

