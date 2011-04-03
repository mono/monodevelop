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


using System.Collections;
using System.CodeDom;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.GtkCore.Dialogs;
using MonoDevelop.Ide;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class ActionGroupDisplayBinding : IViewDisplayBinding
	{
		bool excludeThis = false;
		
		public string Name {
			get { return MonoDevelop.Core.GettextCatalog.GetString ("Action Group Editor"); }
		}
		
		public bool CanUseAsDefault {
			get { return true; }
		}
		
		public bool CanHandle (FilePath fileName, string mimeType, Project ownerProject)
		{
			if (excludeThis)
				return false;
			
			if (fileName.IsNullOrEmpty)
				return false;
			
			if (!IdeApp.Workspace.IsOpen)
				return false;
			
			if (GetActionGroup (fileName) == null)
				return false;
			
			excludeThis = true;
			var db = DisplayBindingService.GetDefaultViewBinding (fileName, mimeType, ownerProject);
			excludeThis = false;
			return db != null;
		}
		
		public IViewContent CreateContent (FilePath fileName, string mimeType, Project ownerProject)
		{
			excludeThis = true;
			var db = DisplayBindingService.GetDefaultViewBinding (fileName, mimeType, ownerProject);
			GtkDesignInfo info = GtkDesignInfo.FromProject ((DotNetProject) ownerProject);
			
			var content = db.CreateContent (fileName, mimeType, ownerProject);
			ActionGroupView view = new ActionGroupView (content, GetActionGroup (fileName), info.GuiBuilderProject);
			excludeThis = false;
			return view;
		}
		
		Stetic.ActionGroupInfo GetActionGroup (string file)
		{
			Project project = IdeApp.Workspace.GetProjectContainingFile (file);
			if (!GtkDesignInfo.HasDesignedObjects (project))
				return null;
				
			return GtkDesignInfo.FromProject (project).GuiBuilderProject.GetActionGroupForFile (file);
		}
		
		internal static string BindToClass (Project project, Stetic.ActionGroupInfo group)
		{
			GuiBuilderProject gproject = GtkDesignInfo.FromProject (project).GuiBuilderProject;
			string file = gproject.GetSourceCodeFile (group);
			if (file != null)
				return file;
				
			// Find the classes that could be bound to this design
			
			ArrayList list = new ArrayList ();
			ProjectDom ctx = gproject.GetParserContext ();
			foreach (IType cls in ctx.Types)
				if (IsValidClass (ctx, cls))
					list.Add (cls.FullName);
		
			// Ask what to do
			
			using (BindDesignDialog dialog = new BindDesignDialog (group.Name, list, project.BaseDirectory)) {
				if (!dialog.Run ())
					return null;
				
				if (dialog.CreateNew)
					CreateClass (project, (Stetic.ActionGroupComponent) group.Component, dialog.ClassName, dialog.Namespace, dialog.Folder);

				string fullName = dialog.Namespace.Length > 0 ? dialog.Namespace + "." + dialog.ClassName : dialog.ClassName;
				group.Name = fullName;
			}
			return gproject.GetSourceCodeFile (group);
		}
		
		static IType CreateClass (Project project, Stetic.ActionGroupComponent group, string name, string namspace, string folder)
		{
			string fullName = namspace.Length > 0 ? namspace + "." + name : name;
			
			CodeRefactorer gen = new CodeRefactorer (project.ParentSolution);
			
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
			
			foreach (Stetic.ActionComponent action in group.GetActions ()) {
				foreach (Stetic.Signal signal in action.GetSignals ()) {
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
			
			IType cls = null;
			cls = gen.CreateClass (project, ((DotNetProject)project).LanguageName, folder, namspace, type);
			if (cls == null)
				throw new UserException ("Could not create class " + fullName);
			
			project.AddFile (cls.CompilationUnit.FileName, BuildAction.Compile);
			IdeApp.ProjectOperations.Save (project);
			
			// Make sure the database is up-to-date
			ProjectDomService.Parse (project, cls.CompilationUnit.FileName);
			return cls;
		}
		
		internal static bool IsValidClass (ProjectDom ctx, IType cls)
		{
			if (cls.BaseTypes != null) {
				foreach (IReturnType bt in cls.BaseTypes) {
					if (bt.FullName == "Gtk.ActionGroup")
						return true;
					
					IType baseCls = ctx.GetType (bt);
					if (baseCls != null && IsValidClass (ctx, baseCls))
						return true;
				}
			}
			return false;
		}
	}
}
