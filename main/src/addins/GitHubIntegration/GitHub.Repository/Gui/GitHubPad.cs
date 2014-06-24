﻿//
// GitHubPad.cs
//
// Author:
//       Praveena <>
//
// Copyright (c) 2014 Praveena
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using pg = MonoDevelop.Components.PropertyGrid;
using GitHub.Repository;

namespace GitHub.Repository.Gui
{
	public class GitHubPad : AbstractPadContent //, ICommandDelegator
	{
		pg.PropertyGrid grid;
		InvisibleFrame frame;
		bool customWidget;
		IPadWindow container;
		DockToolbarProvider toolbarProvider = new DockToolbarProvider ();
		Gtk.Notebook gNoteBook = new Gtk.Notebook();

		internal object CommandRouteOrigin { get; set; }

		public GitHubPad ()
		{
			grid = new pg.PropertyGrid ();
			frame = new InvisibleFrame ();
			frame.Add (grid);

			frame.ShowAll ();
		}

		public override void Initialize (IPadWindow container)
		{
			base.Initialize (container);
			toolbarProvider.Attach (container.GetToolbar (Gtk.PositionType.Top));
			grid.SetToolbarProvider (toolbarProvider);
			this.container = container;
			//DesignerSupport.Service.SetPad (this);
		}

		internal IPadWindow PadWindow {
			get { return container; }
		}

		#region AbstractPadContent implementations

		public override Gtk.Widget Control {
			get { return frame; }
		}

		public override void Dispose()
		{
			//DesignerSupport.Service.SetPad (null);
		}

		#endregion

//		#region ICommandDelegatorRouter implementation
//
//		object ICommandDelegator.GetDelegatedCommandTarget ()
//		{
//			// Route the save command to the object for which we are inspecting the properties,
//			// so pressing the Save shortcut when doing changes in the property pad will save
//			// the document we are changing
//			if (IdeApp.CommandService.CurrentCommand == IdeApp.CommandService.GetCommand (FileCommands.Save))
//				return CommandRouteOrigin;
//			else
//				return null;
//		}
//
//		#endregion
	
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

