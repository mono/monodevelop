//
// GuiBuilderPropertiesPad.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderPropertiesPad: AbstractPadContent
	{
		Stetic.WidgetPropertyTree grid;
		Stetic.SignalsEditor signalsEditor;
		Gtk.Widget widget;
		
		public GuiBuilderPropertiesPad (): base ("")
		{
			GuiBuilderService.ActiveProjectChanged += new EventHandler (OnActiveProjectChanged);

			grid = new Stetic.WidgetPropertyTree ();
			
			DefaultPlacement = "MonoDevelop.GtkCore.GuiBuilder.GuiBuilderPalettePad/bottom; right";
			
			Notebook tabs = new Notebook ();
			
			ScrolledWindow sw = new ScrolledWindow ();
			sw.AddWithViewport (grid);
			tabs.AppendPage (sw, new Label (GettextCatalog.GetString ("Properties")));
			
			signalsEditor = new Stetic.SignalsEditor ();
			signalsEditor.SignalActivated += new EventHandler (OnSignalActivated);
			tabs.AppendPage (signalsEditor, new Label (GettextCatalog.GetString ("Signals")));
			
			widget = tabs;
			
			widget.ShowAll ();
			tabs.Page = 0;
		}
		
		public override Gtk.Widget Control {
			get { return widget; }
		}
		
		void OnActiveProjectChanged (object o, EventArgs a)
		{
			grid.Project = GuiBuilderService.ActiveProject;
			signalsEditor.Project = GuiBuilderService.ActiveProject;
		}
		
		void OnSignalActivated (object s, EventArgs a)
		{
			GuiBuilderService.JumpToSignalHandler (signalsEditor.SelectedSignal);
		}
		
//		[CommandHandler (EditCommands.Copy)]
//		void 
	}
}
