// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Threading;
using MonoDevelop.Internal.Serialization;
using MonoDevelop.Internal.Templates;
using MonoDevelop.Services;

namespace MonoDevelop.Internal.Project
{
	[DataInclude (typeof(DotNetProjectConfiguration))]
	public class DotNetProject : Project
	{
		[ItemProperty]
		string language;
		
		ILanguageBinding languageBinding;
		
		public override string ProjectType {
			get { return "DotNet"; }
		}
		
		public string LanguageName {
			get { return language; }
		}
		
		internal DotNetProject ()
		{
		}
		
		internal DotNetProject (string languageName)
		{
			language = languageName;
			languageBinding = FindLanguage (language);
		}
		
		public DotNetProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
		{
			string binPath;
			if (info != null) {
				Name = info.ProjectName;
				binPath = info.BinPath;
			} else {
				binPath = ".";
			}
			
			language = languageName;
			languageBinding = FindLanguage (language);
			
			DotNetProjectConfiguration configuration = (DotNetProjectConfiguration) CreateConfiguration ("Debug");
			configuration.CompilationParameters = languageBinding.CreateCompilationParameters (projectOptions);
			Configurations.Add (configuration);
			
			configuration = (DotNetProjectConfiguration) CreateConfiguration ("Release");
			configuration.DebugMode = false;
			configuration.CompilationParameters = languageBinding.CreateCompilationParameters (projectOptions);
			Configurations.Add (configuration);
			
			foreach (DotNetProjectConfiguration parameter in Configurations) {
				parameter.OutputDirectory = Path.Combine (binPath, parameter.Name);
				parameter.OutputAssembly  = Name;
				
				if (projectOptions != null) {
					if (projectOptions.Attributes["Target"] != null) {
						parameter.CompileTarget = (CompileTarget)Enum.Parse(typeof(CompileTarget), projectOptions.Attributes["Target"].InnerText);
					}
					if (projectOptions.Attributes["PauseConsoleOutput"] != null) {
						parameter.PauseConsoleOutput = Boolean.Parse(projectOptions.Attributes["PauseConsoleOutput"].InnerText);
					}
				}
			}
		}
		
		public override void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			base.Deserialize (handler, data);
			languageBinding = FindLanguage (language);
		}
		
		ILanguageBinding FindLanguage (string name)
		{
			ILanguageBinding binding = Runtime.Languages.GetBindingPerLanguageName (language);
			if (binding == null)
				throw new InvalidOperationException ("Language not supported: " + language);
			return binding;
		}

		public override IConfiguration CreateConfiguration (string name)
		{
			DotNetProjectConfiguration conf = new DotNetProjectConfiguration ();
			conf.Name = name;
			conf.CompilationParameters = languageBinding.CreateCompilationParameters (null);
			return conf;
		}
		
		protected override ICompilerResult DoBuild (IProgressMonitor monitor)
		{
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) ActiveConfiguration;
			conf.SourceDirectory = BaseDirectory;
			
			foreach (ProjectFile finfo in ProjectFiles) {
				// Treat app.config in the project root directory as the application config
				if (Path.GetFileName (finfo.Name).ToUpper () == "app.config".ToUpper() &&
					Path.GetDirectoryName (finfo.Name) == BaseDirectory)
				{
					File.Copy (finfo.Name, conf.CompiledOutputName + ".config",true);
				}
			}

			ICompilerResult res = languageBinding.Compile (ProjectFiles, ProjectReferences, conf, monitor);
			CopyReferencesToOutputPath (false);
			return res;
		}
		
		public override string GetOutputFileName ()
		{
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) ActiveConfiguration;
			return conf.CompiledOutputName;
		}
		
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context)
		{
			CopyReferencesToOutputPath (true);
			
			DotNetProjectConfiguration configuration = (DotNetProjectConfiguration) ActiveConfiguration;
			monitor.Log.WriteLine ("Running " + configuration.CompiledOutputName + " ...");
			
			string platform = "Mono";
			
			switch (configuration.NetRuntime) {
				case NetRuntime.Mono:
					platform = "Mono";
					break;
				case NetRuntime.MonoInterpreter:
					platform = "Mint";
					break;
			}

			IConsole console;
			if (configuration.ExternalConsole)
				console = context.ExternalConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
			else
				console = context.ConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
			
			AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor (monitor);
			
			try {
				IExecutionHandler handler = context.ExecutionHandlerFactory.CreateExecutionHandler (platform);
				if (handler == null) {
					monitor.ReportError ("Can not execute \"" + configuration.CompiledOutputName + "\". The selected execution mode is not supported in the " + platform + " platform.", null);
					return;
				}
			
				IProcessAsyncOperation op = handler.Execute (configuration.CompiledOutputName, configuration.CommandLineParameters, Path.GetDirectoryName (configuration.CompiledOutputName), console);
				
				operationMonitor.AddOperation (op);
				op.WaitForCompleted ();
				monitor.Log.WriteLine ("The application exited with code: {0}", op.ExitCode);
			} catch (Exception ex) {
				monitor.ReportError ("Can not execute " + "\"" + configuration.CompiledOutputName + "\"", ex);
			} finally {
				operationMonitor.Dispose ();
				console.Dispose ();
			}
		}
		
		public override void GenerateMakefiles (Combine parentCombine)
		{
			Runtime.LoggingService.Info ("Generating makefiles for " + Name);
			languageBinding.GenerateMakefile (this, parentCombine);
		}
		
		public override bool IsCompileable(string fileName)
		{
			return languageBinding.CanCompile(fileName);
		}
	}
}
