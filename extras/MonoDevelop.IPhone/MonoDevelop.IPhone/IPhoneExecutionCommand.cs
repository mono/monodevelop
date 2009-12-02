// 
// IPhoneExecutionCommand.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Assemblies;
using System.Collections.Generic;

namespace MonoDevelop.IPhone
{
	public class IPhoneExecutionCommand: ExecutionCommand
	{
		public IPhoneExecutionCommand (TargetRuntime runtime, TargetFramework framework, FilePath appPath, 
		                               FilePath logDirectory, bool simulator, bool debugMode)
		{
			this.AppPath = appPath;
			this.LogDirectory = logDirectory;
			this.Framework = framework;
			this.Runtime = runtime;
			this.DebugMode = debugMode;
			this.Simulator = simulator;
		}
		
		public FilePath AppPath { get; private set; }
		public FilePath LogDirectory { get; private set; }
		public bool DebugMode { get; private set; }
		public bool Simulator { get; private set; }
		public TargetRuntime Runtime { get; private set; }
		public TargetFramework Framework { get; private set; }
		public IList<string> UserAssemblyPaths { get; set; }
		
		public override string CommandString {
			get { return "[iphone]"; }
		}
	}
}
