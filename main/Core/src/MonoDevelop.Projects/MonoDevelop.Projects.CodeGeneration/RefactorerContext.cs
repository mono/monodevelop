//
// RefactorerContext.cs
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

using System.Collections;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Ambience;

namespace MonoDevelop.Projects.CodeGeneration
{
	public sealed class RefactorerContext
	{
		ITextFileProvider files;
		ArrayList textFiles = new ArrayList ();
		IParserContext ctx;
		static TypeNameResolver defaultResolver = new TypeNameResolver ();
		ITypeNameResolver typeResolver;
		
		internal RefactorerContext (IParserContext ctx, ITextFileProvider files, IClass cls)
		{
			this.files = files;
			this.ctx = ctx;
			if (cls != null)
				typeResolver = GetTypeNameResolver (cls);
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
		
		public ITypeNameResolver TypeNameResolver {
			get {
				return typeResolver ?? defaultResolver;
			}
		}
		
		ITypeNameResolver GetTypeNameResolver (IClass cls)
		{
			if (cls.Region == null || cls.Region.FileName == null)
				return null;
			string file = cls.Region.FileName;
			IParseInformation pi = ctx.GetParseInformation (file);
			if (pi == null)
				return null;
			ICompilationUnit unit = pi.MostRecentCompilationUnit as ICompilationUnit;
			if (unit != null)
				return new TypeNameResolver (unit, cls);
			else
				return null;
		}
		
		internal void Save ()
		{
			foreach (TextFile file in textFiles) {
				if (file.Modified)
					file.Save ();
			}
		}
	}
}
