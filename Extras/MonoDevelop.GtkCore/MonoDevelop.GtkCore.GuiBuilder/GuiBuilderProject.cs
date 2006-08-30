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

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderProject
	{
		ArrayList formInfos = new ArrayList ();
		Stetic.Project gproject;
		Project project;
		string fileName;
		internal bool UpdatingWindow;
		bool hasError;
		bool needsUpdate = true;
		
		public event WindowEventHandler WindowAdded;
		public event WindowEventHandler WindowRemoved;
	
		public GuiBuilderProject (Project project, string fileName)
		{
			this.fileName = fileName;
			
			gproject = new Stetic.Project ();
			try {
				gproject.Load (fileName);
			} catch (Exception ex) {
				IdeApp.Services.MessageService.ShowError (ex, "The GUI designer project file '" + fileName + "' could not be loaded.");
				hasError = true;
			}
			 
			this.project = project;
			gproject.WidgetAdded += new Stetic.Wrapper.WidgetEventHandler (OnAddWidget);
			gproject.WidgetRemoved += new Stetic.Wrapper.WidgetEventHandler (OnRemoveWidget);
			
			foreach (Gtk.Widget ob in gproject.Toplevels) {
				Stetic.Wrapper.Widget w = Stetic.Wrapper.Widget.Lookup (ob);
				RegisterWindow (w);
			}
			project.FileRemovedFromProject += new ProjectFileEventHandler (OnFileRemoved);
		}
		
		public bool IsEmpty {
			get { return formInfos.Count == 0; }
		}
		
		public void Save ()
		{
			if (!hasError)
				gproject.Save (fileName);
		}
		
		public string File {
			get { return fileName; }
		}
		
		public Stetic.Project SteticProject {
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
			gproject.WidgetAdded -= new Stetic.Wrapper.WidgetEventHandler (OnAddWidget);
			gproject.WidgetRemoved -= new Stetic.Wrapper.WidgetEventHandler (OnRemoveWidget);
			project.FileRemovedFromProject -= new ProjectFileEventHandler (OnFileRemoved);
		}
		
		public bool IsActive ()
		{
			return GuiBuilderService.ActiveProject == gproject;
		}
		
		public void NewWidget (Type type, string name)
		{
			Stetic.ClassDescriptor klass = Stetic.Registry.LookupClassByName (type.FullName);
			if (klass == null)
				throw new ApplicationException ("Widget type not registered: " + type);

			Gtk.Widget w = klass.NewInstance (gproject) as Gtk.Widget;
			w.Name = name;
			gproject.AddWidget (w);
		}
	
		GuiBuilderWindow RegisterWindow (Stetic.Wrapper.Widget widget)
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
		
		public void Remove (GuiBuilderWindow win)
		{
			gproject.RemoveWidget ((Gtk.Container) win.RootWidget.Wrapped);
		}
	
		public void Remove (Stetic.Wrapper.ActionGroup group)
		{
			gproject.ActionGroups.Remove (group);
		}
	
		void OnAddWidget (object s, Stetic.Wrapper.WidgetEventArgs args)
		{
			if (UpdatingWindow)
				return;
			if (args.WidgetWrapper.ParentWrapper == null) {
				GuiBuilderWindow win = RegisterWindow (args.WidgetWrapper);
				if (WindowAdded != null)
					WindowAdded (this, new WindowEventArgs (win));
			}
		}
		
		void OnRemoveWidget (object s, Stetic.Wrapper.WidgetEventArgs args)
		{
			if (UpdatingWindow)
				return;
			if (args.WidgetWrapper.ParentWrapper == null) {
				GuiBuilderWindow win = GetWindowForWidget (args.WidgetWrapper);
				if (win != null) {
					UnregisterWindow (win);
					if (WindowRemoved != null)
						WindowRemoved (this, new WindowEventArgs (win));
				}
			}
		}
		
		void OnFileRemoved (object sender, ProjectFileEventArgs args)
		{
			// Disable for now since it may have issues when moving files.
			
/*			ArrayList toDelete = new ArrayList ();

			foreach (GuiBuilderWindow win in formInfos) {
				if (win.SourceCodeFile == args.ProjectFile.Name)
					toDelete.Add (win);
			}
			
			foreach (GuiBuilderWindow win in toDelete)
				Remove (win);
*/		}

		
		public void ImportGladeFile ()
		{
			Gtk.FileChooserDialog dialog =
				new Gtk.FileChooserDialog ("Open Glade File", null, Gtk.FileChooserAction.Open,
						       Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
						       Gtk.Stock.Open, Gtk.ResponseType.Ok);
			int response = dialog.Run ();
			if (response == (int)Gtk.ResponseType.Ok) {
				Stetic.GladeFiles.Import (gproject, dialog.Filename);
				Save ();
			}
			dialog.Destroy ();
		}
		
		public GuiBuilderWindow GetWindowForWidget (Stetic.Wrapper.Widget w)
		{
			while (w.ParentWrapper != null)
				w = w.ParentWrapper;
			foreach (GuiBuilderWindow form in formInfos)
				if (form.RootWidget == w)
					return form;
			return null;
		}
		
		public GuiBuilderWindow GetWindowForClass (string className)
		{
			foreach (GuiBuilderWindow form in formInfos) {
				if (CodeBinder.GetObjectName (form.RootWidget) == className)
					return form;
			}
			return null;
		}
		
		public GuiBuilderWindow GetWindowForFile (string fileName)
		{
			foreach (GuiBuilderWindow win in Windows) {
				if (fileName == win.SourceCodeFile)
					return win;
			}
			return null;
		}
		
		public Stetic.Wrapper.ActionGroup GetActionGroupForFile (string fileName)
		{
			foreach (Stetic.Wrapper.ActionGroup group in SteticProject.ActionGroups) {
				if (fileName == GetSourceCodeFile (group))
					return group;
			}
			return null;
		}
		
		public string GetSourceCodeFile (object obj)
		{
			IClass cls = GetClass (obj);
			if (cls != null) return cls.Region.FileName;
			else return null;
		}
		
		IClass GetClass (object obj)
		{
			string name = CodeBinder.GetClassName (obj);
			return FindClass (name);
		}
		
		public IClass FindClass (string className)
		{
			IParserContext ctx = GetParserContext ();
			IClass[] classes = ctx.GetProjectContents ();
			foreach (IClass cls in classes) {
				if (cls.FullyQualifiedName == className)
					return cls;
			}
			return null;
		}
		
		public IParserContext GetParserContext ()
		{
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (Project);
			if (needsUpdate) {
				needsUpdate = false;
				ctx.UpdateDatabase ();
			}
			return ctx;
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
