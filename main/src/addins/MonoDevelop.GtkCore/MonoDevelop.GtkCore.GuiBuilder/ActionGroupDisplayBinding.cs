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

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.GtkCore.Dialogs;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide.Gui.Documents;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Collections.Immutable;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	[ExportDocumentControllerFactory (FileExtension = ".cs")]
	public class ActionGroupDisplayBinding : FileDocumentControllerFactory
	{
		bool excludeThis = false;
		
		protected override async Task<IEnumerable<DocumentControllerDescription>> GetSupportedControllersAsync (FileDescriptor file)
		{
			var list = ImmutableList<DocumentControllerDescription>.Empty;

			if (excludeThis)
				return list;

			if (file.FilePath.IsNullOrEmpty || !(file.Owner is DotNetProject))
				return list;

			if (!IdeApp.Workspace.IsOpen)
				return list;

			if (GetActionGroup (file.FilePath) == null)
				return list;

			excludeThis = true;
			var db = (await IdeServices.DocumentControllerService.GetSupportedControllers (file)).FirstOrDefault (d => d.Role == DocumentControllerRole.Source);
			excludeThis = false;
			if (db != null) {
				list = list.Add (
				new DocumentControllerDescription {
					CanUseAsDefault = true,
					Role = DocumentControllerRole.VisualDesign,
					Name = MonoDevelop.Core.GettextCatalog.GetString ("Action Group Editor")
				});
			}
			return list;
		}

		public override async Task<DocumentController> CreateController (FileDescriptor file, DocumentControllerDescription controllerDescription)
		{
			excludeThis = true;
			var db = (await IdeServices.DocumentControllerService.GetSupportedControllers (file)).FirstOrDefault (d => d.Role == DocumentControllerRole.Source);
			var info = GtkDesignInfo.FromProject ((DotNetProject)file.Owner);
			
			var content = await db.CreateController (file);
			var view = new ActionGroupView (content, GetActionGroup (file.FilePath), info.GuiBuilderProject);
			excludeThis = false;
			return view;
		}
		
		Stetic.ActionGroupInfo GetActionGroup (string file)
		{
			var project = IdeApp.Workspace.GetProjectsContainingFile (file).FirstOrDefault ();
			if (!GtkDesignInfo.HasDesignedObjects (project))
				return null;
				
			return GtkDesignInfo.FromProject (project).GuiBuilderProject.GetActionGroupForFile (file);
		}
		
		internal static string BindToClass (MonoDevelop.Projects.Project project, Stetic.ActionGroupInfo group)
		{
			GuiBuilderProject gproject = GtkDesignInfo.FromProject (project).GuiBuilderProject;
			string file = gproject.GetSourceCodeFile (group);
			if (file != null)
				return file;
				
			// Find the classes that could be bound to this design
			
			ArrayList list = new ArrayList ();
			var ctx = gproject.GetParserContext ();
			foreach (var cls in ctx.GetAllTypesInMainAssembly ())
				if (IsValidClass (cls))
					list.Add (cls.GetFullName ());
		
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
		
		static ITypeSymbol CreateClass (MonoDevelop.Projects.Project project, Stetic.ActionGroupComponent group, string name, string namspace, string folder)
		{
			string fullName = namspace.Length > 0 ? namspace + "." + name : name;
			
			var type = SyntaxFactory.ClassDeclaration (name)
				.AddBaseListTypes (SyntaxFactory.SimpleBaseType (SyntaxFactory.ParseTypeName ("Gtk.ActionGroup")));

			// Generate the constructor. It contains the call that builds the widget.
			var ctor = SyntaxFactory.ConstructorDeclaration (
				new SyntaxList<AttributeListSyntax> (),
				SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.PublicKeyword)),
				SyntaxFactory.Identifier (name),
				SyntaxFactory.ParameterList (),
				SyntaxFactory.ConstructorInitializer (SyntaxKind.BaseKeyword, SyntaxFactory.ArgumentList (new SeparatedSyntaxList<ArgumentSyntax> { SyntaxFactory.Argument (SyntaxFactory.ParseExpression (fullName)) } )),
				SyntaxFactory.Block (
					SyntaxFactory.ExpressionStatement (
						SyntaxFactory.InvocationExpression (
							SyntaxFactory.ParseExpression ("Stetic.Gui.Build"),
							SyntaxFactory.ArgumentList (
								new SeparatedSyntaxList<ArgumentSyntax> {
									SyntaxFactory.Argument (SyntaxFactory.ThisExpression ()),
									SyntaxFactory.Argument (SyntaxFactory.ParseExpression (fullName))
								}
							)
						) 
					)
				)
			);
			
			type = type.AddMembers (ctor);
			
			// Add signal handlers
			foreach (Stetic.ActionComponent action in group.GetActions ()) {
				foreach (Stetic.Signal signal in action.GetSignals ()) {
					
					var parameters = new SeparatedSyntaxList<ParameterSyntax> ();
					foreach (var p in signal.SignalDescriptor.HandlerParameters) {
						parameters = parameters.Add (SyntaxFactory.Parameter (new SyntaxList<AttributeListSyntax> (), SyntaxFactory.TokenList (), SyntaxFactory.ParseTypeName (p.TypeName), SyntaxFactory.Identifier (p.Name), null)); 
					}
					
					var met = SyntaxFactory.MethodDeclaration (
				          new SyntaxList<AttributeListSyntax> (),
				          SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.ProtectedKeyword)),
				          SyntaxFactory.ParseTypeName (signal.SignalDescriptor.HandlerReturnTypeName),
				          null,
				          SyntaxFactory.Identifier (signal.Handler),
				          null,
				          SyntaxFactory.ParameterList (parameters),
				          new SyntaxList<TypeParameterConstraintClauseSyntax> (),
				          SyntaxFactory.Block (),
				          null
			          );
					
						
					type = type.AddMembers (met);
				}
			}
			
			// Create the class
			return CodeGenerationService.AddType ((DotNetProject)project, folder, namspace, type);
		}
		
		internal static bool IsValidClass (ITypeSymbol cls)
		{
			if (cls.SpecialType == SpecialType.System_Object)
				return false;
			if (cls.BaseType.GetFullName () == "Gtk.ActionGroup")
				return true;
			return IsValidClass (cls.BaseType);
		}
	}
}
