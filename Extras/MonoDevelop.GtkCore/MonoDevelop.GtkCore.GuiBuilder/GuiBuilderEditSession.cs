//
// GuiBuilderEditSession.cs
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
using System.Reflection;
using System.Collections;
using System.CodeDom;

using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.GtkCore.Dialogs;

namespace MonoDevelop.GtkCore.GuiBuilder {
    
	public class GuiBuilderEditSession: IDisposable
	{
		GuiBuilderWindow window;
		Stetic.Project gproject;
		Stetic.Wrapper.Container rootWidget;
		Stetic.WidgetDesigner widget;
		CodeBinder codeBinder;
		
		public event EventHandler ModifiedChanged;
		public event EventHandler RootWidgetChanged;
		
		public GuiBuilderEditSession (GuiBuilderWindow win, ITextFileProvider textFileProvider)
		{
			this.window = win;
			
			// Create a local project and load into it the widget being edited.
			// When saving the file, this project will be merged with the main project.
			gproject = new Stetic.Project ();
			gproject.ResourceProvider = GtkCoreService.GetGtkInfo (win.Project.Project).ResourceProvider;
			
			// Reuse the action groups and icon factory of the main project
			gproject.ActionGroups = win.Project.SteticProject.ActionGroups;
			gproject.IconFactory = win.Project.SteticProject.IconFactory;
			
			XmlElement data = Stetic.WidgetUtils.ExportWidget (win.RootWidget.Wrapped);
			Gtk.Widget w = Stetic.WidgetUtils.ImportWidget (gproject, data);
			gproject.AddWidget (w);
			rootWidget = Stetic.Wrapper.Container.Lookup (w);
			
			codeBinder = new CodeBinder (win.Project.Project, textFileProvider, rootWidget);
			
			gproject.Modified = false;
			gproject.WidgetMemberNameChanged += new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
			gproject.ModifiedChanged += new EventHandler (OnModifiedChanged);
			
			gproject.SignalAdded += new Stetic.SignalEventHandler (OnSignalAdded);
			gproject.SignalRemoved += new Stetic.SignalEventHandler (OnSignalRemoved);
			gproject.SignalChanged += new Stetic.SignalChangedEventHandler (OnSignalChanged);
			gproject.ProjectReloaded += new EventHandler (OnProjectReloaded);
		}
		
		public Stetic.Project SteticProject {
			get { return gproject; }
		}
		
		public Stetic.Wrapper.Widget GladeWidget {
			get { return rootWidget; }
		}
		
		public Stetic.Wrapper.Container RootWidget {
			get { return rootWidget; }
		}
		
		public Gtk.Widget WrapperWidget {
			get {
				if (widget == null) {
					Gtk.Container w = rootWidget.Wrapped as Gtk.Container;
					widget = Stetic.UserInterface.CreateWidgetDesigner (w, rootWidget.DesignWidth, rootWidget.DesignHeight);
				}
				return widget; 
			}
		}
		
		public void Save ()
		{
			XmlElement data = Stetic.WidgetUtils.ExportWidget (rootWidget.Wrapped);
			window.SetWidgetInfo (data);
			window.Save ();
			gproject.Modified = false;
		}
		
		public void Dispose ()
		{
			gproject.WidgetMemberNameChanged -= new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
			GuiBuilderService.ActiveProject = null;
			gproject.Dispose ();
		}
		
		public void SetDesignerActive ()
		{
			widget.UpdateObjectViewers ();
		}
		
		public bool Modified {
			get { return gproject.Modified; }
		}
		
		void OnModifiedChanged (object s, EventArgs a)
		{
			if (ModifiedChanged != null)
				ModifiedChanged (this, a);
		}
		
		public void BindCurrentWidget ()
		{
			if (widget.Selection is Gtk.Action)
				BindAction ((Stetic.Wrapper.Action) Stetic.ObjectWrapper.Lookup (widget.Selection));
			else if (widget.Selection is Gtk.Widget) {
				Stetic.Wrapper.Widget w = Stetic.Wrapper.Widget.Lookup ((Gtk.Widget) widget.Selection);
				if (w.MemberName.Length == 0)
					w.MemberName = w.Wrapped.Name;
				codeBinder.BindToField (w);
			}
		}
		
		public void BindAction (Stetic.Wrapper.Action action)
		{
			codeBinder.BindToField (action);
		}
		
		public void UpdateBindings (string fileName)
		{
			codeBinder.UpdateBindings (fileName);
		}
		
		void OnProjectReloaded (object s, EventArgs a)
		{
			Gtk.Widget[] tops = gproject.Toplevels;
			if (tops.Length > 0) {
				rootWidget = Stetic.Wrapper.Container.Lookup (tops[0]);
				if (rootWidget != null) {
					codeBinder.TargetObject = rootWidget;
					widget = null;
					if (RootWidgetChanged != null)
						RootWidgetChanged (this, EventArgs.Empty);
					return;
				}
			}
			SetErrorMode ();
		}
		
		void SetErrorMode ()
		{
			Gtk.Label lab = new Gtk.Label ();
			lab.Markup = "<b>" + GettextCatalog.GetString ("The form designer could not be loaded") + "</b>";
			Gtk.EventBox box = new Gtk.EventBox ();
			box.Add (lab);
			
			widget = Stetic.UserInterface.CreateWidgetDesigner (box, 100, 100);
			
			if (RootWidgetChanged != null)
				RootWidgetChanged (this, EventArgs.Empty);
		}
		
		void OnWidgetNameChanged (object s, Stetic.Wrapper.WidgetNameChangedArgs args)
		{
			codeBinder.UpdateField (args.Widget, args.OldName);
		}
		
		void OnSignalAdded (object sender, Stetic.SignalEventArgs args)
		{
			codeBinder.BindSignal (args.Signal);
		}

		void OnSignalRemoved (object sender, Stetic.SignalEventArgs args)
		{
		}

		void OnSignalChanged (object sender, Stetic.SignalChangedEventArgs args)
		{
			codeBinder.UpdateSignal (args.OldSignal, args.Signal);
		}
		
		public IClass GetClass ()
		{
			return codeBinder.GetClass ();
		}
	}
}
