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

using MonoDevelop.DesignerSupport;
using pg = MonoDevelop.Components.PropertyGrid;
using MonoDevelop.Components.Docking;
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components;
using System;
using Gtk;
using MonoDevelop.Core.FeatureConfiguration;

namespace MonoDevelop.DesignerSupport
{
	public class PropertyPad : PadContent, ICommandDelegator, IPropertyPad
	{
		public event EventHandler PropertyGridChanged;

		readonly bool isNative;
		readonly IPropertyGrid propertyGrid;

		MacPropertyGrid nativeGrid;
		Gtk.Widget gtkWidget;

		pg.PropertyGrid grid;

		InvisibleFrame frame;
		bool customWidget;
		IPadWindow container;
		DockToolbarProvider toolbarProvider = new DockToolbarProvider ();

		internal object CommandRouteOrigin { get; set; }


		public PropertyPad ()
		{
			isNative = FeatureSwitchService.IsFeatureEnabled ("NATIVE_PROPERTYPANEL") ?? false;
			frame = new InvisibleFrame ();

			if (isNative) {

				nativeGrid = new MacPropertyGrid ();
				propertyGrid = nativeGrid;

				gtkWidget = Components.Mac.GtkMacInterop.NSViewToGtkWidget (nativeGrid);
				gtkWidget.CanFocus = true;
				gtkWidget.Sensitive = true;
				gtkWidget.Focused += Widget_Focused;

				nativeGrid.Focused += PropertyGrid_Focused;
				frame.Add (gtkWidget);
			} else {

				grid = new pg.PropertyGrid ();
				propertyGrid = grid;
				grid.Changed += Grid_Changed;
				frame.Add (grid);
			}

			frame.ShowAll ();
		}

		void Grid_Changed (object sender, EventArgs e)
		{
			PropertyGridChanged?.Invoke (this, e);
		}

		void Widget_Focused (object o, Gtk.FocusedArgs args)
		{
			nativeGrid.BecomeFirstResponder ();
		}

		protected override void Initialize (IPadWindow container)
		{
			base.Initialize (container);
			toolbarProvider.Attach (container.GetToolbar (DockPositionType.Top));

			propertyGrid.SetToolbarProvider (toolbarProvider);

			//native cocoa needs content shown to initialize stuff
			if (isNative) {
				container.PadContentShown += Window_PadContentShown;
			}
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
			if (isNative) {
				container.PadContentShown -= Window_PadContentShown;
				nativeGrid.Focused -= PropertyGrid_Focused;
				gtkWidget.Focused -= Widget_Focused;
			} else {
				grid.Changed -= Grid_Changed;
			}
			propertyGrid.Dispose ();
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
				return propertyGrid.IsEditing;
 			}
		}

		//HACK: Mocked gtk property grid to satisfy for customizer.Customize call
		readonly static pg.PropertyGrid pGrid = new pg.PropertyGrid ();
		internal pg.PropertyGrid PropertyGrid {
			get {
				AttachToolbarIfCustomWidget ();
				return isNative ? pGrid : grid;
			}
		}

		public void BlankPad ()
		{
			if (isNative) {
				AttachToolbarIfCustomWidget ();
			}
			propertyGrid.BlankPad ();
			CommandRouteOrigin = null;
		}

		void Window_PadContentShown (object sender, EventArgs e)
		{
			propertyGrid.OnPadContentShown ();
		}

		void PropertyGrid_Focused (object sender, EventArgs e)
		{
			if (!gtkWidget.HasFocus) {
				gtkWidget.HasFocus = true;
			}
		}

		void AttachToolbarIfCustomWidget ()
		{
			if (customWidget) {
				customWidget = false;
				frame.Remove (frame.Child);

				if (isNative) {
					frame.Add (gtkWidget);
				} else {
					frame.Add (grid);
				}
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
			propertyGrid.SetCurrentObject (lastComponent, propertyProviders);
		}

		public void PopulateGrid (bool saveEditSession)
		{
			propertyGrid.Populate (saveEditSession);
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
