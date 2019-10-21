﻿// 
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
using System.Linq;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Shell;

namespace MonoDevelop.Components
{
	class Tabstrip : DrawingArea
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
				if (activeTab != -1)
					tabs [activeTab].Active = false;
				activeTab = value;
				if (activeTab != -1)
					tabs [activeTab].Active = true;
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
			Accessible.SetRole (AtkCocoa.Roles.AXTabGroup);
			Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.FocusChangeMask;
			CanFocus = true;
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			tabs.ForEach (t => t.Dispose ());
		}
		
		public void AddTab (Tab tab)
		{
			InsertTab (tabs.Count, tab);
		}

		public void InsertTab (int index, Tab tab)
		{
			if (index < 0 || index >= tabs.Count) {
				tabs.Add (tab);
				tabSizes.Add (tab.Size);
			} else {
				tabs.Insert (index, tab);
				tabSizes.Insert (index, tab.Size);
			}
			if (tabs.Count == 1)
				tab.Active = tab.Visible;
			else if (activeTab >= index)
				activeTab++;

			if (FocusedTab >= index) {
				FocusedTab++;
			}

			QueueResize ();

			tab.Allocation = GetBounds (tab);
			if (tab.Accessible != null) {
				Accessible.AddAccessibleElement (tab.Accessible);
				tab.AccessibilityPressed += OnTabPressed;
				UpdateAccessibilityTabs ();
			}
		}

		public void RemoveTab (int index)
		{
			if (activeTab == index) {
				if (index < tabs.Count - 1)
					ActiveTab = index + 1;
				else
					ActiveTab = index - 1;
			}

			if (FocusedTab == index) {
				if (index == tabs.Count - 1)
					FocusedTab--;
			} else if (FocusedTab > index)
				FocusedTab--;

			var tab = tabs [index];
			tabs.RemoveAt (index);
			tabSizes.RemoveAt (index);
			if (activeTab > index)
				activeTab--;
			if (FocusedTab >= index)
				FocusedTab = index;

			QueueResize ();

			if (tab.Accessible != null) {
				Accessible.RemoveAccessibleElement (tab.Accessible);
				tab.AccessibilityPressed -= OnTabPressed;
				UpdateAccessibilityTabs ();
			}

			tab.Dispose ();
		}

		public void ReplaceTab (int position, Tab tab)
		{
			var oldTab = tabs [position];
			tabs [position] = tab;
			tabSizes [position] = tab.Size;

			if (oldTab.Active)
				tab.Active = tab.Visible;

			if (oldTab.Accessible != null) {
				Accessible.RemoveAccessibleElement (oldTab.Accessible);
				oldTab.AccessibilityPressed -= OnTabPressed;
				UpdateAccessibilityTabs ();
			}

			oldTab.Dispose ();

			QueueResize ();

			tab.Allocation = GetBounds (tab);
			if (tab.Accessible != null) {
				Accessible.AddAccessibleElement (tab.Accessible);
				tab.AccessibilityPressed += OnTabPressed;
				UpdateAccessibilityTabs ();
			}
		}

		internal void ReorderTabs (int currentIndex, int newIndex)
		{
			if (currentIndex == newIndex)
				return;

			var replaced = tabs [newIndex];
			tabs [newIndex] = tabs [currentIndex];
			tabs [currentIndex] = replaced;

			if (FocusedTab == currentIndex)
				FocusedTab = newIndex;
			else if (FocusedTab == newIndex)
				FocusedTab = currentIndex;

			// Active status of the tabs won't change, but the activeTab field may need to be updated
			if (activeTab == currentIndex)
				activeTab = newIndex;
			else if (activeTab == newIndex)
				activeTab = currentIndex;
			Relayout ();
		}

		internal void Relayout ()
		{
			tabSizes.Clear ();
			foreach (var t in tabs)
				tabSizes.Add (t.Size);
			QueueResize ();
			UpdateAccessibilityTabs ();
		}

		void OnTabPressed (object sender, EventArgs args)
		{
			ActiveTab = tabs.IndexOf ((Tab)sender);
		}

		void UpdateAccessibilityTabs ()
		{
			if (!AccessibilityElementProxy.Enabled) {
				return;
			}

			var proxies = new List<AtkCocoaHelper.AccessibilityElementProxy> (tabs.Count);
			foreach (var tab in tabs) {
				if (!tab.Visible)
					continue;
				tab.Accessible.Index = proxies.Count;
				proxies.Add (tab.Accessible);
				tab.Allocation = GetBounds (tab);
			}

			Accessible.SetTabs (proxies.ToArray ());
		}

		Cairo.Rectangle GetBounds (Tab tab)
		{
			if (tab == null)
				return new Cairo.Rectangle (0, 0, 0, 0);
			
			int spacerWidth = 0;
			int idx = tabs.IndexOf (tab);
			double distance = 0;
			for (int i = 0; i < idx; i++) {
				var t = tabs [i];
				if (t.Visible && t.TabPosition == tab.TabPosition)
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
				if (tab.IsSeparator || !tab.Visible)
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
				try {
					ActiveTab = tabs.IndexOf (hoverTab);
				} catch (Exception ex) {
					LoggingService.LogInternalError (ex);	
				}
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
			requisition.Height = (int)Math.Ceiling (tabSizes.Max (p => p.Y));
			requisition.Width = tabs.Count == 0 ? 10 : tabs.Where (t => t.Visible).Sum (t => (int)Math.Ceiling (t.Size.X));
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				cr.SetSourceColor (Styles.SubTabBarBackgroundColor.ToCairoColor ());
				cr.Fill ();

				Tab active = null;
				for (int i = tabs.Count; i --> 0;) {
					if (i == ActiveTab) {
						active = tabs [i];
						continue;
					}
					var tab = tabs[i];
					if (!tab.Visible)
						continue;
					var bounds = GetBounds (tab);
					tab.HoverPosition = tab == hoverTab ? new Cairo.PointD (mx - bounds.X, my) : new Cairo.PointD (-1, -1);
					tab.Draw (cr, bounds);
				}

				if (active != null && active.Visible) {
					active.Draw (cr, GetBounds (active));
				}
			}

			return base.OnExposeEvent (evnt);
		}

		int focusedTab = -1;

		public int FocusedTab {
			get => focusedTab;
			set {
				if (focusedTab == value)
					return;
				int oldFocus = FocusedTab;
				if (oldFocus >= 0 && oldFocus < tabs.Count) {
					tabs [oldFocus].Focused = false;
				}
				focusedTab = value;
				if (focusedTab >= 0 && focusedTab < tabs.Count) {
					tabs [focusedTab].Focused = true;
				}
				QueueDraw ();
			}
		}

		protected override bool OnFocused (DirectionType direction)
		{
			bool ret = true;
			var newFocusedTab = FocusedTab;

			switch (direction) {
			case DirectionType.TabForward:
			case DirectionType.Right:
				do {
					newFocusedTab++;
				} while (newFocusedTab < tabs.Count && !tabs [newFocusedTab].Visible);

				if (newFocusedTab >= tabs.Count) {
					newFocusedTab = -1;
					ret = false;
				}
				break;

			case DirectionType.TabBackward:
			case DirectionType.Left:
				if (newFocusedTab <= -1) {
					newFocusedTab = tabs.Count;
				}

				do {
					newFocusedTab--;
				} while (newFocusedTab >= 0 && !tabs [newFocusedTab].Visible);

				if (newFocusedTab < 0) {
					newFocusedTab = -1;
					ret = false;
				}
				break;
			}

			if (ret) {
				FocusedTab = newFocusedTab;
				GrabFocus ();
			} else {
				FocusedTab = 0;
			}

			return ret;
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			QueueDraw ();
			return base.OnFocusInEvent (evnt);
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			FocusedTab = -1;
			return base.OnFocusOutEvent (evnt);
		}

		protected override void OnActivate ()
		{
			if (FocusedTab >= 0 && FocusedTab < tabs.Count) {
				ActiveTab = FocusedTab;
			}
			base.OnActivate ();
		}
	}

	public enum TabPosition {
		Left,
		Right
	}
	
	class Tab : IDisposable
	{
		internal static readonly int SpacerWidth = 8;
		const int Padding = 6;
		string label;
		Pango.Layout layout;
		Tabstrip parent;
		int w, h;
		bool visible = true;

		public string Label {
			get => label;
			set {
				if (value != label) {
					label = value;
					CreateLayout ();
					if (parent != null)
						parent.Relayout ();
				}
			}
		}

		public bool Visible {
			get => visible;
			set {
				if (value != visible) {
					visible = value;
					if (parent != null)
						parent.Relayout ();
				}
			}
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
				if (active) {
					OnActivated (EventArgs.Empty);
				}
				UpdateAccessibility ();
			}
		}

		void UpdateAccessibility ()
		{
			Accessible?.SetRole (AtkCocoa.Roles.AXRadioButton, active ? "active tab" : "tab");
		}

		public bool IsSeparator {
			get { return Label == "|"; }
		}
		
		public object Tag {
			get;
			set;
		}

		public bool Focused {
			get;
			set;
		}

		Cairo.Rectangle allocation;
		public Cairo.Rectangle Allocation {
			get {
				return allocation;
			}

			set {
				allocation = value;

				if (Accessible != null) {
					Gdk.Rectangle gdkRect = new Gdk.Rectangle ((int)allocation.X, (int)allocation.Y, (int)allocation.Width, (int)allocation.Height);
					Accessible.FrameInGtkParent = gdkRect;
					// If Y != 0, then we need to flip the y axis
					Accessible.FrameInParent = gdkRect;
				}
			}
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
			if (Accessible != null) {
				Accessible.PerformPress -= OnTabPressed;
				Accessible = null;
			}

			if (layout != null)
				layout.Dispose();

			OnDispose ();

			Tag = null;
		}

		protected virtual void OnDispose ()
		{
		}

		public AtkCocoaHelper.AccessibilityElementProxy Accessible { get; private set; }

		public Tab (Tabstrip parent, string label, TabPosition tabPosition)
		{
			this.parent = parent;
			this.Label = label ?? "";

			this.TabPosition = tabPosition;

			if (AccessibilityElementProxy.Enabled) {
				Accessible = AccessibilityElementProxy.ButtonElementProxy ();
				Accessible.Title = label ?? "";
				Accessible.GtkParent = parent;
				Accessible.Identifier = "Tabstrip.Tab";
				Accessible.PerformPress += OnTabPressed;
				UpdateAccessibility ();
			}
		}

		void CreateLayout ()
		{
			if (layout != null)
				layout.Dispose ();

			layout = PangoUtil.CreateLayout (parent);
			layout.FontDescription = IdeServices.FontService.SansFont.CopyModified (Styles.FontScale11);
			layout.SetText (label ?? "");
			layout.Alignment = Pango.Alignment.Center;
			layout.GetPixelSize (out w, out h);

			if (IsSeparator)
				w = SpacerWidth * 2;
		}

		public Cairo.PointD Size {
			get {
				if (IsSeparator)
					return new Cairo.PointD (w, h + Padding * 2);
				else
					return new Cairo.PointD (Math.Max (45, w + SpacerWidth * 2), h + Padding * 2);
			}
		}
		
		public void Draw (Cairo.Context cr, Cairo.Rectangle rectangle)
		{
			if (IsSeparator) {
				cr.NewPath ();
				double x = Math.Ceiling (rectangle.X + rectangle.Width / 2) + 0.5;
				cr.MoveTo (x, rectangle.Y + 0.5 + 2);
				cr.RelLineTo (0, rectangle.Height - 1 - 4);
				cr.ClosePath ();
				cr.SetSourceColor (Styles.SubTabBarSeparatorColor.ToCairoColor ());
				cr.LineWidth = 1;
				cr.Stroke ();
				return;
			}
			
			if (Active || HoverPosition.X >= 0) {
				if (Active) {
					cr.Rectangle (rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
					cr.SetSourceColor (Styles.SubTabBarActiveBackgroundColor.ToCairoColor ());
					cr.Fill ();
				} else if (HoverPosition.X >= 0) {
					cr.Rectangle (rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
					cr.SetSourceColor (Styles.SubTabBarHoverBackgroundColor.ToCairoColor ());
					cr.Fill ();
				}
			}

			if (Active) {
				cr.SetSourceColor (Styles.SubTabBarActiveTextColor.ToCairoColor ());
				layout.FontDescription = IdeServices.FontService.SansFont.CopyModified (Styles.FontScale11, Pango.Weight.Bold);
			} else {
				cr.SetSourceColor (Styles.SubTabBarTextColor.ToCairoColor ());
				layout.FontDescription = IdeServices.FontService.SansFont.CopyModified (Styles.FontScale11);
			}

			// Pango.Layout.Width is in pango units
			layout.Width = (int)rectangle.Width * (int)Pango.Scale.PangoScale;

			cr.MoveTo (rectangle.X, (rectangle.Height - h) / 2 - 1);
			Pango.CairoHelper.ShowLayout (cr, layout);

			if (parent.HasFocus && Focused) {
				cr.LineWidth = 1.0;
				cr.SetDash (new double[] { 1, 1 }, 0.5);
				if (Active) {
					cr.SetSourceColor (Styles.SubTabBarActiveTextColor.ToCairoColor ());
				} else {
					cr.SetSourceColor (Styles.FocusColor.ToCairoColor ());
				}
				cr.Rectangle (rectangle.X + 2, rectangle.Y + 2, rectangle.Width - 4, rectangle.Height - 4);
				cr.Stroke ();
			}
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

		void OnTabPressed (object sender, EventArgs e)
		{
			// Proxy the event to the tab bar so it can set this tab as active
			AccessibilityPressed?.Invoke (this, e);
		}

		public event EventHandler Activated;
		public event EventHandler AccessibilityPressed;
	}
}

