// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects.Ambience;

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
		
		public override MonoDevelop.Projects.Ambience.Ambience Ambience {
			get { return Services.Ambience.AmbienceFromName (LanguageName); }
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
			
			if (languageBinding != null) {
				DotNetProjectConfiguration configuration = (DotNetProjectConfiguration) CreateConfiguration ("Debug");
				configuration.CompilationParameters = languageBinding.CreateCompilationParameters (projectOptions);
				Configurations.Add (configuration);
				
				configuration = (DotNetProjectConfiguration) CreateConfiguration ("Release");
				configuration.DebugMode = false;
				configuration.CompilationParameters = languageBinding.CreateCompilationParameters (projectOptions);
				Configurations.Add (configuration);
			}
			
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
		
		IDotNetLanguageBinding FindLanguage (string name)
		{
			IDotNetLanguageBinding binding = Services.Languages.GetBindingPerLanguageName (language) as IDotNetLanguageBinding;
			return binding;
		}

		public override IConfiguration CreateConfiguration (string name)
		{
			DotNetProjectConfiguration conf = new DotNetProjectConfiguration ();
			conf.Name = name;
			if (languageBinding != null)
				conf.CompilationParameters = languageBinding.CreateCompilationParameters (null);
			return conf;
		}
		
		protected override ICompilerResult DoBuild (IProgressMonitor monitor)
		{
			if (languageBinding == null) {
				DefaultCompilerResult refres = new DefaultCompilerResult ();
				string msg = GettextCatalog.GetString ("Unknown language '{0}'. You may need to install an additional add-in to support this language.", language);
				refres.AddError (msg);
				monitor.ReportError (msg, null);
				return refres;
			}
			
			foreach (ProjectReference pr in ProjectReferences) {
				DefaultCompilerResult refres = null;
				if (pr.ReferenceType == ReferenceType.Project) {
					// Ignore non-dotnet projects
					Project p = RootCombine != null ? RootCombine.FindProject (pr.Reference) : null;
					if (p != null && !(p is DotNetProject))
						continue;

					if (p == null || pr.GetReferencedFileNames ().Length == 0) {
						if (refres == null)
							refres = new DefaultCompilerResult ();
						string msg = GettextCatalog.GetString ("Referenced project '{0}' not found in the solution.", pr.Reference);
						monitor.ReportError (msg, null);
						refres.AddError (msg);
					}
				}
				if (refres != null)
					return refres;
			}
			
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) ActiveConfiguration;
			conf.SourceDirectory = BaseDirectory;
			
			List<string> supportAssemblies = new List<string> ();
			CopySupportAssemblies (supportAssemblies);
			
			try {
				return languageBinding.Compile (ProjectFiles, ProjectReferences, conf, monitor);
			}
			finally {
				// Delete support assemblies
				foreach (string s in supportAssemblies) {
					try {
						File.Delete (s);
					} catch {
						// Ignore
					}
				}
			}
		}
		
		
		void CopySupportAssemblies (List<string> files)
		{
			foreach (ProjectReference projectReference in ProjectReferences) {
				if (projectReference.ReferenceType == ReferenceType.Project) {
					// It is a project reference. If this project depends
					// on other (non-gac) assemblies there may be a compilation problem because
					// the compiler won't be able to indirectly find them.
					// The solution is to copy them in the project directory, and delete
					// them after compilation.
					Project p = RootCombine.FindProject (projectReference.Reference);
					if (p == null)
						continue;
					
					string tdir = Path.GetDirectoryName (p.GetOutputFileName ());
					CopySupportAssemblies (p, tdir, files);
				}
			}
		}
		
		void CopySupportAssemblies (Project prj, string targetDir, List<string> files)
		{
			foreach (ProjectReference pref in prj.ProjectReferences) {
				if (pref.ReferenceType == ReferenceType.Gac)
					continue;
				foreach (string referenceFileName in pref.GetReferencedFileNames ()) {
					string asmName = Path.GetFileName (referenceFileName);
					asmName = Path.Combine (targetDir, asmName);
					if (!File.Exists (asmName)) {
						File.Copy (referenceFileName, asmName);
						files.Add (asmName);
					}
				}
				if (pref.ReferenceType == ReferenceType.Project) {
					Project sp = RootCombine.FindProject (pref.Reference);
					if (sp != null)
						CopySupportAssemblies (sp, targetDir, files);
				}
			}
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
			if (languageBinding == null)
				return false;
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
