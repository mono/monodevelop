// PythonModule.cs
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
using System.Collections.Generic;

namespace PyBinding.Parser.Dom
{
	[Serializable]
	public class PythonModule : PythonNode
	{
		List<PythonImport>    m_Imports    = new List<PythonImport>    ();
		List<PythonClass>     m_Classes    = new List<PythonClass>     ();
		List<PythonFunction>  m_Functions  = new List<PythonFunction>  ();
		List<PythonAttribute> m_Attributes = new List<PythonAttribute> ();
		List<PythonComment>   m_Comments   = new List<PythonComment>   ();

		public string FullName {
			get;
			set;
		}

		public IList<PythonImport> Imports {
			get {
				return m_Imports;
			}
		}

		public IList<PythonClass> Classes {
			get {
				return m_Classes;
			}
		}

		public IList<PythonFunction> Functions {
			get {
				return m_Functions;
			}
		}

		public IList<PythonAttribute> Attributes {
			get {
				return m_Attributes;
			}
		}

		public IList<PythonComment> Comments {
			get {
				return m_Comments;
			}
		}
	}
}