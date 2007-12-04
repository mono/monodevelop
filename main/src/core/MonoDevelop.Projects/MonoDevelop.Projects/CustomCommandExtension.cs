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
	public class CustomCommandExtension: ProjectServiceExtension
	{
		public override ICompilerResult Build (IProgressMonitor monitor, CombineEntry entry)
		{
			AbstractConfiguration conf = entry.ActiveConfiguration as AbstractConfiguration;
			if (conf != null) {
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.BeforeBuild);
				if (monitor.IsCancelRequested)
					return new DefaultCompilerResult (new CompilerResults (null), "");
			}
			
			ICompilerResult res = base.Build (monitor, entry);
			
			if (conf != null && !monitor.IsCancelRequested)
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.AfterBuild);
				                                    
			return res;
		}

		public override void Clean (IProgressMonitor monitor, CombineEntry entry)
		{
			AbstractConfiguration conf = entry.ActiveConfiguration as AbstractConfiguration;
			if (conf != null) {
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.BeforeClean);
				if (monitor.IsCancelRequested)
					return;
			}
			
			base.Clean (monitor, entry);
			
			if (conf != null && !monitor.IsCancelRequested)
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.AfterClean);
		}

		public override void Execute (IProgressMonitor monitor, CombineEntry entry, ExecutionContext context)
		{
			AbstractConfiguration conf = entry.ActiveConfiguration as AbstractConfiguration;
			if (conf != null) {
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.BeforeExecute, context);
				if (monitor.IsCancelRequested)
					return;
			}
			
			base.Execute (monitor, entry, context);
			
			if (conf != null && !monitor.IsCancelRequested)
				conf.CustomCommands.ExecuteCommand (monitor, entry, CustomCommandType.AfterExecute, context);
		}
	}
}
