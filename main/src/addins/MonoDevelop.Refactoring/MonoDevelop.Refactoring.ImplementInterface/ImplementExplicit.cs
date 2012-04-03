// 
// ImplementExplicit.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;
using Mono.TextEditor.PopupWindow;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.TypeSystem;

namespace MonoDevelop.Refactoring.ImplementInterface
{
	public class ImplementExplicit : RefactoringOperation
	{
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("I_mplement explicit");
		}
		
		internal static bool InternalIsValid (RefactoringOptions options, out IType interfaceType)
		{
			var unit = options.Document.ParsedDocument.GetAst<CompilationUnit> ();
			interfaceType = null;
			if (unit == null)
				return false;
			var loc = options.Document.Editor.Caret.Location;
			var declaration = unit.GetNodeAt<TypeDeclaration> (loc.Line, loc.Column);
			if (declaration == null)
				return false;
			if (!declaration.BaseTypes.Any (bt => bt.Contains (loc.Line, loc.Column)))
				return false;
			if (options.ResolveResult == null)
				return false;
			interfaceType = options.ResolveResult.Type;
			var def = interfaceType.GetDefinition ();
			if (def == null)
				return false;
			if (def.Kind != TypeKind.Interface)
				return false;
			
			var declaringType = options.Document.ParsedDocument.GetInnermostTypeDefinition (loc);
			var type = declaringType.Resolve (options.Document.ParsedDocument.ParsedFile.GetTypeResolveContext (options.Document.Compilation, loc)).GetDefinition ();
			return interfaceType.GetAllBaseTypes ().Any (bt => CodeGenerator.CollectMembersToImplement (type, bt, false).Any ());
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			IType interfaceType;
			return InternalIsValid (options, out interfaceType);
		}
		
		internal static void InternalRun (RefactoringOptions options, bool implementExplicit)
		{
			IType interfaceType;
			
			if (!InternalIsValid (options, out interfaceType))
				return;
			var loc = options.Document.Editor.Caret.Location;
			var declaringType = options.Document.ParsedDocument.GetInnermostTypeDefinition (loc);
			if (declaringType == null)
				return;
			
			var editor = options.GetTextEditorData ().Parent;
			
			var mode = new InsertionCursorEditMode (editor, CodeGenerationService.GetInsertionPoints (options.Document, declaringType));
			var helpWindow = new InsertionCursorLayoutModeHelpWindow ();
			helpWindow.TransientFor = IdeApp.Workbench.RootWindow;
			helpWindow.TitleText = GettextCatalog.GetString ("Implement Interface");
			mode.HelpWindow = helpWindow;
			mode.CurIndex = mode.InsertionPoints.Count - 1;
			mode.StartMode ();
			mode.Exited += delegate(object s, InsertionCursorEventArgs args) {
				if (args.Success) {
					var generator = options.CreateCodeGenerator ();
					if (generator == null) 
						return;
					var type = declaringType.Resolve (options.Document.ParsedDocument.GetTypeResolveContext (options.Document.Compilation, loc)).GetDefinition ();
					args.InsertionPoint.Insert (options.GetTextEditorData (), generator.CreateInterfaceImplementation (type, declaringType, interfaceType, implementExplicit));
				}
			};
		}
		
		public override void Run (RefactoringOptions options)
		{
			InternalRun (options, true);
		}
	}
}
