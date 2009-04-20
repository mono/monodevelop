// PythonLanguage.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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

using System;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;

using PyBinding.Parser;

namespace PyBinding
{
	public class PythonLanguageBinding : ILanguageBinding
	{
		static readonly string s_Language   = "Python";
		static readonly string s_CommentTag = "#";
		static readonly string s_PythonExt  = ".py";

		PythonParser m_Parser;
		
		public string Language {
			get {
				return s_Language;
			}
		}
		
		public string SingleLineCommentTag { get { return s_CommentTag; } }
		public string BlockCommentStartTag { get { return null; } }
		public string BlockCommentEndTag { get { return null; } }
		
		public IParser Parser {
			get {
				if (m_Parser == null)
					m_Parser = new PythonParser ();
				return m_Parser;
			}
		}
		
		public IRefactorer Refactorer {
			get {
				return null;
			}
		}
		
		public string GetFileName (string baseName)
		{
			return String.Concat (baseName, s_PythonExt);
		}
		
		public bool IsSourceCodeFile (string fileName)
		{
			return Path.GetExtension (fileName).ToLower () == s_PythonExt;
		}
	}
}
