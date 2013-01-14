//  DotNetProject.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.CodeDom.Compiler;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Projects.Formats.MD1;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.Formats.MSBuild;
using MonoDevelop.Core.Assemblies;
using System.Globalization;

namespace MonoDevelop.Projects
{
	[DataInclude(typeof(DotNetProjectConfiguration))]
	[ProjectModelDataItem ("AbstractDotNetProject")]
	public abstract class DotNetProject : Project, IAssemblyProject
	{

		bool usePartialTypes = true;
		ProjectParameters languageParameters;

		DirectoryAssemblyContext privateAssemblyContext;
		ComposedAssemblyContext composedAssemblyContext;
		IAssemblyContext currentRuntimeContext;

		[ItemProperty("OutputType")]
		CompileTarget compileTarget;

		IDotNetLanguageBinding languageBinding;

		protected ProjectReferenceCollection projectReferences;

		[ItemProperty("RootNamespace", DefaultValue = "")]
		protected string defaultNamespace = String.Empty;
		
		public DotNetProject ()
		{
			Runtime.SystemAssemblyService.DefaultRuntimeChanged += RuntimeSystemAssemblyServiceDefaultRuntimeChanged;
			projectReferences = new ProjectReferenceCollection ();
			Items.Bind (projectReferences);
			if (IsLibraryBasedProjectType)
				CompileTarget = CompileTarget.Library;
			FileService.FileRemoved += OnFileRemoved;
		}

		public DotNetProject (string languageName) : this()
		{
			// Language name must be set before the item handler is assigned
			this.languageName = languageName;
			this.languageBinding = FindLanguage (languageName);

			if (this.languageBinding != null)
				this.StockIcon = this.languageBinding.ProjectStockIcon;

			this.usePartialTypes = SupportsPartialTypes;
		}

		public DotNetProject (string languageName, ProjectCreateInformation projectCreateInfo, XmlElement projectOptions) : this(languageName)
		{
			if ((projectOptions != null) && (projectOptions.Attributes ["Target"] != null))
				CompileTarget = (CompileTarget)Enum.Parse (typeof(CompileTarget), projectOptions.Attributes ["Target"].Value);
			else if (IsLibraryBasedProjectType)
				CompileTarget = CompileTarget.Library;

			if (this.LanguageBinding != null) {
				LanguageParameters = languageBinding.CreateProjectParameters (projectOptions);
				
				bool externalConsole = false;

				string platform = null;
				if (projectOptions != null) {
					projectOptions.SetAttribute ("DefineDebug", "True");
					if (!projectOptions.HasAttribute ("Platform")) {
						// Clone the element since we are going to change it
						platform = GetDefaultTargetPlatform (projectCreateInfo);
						projectOptions = (XmlElement)projectOptions.CloneNode (true);
						projectOptions.SetAttribute ("Platform", platform);
					} else
						platform = projectOptions.GetAttribute ("Platform");
					if (projectOptions.GetAttribute ("ExternalConsole") == "True")
						externalConsole = true;
				}
				string platformSuffix = string.IsNullOrEmpty (platform) ? string.Empty : "|" + platform;
				DotNetProjectConfiguration configDebug = CreateConfiguration ("Debug" + platformSuffix) as DotNetProjectConfiguration;
				configDebug.CompilationParameters = languageBinding.CreateCompilationParameters (projectOptions);
				configDebug.DebugMode = true;
				configDebug.ExternalConsole = externalConsole;
				configDebug.PauseConsoleOutput = externalConsole;
				Configurations.Add (configDebug);

				DotNetProjectConfiguration configRelease = CreateConfiguration ("Release" + platformSuffix) as DotNetProjectConfiguration;
				
				if (projectOptions != null) {
					XmlElement releaseProjectOptions = (XmlElement)projectOptions.CloneNode (true);
					releaseProjectOptions.SetAttribute ("Release", "True");
					configRelease.CompilationParameters = languageBinding.CreateCompilationParameters (releaseProjectOptions);
				} else {
					configRelease.CompilationParameters = languageBinding.CreateCompilationParameters (null);
				}
				
				configRelease.CompilationParameters.RemoveDefineSymbol ("DEBUG");
				configRelease.DebugMode = false;
				configRelease.ExternalConsole = externalConsole;
				configRelease.PauseConsoleOutput = externalConsole;
				Configurations.Add (configRelease);
			}

			if ((projectOptions != null) && (projectOptions.Attributes["TargetFrameworkVersion"] != null))
				newProjectTargetFrameworkId = TargetFrameworkMoniker.Parse (projectOptions.Attributes["TargetFrameworkVersion"].Value);

			string binPath;
			if (projectCreateInfo != null) {
				Name = projectCreateInfo.ProjectName;
				binPath = projectCreateInfo.BinPath;
				defaultNamespace = SanitisePotentialNamespace (projectCreateInfo.ProjectName);
			} else {
				binPath = ".";
			}

			foreach (DotNetProjectConfiguration dotNetProjectConfig in Configurations) {
				dotNetProjectConfig.OutputDirectory = Path.Combine (binPath, dotNetProjectConfig.Name);

				if ((projectOptions != null) && (projectOptions.Attributes["PauseConsoleOutput"] != null))
					dotNetProjectConfig.PauseConsoleOutput = Boolean.Parse (projectOptions.Attributes["PauseConsoleOutput"].Value);

				if (projectCreateInfo != null)
					dotNetProjectConfig.OutputAssembly = projectCreateInfo.ProjectName;
			}
		}

		public override string ProjectType {
			get { return "DotNet"; }
		}

		private string languageName;
		public string LanguageName {
			get { return languageName; }
		}

		public override string[] SupportedLanguages {
			get { return new string[] {"",languageName}; }
		}

		public virtual bool IsLibraryBasedProjectType {
			get { return false; }
		}

		public virtual bool GeneratesDebugInfoFile {
			get { return true; }
		}
		
		protected virtual string GetDefaultTargetPlatform (ProjectCreateInformation projectCreateInfo)
		{
			return string.Empty;
		}
		
		public ProjectReferenceCollection References {
			get { return projectReferences; }
		}

		public IDotNetLanguageBinding LanguageBinding {
			get {
				if (languageBinding == null) {
					languageBinding = FindLanguage (languageName);

					//older projects may not have this property but may not support partial types
					//so need to verify that the default attribute is OK
					if (languageBinding != null && UsePartialTypes && !SupportsPartialTypes) {
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

		[ItemProperty("LanguageParameters")]
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
		
		/// <summary>
		/// Default namespace setting. May be empty, use GetDefaultNamespace to get a usable value.
		/// </summary>
		public string DefaultNamespace {
			get { return defaultNamespace; }
			set {
				defaultNamespace = value;
				NotifyModified ("DefaultNamespace");
			}
		}
		
		/// <summary>
		/// Given a namespace, removes from it the implicit namespace of the project,
		/// if there is one. This depends on the target language. For example, in VB.NET
		/// the default namespace is implicit.
		/// </summary>
		public string StripImplicitNamespace (string ns)
		{
			if ((LanguageParameters is DotNetProjectParameters) && ((DotNetProjectParameters)LanguageParameters).DefaultNamespaceIsImplicit) {
				if (DefaultNamespace.Length > 0 && ns.StartsWith (DefaultNamespace + "."))
					return ns.Substring (DefaultNamespace.Length + 1);
				else if (DefaultNamespace == ns)
					return string.Empty;
			}
			return ns;
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
		
		//only used for initializing new projects
		TargetFrameworkMoniker newProjectTargetFrameworkId;
		
		TargetFramework targetFramework;

		public TargetFramework TargetFramework {
			get {
				if (targetFramework == null) {
					var id = newProjectTargetFrameworkId ?? GetDefaultTargetFrameworkId ();
					targetFramework = Runtime.SystemAssemblyService.GetTargetFramework (id);
				}
				return targetFramework;
			}
			set {
				if (!SupportsFramework (value))
					throw new ArgumentException ("Project does not support framework '" + value.Id.ToString () +"'");
				if (value == null)
					value = Runtime.SystemAssemblyService.GetTargetFramework (GetDefaultTargetFrameworkForFormat (FileFormat));
				if (targetFramework != null && value.Id == targetFramework.Id)
					return;
				bool updateReferences = targetFramework != null;
				targetFramework = value;
				if (updateReferences)
					UpdateSystemReferences ();
				NotifyModified ("TargetFramework");
			}
		}

		public TargetRuntime TargetRuntime {
			get { return Runtime.SystemAssemblyService.DefaultRuntime; }
		}
		
		/// <summary>
		/// Gets the target framework for new projects
		/// </summary>
		/// <returns>
		/// The default target framework identifier.
		/// </returns>
		public virtual TargetFrameworkMoniker GetDefaultTargetFrameworkId ()
		{
			return Services.ProjectService.DefaultTargetFramework.Id;
		}
		
		/// <summary>
		/// Returns the default framework for a given format
		/// </summary>
		/// <returns>
		/// The default target framework for the format.
		/// </returns>
		/// <param name='format'>
		/// A format
		/// </param>
		/// <remarks>
		/// This method is used to determine what's the correct target framework for a project
		/// deserialized using a specific format.
		/// </remarks>
		public virtual TargetFrameworkMoniker GetDefaultTargetFrameworkForFormat (FileFormat format)
		{
			return GetDefaultTargetFrameworkId ();
		}
		
		public IAssemblyContext AssemblyContext {
			get {
				if (composedAssemblyContext == null) {
					composedAssemblyContext = new ComposedAssemblyContext ();
					composedAssemblyContext.Add (PrivateAssemblyContext);
					currentRuntimeContext = TargetRuntime.AssemblyContext;
					composedAssemblyContext.Add (currentRuntimeContext);
				}
				return composedAssemblyContext;
			}
		}

		public IAssemblyContext PrivateAssemblyContext {
			get {
				if (privateAssemblyContext == null)
					privateAssemblyContext = new DirectoryAssemblyContext ();
				return privateAssemblyContext;
			}
		}

		public virtual bool SupportsFramework (TargetFramework framework)
		{
			if (LanguageBinding == null)
				return false;
			ClrVersion[] versions = LanguageBinding.GetSupportedClrVersions ();
			if (versions != null && versions.Length > 0 && framework != null) {
				foreach (ClrVersion v in versions) {
					if (v == framework.ClrVersion)
						return true;
				}
			}
			return false;
		}

		[ItemProperty(DefaultValue = true)]
		public bool UsePartialTypes {
			get { return usePartialTypes; }
			set { usePartialTypes = value; }
		}

		public override void Dispose ()
		{
			if (composedAssemblyContext != null) {
				composedAssemblyContext.Dispose ();
				// composedAssemblyContext = null;
			}
				
			// languageParameters = null;
			// privateAssemblyContext = null;
			// currentRuntimeContext = null;
			// languageBinding = null;
			// projectReferences = null;
			
			Runtime.SystemAssemblyService.DefaultRuntimeChanged -= RuntimeSystemAssemblyServiceDefaultRuntimeChanged;
			FileService.FileRemoved -= OnFileRemoved;
			
			base.Dispose ();
		}

		public virtual bool SupportsPartialTypes {
			get {
				if (LanguageBinding == null)
					return false;
				System.CodeDom.Compiler.CodeDomProvider provider = LanguageBinding.GetCodeDomProvider ();
				if (provider == null)
					return false;
				return provider.Supports (System.CodeDom.Compiler.GeneratorSupport.PartialTypes);
			}
		}

		public override string[] SupportedPlatforms {
			get { return new string[] { "AnyCPU" }; }
		}

		void CheckReferenceChange (string updatedFile)
		{
			foreach (ProjectReference pr in References) {
				if (pr.ReferenceType == ReferenceType.Assembly) {
					if (updatedFile == Path.GetFullPath (pr.Reference))
						pr.NotifyStatusChanged ();
				}
			}
		}

		internal override void OnFileChanged (object source, MonoDevelop.Core.FileEventArgs e)
		{
			// The OnFileChanged handler is unsubscibed in the Dispose method, so in theory we shouldn't need
			// to check for disposed here. However, it might happen that this project is disposed while the
			// FileService.FileChanged event is being dispatched, in which case the event handler list is already
			// cached and won't take into account unsubscriptions until the next dispatch
			if (Disposed)
				return;
			
			base.OnFileChanged (source, e);
			foreach (FileEventInfo ei in e)
				CheckReferenceChange (ei.FileName);
		}


		internal void RenameReferences (string oldName, string newName)
		{
			ArrayList toBeRenamed = new ArrayList ();

			foreach (ProjectReference refInfo in this.References) {
				if (refInfo.ReferenceType == ReferenceType.Project) {
					if (refInfo.Reference == oldName)
						toBeRenamed.Add (refInfo);
				}
			}

			foreach (ProjectReference pr in toBeRenamed) {
				this.References.Remove (pr);
				ProjectReference prNew = ProjectReference.RenameReference (pr, newName);
				this.References.Add (prNew);
			}
		}
		
		internal protected override void PopulateOutputFileList (List<FilePath> list, ConfigurationSelector configuration)
		{
			base.PopulateOutputFileList (list, configuration);
			DotNetProjectConfiguration conf = GetConfiguration (configuration) as DotNetProjectConfiguration;
			
			// Debug info file
			
			if (conf.DebugMode) {
				string mdbFile = TargetRuntime.GetAssemblyDebugInfoFile (conf.CompiledOutputName);
				list.Add (mdbFile);
			}
			
			// Generated satellite resource files
			
			FilePath outputDir = conf.OutputDirectory;
			string satelliteAsmName = Path.GetFileNameWithoutExtension (conf.CompiledOutputName) + ".resources.dll";
			
			HashSet<string> cultures = new HashSet<string> ();
			foreach (ProjectFile finfo in Files) {
				if (finfo.Subtype == Subtype.Directory || finfo.BuildAction != BuildAction.EmbeddedResource)
					continue;

				string culture = GetResourceCulture (finfo.Name);
				if (culture != null && cultures.Add (culture)) {
					cultures.Add (culture);
					FilePath path = outputDir.Combine (culture, satelliteAsmName);
					list.Add (path);
				}
			}
		}
		
		[ThreadStatic]
		static int supportReferDistance;
		[ThreadStatic]
		static HashSet<DotNetProject> processedProjects;

		internal protected override void PopulateSupportFileList (FileCopySet list, ConfigurationSelector configuration)
		{
			try {
				if (supportReferDistance == 0)
					processedProjects = new HashSet<DotNetProject> ();
				supportReferDistance++;

				PopulateSupportFileListInternal (list, configuration);
			} finally {
				supportReferDistance--;
				if (supportReferDistance == 0)
					processedProjects = null;
			}
		}

		void PopulateSupportFileListInternal (FileCopySet list, ConfigurationSelector configuration)
		{
			if (supportReferDistance <= 2)
				base.PopulateSupportFileList (list, configuration);

			//rename the app.config file
			list.Remove ("app.config");
			list.Remove ("App.config");
			
			ProjectFile appConfig = Files.FirstOrDefault (f => f.FilePath.FileName.Equals ("app.config", StringComparison.CurrentCultureIgnoreCase));
			if (appConfig != null) {
				string output = GetOutputFileName (configuration).FileName;
				list.Add (appConfig.FilePath, true, output + ".config");
			}
			
			//collect all the "local copy" references and their attendant files
			foreach (ProjectReference projectReference in References) {
				if (!projectReference.LocalCopy || !projectReference.CanSetLocalCopy)
					continue;

				if (ParentSolution != null && projectReference.ReferenceType == ReferenceType.Project) {
					DotNetProject p = ParentSolution.FindProjectByName (projectReference.Reference) as DotNetProject;

					if (p == null) {
						LoggingService.LogWarning ("Project '{0}' referenced from '{1}' could not be found", projectReference.Reference, this.Name);
						continue;
					}
					DotNetProjectConfiguration conf = p.GetConfiguration (configuration) as DotNetProjectConfiguration;
					//VS COMPAT: recursively copy references's "local copy" files
					//but only copy the "copy to output" files from the immediate references
					if (processedProjects.Add (p) || supportReferDistance == 1) {
						foreach (var v in p.GetOutputFiles (configuration))
							list.Add (v, true, v.CanonicalPath.ToString ().Substring (conf.OutputDirectory.CanonicalPath.ToString ().Length + 1));

						foreach (var v in p.GetSupportFileList (configuration))
							list.Add (v.Src, v.CopyOnlyIfNewer, v.Target);
					}
				}
				else if (projectReference.ReferenceType == ReferenceType.Assembly) {
					// VS COMPAT: Copy the assembly, but also all other assemblies referenced by it
					// that are located in the same folder
					foreach (string file in GetAssemblyRefsRec (projectReference.Reference, new HashSet<string> ())) {
						// Indirectly referenced assemblies are only copied if a newer copy doesn't exist. This avoids overwritting directly referenced assemblies
						// by indirectly referenced stale copies of the same assembly. See bug #655566.
						bool copyIfNewer = file != projectReference.Reference;
						list.Add (file, copyIfNewer);
						if (File.Exists (file + ".config"))
							list.Add (file + ".config", copyIfNewer);
						string mdbFile = TargetRuntime.GetAssemblyDebugInfoFile (file);
						if (File.Exists (mdbFile))
							list.Add (mdbFile, copyIfNewer);
					}
				}
				else if (projectReference.ReferenceType == ReferenceType.Custom) {
					foreach (string refFile in projectReference.GetReferencedFileNames (configuration))
						list.Add (refFile);
				}
			}
		}
		
		//Given a filename like foo.it.resx, get 'it', if its
		//a valid culture
		//Note: hand-written as this can get called lotsa times
		//Note: code duplicated in prj2make/Utils.cs as TrySplitResourceName
		internal static string GetResourceCulture (string fname)
		{
			int last_dot = -1;
			int culture_dot = -1;
			int i = fname.Length - 1;
			while (i >= 0) {
				if (fname [i] == '.') {
					last_dot = i;
					break;
				}
				i --;
			}
			if (i < 0)
				return null;

			i--;
			while (i >= 0) {
				if (fname [i] == '.') {
					culture_dot = i;
					break;
				}
				i --;
			}
			if (culture_dot < 0)
				return null;

			string culture = fname.Substring (culture_dot + 1, last_dot - culture_dot - 1);
			if (!CultureNamesTable.ContainsKey (culture))
				return null;

			return culture;
		}

		static Dictionary<string, string> cultureNamesTable;
		static Dictionary<string, string> CultureNamesTable {
			get {
				if (cultureNamesTable == null) {
					cultureNamesTable = new Dictionary<string, string> ();
					foreach (CultureInfo ci in CultureInfo.GetCultures (CultureTypes.AllCultures))
						cultureNamesTable [ci.Name] = ci.Name;
				}

				return cultureNamesTable;
			}
		}
		
		IEnumerable<string> GetAssemblyRefsRec (string fileName, HashSet<string> visited)
		{
			// Recursivelly finds assemblies referenced by the given assembly
			
			if (!visited.Add (fileName))
				yield break;
			
			if (!File.Exists (fileName)) {
				string ext = Path.GetExtension (fileName).ToLower ();
				if (ext == ".dll" || ext == ".exe")
					yield break;
				if (File.Exists (fileName + ".dll"))
					fileName = fileName + ".dll";
				else if (File.Exists (fileName + ".exe"))
					fileName = fileName + ".exe";
				else
					yield break;
			}
			
			yield return fileName;

			foreach (var reference in SystemAssemblyService.GetAssemblyReferences (fileName)) {
				string asmFile = Path.Combine (Path.GetDirectoryName (fileName), reference);
				foreach (string refa in GetAssemblyRefsRec (asmFile, visited))
					yield return refa;
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

		public override IEnumerable<SolutionItem> GetReferencedItems (ConfigurationSelector configuration)
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

		/// <summary>
		/// Returns all assemblies referenced by this project, including assemblies generated
		/// by referenced projects.
		/// </summary>
		/// <param name="configuration">
		/// Configuration for which to get the assemblies.
		/// </param>
		public IEnumerable<string> GetReferencedAssemblies (ConfigurationSelector configuration)
		{
			return GetReferencedAssemblies (configuration, true);
		}

		/// <summary>
		/// Returns all assemblies referenced by this project.
		/// </summary>
		/// <param name="configuration">
		/// Configuration for which to get the assemblies.
		/// </param>
		/// <param name="includeProjectReferences">
		/// When set to true, it will include assemblies generated by referenced project. When set to false,
		/// it will only include package and direct assembly references.
		/// </param>
		public virtual IEnumerable<string> GetReferencedAssemblies (ConfigurationSelector configuration, bool includeProjectReferences)
		{
			IAssemblyReferenceHandler handler = this.ItemHandler as IAssemblyReferenceHandler;
			if (handler != null) {
				if (includeProjectReferences) {
					foreach (ProjectReference pref in References.Where (pr => pr.ReferenceType == ReferenceType.Project)) {
						foreach (string asm in pref.GetReferencedFileNames (configuration))
							yield return asm;
					}
				}
				foreach (string file in handler.GetAssemblyReferences (configuration))
					yield return file;
			}
			else {
				foreach (ProjectReference pref in References) {
					if (includeProjectReferences || pref.ReferenceType != ReferenceType.Project) {
						foreach (string asm in pref.GetReferencedFileNames (configuration))
							yield return asm;
					}
				}
			}
		}

		protected internal override void OnSave (IProgressMonitor monitor)
		{
			// Make sure the fx version is sorted out before saving
			// to avoid changes in project references while saving 
			if (targetFramework == null)
				targetFramework = Runtime.SystemAssemblyService.GetTargetFramework (GetDefaultTargetFrameworkForFormat (FileFormat));
			base.OnSave (monitor);
		}

		IDotNetLanguageBinding FindLanguage (string name)
		{
			IDotNetLanguageBinding binding = LanguageBindingService.GetBindingPerLanguageName (languageName) as IDotNetLanguageBinding;
			return binding;
		}

		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			DotNetProjectConfiguration conf = new DotNetProjectConfiguration (name);
			string dir;
			if (conf.Platform.Length == 0)
				dir = Path.Combine ("bin", conf.Name);
			else
				dir = Path.Combine (Path.Combine ("bin", conf.Platform), conf.Name);

			conf.OutputDirectory = String.IsNullOrEmpty (BaseDirectory) ? dir : Path.Combine (BaseDirectory, dir);
			conf.OutputAssembly = Name;
			if (LanguageBinding != null) {
				XmlElement xconf = null;
				if (!string.IsNullOrEmpty (conf.Platform)) {
					XmlDocument doc = new XmlDocument ();
					xconf = doc.CreateElement ("Options");
					xconf.SetAttribute ("Platform", conf.Platform);
				}
				conf.CompilationParameters = LanguageBinding.CreateCompilationParameters (xconf);
			}
			return conf;
		}


		public override FilePath GetOutputFileName (ConfigurationSelector configuration)
		{
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration)GetConfiguration (configuration);
			if (conf != null)
				return conf.CompiledOutputName;
			else
				return null;
		}

		protected override bool CheckNeedsBuild (ConfigurationSelector configuration)
		{
			if (base.CheckNeedsBuild (configuration))
				return true;
			
			// base.CheckNeedsBuild() checks Project references, but not Assembly, Package, or Custom.
			DateTime mtime = GetLastBuildTime (configuration);
			foreach (ProjectReference pref in References) {
				switch (pref.ReferenceType) {
				case ReferenceType.Assembly:
					foreach (var file in GetAssemblyRefsRec (pref.Reference, new HashSet<string> ())) {
						try {
							if (File.GetLastWriteTime (file) > mtime)
								return true;
						} catch (IOException) {
							// Ignore.
						}
					}
					break;
				case ReferenceType.Package:
					if (pref.Package == null) {
						break;
					}
					foreach (var assembly in pref.Package.Assemblies) {
						try {
							if (File.GetLastWriteTime (assembly.Location) > mtime)
								return true;
						} catch (IOException) {
							// Ignore.
						}
					}
					break;
				}
			}

			var config = (DotNetProjectConfiguration) GetConfiguration (configuration);
			return Files.Any (file => file.BuildAction == BuildAction.EmbeddedResource
					&& String.Compare (Path.GetExtension (file.FilePath), ".resx", StringComparison.OrdinalIgnoreCase) == 0
					&& MD1DotNetProjectHandler.IsResgenRequired (file.FilePath, config.IntermediateOutputDirectory.Combine (file.ResourceId)));
		}
		
		protected internal override DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			var outputBuildTime = base.OnGetLastBuildTime (configuration);
			
			//if the debug file is newer than the output file, use that as the build time
			var conf = (DotNetProjectConfiguration) GetConfiguration (configuration);
			if (GeneratesDebugInfoFile && conf != null && conf.DebugMode) {
				string file = GetOutputFileName (configuration);
				if (file != null) {
					file = TargetRuntime.GetAssemblyDebugInfoFile (file);
					var finfo = new FileInfo (file);
					if (finfo.Exists)  {
						var debugFileBuildTime = finfo.LastWriteTime;
						if (debugFileBuildTime > outputBuildTime)
							return debugFileBuildTime;
					}
				}
			}
			return outputBuildTime;
		}
		
		public IList<string> GetUserAssemblyPaths (ConfigurationSelector configuration)
		{
			if (ParentSolution == null)
				return null;
			//return all projects in the sln in case some are loaded dynamically
			//FIXME: should we do this for the whole workspace?
			return ParentSolution.GetAllProjects ().OfType<DotNetProject> ()
				.Select (d => (string) d.GetOutputFileName (configuration))
				.Where (d => !string.IsNullOrEmpty (d)).ToList ();
		}

		protected virtual ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration)
		{
			DotNetExecutionCommand cmd = new DotNetExecutionCommand (configuration.CompiledOutputName);
			cmd.Arguments = configuration.CommandLineParameters;
			cmd.WorkingDirectory = Path.GetDirectoryName (configuration.CompiledOutputName);
			cmd.EnvironmentVariables = configuration.GetParsedEnvironmentVariables ();
			cmd.TargetRuntime = TargetRuntime;
			cmd.UserAssemblyPaths = GetUserAssemblyPaths (configSel);
			return cmd;
		}

		protected internal override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			DotNetProjectConfiguration config = (DotNetProjectConfiguration) GetConfiguration (configuration);
			if (config == null)
				return false;
			ExecutionCommand cmd = CreateExecutionCommand (configuration, config);
			if (context.ExecutionTarget != null)
				cmd.Target = context.ExecutionTarget;

			return (compileTarget == CompileTarget.Exe || compileTarget == CompileTarget.WinExe) && context.ExecutionHandler.CanExecute (cmd);
		}

		protected internal override List<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> col = base.OnGetItemFiles (includeReferencedFiles);
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


		public override bool IsCompileable (string fileName)
		{
			if (LanguageBinding == null)
				return false;
			return LanguageBinding.IsSourceCodeFile (fileName);
		}
		
		/// <summary>
		/// Gets the default namespace for the file, according to the naming policy.
		/// </summary>
		/// <remarks>Always returns a valid namespace, even if the fileName is null.</remarks>
		public virtual string GetDefaultNamespace (string fileName)
		{
			DotNetNamingPolicy pol = Policies.Get<DotNetNamingPolicy> ();

			string root = null;
			string dirNamespc = null;
			string defaultNmspc = !string.IsNullOrEmpty (DefaultNamespace)
				? DefaultNamespace
				: SanitisePotentialNamespace (Name) ?? "Application";
			
			if (string.IsNullOrEmpty (fileName)) {
				return defaultNmspc;
			}

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
						//use the last component only
						dirNamespc = SanitisePotentialNamespace (Path.GetFileName (relativeDirname));
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
				if (char.IsLetter (c) || c == '_' || (sb.Length > 0 && (char.IsLetterOrDigit (sb[sb.Length - 1]) || sb[sb.Length - 1] == '_') && (c == '.' || char.IsNumber (c)))) {
					sb.Append (c);
				}
			}
			if (sb.Length > 0)
				return sb.ToString ();
			else
				return null;
		}

		void RuntimeSystemAssemblyServiceDefaultRuntimeChanged (object sender, EventArgs e)
		{
			if (composedAssemblyContext != null) {
				composedAssemblyContext.Replace (currentRuntimeContext, TargetRuntime.AssemblyContext);
				currentRuntimeContext = TargetRuntime.AssemblyContext;
			}
			UpdateSystemReferences ();
		}

		// Make sure that the project references are valid for the target clr version.
		void UpdateSystemReferences ()
		{
			foreach (ProjectReference pref in References) {
				if (pref.ReferenceType == ReferenceType.Package) {
					string newRef = AssemblyContext.GetAssemblyNameForVersion (pref.Reference, pref.Package != null ? pref.Package.Name : null, this.TargetFramework);
					if (newRef == null) {
						pref.ResetReference ();
					} else if (newRef != pref.Reference) {
						pref.Reference = newRef;
					} else if (!pref.IsValid) {
						pref.ResetReference ();
					}
				}
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
			// The resource handler policy may have changed after loading, so reset any
			// previously allocated resource handler
			resourceHandler = null;

			// Just after loading, the resource Ids are using the file format's policy.
			// They have to be converted to the new policy
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
				foreach (ProjectFile file in Files.Where (f => f.BuildAction == BuildAction.EmbeddedResource)) {
					if (file.Subtype == Subtype.Directory)
						continue;
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

		protected internal override void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsAdded (objs);
			foreach (var pref in objs.OfType<ProjectReference> ()) {
				pref.SetOwnerProject (this);
				NotifyReferenceAddedToProject (pref);
			}
		}

		protected internal override void OnItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsRemoved (objs);
			foreach (var pref in objs.OfType<ProjectReference> ()) {
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


		private void OnFileRemoved (Object o, FileEventArgs e)
		{
			foreach (FileEventInfo ei in e)
				CheckReferenceChange (ei.FileName);
		}

		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			DotNetProjectConfiguration dotNetProjectConfig = GetConfiguration (configuration) as DotNetProjectConfiguration;
			monitor.Log.WriteLine (GettextCatalog.GetString ("Running {0} ...", dotNetProjectConfig.CompiledOutputName));

			IConsole console = dotNetProjectConfig.ExternalConsole
				? context.ExternalConsoleFactory.CreateConsole (!dotNetProjectConfig.PauseConsoleOutput)
				: context.ConsoleFactory.CreateConsole (!dotNetProjectConfig.PauseConsoleOutput);
			
			AggregatedOperationMonitor aggregatedOperationMonitor = new AggregatedOperationMonitor (monitor);

			try {
				try {
					ExecutionCommand executionCommand = CreateExecutionCommand (configuration, dotNetProjectConfig);
					if (context.ExecutionTarget != null)
						executionCommand.Target = context.ExecutionTarget;

					if (!context.ExecutionHandler.CanExecute (executionCommand)) {
						monitor.ReportError (GettextCatalog.GetString ("Can not execute \"{0}\". The selected execution mode is not supported for .NET projects.", dotNetProjectConfig.CompiledOutputName), null);
						return;
					}

					IProcessAsyncOperation asyncOp = context.ExecutionHandler.Execute (executionCommand, console);
					aggregatedOperationMonitor.AddOperation (asyncOp);
					asyncOp.WaitForCompleted ();

					monitor.Log.WriteLine (GettextCatalog.GetString ("The application exited with code: {0}", asyncOp.ExitCode));
				} finally {
					console.Dispose ();
					aggregatedOperationMonitor.Dispose ();
				}
			} catch (Exception ex) {
				LoggingService.LogError (string.Format ("Cannot execute \"{0}\"", dotNetProjectConfig.CompiledOutputName), ex);
				monitor.ReportError (GettextCatalog.GetString ("Cannot execute \"{0}\"", dotNetProjectConfig.CompiledOutputName), ex);
			}
		}
	}
}
