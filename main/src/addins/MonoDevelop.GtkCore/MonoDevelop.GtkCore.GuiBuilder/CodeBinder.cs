//
// CodeBinder.cs
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

using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.GtkCore.Dialogs;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.FindInFiles;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	/// This class provides several methods for managing the relation
	/// between an object (e.g. a window) and the source code that will implement the
	/// code for that object.
	///
	/// Once created, a CodeBinder object will keep track of the class bound to the
	/// object. If the class is renamed, it will properly update the object name.

	public class CodeBinder
	{
		ITextFileProvider textFileProvider;
		Stetic.Component targetObject;
		Project project;
		GuiBuilderProject gproject;
		string className;
		string classFile;
		
		public CodeBinder (Project project, ITextFileProvider textFileProvider, Stetic.Component targetObject)
		{
			this.project = project;
			this.textFileProvider = textFileProvider;

			gproject = GtkDesignInfo.FromProject (project).GuiBuilderProject;

			TargetObject = targetObject;
		}
		
		public Stetic.Component TargetObject {
			get { return targetObject; }
			set {
				this.targetObject = value;
				if (targetObject != null) {
					var cls = gproject.FindClass (GetClassName (targetObject));
					if (cls != null) {
						className = cls.GetFullName ();
						classFile = cls.Locations.First ().SourceTree.FilePath;
					}
				}
			}
		}
		
		/// Synchronizes the bindings between the object and the source code
		public void UpdateBindings (string fileName)
		{
			if (targetObject == null)
				return;
			
			var doc = TypeSystemService.ParseFile (project, fileName);
			classFile = fileName;
			
			if (doc != null) {
				var cls = GetClass ();
				UpdateBindings (targetObject, cls);
			
				if (cls != null)
					targetObject.GeneratePublic = cls.DeclaredAccessibility == Accessibility.Public;
			}
		}
		
		void UpdateBindings (Stetic.Component obj, ITypeSymbol cls)
		{
			if (targetObject == null || cls == null)
				return;

			// Remove signals for which there isn't a handler in the class
			
			Stetic.SignalCollection objectSignals = obj.GetSignals ();
			if (objectSignals != null) {
				Stetic.Signal[] signals = new Stetic.Signal [objectSignals.Count];
				objectSignals.CopyTo (signals, 0);
				foreach (Stetic.Signal signal in signals) {
					if (FindSignalHandler (cls, signal) == null) {
						obj.RemoveSignal (signal);
					}
				}
			}

			// Update children
			
			foreach (Stetic.Component ob in obj.GetChildren ())
				UpdateBindings (ob, cls);
		}
		
		static IMethodSymbol FindSignalHandler (ITypeSymbol cls, Stetic.Signal signal)
		{
			return cls.GetMembers (signal.Handler).OfType<IMethodSymbol> ().FirstOrDefault ();
		}

		public void UpdateField (Stetic.Component obj, string oldName)
		{
			if (targetObject == null)
				return;
				
			if (obj == targetObject)
				return;	// The root widget name can only be changed internally.
			
			var cls = GetClass (false);
			
			string newName = GetObjectName (obj);
			if (newName.Length == 0)
				return;
			
			if (cls != null) {
				var f = ClassUtils.FindWidgetField (cls, oldName);
				if (f != null) {
					MonoDevelop.Refactoring.Rename.RenameRefactoring.Rename (f, newName);
				}
			}
		}
		
		/// Adds a signal handler to the class
		public void BindSignal (Stetic.Signal signal)
		{
			if (targetObject == null)
				return;

			var cls = GetClass ();
			if (cls == null)
				return;
			
			if (FindSignalHandler (cls, signal) != null)
				return;
			
			var met = SyntaxFactory.MethodDeclaration (
				SyntaxFactory.ParseTypeName (signal.SignalDescriptor.HandlerReturnTypeName),
				signal.Handler
			).AddModifiers (SyntaxFactory.Token (SyntaxKind.ProtectedKeyword));
			
			var parameters = new List<ParameterSyntax> ();
			foreach (Stetic.ParameterDescriptor pinfo in signal.SignalDescriptor.HandlerParameters)
				parameters.Add (SyntaxFactory.Parameter (new SyntaxList<AttributeListSyntax>(), new SyntaxTokenList (), SyntaxFactory.ParseTypeName (pinfo.TypeName), SyntaxFactory.Identifier (pinfo.Name), null));
			met = met.AddParameterListParameters (parameters.ToArray ());
			
			CodeGenerationService.AddNewMember (cls, cls.Locations.First (), met);
		}
		
		public void UpdateSignal (Stetic.Signal oldSignal, Stetic.Signal newSignal)
		{
			if (targetObject == null)
				return;

			if (oldSignal.Handler == newSignal.Handler)
				return;

			var cls = GetClass ();
			if (cls == null)
				return;
			var met = FindSignalHandler (cls, oldSignal);
			if (met == null)
				return;
			MonoDevelop.Refactoring.Rename.RenameRefactoring.Rename (met, newSignal.Handler);
		}

		/// Adds a field to the class
		public void BindToField (Stetic.Component obj)
		{
			if (targetObject == null)
				return;

			string name = GetMemberName (obj);
			var cls = GetClass ();
			
			if (FindField (cls, name) != null)
				return;

			Document doc = IdeApp.Workbench.OpenDocument (cls.Locations.First ().SourceTree.FilePath, true);
			
			var editor = doc.Editor;
			if (editor != null) {
				CodeGenerationService.AddNewMember (cls, cls.Locations.First (), GetFieldCode (cls, obj, name));
			}
		}
		
		FieldDeclarationSyntax GetFieldCode (ITypeSymbol cls, Stetic.Component obj, string name)
		{
			return SyntaxFactory.FieldDeclaration (
				SyntaxFactory.VariableDeclaration (
					SyntaxFactory.ParseTypeName (obj.Type.ClassName),
					new SeparatedSyntaxList<VariableDeclaratorSyntax> {
						SyntaxFactory.VariableDeclarator (name)
					}
				)
			).AddModifiers (SyntaxFactory.Token (SyntaxKind.ProtectedKeyword));
		}
	
		
		static IFieldSymbol FindField (ITypeSymbol cls, string name)
		{
			return cls
				.GetMembers (name)
				.OfType<IFieldSymbol> ()
				.FirstOrDefault ();
		}
		
		public INamedTypeSymbol GetClass ()
		{
			return GetClass (true);
		}
		
		public INamedTypeSymbol GetClass (bool getUserClass)
		{
			if (targetObject == null)
				return null;

			var cls = gproject.FindClass (className, getUserClass);
			if (cls != null)
				return cls;
				
			// The class name may have changed. Try to guess the new name.
//			TODO (roslyn port) - is that really required ? 
//			var matches = new List<INamedTypeSymbol> ();
//			ParsedDocument unit = null;
//			var ctx = gproject.GetParserContext ();
//			var doc = TypeSystemService.ParseFile (project, classFile);
//			if (doc != null) {
//				unit = doc;
//				foreach (var fcls in unit.TopLevelTypeDefinitions) {
//					if (IsValidClass (fcls, targetObject))
//						matches.Add (fcls);
//				}
//			}
//			
//			// If found the class, just return it
//			if (matches.Count == 1) {
//				cls = matches [0];
//				className = cls.GetFullName ();
//				targetObject.Name = className;
//				gproject.SaveWindow (true, targetObject.Name);
//				return cls;
//			}
//			
//			// If not found, warn the user.
//			
//			if (unit != null && unit.TopLevelTypeDefinitions.Count > 0) {
//				using (var dialog = new SelectRenamedClassDialog (unit.TopLevelTypeDefinitions.Select (c => c.Resolve (project)))) {
//					if (dialog.Run ()) {
//						className = dialog.SelectedClass;
//						if (className == null)
//							return null;
//						else {
//							targetObject.Name = className;
//							gproject.SaveWindow (true, targetObject.Name);
//							return gproject.FindClass (className);
//						}
//					}
//				}
//			} else {
//				MessageService.ShowError (GettextCatalog.GetString ("The class bound to the component '{0}' could not be found. This may be due to syntax errors in the source code file.", GetObjectName(targetObject)));
//			}
			
			return null;
		}
		
		static bool IsValidClass (ITypeSymbol cls, Stetic.Component obj)
		{
			if (cls.BaseType.SpecialType == SpecialType.System_Object)
				return false;
			string typeName = obj.Type.ClassName;
			
			if (cls.BaseType.GetFullName () == typeName)
				return true;
			return IsValidClass (cls.BaseType, obj);
		}
		
		internal static string GetClassName (Stetic.Component obj)
		{
			return GetObjectName (obj);
		}
		
		internal static string GetMemberName (Stetic.Component obj)
		{
			return obj.Name;
		}
		
		internal static string GetObjectName (Stetic.Component obj)
		{
			return obj.Name;
		}
		
		internal static string GetClassName (Stetic.ProjectItemInfo obj)
		{
			return GetObjectName (obj);
		}
		
		internal static string GetObjectName (Stetic.ProjectItemInfo obj)
		{
			return obj.Name;
		}
	}
}
