
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
