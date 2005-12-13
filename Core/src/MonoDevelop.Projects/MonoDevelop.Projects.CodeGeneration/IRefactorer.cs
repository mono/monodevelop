//
// IRefactorer.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.CodeDom;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.Projects.CodeGeneration
{
	public interface IRefactorer
	{
		RefactorOperations SupportedOperations { get; }
		
		IClass CreateClass (IRefactorerContext ctx, string directory, string namspace, CodeTypeDeclaration type);
		void RenameClass (IRefactorerContext ctx, IClass cls, string newName);
		void RenameClassReferences (IRefactorerContext ctx, string file, IClass cls, string newName);
		
		IMethod AddMethod (IRefactorerContext ctx, IClass cls, CodeMemberMethod method);
		void RemoveMethod (IRefactorerContext ctx, IClass cls, IMethod method);
		void RenameMethod (IRefactorerContext ctx, IClass cls, IMethod method, string newName);
		void RenameMethodReferences (IRefactorerContext ctx, string fileName, IClass cls, IMethod method, string newName);
		
		IField AddField (IRefactorerContext ctx, IClass cls, CodeMemberField field);
		void RemoveField (IRefactorerContext ctx, IClass cls, IField field);
		void RenameField (IRefactorerContext ctx, IClass cls, IField field, string newName);
		
		void RenameFieldReferences (IRefactorerContext ctx, string fileName, IClass cls, IField field, string newName);
	}
}
