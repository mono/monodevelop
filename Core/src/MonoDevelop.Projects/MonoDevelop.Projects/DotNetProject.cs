// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Text;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Projects
{
	[DataInclude (typeof(DotNetProjectConfiguration))]
	public class DotNetProject : Project
	{
		[ItemProperty]
		string language;
		ClrVersion clrVersion = ClrVersion.Net_1_1;
		
		IDotNetLanguageBinding languageBinding;
		
		public override string ProjectType {
			get { return "DotNet"; }
		}
		
		public string LanguageName {
			get { return language; }
		}
		
		public override string [] SupportedLanguages {
			get {
				return new string [] { "", language };
			}
		}
		
		public IDotNetLanguageBinding LanguageBinding {
			get { return languageBinding; }
		}
		
		[ItemProperty ("clr-version")]
		public ClrVersion ClrVersion {
			get {
				return (clrVersion == ClrVersion.Default) ? ClrVersion.Net_1_1 : clrVersion;
			}
			set {
				if (clrVersion == value)
					return;
				clrVersion = value;
				
				// Propagate the clr version to configurations. We don't support
				// per-project clr versions right now, but we might support it
				// in the future.
				foreach (DotNetProjectConfiguration conf in Configurations)
					conf.ClrVersion = clrVersion;

				UpdateSystemReferences ();
			}
		}
		
		public DotNetProject ()
		{
		}
		
		public DotNetProject (string languageName)
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
			DataValue val = data ["language"] as DataValue;
			if (val != null && Services.Languages.GetBindingPerLanguageName (val.Value) == null)
				throw new UserException (GettextCatalog.GetString ("Unknown language: {0}", val.Value), GettextCatalog.GetString ("You may need to install an additional add-in to support projects for the language '{0}'", val.Value));

			base.Deserialize (handler, data);
			languageBinding = FindLanguage (language);
		}
		
		IDotNetLanguageBinding FindLanguage (string name)
		{
			IDotNetLanguageBinding binding = Services.Languages.GetBindingPerLanguageName (language) as IDotNetLanguageBinding;
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
		
		public override bool IsCompileable(string fileName)
		{
			return languageBinding.IsSourceCodeFile (fileName);
		}
		
		public virtual string GetDefaultNamespace (string fileName)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (char c in Name) {
				if (char.IsLetterOrDigit (c) || c == '_' || c == '.')
					sb.Append (c);
			}
			if (sb.Length > 0)
				return sb.ToString ();
			else
				return "Application";
		}
		
		// Make sure that the project references are valid for the target clr version.
		void UpdateSystemReferences ()
		{
			ArrayList toDelete = new ArrayList ();
			ArrayList toAdd = new ArrayList ();
			
			foreach (ProjectReference pref in ProjectReferences) {
				if (pref.ReferenceType == ReferenceType.Gac) {
					string newRef = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (pref.Reference, this.ClrVersion);
					if (newRef == null)
						toDelete.Add (pref);
					else if (newRef != pref.Reference) {
						toDelete.Add (pref);
						toAdd.Add (new ProjectReference (ReferenceType.Gac, newRef));
					}
				}
			}
			foreach (ProjectReference pref in toDelete) {
				ProjectReferences.Remove (pref);
			}
			foreach (ProjectReference pref in toAdd) {
				ProjectReferences.Add (pref);
			}
		}
	}
}
