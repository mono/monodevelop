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

using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;

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
			if (options.ResolveResult == null)
				return false;
			
			IType type = options.Dom.GetType (options.ResolveResult.ResolvedType);
			if (type == null || type.ClassType != MonoDevelop.Projects.Dom.ClassType.Class)
				return false;
			return CurrentRefactoryOperationsHandler.ContainsAbstractMembers (type);
		}
		
		public override void Run (RefactoringOptions options)
		{
			DocumentLocation location = options.GetTextEditorData ().Caret.Location;
			IType interfaceType = options.Dom.GetType (options.ResolveResult.ResolvedType);
			IType declaringType = options.Document.CompilationUnit.GetTypeAt (location.Line, location.Column);
			options.Document.Editor.Document.BeginAtomicUndo ();
			
			var missingAbstractMembers = interfaceType.Members.Where (member => member.IsAbstract && !declaringType.Members.Any (m => member.Name == m.Name));
			CodeGenerationService.AddNewMembers (declaringType, missingAbstractMembers, "implemented abstract members of " + interfaceType.FullName);
			options.Document.Editor.Document.EndAtomicUndo ();
		}
	}
}

