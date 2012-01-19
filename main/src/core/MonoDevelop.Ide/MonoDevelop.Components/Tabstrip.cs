// 
// Tabstrip.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.ComponentModel;
using System.Drawing.Design;
using Cairo;
using Gtk;
using Mono.TextEditor;
using System.Linq;

namespace MonoDevelop.Components
{
	[ToolboxItem (true)]
	public class Tabstrip : DrawingArea
	{
		readonly List<Tab> tabs = new List<Tab> ();
		readonly List<Cairo.PointD> tabSizes = new List<Cairo.PointD> ();
		
		double mx, my;
		Tab hoverTab;
		TabstripVisualStyle visualStyle = TabstripVisualStyle.Buttons;
		
		int activeTab;
		public int ActiveTab {
			get {
				return activeTab; 
			}
			set {
				if (activeTab == value)
					return;
				tabs[activeTab].Active = false;
				activeTab = value;
				tabs[activeTab].Active = true;
				QueueDraw ();
			}
		}
		
		public IList<Tab> Tabs {
			get { return tabs.AsReadOnly (); }
		}
		
		public int TabCount {
			get {
				return tabs.Count;
			}
		}
		
		public Tabstrip ()
		{
			Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.LeaveNotifyMask;
		}
		
		public TabstripVisualStyle VisualStyle {
			get { return visualStyle; }
			set { visualStyle = value; QueueDraw (); }
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			tabs.ForEach (t => t.Dispose ());
		}
		
		public void AddTab (Tab tab)
		{
			tabs.Add (tab);
			tabSizes.Add (tab.Size);
			if (tabs.Count == 1)
				tab.Active = true;
			QueueResize ();
		}
		
		Cairo.Rectangle GetBounds (Tab tab)
		{
			if (tab == null)
				return new Cairo.Rectangle (0, 0, 0, 0);
			
			int spacerWidth = visualStyle == TabstripVisualStyle.CurveTabs ? Tab.SpacerWidth : 0;
			int idx = tabs.IndexOf (tab);
			double distance = 0;
			for (int i = 0; i < idx; i++) {
				if (tabs[i].TabPosition == tab.TabPosition)
					distance += tabSizes[i].X - spacerWidth;
			}
			return new Cairo.Rectangle (tab.TabPosition == TabPosition.Left ? distance : Allocation.Width - distance - tabSizes[idx].X,
				0,
				tabSizes[idx].X,
				tabSizes[idx].Y);
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			mx = evnt.X;
			my = evnt.Y;
			var oldHoverTab = hoverTab;
			hoverTab = null;
			
			foreach (var tab in tabs) {
				if (tab.IsSeparator)
					continue;
				var bounds = GetBounds (tab);
				if (bounds.X < mx && mx < bounds.X + bounds.Width) {
					hoverTab = tab;
					break;
				}
			}
			
			if (hoverTab != oldHoverTab && oldHoverTab != null) {
				var oldBounds = GetBounds (oldHoverTab);
				QueueDrawArea ((int)oldBounds.X, (int)oldBounds.Y, (int)oldBounds.Width, (int)oldBounds.Height);
			}
			
			if (hoverTab != null) {
				var bounds = GetBounds (hoverTab);
				QueueDrawArea ((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height);
			}
			
			return base.OnMotionNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (hoverTab != null) {
				ActiveTab = tabs.IndexOf (hoverTab);
			}
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			if (hoverTab != null) {
				var bounds = GetBounds (hoverTab);
				hoverTab = null;
				QueueDrawArea ((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height);
			}
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Height = 1 + (int)Math.Ceiling (tabSizes.Max (p => p.Y));
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.Rectangle (evnt.Region.Clipbox.X, evnt.Region.Clipbox.Y, evnt.Region.Clipbox.Width, evnt.Region.Clipbox.Height);
				cr.Color = (HslColor)Style.Background (StateType.Normal);
				cr.FillPreserve ();
				cr.Clip ();
				
				for (int i = tabs.Count; i --> 0;) {
					if (i == ActiveTab)
						continue;
					var tab = tabs[i];
					var bounds = GetBounds (tab);
					tab.HoverPosition = tab == hoverTab ? new Cairo.PointD (mx - bounds.X, my) : new Cairo.PointD (-1, -1);
					tab.Draw (cr, bounds);
				}
				
				tabs[ActiveTab].Draw (cr, GetBounds (tabs[ActiveTab]));
			}
			return base.OnExposeEvent (evnt);
		}
	}

	public enum TabPosition {
		Left,
		Right
	}
	
	public class Tab : IDisposable
	{
		internal static readonly int SpacerWidth = 8;
		Pango.Layout layout;
		Tabstrip parent;
		int w, h;
		
		public string Label {
			get;
			private set;
		}
		
		public TabPosition TabPosition {
			get;
			private set;
		}
		
		public Cairo.PointD HoverPosition {
			get;
			set;
		}
		
		bool active;
		public bool Active {
			get { return active; }
			set {
				active = value;
				if (active)
					OnActivated (EventArgs.Empty);
			}
		}
		
		public bool IsSeparator {
			get { return Label == "|"; }
		}
		
		public object Tag {
			get;
			set;
		}
		
		public Tab (Tabstrip parent, string label) : this (parent, label, TabPosition.Left)
		{
		}
		
		public void Activate ()
		{
			OnActivated (EventArgs.Empty);
		}
		
		public void Dispose ()
		{
			if (layout != null)
				layout.Dispose ();
		}
		
		public Tab (Tabstrip parent, string label, TabPosition tabPosition)
		{
			this.parent = parent;
			this.Label = label;
			layout = PangoUtil.CreateLayout (parent);
			layout.SetText (label);
			layout.GetPixelSize (out w, out h);
			h += 2;
			
			if (IsSeparator)
				w = SpacerWidth * 2;
			
			this.TabPosition = tabPosition;
		}
		
		public Cairo.PointD Size {
			get {
				if (IsSeparator)
					return new Cairo.PointD (w, h);
				else
					return new Cairo.PointD (Math.Max (45, w + SpacerWidth * 2), h);
			}
		}
		
		public void Draw (Cairo.Context cr, Cairo.Rectangle rectangle)
		{
			switch (parent.VisualStyle) {
			case TabstripVisualStyle.CurveTabs: DrawCurveTabs (cr, rectangle); break;
			case TabstripVisualStyle.Buttons: DrawButtonTabs (cr, rectangle); break;
			}
		}
		
		void DrawCurveTabs (Cairo.Context cr, Cairo.Rectangle rectangle)
		{
			if (IsSeparator)
				return;
			
			cr.MoveTo (rectangle.X, rectangle.Y);
			
			double bottom = rectangle.Y + rectangle.Height - 1;
			
			cr.CurveTo (
				rectangle.X + SpacerWidth / 2, rectangle.Y,
				rectangle.X + SpacerWidth / 2, bottom,
				rectangle.X + SpacerWidth, bottom);
			
			cr.LineTo (rectangle.X + rectangle.Width - SpacerWidth, bottom);
			
			cr.CurveTo (
				rectangle.X + rectangle.Width - SpacerWidth / 2, bottom,
				rectangle.X + rectangle.Width - SpacerWidth / 2, rectangle.Y,
				rectangle.X + rectangle.Width, rectangle.Y);
			
			cr.Color = (HslColor)parent.Style.Dark (StateType.Normal);
			cr.StrokePreserve ();
			cr.ClosePath ();
			if (Active) {
				cr.Color = (HslColor)parent.Style.Background (StateType.Prelight);
			} else if (HoverPosition.X >= 0) {
				double rx = rectangle.X + HoverPosition.X;
				double ry = rectangle.Y + HoverPosition.Y;
				Cairo.RadialGradient gradient = new Cairo.RadialGradient (rx, ry, rectangle.Height * 1.5, 
					rx, ry, 2);
				var color = (HslColor)parent.Style.Mid (StateType.Normal);
				color.L *= 1.05;
				gradient.AddColorStop (0, color);
				color.L *= 1.07;
				gradient.AddColorStop (1, color);
				cr.Pattern = gradient;
			} else {
				cr.Color = (HslColor)parent.Style.Mid (StateType.Normal);
			}
			cr.Fill ();
			
			cr.Save ();
			cr.Translate (rectangle.X + (rectangle.Width - w) / 2, (rectangle.Height - h) / 2);
			cr.Color = (HslColor)parent.Style.Text (StateType.Normal);
			
			cr.ShowLayout (layout);
			
			cr.Restore ();
		}
		
		void DrawButtonTabs (Cairo.Context cr, Cairo.Rectangle rectangle)
		{
			if (IsSeparator) {
				cr.NewPath ();
				double x = Math.Ceiling (rectangle.X + rectangle.Width / 2) + 0.5;
				cr.MoveTo (x, rectangle.Y + 0.5 + 2);
				cr.RelLineTo (0, rectangle.Height - 1 - 4);
				cr.ClosePath ();
				cr.Color = (HslColor)parent.Style.Dark (StateType.Normal);
				cr.LineWidth = 1;
				cr.Stroke ();
				return;
			}
			
			int topPadding = 2;
			
			if (Active || HoverPosition.X >= 0) {
				cr.Rectangle (rectangle.X + 1, rectangle.Y + 1 + topPadding, rectangle.Width - 1, rectangle.Height - topPadding);
				if (Active) {
					cr.Color = (HslColor)parent.Style.Background (StateType.Prelight);
				} else if (HoverPosition.X >= 0) {
					double rx = rectangle.X + HoverPosition.X;
					double ry = rectangle.Y + HoverPosition.Y;
					Cairo.RadialGradient gradient = new Cairo.RadialGradient (rx, ry, rectangle.Height * 1.5, 
						rx, ry, 2);
					var color = (HslColor)parent.Style.Dark (StateType.Normal);
					color.L *= 1.1;
					gradient.AddColorStop (0, color);
					color.L *= 1.1;
					gradient.AddColorStop (1, color);
					cr.Pattern = gradient;
				}
				cr.Fill ();
				
				if (Active) {
					cr.Rectangle (rectangle.X + 0.5, rectangle.Y + 0.5 + topPadding, rectangle.Width - 1, rectangle.Height - topPadding);
					cr.Color = (HslColor)parent.Style.Dark (StateType.Normal);
					cr.LineWidth = 1;
					cr.Stroke ();
				}
			}
			
			cr.Save ();
			cr.Translate (rectangle.X + (rectangle.Width - w) / 2, (rectangle.Height - h) / 2 + topPadding);
			cr.Color = (HslColor)parent.Style.Text (StateType.Normal);
			
			cr.ShowLayout (layout);
			
			cr.Restore ();
		}
		
		public override string ToString ()
		{
			return string.Format ("[Tab: Label={0}, TabPosition={1}, Active={2}]", Label, TabPosition, Active);
		}
		
		protected virtual void OnActivated (EventArgs e)
		{
			EventHandler handler = this.Activated;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler Activated;
	}
	
	public enum TabstripVisualStyle
	{
		CurveTabs,
		Buttons
	}
}

