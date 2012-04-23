// 
// AspNetExecutionCommand.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.AspNet
{
	public class AspNetExecutionCommand: ExecutionCommand
	{
		IDictionary<string, string> environmentVariables;
    
		public AspNetExecutionCommand()
		{
		}
		
		public ClrVersion ClrVersion { get; set; }
		
		public XspParameters XspParameters { get; set; }
		
		public bool DebugMode { get; set; }
		
		public string BaseDirectory { get; set; }
		
		public TargetRuntime TargetRuntime { get; set; }
		
		public TargetFramework TargetFramework { get; set; }
		
		public IList<string> UserAssemblyPaths { get; set; }
		
		public override string CommandString {
			get {
				return "[asp-net]";
			}
		}

		public IDictionary<string, string> EnvironmentVariables {
			get {
				if (environmentVariables == null)
					environmentVariables = new Dictionary<string, string> ();
				return environmentVariables;
			}
			set {
				environmentVariables = value;
			}
		}
	}
}
