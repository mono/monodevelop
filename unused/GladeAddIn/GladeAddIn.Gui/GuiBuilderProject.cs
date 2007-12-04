//
// GuiBuilderProject.cs
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
using System.Xml;
using System.IO;
using System.Reflection;
using System.Collections;
using System.CodeDom;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Text;

namespace GladeAddIn.Gui
{
	public class GuiBuilderProject
	{
		ArrayList formInfos = new ArrayList ();
		Gladeui.Project gproject;
		Project project;
		string fileName;
		
		public event WindowEventHandler WindowAdded;
		public event WindowEventHandler WindowRemoved;
	
		public GuiBuilderProject (Gladeui.App gladeApp, Project project, string fileName)
		{
			this.fileName = fileName;
			
			gproject = Gladeui.Project.Open (fileName);
			//gladeApp.AddProject (gproject);
			 
			this.project = project;
			gproject.AddWidget += new Gladeui.AddWidgetHandler (OnAddWidget);
			gproject.RemoveWidget += new Gladeui.RemoveWidgetHandler (OnRemoveWidget);
			
			foreach (GLib.Object ob in gproject.Objects) {
				Gladeui.Widget w = Gladeui.Widget.FromObject (ob);
				if (w.Parent == null)
					RegisterWindow (w);
			}
		}
		
		public string File {
			get { return fileName; }
		}
		
		public Gladeui.Project GladeProject {
			get { return gproject; }
		}
		
		public ICollection Windows {
			get { return formInfos; }
		}
		
		public Project Project {
			get { return project; }
		}
		
		public void Dispose ()
		{
			gproject.AddWidget -= new Gladeui.AddWidgetHandler (OnAddWidget);
			gproject.RemoveWidget -= new Gladeui.RemoveWidgetHandler (OnRemoveWidget);
		}
		
		public bool IsActive ()
		{
			return GladeService.App.ActiveProject == gproject;
		}
		
		public void NewWindow (string name)
		{
			Gladeui.WidgetClass wc = Gladeui.WidgetClass.GetByType (Gtk.Window.GType);
			Gladeui.Widget gw = new Gladeui.Widget (null, wc, gproject);
			gw.Name = name;
			gproject.AddObject (gw.Object);
			gproject.Save (gproject.Path);
		}
	
		public void NewDialog (string name)
		{
			Gladeui.WidgetClass wc = Gladeui.WidgetClass.GetByType (Gtk.Dialog.GType);
			Gladeui.Widget gw = new Gladeui.Widget (null, wc, gproject);
			gw.Name = name;
			gproject.AddObject (gw.Object);
			gproject.Save (gproject.Path);
		}
		
		GuiBuilderWindow RegisterWindow (Gladeui.Widget widget)
		{
			GuiBuilderWindow win = new GuiBuilderWindow (this, gproject, widget);
			formInfos.Add (win);
			return win;
		}
	
		void UnregisterWindow (GuiBuilderWindow win)
		{
			formInfos.Remove (win);
			win.Dispose ();
		}
	
		void OnAddWidget (object s, Gladeui.AddWidgetArgs args)
		{
			if (args.Widget.Parent == null) {
				GuiBuilderWindow win = RegisterWindow (args.Widget);
				if (WindowAdded != null)
					WindowAdded (this, new WindowEventArgs (win));
			}
		}
		
		void OnRemoveWidget (object s, Gladeui.RemoveWidgetArgs args)
		{
			if (args.Widget.Parent == null) {
				GuiBuilderWindow win = GetWindowForWidget (args.Widget);
				if (win != null) {
					UnregisterWindow (win);
					if (WindowRemoved != null)
						WindowRemoved (this, new WindowEventArgs (win));
				}
			}
		}
		
		GuiBuilderWindow GetWindowForWidget (Gladeui.Widget w)
		{
			while (w.Parent != null)
				w = w.Parent;
			foreach (GuiBuilderWindow form in formInfos)
				if (form.RootWidget == w)
					return form;
			return null;
		}
	}
	
	public delegate void WindowEventHandler (object s, WindowEventArgs args);
	
	public class WindowEventArgs: EventArgs
	{
		GuiBuilderWindow win;
		
		public WindowEventArgs (GuiBuilderWindow win)
		{
			this.win = win;
		}
		
		public GuiBuilderWindow Window {
			get { return win; }
		}
	}
}
