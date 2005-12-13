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
		
		delegate void RefactorDelegate (IRefactorerContext gctx, IRefactorer gen, string file);
		
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
			InternalRefactorerContext gctx = new InternalRefactorerContext (ctx, fileProvider);
			IRefactorer gen = Services.Languages.GetRefactorerForLanguage (language);
			IClass c = gen.CreateClass (gctx, directory, namspace, type);
			gctx.Save ();
			return c;
		}
		
		public void RenameClass (IClass cls, string newName, RefactoryScope scope)
		{
			Refactor (cls, scope,
				delegate (IRefactorerContext rctx, IRefactorer gen, string file) {
					gen.RenameClassReferences (rctx, file, cls, newName);
				}
			);
			
			InternalRefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer r = GetGeneratorForClass (cls);
			r.RenameClass (gctx, cls, newName);
			gctx.Save ();
		}
		
		public IMethod AddMethod (IClass cls, CodeMemberMethod method)
		{
			InternalRefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer gen = GetGeneratorForClass (cls);
			IMethod m = gen.AddMethod (gctx, cls, method);
			gctx.Save ();
			return m;
		}
		
		public void RemoveMethod (IClass cls, IMethod method)
		{
			InternalRefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer gen = GetGeneratorForClass (cls);
			gen.RemoveMethod (gctx, cls, method);
			gctx.Save ();
		}
		
		public void RenameMethod (IClass cls, IMethod method, string newName, RefactoryScope scope)
		{
			Refactor (cls, scope,
				delegate (IRefactorerContext gctx, IRefactorer gen, string file) {
					gen.RenameMethodReferences (gctx, file, cls, method, newName);
				}
			);
			
			InternalRefactorerContext rctx = GetGeneratorContext (cls);
			IRefactorer r = GetGeneratorForClass (cls);
			r.RenameMethod (rctx, cls, method, newName);
			rctx.Save ();
		}
		
		public IField AddField (IClass cls, CodeMemberField field)
		{
			InternalRefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer gen = GetGeneratorForClass (cls);
			IField f = gen.AddField (gctx, cls, field);
			gctx.Save ();
			return f;
		}
		
		public void RemoveField (IClass cls, IField field)
		{
			InternalRefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer gen = GetGeneratorForClass (cls);
			gen.RemoveField (gctx, cls, field);
			gctx.Save ();
		}
		
		public void RenameField (IClass cls, IField field, string newName, RefactoryScope scope)
		{
			RefactorDelegate del = delegate (IRefactorerContext rctx, IRefactorer r, string file) {
				r.RenameFieldReferences (rctx, file, cls, field, newName);
			};
			
			Refactor (cls, scope, del);
			
			InternalRefactorerContext gctx = GetGeneratorContext (cls);
			IRefactorer gen = GetGeneratorForClass (cls);
			gen.RenameField (gctx, cls, field, newName);
			gctx.Save ();
		}
		
		void Refactor (IClass cls, RefactoryScope scope, RefactorDelegate refactorDelegate)
		{
			if (scope == RefactoryScope.Class) {
				string file = cls.Region.FileName;
				Project prj = GetProjectForFile (file);
				if (prj == null)
					return;
				
				InternalRefactorerContext gctx = GetGeneratorContext (prj);
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
			InternalRefactorerContext gctx = GetGeneratorContext (p);
			foreach (ProjectFile file in p.ProjectFiles) {
				if (file.BuildAction != BuildAction.Compile) continue;
				IRefactorer gen = Services.Languages.GetRefactorerForFile (file.Name);
				if (gen == null) continue;
				refactorDelegate (gctx, gen, file.Name);
				gctx.Save ();
			}
		}
		
		InternalRefactorerContext GetGeneratorContext (Project p)
		{
			IParserContext ctx = pdb.GetProjectParserContext (p);
			return new InternalRefactorerContext (ctx, fileProvider);
		}
		
		InternalRefactorerContext GetGeneratorContext (IClass cls)
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

		class InternalRefactorerContext: IRefactorerContext
		{
			ITextFileProvider files;
			ArrayList textFiles = new ArrayList ();
			IParserContext ctx;
			
			public InternalRefactorerContext (IParserContext ctx, ITextFileProvider files)
			{
				this.files = files;
				this.ctx = ctx;
			}
			
			public IParserContext ParserContext {
				get { return ctx; }
			}
			
			public IEditableTextFile GetFile (string name)
			{
				if (files != null) {
					IEditableTextFile ef = files.GetEditableTextFile (name);
					if (ef != null) return ef;
				}
				foreach (IEditableTextFile f in textFiles)
					if (f.Name == name)
						return f;
						
				TextFile file = new TextFile (name);
				textFiles.Add (file);
				return file;
			}
			
			internal void Save ()
			{
				foreach (TextFile file in textFiles)
					file.Save ();
			}
		}
	}
	
	public enum RefactoryScope
	{
		Class,
		Project,
		Solution
	}
}
