//
// GuiBuilderWindow.cs
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Text;

using MonoDevelop.GtkCore.Dialogs;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderWindow: IDisposable
	{
		Stetic.Wrapper.Widget rootWidget;
		Stetic.Project gproject;
		GuiBuilderProject fproject;
		
		public event WindowEventHandler NameChanged;
		
		internal GuiBuilderWindow (GuiBuilderProject fproject, Stetic.Project gproject, Stetic.Wrapper.Widget rootWidget)
		{
			this.fproject = fproject;
			this.rootWidget = rootWidget;
			this.gproject = gproject;
			gproject.WidgetNameChanged += new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
			gproject.ProjectReloaded += new EventHandler (OnProjectReloaded);
		}
		
		void OnProjectReloaded (object s, EventArgs a)
		{
			string name = rootWidget.Wrapped.Name;
			
			foreach (Gtk.Widget w in gproject.Toplevels) {
				if (w.Name == name) {
					rootWidget = Stetic.Wrapper.Widget.Lookup (w);
					break;
				}
			}
		}
		
		public Stetic.Wrapper.Widget RootWidget {
			get { return rootWidget; }
		}
		
		public GuiBuilderProject Project {
			get { return fproject; }
		}
		
		public string Name {
			get { return rootWidget.Wrapped.Name; }
		}
		
		public string SourceCodeFile {
			get {
				IClass cls = GetClass ();
				if (cls != null) return cls.Region.FileName;
				else return null;
			}
		}
		
		public GuiBuilderEditSession CreateEditSession (ITextFileProvider textFileProvider)
		{
			return new GuiBuilderEditSession (this, textFileProvider);
		}
		
		internal void SetWidgetInfo (XmlElement data)
		{
			fproject.UpdatingWindow = true;
			string oldName = rootWidget.Wrapped.Name;
			
			try {
				rootWidget.Delete ();
				Gtk.Widget w = Stetic.WidgetUtils.ImportWidget (gproject, data);
				rootWidget = Stetic.Wrapper.Container.Lookup (w);
				gproject.AddWidget ((Gtk.Widget) rootWidget.Wrapped);
			} finally {
				fproject.UpdatingWindow = false;
			}

			if (rootWidget.Wrapped.Name != oldName && NameChanged != null)
				NameChanged (this, new WindowEventArgs (this));
		}
		
		public void Dispose ()
		{
			gproject.WidgetNameChanged -= new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
			gproject.ProjectReloaded -= new EventHandler (OnProjectReloaded);
		}
		
		internal void Save ()
		{
			fproject.Save ();
		}
		
		void OnWidgetNameChanged (object s, Stetic.Wrapper.WidgetNameChangedArgs args)
		{
			if (!InsideWindow (args.Widget))
				return;
			
			if (args.Widget == rootWidget && NameChanged != null)
				NameChanged (this, new WindowEventArgs (this));
		}
		
		public IClass GetClass ()
		{
			return GetClass (rootWidget.Wrapped.Name);
		}
		
		internal IClass GetClass (string name)
		{
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (fproject.Project);
			IClass[] classes = ctx.GetProjectContents ();
			foreach (IClass cls in classes) {
				if (cls.FullyQualifiedName == name)
					return cls;
			}
			return null;
		}
		
		public bool InsideWindow (Stetic.Wrapper.Widget widget)
		{
			while (widget.ParentWrapper != null)
				widget = widget.ParentWrapper;
			return widget == rootWidget;
		}
		
		public bool BindToClass ()
		{
			if (SourceCodeFile != null)
				return true;
			
			// Find the classes that could be bound to this design
			
			ArrayList list = new ArrayList ();
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (Project.Project);
			foreach (IClass cls in ctx.GetProjectContents ())
				if (IsValidClass (ctx, cls))
					list.Add (cls.FullyQualifiedName);
		
			// Ask what to do
			
			using (BindDesignDialog dialog = new BindDesignDialog (Name, list, Project.Project.BaseDirectory)) {
				if (!dialog.Run ())
					return false;
				
				if (dialog.CreateNew)
					CreateClass (dialog.ClassName, dialog.Namespace, dialog.Folder);

				string fullName = dialog.Namespace.Length > 0 ? dialog.Namespace + "." + dialog.ClassName : dialog.ClassName;
				rootWidget.Wrapped.Name = fullName;
				Save ();
			}
			return true;
		}
		
		void CreateClass (string name, string namspace, string folder)
		{
			string fullName = namspace.Length > 0 ? namspace + "." + name : name;
			
			CodeRefactorer gen = new CodeRefactorer (IdeApp.ProjectOperations.CurrentOpenCombine, IdeApp.ProjectOperations.ParserDatabase);
			
			CodeTypeDeclaration type = new CodeTypeDeclaration ();
			type.Name = name;
			type.IsClass = true;
			type.BaseTypes.Add (new CodeTypeReference (rootWidget.ClassDescriptor.WrappedTypeName));
			
			// Generate the constructor. It contains the call that builds the widget.
			
			CodeConstructor ctor = new CodeConstructor ();
			ctor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			
			foreach (Stetic.PropertyDescriptor prop in rootWidget.ClassDescriptor.InitializationProperties) {
				if (prop.PropertyType.IsValueType)
					ctor.BaseConstructorArgs.Add (new CodePrimitiveExpression (Activator.CreateInstance (prop.PropertyType)));
				else
					ctor.BaseConstructorArgs.Add (null);
			}
			
			CodeMethodInvokeExpression call = new CodeMethodInvokeExpression (
				new CodeMethodReferenceExpression (
					new CodeTypeReferenceExpression ("Stetic.Gui"),
					"Build"
				),
				new CodeThisReferenceExpression (),
				new CodeTypeOfExpression (fullName)
			);
			ctor.Statements.Add (call);
			type.Members.Add (ctor);
			
			// Add signal handlers
			
			foreach (Stetic.Signal signal in rootWidget.Signals) {
				CodeMemberMethod met = new CodeMemberMethod ();
				met.Name = signal.Handler;
				met.Attributes = MemberAttributes.Family;
				met.ReturnType = new CodeTypeReference (signal.SignalDescriptor.HandlerReturnTypeName);
				
				foreach (Stetic.ParameterDescriptor pinfo in signal.SignalDescriptor.HandlerParameters)
					met.Parameters.Add (new CodeParameterDeclarationExpression (pinfo.TypeName, pinfo.Name));
					
				type.Members.Add (met);
			}
			
			// Create the class
			
			IClass cls = gen.CreateClass (Project.Project, ((DotNetProject)Project.Project).LanguageName, folder, namspace, type);
			if (cls == null)
				throw new UserException ("Could not create class " + fullName);
			
			Project.Project.AddFile (cls.Region.FileName, BuildAction.Compile);
			Project.Project.Save (IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor ());
			
			// Make sure the database is up-to-date
			IdeApp.ProjectOperations.ParserDatabase.UpdateFile (Project.Project, cls.Region.FileName, null);
		}
		
		internal bool IsValidClass (IParserContext ctx, IClass cls)
		{
			if (cls.BaseTypes != null) {
				foreach (string bt in cls.BaseTypes) {
					if (bt == rootWidget.Wrapped.GetType().FullName)
						return true;
					
					IClass baseCls = ctx.GetClass (bt, true, true);
					if (IsValidClass (ctx, baseCls))
						return true;
				}
			}
			return false;
		}
	}
	
	class OpenDocumentFileProvider: ITextFileProvider
	{
		public IEditableTextFile GetEditableTextFile (string filePath)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.FileName == filePath) {
					IEditableTextFile ef = doc.Content as IEditableTextFile;
					if (ef != null) return ef;
				}
			}
			return null;
		}
	}
}
