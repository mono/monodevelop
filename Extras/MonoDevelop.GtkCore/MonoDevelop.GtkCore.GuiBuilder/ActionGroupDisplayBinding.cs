//
// ActionGroupDisplayBinding.cs
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.GtkCore.Dialogs;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class ActionGroupDisplayBinding: IDisplayBinding
	{
		bool excludeThis = false;
		
		public string DisplayName {
			get { return "Action Group Editor"; }
		}
		
		public virtual bool CanCreateContentForFile (string fileName)
		{
			if (excludeThis)
				return false;
			if (IdeApp.ProjectOperations.CurrentOpenCombine == null)
				return false;
			
			if (GetActionGroup (fileName) == null)
				return false;
			
			excludeThis = true;
			IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingPerFileName (fileName);
			excludeThis = false;
			return db != null;
		}

		public virtual bool CanCreateContentForMimeType (string mimetype)
		{
			return false;
		}
		
		public virtual IViewContent CreateContentForFile (string fileName)
		{
			excludeThis = true;
			IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingPerFileName (fileName);
			
			Project project = IdeApp.ProjectOperations.CurrentOpenCombine.GetProjectContainingFile (fileName);
			GtkDesignInfo info = GtkCoreService.EnableGtkSupport (project);
			
			ActionGroupView view = new ActionGroupView (db.CreateContentForFile (fileName), GetActionGroup (fileName), info.GuiBuilderProject);
			excludeThis = false;
			view.Load (fileName);
			return view;
		}
		
		public virtual IViewContent CreateContentForMimeType (string mimeType, System.IO.Stream content)
		{
			return null;
		}
		
		Stetic.Wrapper.ActionGroup GetActionGroup (string file)
		{
			Project project = IdeApp.ProjectOperations.CurrentOpenCombine.GetProjectContainingFile (file);
			if (project == null)
				return null;
				
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info == null)
				return null;

			GuiBuilderProject gproject = info.GuiBuilderProject;
			foreach (Stetic.Wrapper.ActionGroup group in gproject.SteticProject.ActionGroups) {
				if (file == CodeBinder.GetSourceCodeFile (project, group))
					return group;
			}

			return null;
		}
		
		internal static string BindToClass (Project project, Stetic.Wrapper.ActionGroup group)
		{
			string file = CodeBinder.GetSourceCodeFile (project, group);
			if (file != null)
				return file;
				
			// Find the classes that could be bound to this design
			
			ArrayList list = new ArrayList ();
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project);
			foreach (IClass cls in ctx.GetProjectContents ())
				if (IsValidClass (ctx, cls))
					list.Add (cls.FullyQualifiedName);
		
			// Ask what to do
			
			using (BindDesignDialog dialog = new BindDesignDialog (group.Name, list, project.BaseDirectory)) {
				if (!dialog.Run ())
					return null;
				
				if (dialog.CreateNew)
					CreateClass (project, group, dialog.ClassName, dialog.Namespace, dialog.Folder);

				string fullName = dialog.Namespace.Length > 0 ? dialog.Namespace + "." + dialog.ClassName : dialog.ClassName;
				group.Name = fullName;
			}
			return CodeBinder.GetSourceCodeFile (project, group);
		}
		
		static IClass CreateClass (Project project, Stetic.Wrapper.ActionGroup group, string name, string namspace, string folder)
		{
			string fullName = namspace.Length > 0 ? namspace + "." + name : name;
			
			CodeRefactorer gen = new CodeRefactorer (IdeApp.ProjectOperations.CurrentOpenCombine, IdeApp.ProjectOperations.ParserDatabase);
			
			CodeTypeDeclaration type = new CodeTypeDeclaration ();
			type.Name = name;
			type.IsClass = true;
			type.BaseTypes.Add (new CodeTypeReference ("Gtk.ActionGroup"));
			
			// Generate the constructor. It contains the call that builds the widget.
			
			CodeConstructor ctor = new CodeConstructor ();
			ctor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			ctor.BaseConstructorArgs.Add (new CodePrimitiveExpression (fullName));
			
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
			
			foreach (Stetic.Wrapper.Action action in group.Actions) {
				foreach (Stetic.Signal signal in action.Signals) {
					CodeMemberMethod met = new CodeMemberMethod ();
					met.Name = signal.Handler;
					met.Attributes = MemberAttributes.Family;
					met.ReturnType = new CodeTypeReference (signal.SignalDescriptor.HandlerReturnTypeName);
					
					foreach (Stetic.ParameterDescriptor pinfo in signal.SignalDescriptor.HandlerParameters)
						met.Parameters.Add (new CodeParameterDeclarationExpression (pinfo.TypeName, pinfo.Name));
						
					type.Members.Add (met);
				}
			}
			
			// Create the class
			
			IClass cls = gen.CreateClass (project, ((DotNetProject)project).LanguageName, folder, namspace, type);
			if (cls == null)
				throw new UserException ("Could not create class " + fullName);
			
			project.AddFile (cls.Region.FileName, BuildAction.Compile);
			project.Save (IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor ());
			
			// Make sure the database is up-to-date
			IdeApp.ProjectOperations.ParserDatabase.UpdateFile (project, cls.Region.FileName, null);
			return cls;
		}
		
		internal static bool IsValidClass (IParserContext ctx, IClass cls)
		{
			if (cls.BaseTypes != null) {
				foreach (string bt in cls.BaseTypes) {
					if (bt == "Gtk.ActionGroup")
						return true;
					
					IClass baseCls = ctx.GetClass (bt, true, true);
					if (baseCls != null && IsValidClass (ctx, baseCls))
						return true;
				}
			}
			return false;
		}
	}
}
