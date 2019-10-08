//
// PropertyPad.cs: The pad that holds the MD property grid. Can also 
//     hold custom grid widgets.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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

using MonoDevelop.Ide.Gui;

using pg = MonoDevelop.Components.PropertyGrid;
using MonoDevelop.Components.Docking;
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components;
using System;
using Gtk;

namespace MonoDevelop.DesignerSupport
{
	class PropertyMacHostWidget : IPropertyGrid
	{
		readonly GtkNSViewHost host;
		readonly MacPropertyGrid view;

		public bool IsGridEditing => view.IsEditing;

		public event EventHandler PropertyGridChanged;

		public Widget Widget => host;

		public object CurrentObject {
			get => view.CurrentObject;
			set {
				view.SetCurrentObject (value, new object [] { value });
			}
		}

		public PropertyMacHostWidget ()
		{
			view = new MacPropertyGrid ();
			host = new GtkNSViewHost (view);
		}

		public void SetCurrentObject (object obj, object [] propertyProviders)
			=> view.SetCurrentObject (obj, propertyProviders);

		public void BlankPad ()
			=> view.BlankPad ();

		public void Dispose ()
			=> view.Dispose ();

		public void Hide () => view.Hidden = true;
		public void Show () => view.Hidden = false;

		public void OnPadContentShown ()
		{
			//not implemented;
		}

		public void PopulateGrid (bool saveEditSession)
		{
			//view.SetCurrentObject (obj, propertyProviders);
		}

		public void SetToolbarProvider (object toolbarProvider)
		{
			//not implemented;
		}
	}

	public interface IPropertyGrid : IPropertyPad
	{
		object CurrentObject { get; set; }

		void Hide ();
		void Show ();

		void SetToolbarProvider (object toolbarProvider);

		Gtk.Widget Widget { get; }
	}

	public class PropertyGridWrapper : IPropertyGrid
	{
		public bool IsGridEditing => nativeWidget.IsGridEditing;

		public event EventHandler PropertyGridChanged;

		public Gtk.Widget Widget => nativeWidget.Widget;

		public bool ShowToolbar { get; set; }
		public ShadowType ShadowType { get; set; }
		public bool ShowHelp { get; set; }

		public object CurrentObject {
			get => nativeWidget.CurrentObject;
			set => nativeWidget.CurrentObject = value;
		}

		public bool Sensitive { get; set; }

		IPropertyGrid nativeWidget;

		public PropertyGridWrapper ()
		{
#if MAC
			nativeWidget = new PropertyMacHostWidget ();
#else
			nativeWidget = new pg.PropertyGrid ();
#endif
		}

		public void BlankPad ()
			=> nativeWidget.BlankPad ();

		public void PopulateGrid (bool saveEditSession) =>
			nativeWidget.PopulateGrid (saveEditSession);

		public void SetCurrentObject (object lastComponent, object [] propertyProviders)
			=> nativeWidget.SetCurrentObject (lastComponent, propertyProviders);

		public void Show () => nativeWidget.Show ();
		public void Hide () => nativeWidget.Hide ();

		public void Dispose () => nativeWidget.Dispose ();

		public void SetToolbarProvider (object toolbarProvider)
		{
			nativeWidget.SetToolbarProvider (toolbarProvider);
		}

		public void OnPadContentShown ()
		{
			//nativeWidget.SetToolbarProvider (toolbarProvider);
		}

		public void CommitPendingChanges ()
		{
			//to implement
		}
	}

	public class PropertyPad : PadContent, ICommandDelegator, IPropertyPad
	{
		public event EventHandler PropertyGridChanged;

		readonly bool isNative;

		InvisibleFrame frame;
		bool customWidget;
		IPadWindow container;
		DockToolbarProvider toolbarProvider = new DockToolbarProvider ();

		internal object CommandRouteOrigin { get; set; }

		readonly PropertyGridWrapper propertyGridWrapper;

		public PropertyPad ()
		{
			frame = new InvisibleFrame ();

			propertyGridWrapper = new PropertyGridWrapper ();
			frame.Add (propertyGridWrapper.Widget);
			propertyGridWrapper.PropertyGridChanged += Grid_Changed;

			frame.ShowAll ();
		}

		void Grid_Changed (object sender, EventArgs e) =>
			PropertyGridChanged?.Invoke (this, e);

		protected override void Initialize (IPadWindow container)
		{
			base.Initialize (container);
			toolbarProvider.Attach (container.GetToolbar (DockPositionType.Top));

			propertyGridWrapper.SetToolbarProvider (toolbarProvider);

#if MAC
			//native cocoa needs content shown to initialize stuff
			if (isNative) {
				container.PadContentShown += Window_PadContentShown;
				container.PadContentHidden += Window_PadContentHidden;
			}
#endif

			this.container = container;
			DesignerSupport.Service.SetPad (this);
		}
		
		internal IPadWindow PadWindow {
			get { return container; }
		}
		
#region AbstractPadContent implementations
		
		public override Control Control {
			get { return frame; }
		}
		
		public override void Dispose()
		{
#if MAC
			if (isNative) {
				container.PadContentShown -= Window_PadContentShown;
				container.PadContentHidden -= Window_PadContentHidden;
			}
#endif
			
			propertyGridWrapper.PropertyGridChanged -= Grid_Changed;
			propertyGridWrapper.Dispose ();
			DesignerSupport.Service.SetPad (null);
			base.Dispose ();
		}
		
#endregion

#region ICommandDelegatorRouter implementation

		object ICommandDelegator.GetDelegatedCommandTarget ()
		{
			// Route the save command to the object for which we are inspecting the properties,
			// so pressing the Save shortcut when doing changes in the property pad will save
			// the document we are changing
			if (IdeApp.CommandService.CurrentCommand == IdeApp.CommandService.GetCommand (FileCommands.Save))
				return CommandRouteOrigin;
			else
				return null;
		}

#endregion

		public bool IsGridEditing {
			get {
				AttachToolbarIfCustomWidget ();
				return propertyGridWrapper.IsGridEditing;
 			}
		}

		//HACK: Mocked gtk property grid to satisfy for customizer.Customize call
		readonly static pg.PropertyGrid pGrid = new pg.PropertyGrid ();
		internal pg.PropertyGrid PropertyGrid {
			get {
				AttachToolbarIfCustomWidget ();
				return isNative ? pGrid : (pg.PropertyGrid) propertyGridWrapper.Widget;
			}
		}

		public void BlankPad ()
		{
			if (isNative) {
				AttachToolbarIfCustomWidget ();
			}
			propertyGridWrapper.BlankPad ();
			CommandRouteOrigin = null;
		}

#if MAC
		void Window_PadContentShown (object sender, EventArgs e)
		{
			propertyGridWrapper.OnPadContentShown ();

			if (customWidget && frame.Child is GtkNSViewHost viewHost) {
				viewHost.Visible = true;
			}
		}

		void Window_PadContentHidden (object sender, EventArgs e)
		{
			if (customWidget && frame.Child is GtkNSViewHost viewHost) {
				viewHost.Visible = false;
			}
		}
#endif

		void AttachToolbarIfCustomWidget ()
		{
			if (customWidget) {
				customWidget = false;
				frame.Remove (frame.Child);

#if MAC
				if (isNative) {
					frame.Add (propertyGridWrapper.Widget);
				} else {
#endif
					frame.Add (propertyGridWrapper.Widget);
#if MAC
				}
#endif
				toolbarProvider.Attach (container.GetToolbar (DockPositionType.Top));
			}
		}

		internal void UseCustomWidget (Gtk.Widget widget)
		{
			toolbarProvider.Attach (null);
			ClearToolbar ();
			customWidget = true;
			frame.Remove (frame.Child);
			frame.Add (widget);
			widget.Show ();
			if (container != null) {
				widget.Visible = container.ContentVisible;
			}
		}
		
		void ClearToolbar ()
		{
			if (container != null) {
				var toolbar = container.GetToolbar (DockPositionType.Top);
				foreach (var w in toolbar.Children)
					toolbar.Remove (w);
			}
		}

		public void SetCurrentObject (object lastComponent, object [] propertyProviders)
		{
			AttachToolbarIfCustomWidget ();
			propertyGridWrapper.SetCurrentObject (lastComponent, propertyProviders);
		}

		public void PopulateGrid (bool saveEditSession)
		{
			propertyGridWrapper.PopulateGrid (saveEditSession);
		}
	}

	class DockToolbarProvider: pg.PropertyGrid.IToolbarProvider
	{
		DockItemToolbar tb;
		List<Gtk.Widget> buttons = new List<Gtk.Widget> ();
		bool visible = true;
		
		public DockToolbarProvider ()
		{
		}
		
		public void Attach (DockItemToolbar tb)
		{
			if (this.tb == tb)
				return;
			this.tb = tb;
			if (tb != null) {
				tb.Visible = visible;
				foreach (var c in tb.Children)
					tb.Remove (c);
				foreach (var b in buttons)
					tb.Add (b);
			}
		}
		
#region IToolbarProvider implementation
		public void Insert (Gtk.Widget w, int pos)
		{
			if (tb != null)
				tb.Insert (w, pos);
			
			if (pos == -1)
				buttons.Add (w);
			else
				buttons.Insert (pos, w);
		}
		
		
		public void ShowAll ()
		{
			if (tb != null)
				tb.ShowAll ();
			else {
				foreach (var b in buttons)
					b.Show ();
			}
		}
		
		
		public Gtk.Widget[] Children {
			get {
				return buttons.ToArray ();
			}
		}
		
		
		public bool Visible {
			get {
				return visible;
			}
			set {
				visible = value;
				if (tb != null)
					tb.Visible = value;
			}
		}
		
#endregion
	}

	class InvisibleFrame : Gtk.Alignment
	{
		public InvisibleFrame ()
			: base (0, 0, 1, 1)
		{
		}

		public Gtk.Widget ReplaceChild (Gtk.Widget widget)
		{
			Gtk.Widget old = Child;
			if (old != null)
				Remove (old);
			Add (widget);
			return old;
		}
	}

}
