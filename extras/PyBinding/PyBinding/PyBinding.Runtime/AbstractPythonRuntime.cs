// AbstractPythonRuntime.cs
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
using System.Diagnostics;
using System.IO;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Projects;

using PyBinding;
using PyBinding.Compiler;

namespace PyBinding.Runtime
{
	public abstract class AbstractPythonRuntime : IPythonRuntime
	{
		// XXX: This pretty much ignores the fact that volume separators on
		//      windows are also :. Someone should fix this at some point.
		static readonly char[] m_PathSeparators = new char[] {';', ':'};
		
		public abstract string Name {
			get;
		}
		
		public abstract string Path {
			get;
			set;
		}
		
		public abstract IPythonCompiler Compiler {
			get;
		}
		
		public abstract object   Clone ();
		public abstract string[] GetArguments (PythonConfiguration config);
		
		protected virtual string Resolve (string commandName)
		{
			List<string> paths;
			
			paths = new List<string> ();
			paths.Add (".");
			
			foreach (string dirName in Environment.GetEnvironmentVariable (
			    "PATH").Split (m_PathSeparators))
			{
				paths.Add (dirName);
			}
			
			foreach (string dirName in paths) {
				string absPath = System.IO.Path.Combine (dirName, commandName);
				
				if (System.IO.File.Exists (absPath)) {
					return absPath;
				}
			}
			
			throw new FileNotFoundException ("Could not locate executable");
		}
	}
}
