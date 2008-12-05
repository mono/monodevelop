// CustomCommandExtension.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using MonoDevelop.Core;
using System.CodeDom.Compiler;

namespace MonoDevelop.Projects
{
	internal class CustomCommandExtension: ProjectServiceExtension
	{
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem entry, string configuration)
		{
			SolutionItemConfiguration conf = entry.GetConfiguration (configuration) as SolutionItemConfiguration;
			if (conf != null) {
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.BeforeBuild, configuration);
				if (monitor.IsCancelRequested)
					return new BuildResult (new CompilerResults (null), "");
			}
			
			BuildResult res = base.Build (monitor, entry, configuration);
			
			if (conf != null && !monitor.IsCancelRequested && !res.Failed)
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.AfterBuild, configuration);
				                                    
			return res;
		}

		protected override void Clean (IProgressMonitor monitor, SolutionEntityItem entry, string configuration)
		{
			SolutionItemConfiguration conf = entry.GetConfiguration (configuration) as SolutionItemConfiguration;
			if (conf != null) {
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.BeforeClean, configuration);
				if (monitor.IsCancelRequested)
					return;
			}
			
			base.Clean (monitor, entry, configuration);
			
			if (conf != null && !monitor.IsCancelRequested)
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.AfterClean, configuration);
		}

		protected override void Execute (IProgressMonitor monitor, SolutionEntityItem entry, ExecutionContext context, string configuration)
		{
			SolutionItemConfiguration conf = entry.GetConfiguration (configuration) as SolutionItemConfiguration;
			if (conf != null) {
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.BeforeExecute, context, configuration);
				if (monitor.IsCancelRequested)
					return;
			}
			
			base.Execute (monitor, entry, context, configuration);
			
			if (conf != null && !monitor.IsCancelRequested)
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.AfterExecute, context, configuration);
		}
	}
}
