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
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects.Ambience;

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
		
		IDotNetLanguageBinding languageBinding;

		protected ProjectReferenceCollection projectReferences;

		[ItemProperty ("RootNamespace", DefaultValue="")]
		protected string defaultNamespace = String.Empty;
		
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
			set { compileTarget = value; }
		}

		public string DefaultNamespace {
			get { return defaultNamespace; }
			set {
				defaultNamespace = value;
				NotifyModified ();
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
				Configurations.Add (configuration);
				
				configuration = (DotNetProjectConfiguration) CreateConfiguration ("Release");
				configuration.DebugMode = false;
				if (projectOptions != null)
					projectOptions.SetAttribute ("DefineDebug", "False");
				configuration.CompilationParameters = languageBinding.CreateCompilationParameters (projectOptions);
				Configurations.Add (configuration);
			}
			
			if (projectOptions.Attributes["Target"] != null) {
				compileTarget = (CompileTarget) Enum.Parse(typeof(CompileTarget), projectOptions.Attributes["Target"].InnerText);
			}
			
			foreach (DotNetProjectConfiguration parameter in Configurations) {
				parameter.OutputDirectory = Path.Combine (binPath, parameter.Id);
				parameter.OutputAssembly  = Name;
				
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

		public void CopyReferencesToOutputPath (bool force, string configuration)
		{
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config == null) {
				return;
			}
			CopyReferencesToOutputPath (config.OutputDirectory, force, configuration);
		}
		
		void CopyReferencesToOutputPath (string destPath, bool force, string configuration)
		{
			string[] deployFiles = GetReferenceDeployFiles (force, configuration);
			
			foreach (string sourcePath in deployFiles) {
				string destinationFileName = Path.Combine (destPath, Path.GetFileName (sourcePath));
				try {
					if (destinationFileName != sourcePath) {
						// Make sure the target directory exists
						if (!Directory.Exists (Path.GetDirectoryName (destinationFileName)))
							Directory.CreateDirectory (Path.GetDirectoryName (destinationFileName));
						// Copy the file
						FileService.CopyFile (sourcePath, destinationFileName);
					}
				} catch (Exception e) {
					LoggingService.LogError ("Can't copy reference file from {0} to {1}: {2}", sourcePath, destinationFileName, e);
				}
			}
		}
		
		public string[] GetReferenceDeployFiles (bool force, string configuration)
		{
			ArrayList deployFiles = new ArrayList ();

			foreach (ProjectReference projectReference in References) {
				if ((projectReference.LocalCopy || force) && projectReference.ReferenceType != ReferenceType.Gac) {
					foreach (string referenceFileName in projectReference.GetReferencedFileNames (configuration)) {
						deployFiles.Add (referenceFileName);
						if (File.Exists (referenceFileName + ".config"))
							deployFiles.Add (referenceFileName + ".config");
					}
				}
				if (projectReference.ReferenceType == ReferenceType.Project && projectReference.LocalCopy && ParentSolution != null) {
					DotNetProject p = ParentSolution.FindProjectByName (projectReference.Reference) as DotNetProject;
					if (p != null) {
						ProjectConfiguration config = p.GetConfiguration (configuration) as ProjectConfiguration;
						if (config != null && config.DebugMode)
							deployFiles.Add (p.GetOutputFileName (configuration) + ".mdb");

						deployFiles.AddRange (p.GetReferenceDeployFiles (force, configuration));
					}
				}
			}
			return (string[]) deployFiles.ToArray (typeof(string));
		}
		
		void CleanReferencesInOutputPath (string destPath, string configuration)
		{
			string[] deployFiles = GetReferenceDeployFiles (true, configuration);
			
			foreach (string sourcePath in deployFiles) {
				string destinationFileName = Path.Combine (destPath, Path.GetFileName (sourcePath));
				try {
					if (destinationFileName != sourcePath) {
						if (File.Exists (destinationFileName))
							FileService.DeleteFile (destinationFileName);
					}
				} catch (Exception e) {
					LoggingService.LogError ("Can't delete reference file {0}: {2}", destinationFileName, e);
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
			if (languageBinding != null)
				conf.CompilationParameters = languageBinding.CreateCompilationParameters (null);
			return conf;
		}


		public override string GetOutputFileName (string configuration)
		{
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) GetConfiguration (configuration);
			if (conf != null)
				return conf.CompiledOutputName;
			else
				return null;
		}
		
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context, string config)
		{
			CopyReferencesToOutputPath (true, config);
			
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
			
				IProcessAsyncOperation op = handler.Execute (configuration.CompiledOutputName, configuration.CommandLineParameters, Path.GetDirectoryName (configuration.CompiledOutputName), null, console);
				
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
		
		protected internal override void OnClean (IProgressMonitor monitor, string configuration)
		{
			// Delete referenced assemblies
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config != null)
				CleanReferencesInOutputPath (config.OutputDirectory, configuration);
			
			base.OnClean (monitor, configuration);
		}
		
		protected internal override List<string> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<string> col = base.OnGetItemFiles (includeReferencedFiles);
			if (includeReferencedFiles) {
				foreach (ProjectReference pref in References)
					if (pref.ReferenceType == ReferenceType.Assembly)
						col.Add (pref.Reference);
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
					if (directory != null) 
						return directory.Name;
				} catch {
				}
			}
			
			if (!string.IsNullOrEmpty (DefaultNamespace))
				return DefaultNamespace;
			
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
	}
}
