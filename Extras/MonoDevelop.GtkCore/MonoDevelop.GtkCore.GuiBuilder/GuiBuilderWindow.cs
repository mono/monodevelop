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
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Text;

using MonoDevelop.GtkCore.Dialogs;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderWindow: IDisposable
	{
		Stetic.WidgetComponent rootWidget;
		GuiBuilderProject fproject;
		Stetic.Project gproject;
		string name;
		
		public event WindowEventHandler Changed;
		
		internal GuiBuilderWindow (GuiBuilderProject fproject, Stetic.Project gproject, Stetic.WidgetComponent rootWidget)
		{
			this.fproject = fproject;
			this.rootWidget = rootWidget;
			this.gproject = gproject;
			name = rootWidget.Name;
			gproject.ProjectReloaded += OnProjectReloaded;
			rootWidget.Changed += OnChanged;
		}
		
		public Stetic.WidgetComponent RootWidget {
			get { return rootWidget; }
		}
		
		public GuiBuilderProject Project {
			get { return fproject; }
		}
		
		public string Name {
			get { return rootWidget.Name; }
		}
		
		public string SourceCodeFile {
			get { return fproject.GetSourceCodeFile (rootWidget); }
		}
		
		public void Dispose ()
		{
			gproject.ProjectReloaded -= OnProjectReloaded;
			rootWidget.Changed -= OnChanged;
		}
		
		void OnProjectReloaded (object s, EventArgs args)
		{
			rootWidget.Changed -= OnChanged;
			rootWidget = gproject.GetComponent (name);
			if (rootWidget != null)
				rootWidget.Changed += OnChanged;
		}
		
		void OnChanged (object o, EventArgs args)
		{
			// Update the name, it may have changed
			name = rootWidget.Name;
			
			if (Changed != null)
				Changed (this, new WindowEventArgs (this));
		}
		
		public bool BindToClass ()
		{
			if (SourceCodeFile != null)
				return true;
			
			// Find the classes that could be bound to this design
			
			ArrayList list = new ArrayList ();
			IParserContext ctx = fproject.GetParserContext ();
			foreach (IClass cls in ctx.GetProjectContents ())
				if (IsValidClass (ctx, cls))
					list.Add (cls.FullyQualifiedName);
		
			// Ask what to do

			try {
				using (BindDesignDialog dialog = new BindDesignDialog (Name, list, Project.Project.BasePath)) {
					if (!dialog.Run ())
						return false;
					
					if (dialog.CreateNew)
						CreateClass (dialog.ClassName, dialog.Namespace, dialog.Folder);

					string fullName = dialog.Namespace.Length > 0 ? dialog.Namespace + "." + dialog.ClassName : dialog.ClassName;
					rootWidget.Name = fullName;
					fproject.Save (true);
				}
				return true;
			} catch (Exception ex) {
				IdeApp.Services.MessageService.ShowError (ex);
				return false;
			}
		}
		
		void CreateClass (string name, string namspace, string folder)
		{
			string fullName = namspace.Length > 0 ? namspace + "." + name : name;
			// TODO: Project Conversion (null is not correct)
			CodeRefactorer gen = null; //new CodeRefactorer (ProjectService.Solution, IdeApp.ProjectOperations.ParserDatabase);
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (fproject.Project);
			
			CodeTypeDeclaration type = new CodeTypeDeclaration ();
			type.Name = name;
			type.IsClass = true;
			type.IsPartial = info.GeneratePartialClasses;
			type.BaseTypes.Add (new CodeTypeReference (rootWidget.Type.ClassName));
			
			// Generate the constructor. It contains the call that builds the widget.
			
			CodeConstructor ctor = new CodeConstructor ();
			ctor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			
			foreach (object val in rootWidget.Type.InitializationValues) {
				if (val is Enum) {
					ctor.BaseConstructorArgs.Add (
						new CodeFieldReferenceExpression (
							new CodeTypeReferenceExpression (val.GetType ()),
							val.ToString ()
						)
					);
				}
				else
					ctor.BaseConstructorArgs.Add (new CodePrimitiveExpression (val));
			}
			
			if (info.GeneratePartialClasses) {
				CodeMethodInvokeExpression call = new CodeMethodInvokeExpression (
					new CodeMethodReferenceExpression (
						new CodeThisReferenceExpression (),
						"Build"
					)
				);
				ctor.Statements.Add (call);
			} else {
				CodeMethodInvokeExpression call = new CodeMethodInvokeExpression (
					new CodeMethodReferenceExpression (
						new CodeTypeReferenceExpression ("Stetic.Gui"),
						"Build"
					),
					new CodeThisReferenceExpression (),
					new CodeTypeOfExpression (fullName)
				);
				ctor.Statements.Add (call);
			}
			type.Members.Add (ctor);
			
			// Add signal handlers
			
			AddSignalsRec (type, rootWidget);
			foreach (Stetic.Component ag in rootWidget.GetActionGroups ())
				AddSignalsRec (type, ag);
			
			// Create the class
			
			IClass cls = gen.CreateClass (Project.Project, ((MSBuildProject)Project.Project).Language, folder, namspace, type);
			if (cls == null)
				throw new UserException ("Could not create class " + fullName);
			
			Project.Project.Add (new ProjectFile (cls.Region.FileName, FileType.Compile));
			ProjectService.SaveProject (Project.Project);
			
			// Make sure the database is up-to-date
// TODO: Project Conversion
//			IdeApp.ProjectOperations.ParserDatabase.UpdateFile (Project.Project, cls.Region.FileName, null);
		}
		
		void AddSignalsRec (CodeTypeDeclaration type, Stetic.Component comp)
		{
			foreach (Stetic.Signal signal in comp.GetSignals ()) {
				CodeMemberMethod met = new CodeMemberMethod ();
				met.Name = signal.Handler;
				met.Attributes = MemberAttributes.Family;
				met.ReturnType = new CodeTypeReference (signal.SignalDescriptor.HandlerReturnTypeName);
				
				foreach (Stetic.ParameterDescriptor pinfo in signal.SignalDescriptor.HandlerParameters)
					met.Parameters.Add (new CodeParameterDeclarationExpression (pinfo.TypeName, pinfo.Name));
					
				type.Members.Add (met);
			}
			foreach (Stetic.Component cc in comp.GetChildren ()) {
				AddSignalsRec (type, cc);
			}
		}
		
		internal bool IsValidClass (IParserContext ctx, IClass cls)
		{
			if (cls.BaseTypes != null) {
				foreach (IReturnType bt in cls.BaseTypes) {
					if (bt.FullyQualifiedName == rootWidget.Type.ClassName)
						return true;
					
					IClass baseCls = ctx.GetClass (bt.FullyQualifiedName, true, true);
					if (baseCls != null && IsValidClass (ctx, baseCls))
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
					IEditableTextFile ef = doc.GetContent<IEditableTextFile> ();
					if (ef != null) return ef;
				}
			}
			return null;
		}
	}
}
