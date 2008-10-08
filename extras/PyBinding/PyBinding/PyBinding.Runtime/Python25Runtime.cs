// Python25Runtime.cs
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
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.CodeGeneration;

using PyBinding.Compiler;

namespace PyBinding.Runtime
{
	public class Python25Runtime : AbstractPythonRuntime
	{
		static readonly string m_Name        = "Python25";
		static readonly string m_DefaultPath = "python2.5";
		
		[ItemProperty("path")]
		string m_Path = String.Empty;
		
		[ItemProperty("compiler", ValueType = typeof (IPythonCompiler))]
		IPythonCompiler m_Compiler = null;
		
		public override IPythonCompiler Compiler {
			get {
				if (this.m_Compiler == null) {
					this.m_Compiler = new Python25Compiler ();
				}
				
				// Give compiler a reference to this instance
				(this.m_Compiler as Python25Compiler).Runtime = this;
				
				return this.m_Compiler;
			}
		}

		public override string Name {
			get {
				return m_Name;
			}
		}
		
		public override string Path {
			get {
				if (String.IsNullOrEmpty (this.m_Path)) {
					this.m_Path = this.Resolve (m_DefaultPath);
				}
				
				return this.m_Path;
			}
			set {
				this.m_Path = value;
			}
		}
		
		public override object Clone ()
		{
			Python25Runtime clone = new Python25Runtime ();
			clone.Path = m_Path;
			return clone;
		}
		
		public override string[] GetArguments (PythonConfiguration config)
		{
			List<string> args = new List<string> ();
			
			if (config.Optimize)
				args.Add ("-O");
			
			if (config.DebugMode)
				args.Add ("-d");
			
			// Make sure python uses unbuffered files for stdin and stdout
			// so that we can get updates to the console immediately.
			args.Add ("-u");
			
			// The -m argument prevents any more argument passing to
			// python. Therefore, it must be at the end of the list.
			if (!String.IsNullOrEmpty (config.Module)) {
				args.Add ("-m");
				args.Add (config.Module);
			}
			
			return args.ToArray ();
		}
	}
}
