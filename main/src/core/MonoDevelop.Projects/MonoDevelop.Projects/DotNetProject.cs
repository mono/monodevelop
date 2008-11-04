//  DotNetProject.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects.Dom.Output;

namespace MonoDevelop.Projects
{
	[DataInclude (typeof(DotNetProjectConfiguration))]
	public class DotNetProject : Project
	{
		string language;
		ClrVersion clrVersion = ClrVersion.Default;
		bool usePartialTypes = true;
		
		[ItemProperty ("OutputType")]
		CompileTarget compileTarget;
		
		[ItemProperty ("UseParentDirectoryAsNamespace", DefaultValue=false)]
		bool useParentDirectoryAsNamespace = false;
		
		IDotNetLanguageBinding languageBinding;

		protected ProjectReferenceCollection projectReferences;

		[ItemProperty ("RootNamespace", DefaultValue="")]
		protected string defaultNamespace = String.Empty;
		
		public override string ProjectType {
			get { return "DotNet"; }
		}
		
		public override Ambience Ambience {
			get { return AmbienceService.GetAmbienceForLanguage (LanguageName); }
		}
		
		public string LanguageName {
			get { return language; }
		}
		
		public override string [] SupportedLanguages {
			get {
				return new string [] { "", language };
			}
		}
		
		public virtual bool IsLibraryBasedProjectType {
			get { return false; }
		}
		
		public ProjectReferenceCollection References {
			get {
				return projectReferences;
			}
		}
		
		public IDotNetLanguageBinding LanguageBinding {
			get {
				if (languageBinding == null) {
					languageBinding = FindLanguage (language);
					
					//older projects may not have this property but may not support partial types
					//so need to verify that the default attribute is OK
					if (UsePartialTypes && !SupportsPartialTypes) {
						LoggingService.LogWarning ("Project '{0}' has been set to use partial types but does not support them.", Name);
						UsePartialTypes = false;
					}
				}
				return languageBinding; 
			}
		}
		
		public CompileTarget CompileTarget {
			get { return compileTarget; }
			set {
				if (IsLibraryBasedProjectType && value != CompileTarget.Library)
					throw new InvalidOperationException ("CompileTarget cannot be changed on library-based project type.");
				compileTarget = value;
			}
		}

		public string DefaultNamespace {
			get { return defaultNamespace; }
			set {
				defaultNamespace = value;
				NotifyModified ("DefaultNamespace");
			}
		}
		
		public bool UseParentDirectoryAsNamespace {
			get { return useParentDirectoryAsNamespace; }
			set { 
				useParentDirectoryAsNamespace = value; 
				NotifyModified ("UseParentDirectoryAsNamespace");
			}
		}
		
		public ClrVersion ClrVersion {
			get {
				if (clrVersion == ClrVersion.Default)
					ClrVersion = ClrVersion.Default;
				return clrVersion;
			}
			set {
				ClrVersion validValue = GetValidClrVersion (value);
				if (clrVersion == validValue || validValue == ClrVersion.Default)
					return;
				clrVersion = validValue;
				
				// Propagate the clr version to configurations. We don't support
				// per-project clr versions right now, but we might support it
				// in the future.
				foreach (DotNetProjectConfiguration conf in Configurations)
					conf.ClrVersion = clrVersion;
				
				UpdateSystemReferences ();
				NotifyModified ("ClrVersion");
			}
		}
		
		//if possible, find a ClrVersion that the language binding can handle
		ClrVersion GetValidClrVersion (ClrVersion suggestion)
		{
			if (suggestion == ClrVersion.Default) {
				if (languageBinding == null)
					return ClrVersion.Default;
				else
					suggestion = ClrVersion.Net_2_0;
			}
			
			ClrVersion[] versions = SupportedClrVersions;
			if (versions != null && versions.Length > 0) {
				foreach (ClrVersion v in versions) {
					if (v == suggestion) {
						return suggestion;
					}
				}
				
				return versions[0];
			}
			
			return suggestion;
		}
		
		public virtual ClrVersion[] SupportedClrVersions {
			get {
				if (languageBinding != null)
					return languageBinding.GetSupportedClrVersions ();
				return null;
			}
		}
		
		[ItemProperty (DefaultValue=true)]
		public bool UsePartialTypes {
			get { return usePartialTypes; }
			set { usePartialTypes = value; }
		}
		
		public DotNetProject ()
		{
			projectReferences = new ProjectReferenceCollection ();
			projectReferences.SetProject (this);
			if (IsLibraryBasedProjectType)
				CompileTarget = CompileTarget.Library;
		}
		
		public DotNetProject (string languageName): this ()
		{
			language = languageName;
			languageBinding = FindLanguage (language);
			this.usePartialTypes = SupportsPartialTypes;
		}
		
		public DotNetProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions): this ()
		{
			// Language name must be set before the item handler is assigned
			language = languageName;
			languageBinding = FindLanguage (language);
			
			string binPath;
			if (info != null) {
				Name = info.ProjectName;
				binPath = info.BinPath;
			} else {
				binPath = ".";
			}
			
			if (languageBinding != null) {
				if (projectOptions != null)
					projectOptions.SetAttribute ("DefineDebug", "True");
				DotNetProjectConfiguration configuration = (DotNetProjectConfiguration) CreateConfiguration ("Debug");
				configuration.CompilationParameters = languageBinding.CreateCompilationParameters (projectOptions);
				configuration.DebugMode = true;
				Configurations.Add (configuration);
				
				configuration = (DotNetProjectConfiguration) CreateConfiguration ("Release");
				configuration.DebugMode = false;
				if (projectOptions != null)
					projectOptions.SetAttribute ("DefineDebug", "False");
				configuration.CompilationParameters = languageBinding.CreateCompilationParameters (projectOptions);
				Configurations.Add (configuration);
			}
			
			if (projectOptions != null && projectOptions.Attributes["Target"] != null) {
				compileTarget = (CompileTarget) Enum.Parse(typeof(CompileTarget), projectOptions.Attributes["Target"].InnerText);
			} else if (IsLibraryBasedProjectType) {
				CompileTarget = CompileTarget.Library;
			}
			
			foreach (DotNetProjectConfiguration parameter in Configurations) {
				parameter.OutputDirectory = Path.Combine (binPath, parameter.Id);
				parameter.OutputAssembly  = info.ProjectName;
				
				if (projectOptions != null) {
					if (projectOptions.Attributes["PauseConsoleOutput"] != null) {
						parameter.PauseConsoleOutput = Boolean.Parse(projectOptions.Attributes["PauseConsoleOutput"].InnerText);
					}
				}
			}
			
			this.usePartialTypes = SupportsPartialTypes;
		}
		
		public virtual bool SupportsPartialTypes {
			get {
				if (languageBinding == null)
					return false;
				System.CodeDom.Compiler.CodeDomProvider provider = languageBinding.GetCodeDomProvider ();
				if (provider == null)
					return false;
				return provider.Supports ( System.CodeDom.Compiler.GeneratorSupport.PartialTypes);
			}
		}
		
		public override string[] SupportedPlatforms {
			get {
				return new string [] {"AnyCPU"};
			}
		}
		
		internal void RenameReferences (string oldName, string newName)
		{
			ArrayList toBeRemoved = new ArrayList();

			foreach (ProjectReference refInfo in this.References) {
				if (refInfo.ReferenceType == ReferenceType.Project) {
					if (refInfo.Reference == oldName) {
						toBeRemoved.Add(refInfo);
					}
				}
			}
			
			foreach (ProjectReference pr in toBeRemoved) {
				this.References.Remove(pr);
				ProjectReference prNew = new ProjectReference (ReferenceType.Project, newName);
				this.References.Add(prNew);
			}
		}
		
		protected override void PopulateSupportFileList (FileCopySet list, string solutionConfiguration)
		{
			PopulateSupportFileList (list, solutionConfiguration, 0);
		}
		
		void PopulateSupportFileList (FileCopySet list, string solutionConfiguration, int referenceDistance)
		{
			if (referenceDistance < 2)
				base.PopulateSupportFileList (list, solutionConfiguration);
			
			//rename the app.config file
			FileCopySet.Item appConfig = list.Remove ("app.config");
			if (appConfig == null)
				appConfig = list.Remove ("App.config");
			if (appConfig != null) {
				string output = Path.GetFileName (GetOutputFileName (solutionConfiguration));
				list.Add (appConfig.Src, appConfig.CopyOnlyIfNewer, output + ".config");
			}
			
			//collect all the "local copy" references and their attendant files
			foreach (ProjectReference projectReference in References) {
				if (!projectReference.LocalCopy || ParentSolution == null)
					continue;
				
				if (projectReference.ReferenceType == ReferenceType.Project )
				{
					DotNetProject p = ParentSolution.FindProjectByName (projectReference.Reference)
						as DotNetProject;
					
					if (p == null) {
						LoggingService.LogWarning (
							"Project '{0}' referenced from '{1}' could not be found",
							projectReference.Reference, this.Name);
						continue;
					}
					
					string refOutput = p.GetOutputFileName (solutionConfiguration);
					if (string.IsNullOrEmpty (refOutput)) {
						LoggingService.LogWarning (
							"Project '{0}' referenced from '{1}' has an empty output filename",
							p.Name, this.Name);
						continue;
					}
					
					list.Add (refOutput);
					
					//VS COMPAT: recursively copy references's "local copy" files
					//but only copy the "copy to output" files from the immediate references
					p.PopulateSupportFileList (list, solutionConfiguration, referenceDistance + 1);
					
					DotNetProjectConfiguration refConfig = p.GetActiveConfiguration (solutionConfiguration)
						as DotNetProjectConfiguration;
					
					if (refConfig != null && refConfig.DebugMode) {
						string mdbFile = refOutput + ".mdb";
						if (File.Exists (mdbFile)) {
							list.Add (mdbFile);
						}
					}
				}
				else if (projectReference.ReferenceType == ReferenceType.Assembly)
				{
					list.Add (projectReference.Reference);
					if (File.Exists (projectReference.Reference + ".config"))
						list.Add (projectReference.Reference + ".config");
					string mdbFile = projectReference.Reference  + ".mdb";
					if (File.Exists (mdbFile))
						list.Add (mdbFile);
				}
				else if (projectReference.ReferenceType == ReferenceType.Custom)
				{
					foreach (string refFile in projectReference.GetReferencedFileNames (solutionConfiguration))
						list.Add (refFile);
				}
			}
		}
		
		public ProjectReference AddReference (string filename)
		{
			foreach (ProjectReference rInfo in References) {
				if (rInfo.Reference == filename) {
					return rInfo;
				}
			}
			ProjectReference newReferenceInformation = new ProjectReference (ReferenceType.Assembly, filename);
			References.Add (newReferenceInformation);
			return newReferenceInformation;
		}
		
		public override IEnumerable<SolutionItem> GetReferencedItems (string configuration)
		{
			List<SolutionItem> items = new List<SolutionItem> ();
			if (ParentSolution == null)
				return items;
			
			foreach (ProjectReference pref in References) {
				if (pref.ReferenceType == ReferenceType.Project) {
					Project rp = ParentSolution.FindProjectByName (pref.Reference);
					if (rp != null)
						items.Add (rp);
				}
			}
			return items;
		}
		
		protected internal override void OnSave (IProgressMonitor monitor)
		{
			//make sure clr version is sorted out before saving
			ClrVersion v = this.ClrVersion;
			base.OnSave (monitor);
		}

		IDotNetLanguageBinding FindLanguage (string name)
		{
			IDotNetLanguageBinding binding = Services.Languages.GetBindingPerLanguageName (language) as IDotNetLanguageBinding;
			return binding;
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			DotNetProjectConfiguration conf = new DotNetProjectConfiguration (name);
			conf.OutputDirectory = Path.Combine ("bin", name);
			conf.OutputAssembly  = Name;
			if (languageBinding != null)
				conf.CompilationParameters = languageBinding.CreateCompilationParameters (null);
			return conf;
		}


		protected override string OnGetOutputFileName (string configuration)
		{
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) GetConfiguration (configuration);
			if (conf != null)
				return conf.CompiledOutputName;
			else
				return null;
		}
		
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context, string config)
		{
			DotNetProjectConfiguration configuration = (DotNetProjectConfiguration) GetConfiguration (config);
			monitor.Log.WriteLine ("Running " + configuration.CompiledOutputName + " ...");
			
			string platform = "Mono";
			
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
			
				IProcessAsyncOperation op = handler.Execute (configuration.CompiledOutputName, configuration.CommandLineParameters, Path.GetDirectoryName (configuration.CompiledOutputName), configuration.EnvironmentVariables, console);
				
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
		
		protected internal override bool OnGetCanExecute (ExecutionContext context, string configuration)
		{
			return (compileTarget == CompileTarget.Exe || compileTarget == CompileTarget.WinExe) &&
				context.ExecutionHandlerFactory.SupportsPlatform (ExecutionPlatform.Mono);
		}
		
		protected internal override List<string> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<string> col = base.OnGetItemFiles (includeReferencedFiles);
			if (includeReferencedFiles) {
				foreach (ProjectReference pref in References)
					if (pref.ReferenceType == ReferenceType.Assembly)
						col.Add (pref.Reference);
				foreach (DotNetProjectConfiguration c in Configurations) {
					if (c.SignAssembly)
						col.Add (c.AssemblyKeyFile);
				}
			}
			return col;
		}


		public override bool IsCompileable(string fileName)
		{
			if (languageBinding == null)
				return false;
			return languageBinding.IsSourceCodeFile (fileName);
		}
		
		public virtual string GetDefaultNamespace (string fileName)
		{
			if (UseParentDirectoryAsNamespace) {
				try {
					DirectoryInfo directory = new DirectoryInfo (Path.GetDirectoryName (fileName));
					if (directory != null) {
						string potential = SanitisePotentialNamespace (directory.Name);
						if (potential != null)
							return potential;
					}
				} catch {}
			}
			
			if (!string.IsNullOrEmpty (DefaultNamespace))
				return DefaultNamespace;
			
			return SanitisePotentialNamespace (Name) ?? "Application";
		}
		
		string SanitisePotentialNamespace (string potential)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (char c in potential) {
				if (char.IsLetter (c) || c == '_'
					|| (sb.Length > 0 && (char.IsLetterOrDigit (sb[sb.Length-1]) || sb[sb.Length-1] == '_')
						&& (c == '.' || char.IsNumber (c)))
				) {
					sb.Append (c);
				}
			}
			if (sb.Length > 0)
				return sb.ToString ();
			else
				return null;
		}

		// Make sure that the project references are valid for the target clr version.
		void UpdateSystemReferences ()
		{
			ArrayList toDelete = new ArrayList ();
			ArrayList toAdd = new ArrayList ();
			
			foreach (ProjectReference pref in References) {
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
				References.Remove (pref);
			}
			foreach (ProjectReference pref in toAdd) {
				References.Add (pref);
			}
		}
		
		protected override IEnumerable<string> GetStandardBuildActions ()
		{
			return BuildAction.DotNetActions;
		}
		
		protected override IList<string> GetCommonBuildActions () 
		{
			return BuildAction.DotNetCommonActions;
		}
	}
}
