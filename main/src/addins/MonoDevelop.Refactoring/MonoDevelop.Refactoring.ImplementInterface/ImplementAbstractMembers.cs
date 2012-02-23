// 
// ImplementAbstractMembers.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;
using Mono.TextEditor.PopupWindow;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.TypeSystem;

namespace MonoDevelop.Refactoring.ImplementInterface
{
	public class ImplementAbstractMembers : RefactoringOperation
	{
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Implement abstract members");
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			var unit = options.Document.ParsedDocument.GetAst<CompilationUnit> ();
			if (unit == null)
				return false;
			var generator = options.CreateCodeGenerator ();
			if (generator == null) 
				return false;
			
			var loc = options.Document.Editor.Caret.Location;
			var declaration = unit.GetNodeAt<TypeDeclaration> (loc.Line, loc.Column);
			if (declaration == null)
				return false;
			if (options.ResolveResult == null)
				return false;
			var type = options.ResolveResult.Type;
			var def = type.GetDefinition ();
			if (def == null || type.Kind == TypeKind.Interface)
				return false;
			if (!CurrentRefactoryOperationsHandler.ContainsAbstractMembers (type))
				return false;
			var declaringType = options.Document.ParsedDocument.GetInnermostTypeDefinition (loc);
			var missingAbstractMembers = def.Members.Where (member => member.IsAbstract && !declaringType.Members.Any (m => member.Name == m.Name));
			return missingAbstractMembers.Any ();
		}
		
		public static void Implement (RefactoringOptions options, IType abstractType)
		{
			var document = options.Document;
			TextEditor editor = document.Editor.Parent;
			
			var loc = document.Editor.Caret.Location;
			var declaringType = document.ParsedDocument.GetInnermostTypeDefinition (loc);
			var type = options.ResolveResult.Type;
			
			InsertionCursorEditMode mode = new InsertionCursorEditMode (editor, CodeGenerationService.GetInsertionPoints (document, declaringType));
			ModeHelpWindow helpWindow = new ModeHelpWindow ();
			helpWindow.TransientFor = IdeApp.Workbench.RootWindow;
			helpWindow.TitleText = GettextCatalog.GetString ("<b>Implement abstract members -- Targeting</b>");
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Key</b>"), GettextCatalog.GetString ("<b>Behavior</b>")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Up</b>"), GettextCatalog.GetString ("Move to <b>previous</b> target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Down</b>"), GettextCatalog.GetString ("Move to <b>next</b> target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Enter</b>"), GettextCatalog.GetString ("<b>Declare interface implementation</b> at target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Esc</b>"), GettextCatalog.GetString ("<b>Cancel</b> this refactoring.")));
			mode.HelpWindow = helpWindow;
			mode.CurIndex = mode.InsertionPoints.Count - 1;
			mode.StartMode ();
			mode.Exited += delegate(object s, InsertionCursorEventArgs args) {
				if (args.Success) {
					var generator = options.CreateCodeGenerator ();
					if (generator == null) 
						return;
					
					var def = type.GetDefinition ();
					var missingAbstractMembers = def.Members.Where (member => member.IsAbstract && !declaringType.Members.Any (m => member.Name == m.Name));
					StringBuilder sb = new StringBuilder ();
					foreach (var member in missingAbstractMembers) {
						if (sb.Length > 0) {
							sb.Append (editor.EolMarker);
							sb.Append (editor.EolMarker);
						}
						var resolvedType = declaringType.Resolve (options.Document.ParsedDocument.GetTypeResolveContext (options.Document.Compilation, loc)).GetDefinition ();
						sb.Append (generator.CreateMemberImplementation (resolvedType, declaringType, member, false).Code);
					}
					args.InsertionPoint.Insert (document.Editor, generator.WrapInRegions ("implemented abstract members of " + abstractType.FullName, sb.ToString ()));
				}
			};
		}
		
		public override void Run (RefactoringOptions options)
		{
			Implement (options, options.ResolveResult.Type);
		}
	}
}

