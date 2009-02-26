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
using MonoDevelop.Projects.Policies;
using MonoDevelop.Projects.Formats.MD1;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects
{
	[DataInclude (typeof(DotNetProjectConfiguration))]
	public class DotNetProject : Project
	{
		string language;
		bool usePartialTypes = true;
		Ambience ambience;
		ProjectParameters languageParameters;
		
		[ItemProperty ("OutputType")]
		CompileTarget compileTarget;
		
		IDotNetLanguageBinding languageBinding;

		protected ProjectReferenceCollection projectReferences;

		[ItemProperty ("RootNamespace", DefaultValue="")]
		protected string defaultNamespace = String.Empty;
		
		public override string ProjectType {
			get { return "DotNet"; }
		}
		
		public override Ambience Ambience {
			get {
				if (ambience == null)
					ambience = AmbienceService.GetAmbienceForLanguage (LanguageName);
				return ambience;
			}
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
				if (!Loading && IsLibraryBasedProjectType && value != CompileTarget.Library)
					throw new InvalidOperationException ("CompileTarget cannot be changed on library-based project type.");
				compileTarget = value;
			}
		}
		
		[ItemProperty ("LanguageParameters")]
		public ProjectParameters LanguageParameters {
			get {
				if (languageParameters == null && LanguageBinding != null)
					LanguageParameters = LanguageBinding.CreateProjectParameters (null);
				return languageParameters;
			}
			internal set {
				languageParameters = value;
				if (languageParameters != null)
					languageParameters.ParentProject = this;
			}
		}

		public string DefaultNamespace {
			get { return defaultNamespace; }
			set {
				defaultNamespace = value;
				NotifyModified ("DefaultNamespace");
			}
		}
		
		IResourceHandler resourceHandler;
		
		public IResourceHandler ResourceHandler {
			get {
				if (resourceHandler == null) {
					DotNetNamingPolicy pol = Policies.Get<DotNetNamingPolicy> ();
					if (pol.ResourceNamePolicy == ResourceNamePolicy.FileFormatDefault)
						resourceHandler = ItemHandler as IResourceHandler;
					else if (pol.ResourceNamePolicy == ResourceNamePolicy.MSBuild)
						resourceHandler = MSBuildProjectService.GetResourceHandlerForItem (this);
					if (resourceHandler == null)
						resourceHandler = DefaultResourceHandler.Instance;
				}
				return resourceHandler;
			}
		}

		string targetFrameworkVersion;
		TargetFramework targetFramework;
		
		public TargetFramework TargetFramework {
			get {
				SetDefaultFramework ();
				return targetFramework;
			}
			set {
				bool replacingValue = targetFramework != null;
				TargetFramework validValue = GetValidFrameworkVersion (value);
				if (targetFramework == null && validValue == null)
					targetFramework = Services.ProjectService.DefaultTargetFramework;
				if (targetFramework == validValue || validValue == null)
					return;
				targetFramework = validValue;
				targetFrameworkVersion = validValue.Id;
				if (replacingValue)
					UpdateSystemReferences ();
				NotifyModified ("TargetFramework");
			}
		}

		void SetDefaultFramework ()
		{
			if (targetFramework == null) {
				if (targetFrameworkVersion != null)
					targetFramework = Runtime.SystemAssemblyService.GetTargetFramework (targetFrameworkVersion);
				if (targetFramework == null)
					TargetFramework = Services.ProjectService.DefaultTargetFramework;
			}
		}

		public virtual bool SupportsFramework (TargetFramework framework)
		{
			ClrVersion[] versions = SupportedClrVersions;
			if (versions != null && versions.Length > 0 && framework != null) {
				foreach (ClrVersion v in versions) {
					if (v == framework.ClrVersion)
						return true;
				}
			}
			return false;
		}
		
		//if possible, find a ClrVersion that the language binding can handle
		TargetFramework GetValidFrameworkVersion (TargetFramework suggestion)
		{
			if (suggestion == null) {
				if (LanguageBinding == null)
					return null;
				else
					suggestion = Services.ProjectService.DefaultTargetFramework;
			}

			ClrVersion[] versions = SupportedClrVersions;
			if (versions != null && versions.Length > 0) {
				foreach (ClrVersion v in versions) {
					if (v == suggestion.ClrVersion)
						return suggestion;
				}
				TargetFramework oneSupported = null;
				foreach (ClrVersion v in versions) {
					foreach (TargetFramework f in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
						if (f.ClrVersion == v) {
							if (f.IsSupported)
								return f;
							else if (oneSupported == null)
								oneSupported = f;
						}
					}
				}
				if (oneSupported != null)
					return oneSupported;
			}
			
			return null;
		}
		
		public virtual ClrVersion[] SupportedClrVersions {
			get {
				if (LanguageBinding != null)
					return LanguageBinding.GetSupportedClrVersions ();
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
			Items.Bind (projectReferences);
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
				LanguageParameters = languageBinding.CreateProjectParameters (projectOptions);
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
			
			if (projectOptions != null && projectOptions.Attributes["TargetFrameworkVersion"] != null) {
				targetFrameworkVersion = projectOptions.Attributes["TargetFrameworkVersion"].InnerText;
			}
			
			foreach (DotNetProjectConfiguration parameter in Configurations) {
				parameter.OutputDirectory = Path.Combine (binPath, parameter.Id);
				if (info != null)
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
				if (LanguageBinding == null)
					return false;
				System.CodeDom.Compiler.CodeDomProvider provider = LanguageBinding.GetCodeDomProvider ();
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
		
		public virtual IEnumerable<string> GetReferencedAssemblies (string configuration)
		{
			foreach (ProjectReference pref in References) {
				foreach (string asm in pref.GetReferencedFileNames (configuration))
					yield return asm;
			}
		}
		
		protected internal override void OnSave (IProgressMonitor monitor)
		{
			// Make sure the fx version is sorted out before saving
			// to avoid changes in project references while saving 
			SetDefaultFramework ();
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
			if (LanguageBinding != null)
				conf.CompilationParameters = LanguageBinding.CreateCompilationParameters (null);
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
		
		protected override bool CheckNeedsBuild (string solutionConfiguration)
		{
			if (base.CheckNeedsBuild (solutionConfiguration))
				return true;

			foreach (ProjectFile file in Files) {
				if (file.BuildAction == BuildAction.EmbeddedResource &&
					String.Compare (Path.GetExtension (file.FilePath), ".resx", true) == 0 &&
					MD1DotNetProjectHandler.IsResgenRequired (file.FilePath)) {
					return true;
				}
			}

			return false;
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
			if (LanguageBinding == null)
				return false;
			return LanguageBinding.IsSourceCodeFile (fileName);
		}
		
		public virtual string GetDefaultNamespace (string fileName)
		{
			DotNetNamingPolicy pol = Policies.Get<DotNetNamingPolicy> ();
			
			string root = null;
			string dirNamespc = null;
			string defaultNmspc = !string.IsNullOrEmpty (DefaultNamespace)
				? DefaultNamespace
				: SanitisePotentialNamespace (Name) ?? "Application";
			
			string dirname = Path.GetDirectoryName (fileName);
			string relativeDirname = null;
			if (!String.IsNullOrEmpty (dirname)) {
				relativeDirname = GetRelativeChildPath (dirname);
				if (string.IsNullOrEmpty (relativeDirname) || relativeDirname.StartsWith (".."))
					relativeDirname = null;		
			}
			
			if (relativeDirname != null) {
				try {
					switch (pol.DirectoryNamespaceAssociation) {
					case DirectoryNamespaceAssociation.PrefixedFlat:
						root = defaultNmspc;
						goto case DirectoryNamespaceAssociation.Flat;
					case DirectoryNamespaceAssociation.Flat:
						dirNamespc = SanitisePotentialNamespace (relativeDirname);
						break;
						
					case DirectoryNamespaceAssociation.PrefixedHierarchical:
						root = defaultNmspc;
						goto case DirectoryNamespaceAssociation.Hierarchical;
					case DirectoryNamespaceAssociation.Hierarchical:
						dirNamespc = SanitisePotentialNamespace (GetHierarchicalNamespace (relativeDirname));
						break;
					}
				} catch (IOException ex) {
					LoggingService.LogError ("Could not determine namespace for file '" + fileName + "'", ex);
				}
				
			}
			
			if (dirNamespc != null && root == null)
				return dirNamespc;
			if (dirNamespc != null && root != null)
				return root + "." + dirNamespc;
			return defaultNmspc;
		}
		
		string GetHierarchicalNamespace (string relativePath)
		{
			StringBuilder sb = new StringBuilder (relativePath);
			for (int i = 0; i < sb.Length; i++) {
				if (sb[i] == Path.DirectorySeparatorChar)
					sb[i] = '.';
			}
			return sb.ToString ();
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
					string newRef = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (pref.Reference, pref.Package != null ? pref.Package.Name : null, this.TargetFramework);
					if (newRef == null) {
						// re-add the reference. It will be shown as invalid.
						toDelete.Add (pref);
						toAdd.Add (new ProjectReference (ReferenceType.Gac, pref.Reference));
					}
					else if (newRef != pref.Reference) {
						toDelete.Add (pref);
						toAdd.Add (new ProjectReference (ReferenceType.Gac, newRef));
					}
					else if (!pref.IsValid) {
						pref.ResetReference ();
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
		
		internal override void SetItemHandler (ISolutionItemHandler handler)
		{
			if (ProjectExtensionUtil.GetItemHandler (this) == null) {
				// Initial assignment of the item handler
				base.SetItemHandler (handler);
				return;
			}
			IResourceHandler rh = ResourceHandler;
			
			base.SetItemHandler (handler);
			resourceHandler = null;
			// A change in the file format may imply a change in the resource naming policy.
			// Make sure that the resource Id don't change.
			MigrateResourceIds (rh, ResourceHandler);
		}
		
		protected override void OnEndLoad ()
		{
			IResourceHandler handler = ItemHandler as IResourceHandler;
			if (handler != null)
				MigrateResourceIds (handler, ResourceHandler);

			base.OnEndLoad ();
		}
		
		public void UpdateResourceHandler (bool keepOldIds)
		{
			IResourceHandler oldHandler = resourceHandler;
			resourceHandler = null;
			if (keepOldIds && oldHandler != null)
				MigrateResourceIds (oldHandler, ResourceHandler);
		}
		
		void MigrateResourceIds (IResourceHandler oldHandler, IResourceHandler newHandler)
		{
			if (oldHandler.GetType () != newHandler.GetType ()) {
				// If the file format has a default resource handler different from the one
				// choosen for this project, then all resource ids must be converted
				foreach (ProjectFile file in Files) {
					string oldId = file.GetResourceId (oldHandler);
					string newId = file.GetResourceId (newHandler);
					string newDefault = newHandler.GetDefaultResourceId (file);
					if (oldId != newId) {
						if (newDefault == oldId)
							file.ResourceId = null;
						else
							file.ResourceId = oldId;
					} else {
						if (newDefault == oldId)
							file.ResourceId = null;
					}
				}
			}
		}
		
		protected internal override void OnItemAdded (object obj)
		{
			base.OnItemAdded (obj);
			if (obj is ProjectReference) {
				ProjectReference pref = (ProjectReference) obj;
				pref.SetOwnerProject (this);
				NotifyReferenceAddedToProject (pref);
			}
		}

		protected internal override void OnItemRemoved (object obj)
		{
			base.OnItemRemoved (obj);
			if (obj is ProjectReference) {
				ProjectReference pref = (ProjectReference) obj;
				pref.SetOwnerProject (null);
				NotifyReferenceRemovedFromProject (pref);
			}
		}
		
		internal void NotifyReferenceRemovedFromProject (ProjectReference reference)
		{
			SetNeedsBuilding (true);
			NotifyModified ("References");
			OnReferenceRemovedFromProject (new ProjectReferenceEventArgs (this, reference));
		}
		
		internal void NotifyReferenceAddedToProject (ProjectReference reference)
		{
			SetNeedsBuilding (true);
			NotifyModified ("References");
			OnReferenceAddedToProject (new ProjectReferenceEventArgs (this, reference));
		}
		
		protected virtual void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			if (ReferenceRemovedFromProject != null) {
				ReferenceRemovedFromProject (this, e);
			}
		}
		
		protected virtual void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			if (ReferenceAddedToProject != null) {
				ReferenceAddedToProject (this, e);
			}
		}
		
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
	}
}
