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
using System.Collections;
using System.CodeDom;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.Projects.CodeGeneration
{
	public interface IRefactorer
	{
		RefactorOperations SupportedOperations { get; }
		
		IClass CreateClass (RefactorerContext ctx, string directory, string namspace, CodeTypeDeclaration type);
		IClass RenameClass (RefactorerContext ctx, IClass cls, string newName);
		MemberReferenceCollection FindClassReferences (RefactorerContext ctx, string fileName, IClass cls);
		
		IMember AddMember (RefactorerContext ctx, IClass cls, CodeTypeMember memberInfo);
		void RemoveMember (RefactorerContext ctx, IClass cls, IMember member);
		IMember RenameMember (RefactorerContext ctx, IClass cls, IMember member, string newName);
		IMember ReplaceMember (RefactorerContext ctx, IClass cls, IMember oldMember, CodeTypeMember memberInfo);
		MemberReferenceCollection FindMemberReferences (RefactorerContext ctx, string fileName, IClass cls, IMember member);
	}
	
	public class MemberReference
	{
		int position;
		string fileName;
		string name;
		RefactorerContext rctx;
		
		public MemberReference (RefactorerContext rctx, string fileName, int position, string name)
		{
			this.position = position;
			this.fileName = fileName;
			this.name = name;
		}
		
		public int Position {
			get { return position; }
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public virtual void Rename (string newName)
		{
			if (rctx == null)
				throw new InvalidOperationException ("Refactory context not available.");

			IEditableTextFile file = rctx.GetFile (fileName);
			if (file != null) {
				file.DeleteText (position, name.Length);
				file.InsertText (position, newName);
				rctx.Save ();
			}
		}
	}
	
	public class MemberReferenceCollection: CollectionBase
	{
		public void Add (MemberReference reference)
		{
			List.Add (reference);
		}
		
		public void AddRange (IEnumerable collection)
		{
			foreach (MemberReference mref in collection)
				List.Add (mref);
		}
		
		public MemberReference this [int n] {
			get { return (MemberReference) List [n]; }
		}
		
		public void RenameAll (string newName)
		{
			foreach (MemberReference mref in List)
				mref.Rename (newName);
		}
	}
}
