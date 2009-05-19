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
using System.Collections.Generic;
using System.CodeDom;
using MonoDevelop.Core;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.Projects.CodeGeneration
{
	public interface IRefactorer : INameValidator
	{
		RefactorOperations SupportedOperations { get; }
		
		void AddAttribute (RefactorerContext ctx, IType cls, CodeAttributeDeclaration attr);

		IType CreateClass (RefactorerContext ctx, string directory, string namspace, CodeTypeDeclaration type);
		IType RenameClass (RefactorerContext ctx, IType cls, string newName);
		IEnumerable<MemberReference> FindClassReferences (RefactorerContext ctx, string fileName, IType cls);
		
		IMember AddMember (RefactorerContext ctx, IType cls, CodeTypeMember memberInfo);
		IMember ImplementMember (RefactorerContext ctx, IType cls, IMember member, IReturnType privateImplementationType);
		
		// these add contiguous blocks of memebers
		// NOTE: Handling "foldingRegionName" is optional. Also, it can be null, in which case no region should be created.
		void AddMembers (RefactorerContext ctx, IType cls, IEnumerable<CodeTypeMember> memberInfo, string foldingRegionName);
		void ImplementMembers (RefactorerContext ctx, IType cls, IEnumerable<KeyValuePair<IMember,IReturnType>> members,
		                       string foldingRegionName);
		
		// used by base AddMembers and ImplementMembers implementions
		// expected to return file offset of space within the new region
		int AddFoldingRegion (RefactorerContext ctx, IType cls, string regionName);
		
		void RemoveMember (RefactorerContext ctx, IType cls, IMember member);
		IMember RenameMember (RefactorerContext ctx, IType cls, IMember member, string newName);
		IMember ReplaceMember (RefactorerContext ctx, IType cls, IMember oldMember, CodeTypeMember memberInfo);
		IEnumerable<MemberReference> FindMemberReferences (RefactorerContext ctx, string fileName, IType cls, IMember member);
		
		bool RenameVariable (RefactorerContext ctx, LocalVariable var, string newName);
		IEnumerable<MemberReference> FindVariableReferences (RefactorerContext ctx, string fileName, LocalVariable var);
		
		bool RenameParameter (RefactorerContext ctx, IParameter param, string newName);
		IEnumerable<MemberReference> FindParameterReferences (RefactorerContext ctx, string fileName, IParameter param);
		
		IMember EncapsulateField (RefactorerContext ctx, IType cls, IField field, string propName, MemberAttributes attr, bool generateSetter);
		
		string ConvertToLanguageTypeName (string netTypeName);
		void AddNamespaceImport (RefactorerContext ctx, string fileName, string nsName);
		DomLocation CompleteStatement (RefactorerContext ctx, string fileName, DomLocation caretLocation);
	}
	
	public class MemberReference
	{
		int position;
		int line;
		int column;
		FilePath fileName;
		string name;
		string textLine;
		RefactorerContext rctx;

		public MemberReference (RefactorerContext rctx, FilePath fileName, int position, int line, int column, string name, string textLine)
		{
			this.position = position;
			this.line = line;
			this.column = column;
			this.fileName = fileName;
			this.name = name;
			this.rctx = rctx;
			this.textLine = textLine;
			if (textLine == null || textLine.Length == 0)
				textLine = name;
		}
		
		public void SetContext (RefactorerContext rctx)
		{
			this.rctx = rctx;
		}
		
		public int Position {
			get { return position; }
		}
		
		public int Line {
			get { return line; }
		}
		
		public int Column {
			get { return column; }
		}
		
		public FilePath FileName {
			get { return fileName; }
		}
		
		public string TextLine {
			get { return textLine; }
		}
		public string Name {
			get { return name; }
		}
		public virtual void Rename (string newName)
		{
			if (rctx == null)
				throw new InvalidOperationException ("Refactory context not available.");

			IEditableTextFile file = rctx.GetFile (fileName);
			if (file != null) {
				Console.WriteLine ("Replacing text \"{0}\" with \"{1}\" @ {2},{3}", name, newName, line, column);
				file.DeleteText (position, name.Length);
				file.InsertText (position, newName);
				rctx.Save ();
			}
		}
		
		public override int GetHashCode ()
		{
			return (fileName + ":" + name + "@" + position).GetHashCode ();
		}
		
		public override bool Equals (object o)
		{
			MemberReference mref = (MemberReference) o;
			
			return mref.FileName == FileName && mref.name == name && mref.Position == Position;
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
			ArrayList list = new ArrayList ();
			list.AddRange (this);
			list.Sort (new MemberComparer ());
			
			foreach (MemberReference mref in list) {
				mref.Rename (newName);
			}
		}
		
		public class MemberComparer: IComparer
		{
			public int Compare (object o1, object o2)
			{
				MemberReference r1 = (MemberReference) o1;
				MemberReference r2 = (MemberReference) o2;
				int c = r1.FileName.ToString ().CompareTo (r2.FileName.ToString ());
				if (c != 0) return c;
				return r2.Position - r1.Position; 
			}
		}
	}
}
