//
// CodeRefactorer.cs
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
using System.Collections;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.Projects.CodeGeneration
{
	public class CodeRefactorer
	{
		IParserDatabase pdb;
		Combine rootCombine;
		ITextFileProvider fileProvider;
		
		delegate void RefactorDelegate (RefactorerContext gctx, IRefactorer gen, string file);
		
		public CodeRefactorer (Combine rootCombine, IParserDatabase pdb)
		{
			this.rootCombine = rootCombine;
			this.pdb = pdb;
		}
		
		public ITextFileProvider TextFileProvider {
			get { return fileProvider; }
			set { fileProvider = value; } 
		}
		
		public IClass CreateClass (Project project, string language, string directory, string namspace, CodeTypeDeclaration type)
		{
			IParserContext ctx = pdb.GetProjectParserContext (project);
			RefactorerContext gctx = new RefactorerContext (ctx, fileProvider);
			IRefactorer gen = Services.Languages.GetRefactorerForLanguage (language);
			IClass c = gen.CreateClass (gctx, directory, namspace, type);
			gctx.Save ();
			return c;
		}
		
		public void RenameClass (IClass cls, string newName, RefactoryScope scope)
		{
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			Refactor (cls, scope, new RefactorDelegate (new RefactorFindClassReferences (cls, refs).Refactor));
			refs.RenameAll (newName);
			
			RefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer r = GetGeneratorForClass (cls);
			r.RenameClass (gctx, cls, newName);
			gctx.Save ();
		}
		
		public MemberReferenceCollection FindClassReferences (IClass cls, RefactoryScope scope)
		{
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			Refactor (cls, scope, new RefactorDelegate (new RefactorFindClassReferences (cls, refs).Refactor));
			return refs;
		}
		
		public IMember AddMember (IClass cls, CodeTypeMember member)
		{
			RefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer gen = GetGeneratorForClass (cls);
			IMember m = gen.AddMember (gctx, cls, member);
			gctx.Save ();
			return m;
		}
		
		public void RemoveMember (IClass cls, IMember member)
		{
			RefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer gen = GetGeneratorForClass (cls);
			gen.RemoveMember (gctx, cls, member);
			gctx.Save ();
		}
		
		public IMember RenameMember (IClass cls, IMember member, string newName, RefactoryScope scope)
		{
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			Refactor (cls, scope, new RefactorDelegate (new RefactorFindMemberReferences (cls, member, refs).Refactor));
			refs.RenameAll (newName);
			
			RefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer gen = GetGeneratorForClass (cls);
			IMember m = gen.RenameMember (gctx, cls, member, newName);
			gctx.Save ();
			return m;
		}
		
		public MemberReferenceCollection FindMemberReferences (IClass cls, IMember member, RefactoryScope scope)
		{
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			Refactor (cls, scope, new RefactorDelegate (new RefactorFindMemberReferences (cls, member, refs).Refactor));
			return refs;
		}
		
		public IMember ReplaceMember (IClass cls, IMember oldMember, CodeTypeMember member)
		{
			RefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer gen = GetGeneratorForClass (cls);
			IMember m = gen.ReplaceMember (gctx, cls, oldMember, member);
			gctx.Save ();
			return m;
		}
		
		void Refactor (IClass cls, RefactoryScope scope, RefactorDelegate refactorDelegate)
		{
			if (scope == RefactoryScope.Class) {
				string file = cls.Region.FileName;
				Project prj = GetProjectForFile (file);
				if (prj == null)
					return;
				
				RefactorerContext gctx = GetGeneratorContext (prj);
				IRefactorer gen = Services.Languages.GetRefactorerForFile (file);
				if (gen == null)
					return;
				refactorDelegate (gctx, gen, file);
				gctx.Save ();
			}
			else if (scope == RefactoryScope.Project)
			{
				string file = cls.Region.FileName;
				Project prj = GetProjectForFile (file);
				if (prj == null)
					return;
				RefactorProject (prj, refactorDelegate);
			}
			else
			{
				RefactorCombine (rootCombine, refactorDelegate);
			}
		}
		
		void RefactorCombine (CombineEntry ce, RefactorDelegate refactorDelegate)
		{
			if (ce is Combine) {
				foreach (CombineEntry e in ((Combine)ce).Entries)
					RefactorCombine (e, refactorDelegate);
			} else if (ce is Project) {
				RefactorProject ((Project) ce, refactorDelegate);
			}
		}
		
		void RefactorProject (Project p, RefactorDelegate refactorDelegate)
		{
			RefactorerContext gctx = GetGeneratorContext (p);
			foreach (ProjectFile file in p.ProjectFiles) {
				if (file.BuildAction != BuildAction.Compile) continue;
				IRefactorer gen = Services.Languages.GetRefactorerForFile (file.Name);
				if (gen == null) continue;
				refactorDelegate (gctx, gen, file.Name);
				gctx.Save ();
			}
		}
		
		RefactorerContext GetGeneratorContext (Project p)
		{
			IParserContext ctx = pdb.GetProjectParserContext (p);
			return new RefactorerContext (ctx, fileProvider);
		}
		
		RefactorerContext GetGeneratorContext (IClass cls)
		{
			Project p = GetProjectForFile (cls.Region.FileName);
			return p != null ? GetGeneratorContext (p) : null;
		}
		
		Project GetProjectForFile (string file)
		{
			foreach (Project p in rootCombine.GetAllProjects ())
				if (p.IsFileInProject (file))
					return p;
			return null;
		}
		
		IRefactorer GetGeneratorForClass (IClass cls)
		{
			return Services.Languages.GetRefactorerForFile (cls.Region.FileName);
		}
	}
	
	class RefactorFindClassReferences
	{
		MemberReferenceCollection references;
		IClass cls;
		
		public RefactorFindClassReferences (IClass cls, MemberReferenceCollection references)
		{
			this.cls = cls;
			this.references = references;
		}
		
		public void Refactor (RefactorerContext rctx, IRefactorer r, string fileName)
		{
			MemberReferenceCollection refs = r.FindClassReferences (rctx, fileName, cls);
			if (refs != null)
				references.AddRange (refs);
		}
	}
	
	class RefactorFindMemberReferences
	{
		IClass cls;
		MemberReferenceCollection references;
		IMember member;
		
		public RefactorFindMemberReferences (IClass cls, IMember member, MemberReferenceCollection references)
		{
			this.cls = cls;
			this.references = references;
			this.member = member;
		}
		
		public void Refactor (RefactorerContext rctx, IRefactorer r, string fileName)
		{
			MemberReferenceCollection refs = r.FindMemberReferences (rctx, fileName, cls, member);
			if (refs != null)
				references.AddRange (refs);
		}
	}
	
	public enum RefactoryScope
	{
		Class,
		Project,
		Solution
	}
}
