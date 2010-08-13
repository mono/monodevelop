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
		
		public int TabCount {
			get {
				return tabs.Count;
			}
		}
		
		public Tabstrip ()
		{
			Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.LeaveNotifyMask;
		}
		
		public void ActivateButton (int button)
		{
			tabs[button].Activate ();
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
		}
		
		Cairo.Rectangle GetBounds (Tab tab)
		{
			int idx = tabs.IndexOf (tab);
			double distance = 0;
			for (int i = 0; i < idx; i++) {
				if (tabs[i].TabPosition == tab.TabPosition)
					distance += tabSizes[i].X - Tab.SpacerWidth;
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
			foreach (var tab in tabs) {
				var bounds = GetBounds (tab);
				if (bounds.X < mx && mx < bounds.X + bounds.Width) {
					hoverTab = tab;
					QueueDraw ();
					break;
				}
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
			hoverTab = null;
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Height = (int)(this.PangoContext.FontDescription.Size / Pango.Scale.PangoScale) + 4;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.Rectangle (evnt.Region.Clipbox.X, evnt.Region.Clipbox.Y, evnt.Region.Clipbox.Width, evnt.Region.Clipbox.Height);
				cr.Color = (HslColor)Style.Background (StateType.Normal);
				cr.Fill ();
				
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
			layout.Dispose ();
		}
		
		public Tab (Tabstrip parent, string label, TabPosition tabPosition)
		{
			this.parent = parent;
			this.Label = label;
			layout = PangoUtil.CreateLayout (parent);
			layout.SetText (label);
			layout.GetPixelSize (out w, out h);
			
			this.TabPosition = tabPosition;
		}
		
		public Cairo.PointD Size {
			get {
				return new Cairo.PointD (Math.Max (60, w + SpacerWidth * 2), h);
			}
		}
		
		public void Draw (Cairo.Context cr, Cairo.Rectangle rectangle)
		{
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
			cr.Translate (rectangle.X + (rectangle.Width - w) / 2, (rectangle.Height - h) / 2 - 1);
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
}

