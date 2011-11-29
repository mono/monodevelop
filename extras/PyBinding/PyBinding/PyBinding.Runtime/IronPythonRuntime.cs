// IronPythonRuntime.cs
// 
// Copyright (c) 2011 Carlos Alberto Cortez <calberto.cortez@gmail.com>
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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;

using PyBinding.Compiler;

namespace PyBinding.Runtime
{
	public class IronPythonRuntime : AbstractPythonRuntime
	{
		static readonly string RuntimeName = "IronPython";
		
		[ItemProperty ("path")]
		string path = String.Empty;
		
		public override IPythonCompiler Compiler {
			get {
				return null;
			}
		}
		
		public override string Name {
			get {
				return RuntimeName;
			}
		}
		
		public override string Path {
			get {				
				if (String.IsNullOrEmpty (path))
					path = Resolve ("ipy.exe");
				
				return path;
			}
			set {
				path = value;
			}
		}
		
		public override object Clone ()
		{
			return new IronPythonRuntime () {
				Path = path
			};
		}
		
		IExecutionHandler handler;
		
		public override IExecutionHandler GetExecutionHandler ()
		{
			if (handler == null)
				handler = new IronPythonExecutionHandler ();
			
			return handler;
		}
		
		
		public override string[] GetArguments (PythonConfiguration config)
		{
			var args = new List<string> ();
			
			if (!String.IsNullOrEmpty (config.Module))
				args.Add (System.IO.Path.ChangeExtension (config.Module, "py"));
			
			if (!String.IsNullOrEmpty (config.CommandLineParameters))
				args.Add (config.CommandLineParameters);
			
			return args.ToArray ();
		}
	}
}

