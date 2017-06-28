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
using System.Collections;
using System.CodeDom;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.GtkCore.Dialogs;
using MonoDevelop.Ide;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderWindow: IDisposable
	{
		Stetic.WidgetInfo rootWidget;
		GuiBuilderProject fproject;
		Stetic.Project gproject;
		string name;
		
		public event WindowEventHandler Changed;
		
		internal GuiBuilderWindow (GuiBuilderProject fproject, Stetic.Project gproject, Stetic.WidgetInfo rootWidget)
		{
			this.fproject = fproject;
			this.rootWidget = rootWidget;
			this.gproject = gproject;
			name = rootWidget.Name;
			gproject.ProjectReloaded += OnProjectReloaded;
			rootWidget.Changed += OnChanged;
		}
		
		public Stetic.WidgetInfo RootWidget {
			get { return rootWidget; }
		}
		
		public GuiBuilderProject Project {
			get { return fproject; }
		}
		
		public string Name {
			get { return rootWidget.Name; }
		}
		
		public FilePath SourceCodeFile {
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
			rootWidget = gproject.GetWidget (name);
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
			if (SourceCodeFile != FilePath.Null)
				return true;
			
			// Find the classes that could be bound to this design
			var ctx = fproject.GetParserContext ();
			ArrayList list = new ArrayList ();
			foreach (var cls in ctx.GetAllTypesInMainAssembly ()) {
				if (IsValidClass (cls))
					list.Add (cls.GetFullName ());
			}
		
			// Ask what to do

			try {
				using (BindDesignDialog dialog = new BindDesignDialog (Name, list, Project.Project.BaseDirectory)) {
					if (!dialog.Run ())
						return false;
					
					if (dialog.CreateNew)
						CreateClass (dialog.ClassName, dialog.Namespace, dialog.Folder);

					string fullName = dialog.Namespace.Length > 0 ? dialog.Namespace + "." + dialog.ClassName : dialog.ClassName;
					rootWidget.Name = fullName;
					fproject.SaveWindow (true, fullName);
				}
				return true;
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
				return false;
			}
		}
		
		void CreateClass (string name, string namspace, string folder)
		{
			// TODO: Type system conversion.
			
//			string fullName = namspace.Length > 0 ? namspace + "." + name : name;
//			
//			var gen = new CodeRefactorer (fproject.Project.ParentSolution);
//			bool partialSupport = fproject.Project.UsePartialTypes;
//			Stetic.WidgetComponent component = (Stetic.WidgetComponent) rootWidget.Component;
//			
//			CodeTypeDeclaration type = new CodeTypeDeclaration ();
//			type.Name = name;
//			type.IsClass = true;
//			type.IsPartial = partialSupport;
//			type.BaseTypes.Add (new CodeTypeReference (component.Type.ClassName));
//			
//			// Generate the constructor. It contains the call that builds the widget.
//			
//			CodeConstructor ctor = new CodeConstructor ();
//			ctor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
//			
//			foreach (object val in component.Type.InitializationValues) {
//				if (val is Enum) {
//					ctor.BaseConstructorArgs.Add (
//						new CodeFieldReferenceExpression (
//							new CodeTypeReferenceExpression (val.GetType ()),
//							val.ToString ()
//						)
//					);
//				}
//				else
//					ctor.BaseConstructorArgs.Add (new CodePrimitiveExpression (val));
//			}
//			
//			if (partialSupport) {
//				CodeMethodInvokeExpression call = new CodeMethodInvokeExpression (
//					new CodeMethodReferenceExpression (
//						new CodeThisReferenceExpression (),
//						"Build"
//					)
//				);
//				ctor.Statements.Add (call);
//			} else {
//				CodeMethodInvokeExpression call = new CodeMethodInvokeExpression (
//					new CodeMethodReferenceExpression (
//						new CodeTypeReferenceExpression ("Stetic.Gui"),
//						"Build"
//					),
//					new CodeThisReferenceExpression (),
//					new CodeTypeOfExpression (fullName)
//				);
//				ctor.Statements.Add (call);
//			}
//			type.Members.Add (ctor);
//			
//			// Add signal handlers
//			
//			AddSignalsRec (type, component);
//			foreach (Stetic.Component ag in component.GetActionGroups ())
//				AddSignalsRec (type, ag);
//			
//			// Create the class
//			IType cls = gen.CreateClass (Project.Project, ((DotNetProject)Project.Project).LanguageName, folder, namspace, type);
//			if (cls == null)
//				throw new UserException ("Could not create class " + fullName);
//			
//			Project.Project.AddFile (cls.CompilationUnit.FileName, BuildAction.Compile);
//			IdeApp.ProjectOperations.Save (Project.Project);
//			
//			// Make sure the database is up-to-date
//			ProjectDomService.Parse (Project.Project, cls.CompilationUnit.FileName);
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
		
		internal bool IsValidClass (ITypeSymbol cls)
		{
			if (cls.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
				return false;
			if (cls.BaseType.GetFullName () == rootWidget.Component.Type.ClassName)
				return true;
			return IsValidClass (cls.BaseType);
		}
	}
	
	class OpenDocumentFileProvider: ITextFileProvider
	{
		public ITextDocument GetEditableTextFile (FilePath filePath)
		{
			foreach (var doc in IdeApp.Workbench.Documents) {
				if (doc.FileName == filePath) {
					var ef = doc.Editor;
					if (ef != null) return ef;
				}
			}
			return null;
		}
	}
}
