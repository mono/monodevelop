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

namespace MonoDevelop.DesignerSupport
{
	public class PropertyPad : PadContent, ICommandDelegator, IPropertyPad
	{
		public event EventHandler PropertyGridChanged;

#if MAC
		MacPropertyGrid grid;
		Gtk.Widget gtkWidget;
#else
		pg.PropertyGrid grid;
#endif

		InvisibleFrame frame;
		bool customWidget;
		IPadWindow container;
		DockToolbarProvider toolbarProvider = new DockToolbarProvider ();

		internal object CommandRouteOrigin { get; set; }

		public PropertyPad ()
		{
			frame = new InvisibleFrame ();

#if !MAC
			grid = new pg.PropertyGrid ();
			grid.Changed += Grid_Changed;
			frame.Add (grid);
#else
			grid = new MacPropertyGrid ();
			gtkWidget = Components.Mac.GtkMacInterop.NSViewToGtkWidget (grid);
			gtkWidget.CanFocus = true;
			gtkWidget.Sensitive = true;
			gtkWidget.Focused += Widget_Focused;

			grid.Focused += PropertyGrid_Focused;
			frame.Add (gtkWidget);
#endif

			frame.ShowAll ();
		}

		void Grid_Changed (object sender, EventArgs e)
		{
			PropertyGridChanged?.Invoke (this, e);
		}

#if MAC
		void Widget_Focused (object o, Gtk.FocusedArgs args)
		{
			grid.BecomeFirstResponder ();
		}
#endif

		protected override void Initialize (IPadWindow container)
		{
			base.Initialize (container);
			toolbarProvider.Attach (container.GetToolbar (DockPositionType.Top));
			grid.SetToolbarProvider (toolbarProvider);

#if MAC
			container.PadContentShown += Window_PadContentShown;
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
#if !MAC
			grid.Changed -= Grid_Changed;
#else
			container.PadContentShown -= Window_PadContentShown;
			grid.Focused -= PropertyGrid_Focused;
			gtkWidget.Focused -= Widget_Focused;
			grid.Dispose ();
#endif
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
				return grid.IsEditing;
 			}
		}


#if !MAC
		//Grid consumers must call this when they lose focus!
		public void BlankPad ()
		{
			AttachToolbarIfCustomWidget ();
			grid.CurrentObject = null;
			CommandRouteOrigin = null;
		}

#else

		public void BlankPad ()
		{
			AttachToolbarIfCustomWidget ();
			grid.BlankPad ();
			CommandRouteOrigin = null;
		}

		void Window_PadContentShown (object sender, EventArgs e)
		{
			grid.OnPadContentShown ();
		}

		void PropertyGrid_Focused (object sender, EventArgs e)
		{
			if (!gtkWidget.HasFocus) {
				gtkWidget.HasFocus = true;
			}
		}

#endif

		void AttachToolbarIfCustomWidget ()
		{
			if (customWidget) {
				customWidget = false;
				frame.Remove (frame.Child);

#if !MAC
				frame.Add (grid);
#else
				frame.Add (gtkWidget);
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
			grid.SetCurrentObject (lastComponent, propertyProviders);
		}

		public void PopulateGrid (bool saveEditSession)
		{
			grid.Populate (saveEditSession);
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
