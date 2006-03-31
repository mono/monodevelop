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
		Stetic.PreviewBox widget;
		ITextFileProvider textFileProvider;
		string className;
		string classFile;
		
		public event EventHandler ModifiedChanged;
		public event EventHandler RootWidgetChanged;
		
		public GuiBuilderEditSession (GuiBuilderWindow win, ITextFileProvider textFileProvider)
		{
			this.textFileProvider = textFileProvider;
			this.window = win;
			gproject = new Stetic.Project ();
			XmlElement data = Stetic.WidgetUtils.ExportWidget (win.RootWidget.Wrapped);
			Gtk.Widget w = Stetic.WidgetUtils.ImportWidget (gproject, data);
			gproject.AddWidget (w);
			rootWidget = Stetic.Wrapper.Container.Lookup (w);
			
			gproject.WidgetNameChanged += new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
			gproject.ModifiedChanged += new EventHandler (OnModifiedChanged);
			
			gproject.SignalAdded += new Stetic.Wrapper.SignalEventHandler (OnSignalAdded);
			gproject.SignalRemoved += new Stetic.Wrapper.SignalEventHandler (OnSignalRemoved);
			gproject.SignalChanged += new Stetic.Wrapper.SignalChangedEventHandler (OnSignalChanged);
			
			gproject.ProjectReloaded += new EventHandler (OnProjectReloaded);
			
			IClass cls = win.GetClass (); 
			className = cls.FullyQualifiedName;
			classFile = cls.Region.FileName;
			
			gproject.ResourceProvider = GtkCoreService.GetGtkInfo (win.Project.Project).ResourceProvider;
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
					widget = Stetic.EmbedWindow.Wrap (w, rootWidget.DesignWidth, rootWidget.DesignHeight);
					widget.DesignSizeChanged += new EventHandler (OnDesignSizeChanged);
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
			gproject.WidgetNameChanged -= new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
			GuiBuilderService.ActiveProject = null;
			gproject.Dispose ();
		}
		
		public bool Modified {
			get { return gproject.Modified; }
		}
		
		void OnModifiedChanged (object s, EventArgs a)
		{
			if (ModifiedChanged != null)
				ModifiedChanged (this, a);
		}
		
		void OnDesignSizeChanged (object s, EventArgs a)
		{
			if (rootWidget.DesignHeight != widget.DesignHeight)
				rootWidget.DesignHeight = widget.DesignHeight;
			if (rootWidget.DesignWidth != widget.DesignWidth)
				rootWidget.DesignWidth = widget.DesignWidth;
		}
		
		public void AddCurrentWidgetToClass ()
		{
			if (gproject.Selection != null)
				AddWidgetField (Stetic.Wrapper.Widget.Lookup (gproject.Selection));
		}
		
		public bool UpdateBindings (string fileName)
		{
			IdeApp.ProjectOperations.ParserDatabase.UpdateFile (window.Project.Project, fileName, null);
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (window.Project.Project);
			foreach (IClass cls in ctx.GetFileContents (fileName)) {
				if (cls.FullyQualifiedName == className && cls.Region.FileName == fileName) {
					UpdateBindings (cls);
					return true;
				}
			}
			
			// The class may have been renamed
			
			return (GetClass () != null);
		}
		
		void UpdateBindings (IClass cls)
		{
			UpdateBindings (rootWidget, cls);
		}
		
		void UpdateBindings (Stetic.Wrapper.Widget widget, IClass cls)
		{
			Stetic.Wrapper.Signal[] signals = new Stetic.Wrapper.Signal [widget.Signals.Count];
			widget.Signals.CopyTo (signals, 0);
			
			foreach (Stetic.Wrapper.Signal signal in signals) {
				if (FindSignalHandler (cls, signal) == null) {
					widget.Signals.Remove (signal);
				}
			}

			Stetic.Wrapper.Container container = widget as Stetic.Wrapper.Container;
			if (container != null) {
				foreach (Gtk.Widget cw in container.RealChildren) {
					Stetic.Wrapper.Widget gw = Stetic.Wrapper.Widget.Lookup (cw);
					if (gw != null)
						UpdateBindings (gw, cls);
				}
			}
		}
		
		IMethod FindSignalHandler (IClass cls, Stetic.Wrapper.Signal signal)
		{
			foreach (IMethod met in cls.Methods) {
				if (met.Name == signal.Handler) {
					return met;
				}
			}
			return null;
		}
		
		void OnProjectReloaded (object s, EventArgs a)
		{
			Gtk.Widget[] tops = gproject.Toplevels;
			if (tops.Length > 0) {
				rootWidget = Stetic.Wrapper.Container.Lookup (tops[0]);
				if (rootWidget != null) {
					if (widget != null) {
						widget.DesignSizeChanged -= new EventHandler (OnDesignSizeChanged);
						widget = null;
					}
					if (RootWidgetChanged != null)
						RootWidgetChanged (this, EventArgs.Empty);
					return;
				}
			}
			SetErrorMode ();
		}
		
		void SetErrorMode ()
		{
			if (widget != null)
				widget.DesignSizeChanged -= new EventHandler (OnDesignSizeChanged);
			
			Gtk.Label lab = new Gtk.Label ();
			lab.Markup = "<b>" + GettextCatalog.GetString ("The form designer could not be loaded") + "</b>";
			Gtk.EventBox box = new Gtk.EventBox ();
			box.Add (lab);
			
			widget = Stetic.EmbedWindow.Wrap (box, 100, 100);
			widget.DesignSizeChanged += new EventHandler (OnDesignSizeChanged);
			
			if (RootWidgetChanged != null)
				RootWidgetChanged (this, EventArgs.Empty);
		}
		
		void OnWidgetNameChanged (object s, Stetic.Wrapper.WidgetNameChangedArgs args)
		{
			Stetic.Wrapper.Widget widget = args.Widget;
			string oldName = args.OldName;

			CodeRefactorer cr = GetCodeGenerator ();
			
			IClass cls;
			
			if (widget == rootWidget)
				return;	// The root widget name can only be changed internally.
			else
				cls = window.GetClass (rootWidget.Wrapped.Name);
				
			if (cls != null) {
				IField f = ClassUtils.FindWidgetField (cls, oldName);
				if (f != null) {
					if (widget == rootWidget) {
						// Renaming the dialog
						cr.ReplaceMember (cls, f, GetFieldCode (widget, "dialog"));
						if (cls.Name == oldName && widget.Wrapped.Name != "")
							cr.RenameClass (new NullProgressMonitor (), cls, widget.Wrapped.Name, RefactoryScope.File);
					}
					else if (f.Name == oldName && widget.Wrapped.Name != "") {
						// Rename the field
						cr.RenameMember (new NullProgressMonitor (), cls, f, widget.Wrapped.Name, RefactoryScope.File);
					} else {
						// Update the Widget attribute only. Keep the old var name.
						CodeMemberField cmf = GetFieldCode (widget);
						cmf.Name = f.Name;
						cr.ReplaceMember (cls, f, cmf);
					}
				}
			}
		}
		
		void OnSignalAdded (object sender, Stetic.Wrapper.SignalEventArgs args)
		{
			AddSignal ((Stetic.Wrapper.Widget) args.Widget, args.Signal);
		}

		void OnSignalRemoved (object sender, Stetic.Wrapper.SignalEventArgs args)
		{
		}

		void OnSignalChanged (object sender, Stetic.Wrapper.SignalChangedEventArgs args)
		{
			if (args.OldSignal.Handler == args.Signal.Handler)
				return;

			IClass cls = GetClass ();
			if (cls == null) return;

			IMethod met = FindSignalHandler (cls, args.OldSignal);
			if (met == null) return;
			
			CodeRefactorer gen = GetCodeGenerator ();
			gen.RenameMember (new NullProgressMonitor (), cls, met, args.Signal.Handler, RefactoryScope.File);
		}
		
		public void AddSignal (Stetic.Wrapper.Widget childWidget, Stetic.Wrapper.Signal signal)
		{
			IClass cls = GetClass ();
			if (cls == null)
				return;
			
			if (FindSignalHandler (cls, signal) != null)
				return;

			CodeMemberMethod met = new CodeMemberMethod ();
			met.Name = signal.Handler;
			met.Attributes = MemberAttributes.Family;
			met.ReturnType = new CodeTypeReference (signal.SignalDescriptor.HandlerReturnTypeName);
			
			foreach (Stetic.ParameterDescriptor pinfo in signal.SignalDescriptor.HandlerParameters)
				met.Parameters.Add (new CodeParameterDeclarationExpression (pinfo.TypeName, pinfo.Name));
			
			CodeRefactorer gen = GetCodeGenerator ();
			gen.AddMember (cls, met);
		}

		public void AddWidgetField (Stetic.Wrapper.Widget widget)
		{
			IClass cls = GetClass ();
			
			if (FindWidgetField (cls, widget) != null)
				return;

			Document doc = IdeApp.Workbench.OpenDocument (cls.Region.FileName, true);
			
			IEditableTextFile editor = doc.Content as IEditableTextFile;
			if (editor != null) {
				CodeRefactorer gen = GetCodeGenerator ();
				gen.AddMember (cls, GetFieldCode (widget));
			}
		}
		
		CodeMemberField GetFieldCode (Stetic.Wrapper.Widget widget)
		{
			return GetFieldCode (widget, null);
		}
		
		CodeMemberField GetFieldCode (Stetic.Wrapper.Widget widget, string name)
		{
			if (name == null) name = widget.Wrapped.Name;
			string type = widget.ClassDescriptor.WrappedTypeName;
			CodeMemberField field = new CodeMemberField (type, name);
			field.Attributes = MemberAttributes.Family;
			return field;
		}
		
		IField FindWidgetField (IClass cls, Stetic.Wrapper.Widget w)
		{
			foreach (IField field in cls.Fields)
				if (field.Name == w.Wrapped.Name)
					return field;
			return null;
		}
		
		CodeRefactorer GetCodeGenerator ()
		{
			CodeRefactorer cr = new CodeRefactorer (IdeApp.ProjectOperations.CurrentOpenCombine, IdeApp.ProjectOperations.ParserDatabase);
			cr.TextFileProvider = textFileProvider;
			return cr;
		}
		
		public IClass GetClass ()
		{
			IClass cls = window.GetClass (className);
			if (cls != null)
				return cls;
				
			// The class name may have changed. Try to guess the new name.
			
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (window.Project.Project);
			IParseInformation pi = ctx.ParseFile (classFile);
			
			ArrayList matches = new ArrayList ();
			ClassCollection classes = ((ICompilationUnit)pi.BestCompilationUnit).Classes;
			foreach (IClass fcls in classes) {
				if (window.IsValidClass (ctx, fcls))
					matches.Add (fcls);
			}
			
			// If found the class, just return it
			if (matches.Count == 1) {
				cls = (IClass) matches [0];
				className = cls.FullyQualifiedName;
				((Gtk.Widget)rootWidget.Wrapped).Name = className;
				return cls;
			}
			
			// If not found, warn the user.
			
			if (classes.Count > 0) {
				using (SelectRenamedClassDialog dialog = new SelectRenamedClassDialog (classes)) {
					if (dialog.Run ()) {
						className = dialog.SelectedClass;
						if (className == null)
							return null;
						else {
							((Gtk.Widget)rootWidget.Wrapped).Name = className;
							return window.GetClass (className);
						}
					}
				}
			} else {
			}
			
			return null;
		}
	}
}
