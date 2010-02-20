// 
// MeeGoProject.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using System;
using System.Xml;

namespace MonoDevelop.MeeGo
{
	
	public class MeeGoProject : DotNetAssemblyProject
	{
		#region Constructors
		
		public MeeGoProject ()
		{
			Init ();
		}
		
		public MeeGoProject (string language) : base (language)
		{
			Init ();
		}
		
		public MeeGoProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			Init ();
		}
		
		void Init ()
		{
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new MeeGoProjectConfiguration (name);
			
			var dir = new FilePath ("bin");
			if (!String.IsNullOrEmpty (conf.Platform))
				dir.Combine (conf.Platform);
			dir.Combine (conf.Name);
			conf.OutputDirectory = BaseDirectory.IsNullOrEmpty? dir : BaseDirectory.Combine (dir);
			
			conf.OutputAssembly = Name;
			
			if (LanguageBinding != null)
				conf.CompilationParameters = LanguageBinding.CreateCompilationParameters (null);
			return conf;
		}
		
		#endregion
		
		#region Execution
		
		protected override ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel,
		                                                            DotNetProjectConfiguration configuration)
		{
			var conf = (MeeGoProjectConfiguration) configuration;
			return new MeeGoExecutionCommand (conf) {
				UserAssemblyPaths = GetUserAssemblyPaths (configSel),
			};
		}
		
		/*
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configSel)
		{
			var conf = (MeeGoProjectConfiguration) GetConfiguration (configSel);
			var cmd = (MeeGoExecutionCommand)CreateExecutionCommand (configSel, conf);
			
			using (var opMon = new AggregatedOperationMonitor (monitor)) {
				if (MeeGoUtility.NeedsUploading (conf)) {
					using (var op = MeeGoUtility.Upload (cmd.Device, cmd.AppExe)) {
						opMon.AddOperation (op);
						op.WaitForOutput ();
						if (op.ExitCode != 0)
							return;
					}
					MeeGoUtility.TouchUploadMarker (conf);
				}
				
				IConsole console = null;
				try {
						
					console = conf.ExternalConsole
						? context.ExternalConsoleFactory.CreateConsole (!conf.PauseConsoleOutput)
						: context.ConsoleFactory.CreateConsole (!conf.PauseConsoleOutput);
					
					var ex = context.ExecutionHandler.Execute (cmd, console);
					opMon.AddOperation (ex);
					ex.WaitForCompleted ();
				} finally {
					if (console != null)
						console.Dispose ();
				}
			}
		}*/
		
		#endregion
	}
}
