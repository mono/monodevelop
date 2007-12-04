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
using System.Reflection;
using System.Collections;
using System.CodeDom;

using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.ProgressMonitoring;

namespace GladeAddIn.Gui {
    
	public class GuiBuilderEditSession: IDisposable
	{
		GuiBuilderWindow window;
		Gladeui.Project gproject;
		Gladeui.Widget rootWidget;
		Gtk.Widget widget;
		bool disposed;
		bool modified;
		Gladeui.Widget lastWidget;
		Hashtable names = new Hashtable ();
		ITextFileProvider textFileProvider;
		
		public event EventHandler ModifiedChanged;
		
		public GuiBuilderEditSession (GuiBuilderWindow win, ITextFileProvider textFileProvider)
		{
			this.textFileProvider = textFileProvider;
			this.window = win;
			gproject = new Gladeui.Project (true);
			GladeService.App.AddProject (gproject);
			Gladeui.WidgetInfo widgetInfo = win.RootWidget.Write (Gladeui.Interface.Create ());
			rootWidget = Gladeui.Widget.Read (gproject, widgetInfo);
			gproject.AddObject (rootWidget.Object);
			ClearModified ();
			
			GLib.Timeout.Add (1000, new GLib.TimeoutHandler (OnCheckModified));
			gproject.SelectionChangedEvent += new EventHandler (OnWidgetSelectionChanged);
			gproject.WidgetNameChangedEvent += new Gladeui.WidgetNameChangedEventHandler (OnWidgetNameChanged);
			gproject.AddWidget += new Gladeui.AddWidgetHandler (OnAddWidget);
			gproject.RemoveWidget += new Gladeui.RemoveWidgetHandler (OnRemoveWidget);
			
			CollectNames (rootWidget);
		}
		
		public Gladeui.Project GladeProject {
			get { return gproject; }
		}
		
		public Gladeui.Widget GladeWidget {
			get { return rootWidget; }
		}
		
		public Gtk.Widget WrapperWidget {
			get {
				if (widget == null) {
					Gtk.Window w = rootWidget.Object as Gtk.Window;
					widget = EmbedWindow.Wrap (w);
				}
				return widget; 
			}
		}
		
		public void Save ()
		{
			ClearModified ();
			Gladeui.WidgetInfo widgetInfo = rootWidget.Write (Gladeui.Interface.Create ());
			window.SetWidgetInfo (widgetInfo);
			window.Save ();
			if (ModifiedChanged != null)
				ModifiedChanged (this, EventArgs.Empty);
		}
		
		void ClearModified ()
		{
			modified = false;
			string tmpf = System.IO.Path.GetTempFileName ();
			gproject.Save (tmpf);
			System.IO.File.Delete (tmpf);
		}
		
		public void Dispose ()
		{
			disposed = true;
			gproject.SelectionChangedEvent -= new EventHandler (OnWidgetSelectionChanged);
			gproject.WidgetNameChangedEvent -= new Gladeui.WidgetNameChangedEventHandler (OnWidgetNameChanged);
			gproject.AddWidget -= new Gladeui.AddWidgetHandler (OnAddWidget);
			gproject.RemoveWidget -= new Gladeui.RemoveWidgetHandler (OnRemoveWidget);
			GladeService.App.Project = GladeService.EmptyProject;
			GladeService.App.Editor.Refresh ();
			
			// FIXME: This crashes sometimes
//			GladeService.App.RemoveProject (gproject);
		}
		
		public bool Modified {
			get { return modified; }
		}
		
		public void AddCurrentWidgetToClass ()
		{
			AddWidgetField (lastWidget);
		}
		
		public void UpdateBindings (string fileName)
		{
			IParseInformation pi = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (window.Project.Project).ParseFile (fileName);
			foreach (IClass cls in ((ICompilationUnit)pi.BestCompilationUnit).Classes) {
				if (cls.Name == rootWidget.Name) {
					UpdateBindings (cls);
					break;
				}
			}
		}
		
		void UpdateBindings (IClass cls)
		{
			UpdateBindings (rootWidget, cls);
			GladeService.App.Editor.Refresh ();
		}
		
		void UpdateBindings (Gladeui.Widget widget, IClass cls)
		{
			foreach (Gladeui.WidgetClassSignal csig in widget.Class.Signals) {
				foreach (Gladeui.Signal ss in widget.GetSignalsHandlers (csig.Name)) {
					if (FindSignalHandler (cls, ss) == null) {
						widget.RemoveSignalHandler (ss);
					}
				}
			}

			Gtk.Container container = widget.Object as Gtk.Container;
			if (container != null) {
				foreach (Gtk.Widget cw in container.Children) {
					Gladeui.Widget gw = Gladeui.Widget.FromObject (cw);
					if (gw != null)
						UpdateBindings (gw, cls);
				}
			}
		}
		
		IMethod FindSignalHandler (IClass cls, Gladeui.Signal signal)
		{
			foreach (IMethod met in cls.Methods) {
				if (met.Name == signal.Handler) {
					return met;
				}
			}
			return null;
		}
		
		void CollectNames (Gladeui.Widget widget)
		{
			// The WidgetNameChangedEvent event does not say what was the old name,
			// so we need to keep it somewhere.
			
			names [widget] = widget.Name;

			Gtk.Container container = widget.Object as Gtk.Container;
			if (container != null) {
				foreach (Gtk.Widget cw in container.Children) {
					Gladeui.Widget gw = Gladeui.Widget.FromObject (cw);
					if (gw != null)
						CollectNames (gw);
				}
			}
		}
		
		void OnAddWidget (object s, Gladeui.AddWidgetArgs args)
		{
			names [args.Widget] = args.Widget.Name;
		}
		
		void OnRemoveWidget (object s, Gladeui.RemoveWidgetArgs args)
		{
			names.Remove (args.Widget);
		}
		
		void OnWidgetNameChanged (object s, Gladeui.WidgetNameChangedEventArgs args)
		{
			UpdateWidgetName (args.Widget);
		}
		
		bool UpdateWidgetName (Gladeui.Widget widget)
		{
			string oldName = (string) names [widget];
			if (oldName == widget.Name)
				return false;

			CodeRefactorer cr = GetCodeGenerator ();
			IClass cls = window.GetClass ((string) names [rootWidget]);
			
			if (cls != null) {
				IField f = ClassUtils.FindWidgetField (cls, oldName);
				if (f != null) {
					if (widget == rootWidget) {
						// Renaming the dialog
						cr.ReplaceMember (cls, f, GetFieldCode (widget, "dialog"));
						if (cls.Name == oldName && widget.Name != "")
							cr.RenameClass (new NullProgressMonitor (), cls, widget.Name, RefactoryScope.File);
					}
					else if (f.Name == oldName && widget.Name != "") {
						// Rename the field and update the Widget attribute
						f = (IField) cr.RenameMember (new NullProgressMonitor (), cls, f, widget.Name, RefactoryScope.File);
						if (f == null) return false;
						cr.ReplaceMember (cls, f, GetFieldCode (widget));
					} else {
						// Update the Widget attribute only. Keep the old var name.
						CodeMemberField cmf = GetFieldCode (widget);
						cmf.Name = f.Name;
						cr.ReplaceMember (cls, f, cmf);
					}
					names [widget] = widget.Name;
				}
			}
			return true;
		}
		
		bool OnCheckModified ()
		{
			if (disposed)
				return false;

			if (gproject.Modified != modified) {
				modified = gproject.Modified;
				if (ModifiedChanged != null)
					ModifiedChanged (this, EventArgs.Empty);
			}
			
			return true;
		}
		
		void ResetSelection ()
		{
			if (lastWidget != null) {
				lastWidget.AddSignalHandlerEvent -= new Gladeui.AddSignalHandlerEventHandler (OnAddSignal);
				lastWidget.RemoveSignalHandlerEvent -= new Gladeui.RemoveSignalHandlerEventHandler (OnRemoveSignal);
				lastWidget.ChangeSignalHandlerEvent -= new Gladeui.ChangeSignalHandlerEventHandler (OnChangeSignal);
			}
		}

		void OnWidgetSelectionChanged (object s, EventArgs args)
		{
			ResetSelection ();
			GLib.List list = gproject.SelectionGet ();
			if (list.Count > 0) {
				Gladeui.Widget w = Gladeui.Widget.FromObject ((Gtk.Widget)list[0]);
				if (w == lastWidget || w == null)
					return;
				
				w.AddSignalHandlerEvent += new Gladeui.AddSignalHandlerEventHandler (OnAddSignal);
				w.RemoveSignalHandlerEvent += new Gladeui.RemoveSignalHandlerEventHandler (OnRemoveSignal);
				w.ChangeSignalHandlerEvent += new Gladeui.ChangeSignalHandlerEventHandler (OnChangeSignal);
				lastWidget = w;
			} else
				lastWidget = null;
		}
		
		void OnAddSignal (object s, Gladeui.AddSignalHandlerEventArgs args)
		{
			if (lastWidget != null) {
				AddSignal (lastWidget, args.SignalHandler);
			}
		}
		
		void OnRemoveSignal (object s, Gladeui.RemoveSignalHandlerEventArgs args)
		{
		}
		
		void OnChangeSignal (object s, Gladeui.ChangeSignalHandlerEventArgs args)
		{
			if (args.OldSignalHandler.Handler == args.NewSignalHandler.Handler)
				return;

			IClass cls = GetClass ();
			if (cls == null) return;

			IMethod met = FindSignalHandler (cls, args.OldSignalHandler);
			if (met == null) return;
			
			CodeRefactorer gen = GetCodeGenerator ();
			gen.RenameMember (new NullProgressMonitor (), cls, met, args.NewSignalHandler.Handler, RefactoryScope.File);
		}
		
		public void AddSignal (Gladeui.Widget childWidget, Gladeui.Signal signal)
		{
			IClass cls = GetClass ();
			if (cls == null)
				return;
			
			if (FindSignalHandler (cls, signal) != null)
				return;

			Type ht = GetHandlerType (childWidget, signal.Name);
			if (ht == null)
				return;
			
			MethodInfo invoke = ht.GetMethod ("Invoke");
			Type delReturnType = invoke.ReturnType;
			ParameterInfo[] args = invoke.GetParameters ();
			
			CodeMemberMethod met = new CodeMemberMethod ();
			met.Name = signal.Handler;
			met.ReturnType = new CodeTypeReference (delReturnType.FullName);
			
			foreach (ParameterInfo pinfo in args)
				met.Parameters.Add (new CodeParameterDeclarationExpression (pinfo.ParameterType, pinfo.Name));
			
			CodeRefactorer gen = GetCodeGenerator ();
			gen.AddMember (cls, met);
		}
		
		public void AddWidgetField (Gladeui.Widget widget)
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
		
		CodeMemberField GetFieldCode (Gladeui.Widget widget)
		{
			return GetFieldCode (widget, null);
		}
		
		CodeMemberField GetFieldCode (Gladeui.Widget widget, string name)
		{
			if (name == null) name = widget.Name;
			string type = widget.Object.GetType().FullName;
			CodeMemberField field = new CodeMemberField (type, name);
			CodeAttributeArgument attArg = new CodeAttributeArgument (new CodePrimitiveExpression (widget.Name));
			field.CustomAttributes.Add (new CodeAttributeDeclaration ("Glade.Widget", attArg));
			return field;
		}
		
		IField FindWidgetField (IClass cls, Gladeui.Widget w)
		{
			foreach (IField field in cls.Fields)
				if (field.Name == w.Name)
					return field;
			return null;
		}
		
		static Type GetHandlerType (Gladeui.Widget widget, string signalId)
		{
			signalId = signalId.Replace ('-','_');
			Type wtype = widget.Object.GetType ();
			foreach (EventInfo ev in wtype.GetEvents ()) {
				GLib.SignalAttribute sat = (GLib.SignalAttribute) Attribute.GetCustomAttribute (ev, typeof(GLib.SignalAttribute), true);
				if (sat != null && sat.CName == signalId)
					return ev.EventHandlerType;
			}
			return null;
		}
		
		CodeRefactorer GetCodeGenerator ()
		{
			CodeRefactorer cr = new CodeRefactorer (IdeApp.ProjectOperations.CurrentOpenCombine, IdeApp.ProjectOperations.ParserDatabase);
			cr.TextFileProvider = textFileProvider;
			return cr;
		}
		
		IClass GetClass ()
		{
			return window.GetClass (rootWidget.Name);
		}
		
	}
}
