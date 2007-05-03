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
using MonoDevelop.Core;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.Projects.CodeGeneration
{
	public class CodeRefactorer
	{
		IParserDatabase pdb;
		Combine rootCombine;
		ITextFileProvider fileProvider;
		
		delegate void RefactorDelegate (IProgressMonitor monitor, RefactorerContext gctx, IRefactorer gen, string file);
		
		public CodeRefactorer (Combine rootCombine, IParserDatabase pdb)
		{
			this.rootCombine = rootCombine;
			this.pdb = pdb;
		}
		
		public CodeRefactorer (IParserDatabase pdb)
		{
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
		
		public void RenameClass (IProgressMonitor monitor, IClass cls, string newName, RefactoryScope scope)
		{
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			Refactor (monitor, cls, scope, new RefactorDelegate (new RefactorFindClassReferences (cls, refs).Refactor));
			refs.RenameAll (newName);
			
			RefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer r = GetGeneratorForClass (cls);
			
			foreach (IMethod method in cls.Methods) {
				if (method.IsConstructor)
					r.RenameMember (gctx, cls, (IMember) method, newName);
			}
			
			r.RenameClass (gctx, cls, newName);
			
			gctx.Save ();
		}
		
		public MemberReferenceCollection FindClassReferences (IProgressMonitor monitor, IClass cls, RefactoryScope scope)
		{
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			Refactor (monitor, cls, scope, new RefactorDelegate (new RefactorFindClassReferences (cls, refs).Refactor));
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
		
		public IMember RenameMember (IProgressMonitor monitor, IClass cls, IMember member, string newName, RefactoryScope scope)
		{
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			Refactor (monitor, cls, scope, new RefactorDelegate (new RefactorFindMemberReferences (cls, member, refs).Refactor));
			refs.RenameAll (newName);
			
			RefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer gen = GetGeneratorForClass (cls);
			IMember m = gen.RenameMember (gctx, cls, member, newName);
			gctx.Save ();
			return m;
		}
		
		public MemberReferenceCollection FindMemberReferences (IProgressMonitor monitor, IClass cls, IMember member, RefactoryScope scope)
		{
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			Refactor (monitor, cls, scope, new RefactorDelegate (new RefactorFindMemberReferences (cls, member, refs).Refactor));
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
		
		public LocalVariable RenameVariable (IProgressMonitor monitor, LocalVariable var, string newName)
		{
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			Refactor (monitor, var, new RefactorDelegate (new RefactorFindVariableReferences (var, refs).Refactor));
			refs.RenameAll (newName);
			
			RefactorerContext gctx = GetGeneratorContext (var);
			IRefactorer r = GetGeneratorForVariable (var);
			LocalVariable v = r.RenameVariable (gctx, var, newName);
			gctx.Save ();
			
			return v;
		}
		
		public IParameter RenameParameter (IProgressMonitor monitor, IMethod method, IParameter param, string newName)
		{
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			Refactor (monitor, method, new RefactorDelegate (new RefactorFindParameterReferences (method, param, refs).Refactor));
			refs.RenameAll (newName);
			
			RefactorerContext gctx = GetGeneratorContext (method.DeclaringType);
			IRefactorer r = GetGeneratorForClass (method.DeclaringType);
			IParameter p = r.RenameParameter (gctx, method, param, newName);
			gctx.Save ();
			
			return p;
		}
		
		public IClass[] FindDerivedClasses (IClass baseClass)
		{
			ArrayList list = new ArrayList ();
			
			if (rootCombine != null) {
				foreach (Project p in rootCombine.GetAllProjects ()) {
					IParserContext ctx = pdb.GetProjectParserContext (p);
					foreach (IClass cls in ctx.GetProjectContents ()) {
						if (IsSubclass (ctx, baseClass, cls))
							list.Add (cls);
					}
				}
			} else {
				IParserContext ctx = GetParserContext (baseClass);
				foreach (IClass cls in ctx.GetProjectContents ()) {
					if (IsSubclass (ctx, baseClass, cls))
						list.Add (cls);
				}
			}
			return (IClass[]) list.ToArray (typeof(IClass));
		}
		
		bool IsSubclass (IParserContext ctx, IClass baseClass, IClass subclass)
		{
			foreach (IReturnType clsName in subclass.BaseTypes)
				if (clsName.FullyQualifiedName == baseClass.FullyQualifiedName)
					return true;

			foreach (IReturnType clsName in subclass.BaseTypes) {
				IClass cls = ctx.GetClass (clsName.FullyQualifiedName, true, true);
				if (cls != null && IsSubclass (ctx, baseClass, cls))
					return true;
			}
			return false;
		}
		
		void Refactor (IProgressMonitor monitor, IClass cls, RefactoryScope scope, RefactorDelegate refactorDelegate)
		{
			if (scope == RefactoryScope.File || rootCombine == null) {
				string file = cls.Region.FileName;
				RefactorerContext gctx = GetGeneratorContext (cls);
				IRefactorer gen = Services.Languages.GetRefactorerForFile (file);
				if (gen == null)
					return;
				refactorDelegate (monitor, gctx, gen, file);
				gctx.Save ();
			}
			else if (scope == RefactoryScope.Project)
			{
				string file = cls.Region.FileName;
				Project prj = GetProjectForFile (file);
				if (prj == null)
					return;
				RefactorProject (monitor, prj, refactorDelegate);
			}
			else
			{
				RefactorCombine (monitor, rootCombine, refactorDelegate);
			}
		}
		
		void Refactor (IProgressMonitor monitor, LocalVariable var, RefactorDelegate refactorDelegate)
		{
			RefactorerContext gctx = GetGeneratorContext (var);
			string file = var.Region.FileName;
			
			IRefactorer gen = Services.Languages.GetRefactorerForFile (file);
			if (gen == null)
				return;
			
			refactorDelegate (monitor, gctx, gen, file);
			gctx.Save ();
		}
		
		void Refactor (IProgressMonitor monitor, IMethod method, RefactorDelegate refactorDelegate)
		{
			RefactorerContext gctx = GetGeneratorContext (method.DeclaringType);
			string file = method.DeclaringType.Region.FileName;
			
			IRefactorer gen = Services.Languages.GetRefactorerForFile (file);
			if (gen == null)
				return;
			
			refactorDelegate (monitor, gctx, gen, file);
			gctx.Save ();
		}
		
		void RefactorCombine (IProgressMonitor monitor, CombineEntry ce, RefactorDelegate refactorDelegate)
		{
			if (ce is Combine) {
				foreach (CombineEntry e in ((Combine)ce).Entries)
					RefactorCombine (monitor, e, refactorDelegate);
			} else if (ce is Project) {
				RefactorProject (monitor, (Project) ce, refactorDelegate);
			}
		}
		
		void RefactorProject (IProgressMonitor monitor, Project p, RefactorDelegate refactorDelegate)
		{
			RefactorerContext gctx = GetGeneratorContext (p);
			monitor.Log.WriteLine (GettextCatalog.GetString ("Refactoring project {0}", p.Name));
			foreach (ProjectFile file in p.ProjectFiles) {
				if (file.BuildAction != BuildAction.Compile) continue;
				IRefactorer gen = Services.Languages.GetRefactorerForFile (file.Name);
				if (gen == null) continue;
				refactorDelegate (monitor, gctx, gen, file.Name);
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
			return new RefactorerContext (GetParserContext (cls), fileProvider);
		}
		
		RefactorerContext GetGeneratorContext (LocalVariable var)
		{
			return new RefactorerContext (GetParserContext (var), fileProvider);
		}
		
		IParserContext GetParserContext (IClass cls)
		{
			Project p = GetProjectForFile (cls.Region.FileName);
			if (p != null)
				return pdb.GetProjectParserContext (p);
			else
				return pdb.GetFileParserContext (cls.Region.FileName);
		}
		
		IParserContext GetParserContext (LocalVariable var)
		{
			Project p = GetProjectForFile (var.Region.FileName);
			if (p != null)
				return pdb.GetProjectParserContext (p);
			else
				return pdb.GetFileParserContext (var.Region.FileName);
		}
		
		Project GetProjectForFile (string file)
		{
			if (rootCombine == null)
				return null;

			foreach (Project p in rootCombine.GetAllProjects ())
				if (p.IsFileInProject (file))
					return p;
			return null;
		}
		
		IRefactorer GetGeneratorForClass (IClass cls)
		{
			return Services.Languages.GetRefactorerForFile (cls.Region.FileName);
		}
		
		IRefactorer GetGeneratorForVariable (LocalVariable var)
		{
			return Services.Languages.GetRefactorerForFile (var.Region.FileName);
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
		
		public void Refactor (IProgressMonitor monitor, RefactorerContext rctx, IRefactorer r, string fileName)
		{
			try {
				MemberReferenceCollection refs = r.FindClassReferences (rctx, fileName, cls);
				if (refs != null)
					references.AddRange (refs);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not look for references in file '{0}': {1}", fileName, ex.Message), ex);
			}
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
		
		public void Refactor (IProgressMonitor monitor, RefactorerContext rctx, IRefactorer r, string fileName)
		{
			try {
				MemberReferenceCollection refs = r.FindMemberReferences (rctx, fileName, cls, member);
				if (refs != null)
					references.AddRange (refs);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not look for references in file '{0}': {1}", fileName, ex.Message), ex);
			}
		}
	}
	
	class RefactorFindVariableReferences
	{
		MemberReferenceCollection references;
		LocalVariable var;
		
		public RefactorFindVariableReferences (LocalVariable var, MemberReferenceCollection references)
		{
			this.references = references;
			this.var = var;
		}
		
		public void Refactor (IProgressMonitor monitor, RefactorerContext rctx, IRefactorer r, string fileName)
		{
			try {
				MemberReferenceCollection refs = r.FindVariableReferences (rctx, fileName, var);
				if (refs != null)
					references.AddRange (refs);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not look for references in file '{0}': {1}", fileName, ex.Message), ex);
			}
		}
	}
	
	class RefactorFindParameterReferences
	{
		MemberReferenceCollection references;
		IParameter param;
		IMethod method;
		
		public RefactorFindParameterReferences (IMethod method, IParameter param, MemberReferenceCollection references)
		{
			this.references = references;
			this.method = method;
			this.param = param;
		}
		
		public void Refactor (IProgressMonitor monitor, RefactorerContext rctx, IRefactorer r, string fileName)
		{
			try {
				MemberReferenceCollection refs = r.FindParameterReferences (rctx, fileName, method, param);
				if (refs != null)
					references.AddRange (refs);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not look for references in file '{0}': {1}", fileName, ex.Message), ex);
			}
		}
	}
	
	public enum RefactoryScope
	{
		File,
		Project,
		Solution
	}
}
