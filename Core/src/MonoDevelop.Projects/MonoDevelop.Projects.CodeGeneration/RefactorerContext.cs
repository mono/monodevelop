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

namespace MonoDevelop.Projects.CodeGeneration
{
	public sealed class RefactorerContext
	{
		ITextFileProvider files;
		ArrayList textFiles = new ArrayList ();
		IParserContext ctx;
		
		internal RefactorerContext (IParserContext ctx, ITextFileProvider files)
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
