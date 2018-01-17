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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Projects.MD1;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Core.Assemblies;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Immutable;
using MonoDevelop.Projects.MSBuild.Conditions;

namespace MonoDevelop.Projects
{
	public abstract class DotNetProject : Project, IAssemblyProject, IDotNetFileContainer
	{
		bool usePartialTypes = true;

		DirectoryAssemblyContext privateAssemblyContext;
		ComposedAssemblyContext composedAssemblyContext;
		IAssemblyContext currentRuntimeContext;

		CompileTarget compileTarget;

		LanguageBinding languageBinding;

		protected ProjectReferenceCollection projectReferences;

		protected string defaultNamespace = String.Empty;

		DotNetProjectFlags flags;

		protected DotNetProject ()
		{
			Initialize (this);
		}

		protected DotNetProject (string languageName, params string[] flavorIds): base (flavorIds)
		{
			this.languageName = languageName;
			Initialize (this);
		}

		public static DotNetProject CreateProject (string language, params string[] typeGuids)
		{
			string typeGuid = MSBuildProjectService.GetLanguageGuid (language);
			return (DotNetProject) MSBuildProjectService.CreateProject (typeGuid, typeGuids);
		}

		protected override void OnInitialize ()
		{
			projectReferences = new ProjectReferenceCollection ();
			Items.Bind (projectReferences);
			FileService.FileRemoved += OnFileRemoved;
			Runtime.SystemAssemblyService.DefaultRuntimeChanged += RuntimeSystemAssemblyServiceDefaultRuntimeChanged;

			base.OnInitialize ();

			if (languageName == null)
				languageName = MSBuildProjectService.GetLanguageFromGuid (TypeGuid);
		}

		protected override void OnExtensionChainInitialized ()
		{
			projectExtension = ExtensionChain.GetExtension<DotNetProjectExtension> ();
			base.OnExtensionChainInitialized ();

			if (IsLibraryBasedProjectType)
				CompileTarget = CompileTarget.Library;

			flags = ProjectExtension.OnGetDotNetProjectFlags ();
			usePartialTypes = SupportsPartialTypes;
		}

		protected override void OnSetShared ()
		{
			base.OnSetShared ();
			projectReferences.SetShared ();
		}

		protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement projectOptions)
		{
			base.OnInitializeFromTemplate (projectCreateInfo, projectOptions);

			if ((projectOptions != null) && (projectOptions.Attributes ["Target"] != null))
				CompileTarget = (CompileTarget)Enum.Parse (typeof(CompileTarget), projectOptions.Attributes ["Target"].Value);
			else if (IsLibraryBasedProjectType)
				CompileTarget = CompileTarget.Library;

			if (this.LanguageBinding != null) {

				bool externalConsole = false;

				string platform = null;
				if (!projectOptions.HasAttribute ("Platform")) {
					// Clone the element since we are going to change it
					platform = GetDefaultTargetPlatform (projectCreateInfo);
					projectOptions = (XmlElement)projectOptions.CloneNode (true);
					projectOptions.SetAttribute ("Platform", platform);
				} else
					platform = projectOptions.GetAttribute ("Platform");
				
				if (projectOptions.GetAttribute ("ExternalConsole") == "True")
					externalConsole = true;

				string platformSuffix = string.IsNullOrEmpty (platform) ? string.Empty : "|" + platform;
				DotNetProjectConfiguration configDebug = CreateConfiguration ("Debug" + platformSuffix, ConfigurationKind.Debug) as DotNetProjectConfiguration;
				DefineSymbols (configDebug.CompilationParameters, projectOptions, "DefineConstantsDebug");
				configDebug.ExternalConsole = externalConsole;
				Configurations.Add (configDebug);

				DotNetProjectConfiguration configRelease = CreateConfiguration ("Release" + platformSuffix, ConfigurationKind.Release) as DotNetProjectConfiguration;
				DefineSymbols (configRelease.CompilationParameters, projectOptions, "DefineConstantsRelease");
				configRelease.CompilationParameters.RemoveDefineSymbol ("DEBUG");
				configRelease.ExternalConsole = externalConsole;
				Configurations.Add (configRelease);
			}

			targetFramework = GetTargetFrameworkForNewProject (projectOptions, GetDefaultTargetFrameworkId ());

			string binPath;
			if (projectCreateInfo != null) {
				Name = projectCreateInfo.ProjectName;
				binPath = projectCreateInfo.BinPath;
				string templateDefaultNamespace = GetDefaultNamespace (projectCreateInfo, projectOptions);
				defaultNamespace = SanitisePotentialNamespace (templateDefaultNamespace ?? projectCreateInfo.ProjectName);
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

		static string GetDefaultNamespace (ProjectCreateInformation projectCreateInfo, XmlElement projectOptions)
		{
			string defaultNamespace = projectOptions.Attributes["DefaultNamespace"]?.Value;
			if (defaultNamespace != null)
				return StringParserService.Parse (defaultNamespace, projectCreateInfo.Parameters);

			return null;
		}

		void DefineSymbols (DotNetCompilerParameters pars, XmlElement projectOptions, string attributeName)
		{
			if (projectOptions != null) {
				string symbols = projectOptions.GetAttribute (attributeName);
				if (!String.IsNullOrEmpty (symbols)) {
					pars.AddDefineSymbol (symbols);
				}
			}
		}

		TargetFramework GetTargetFrameworkForNewProject (XmlElement projectOptions, TargetFrameworkMoniker defaultMoniker)
		{
			if (projectOptions == null)
				return Runtime.SystemAssemblyService.GetTargetFramework (defaultMoniker);

			var att = projectOptions.Attributes ["TargetFrameworkVersion"];
			if (att == null) {
				att = projectOptions.Attributes ["TargetFramework"];
				if (att == null)
					return Runtime.SystemAssemblyService.GetTargetFramework (defaultMoniker);
			}

			var moniker = TargetFrameworkMoniker.Parse (att.Value);

			//if the string did not include a framework identifier, use the project's default
			var netID = TargetFrameworkMoniker.ID_NET_FRAMEWORK;
			if (moniker.Identifier == netID && !att.Value.StartsWith (netID, StringComparison.Ordinal))
				moniker = new TargetFrameworkMoniker (defaultMoniker.Identifier, moniker.Version, moniker.Profile);

			return Runtime.SystemAssemblyService.GetTargetFramework (moniker);
		}

		protected override void OnGetTypeTags (HashSet<string> types)
		{
			base.OnGetTypeTags (types);
			types.Add ("DotNet");
			types.Add ("DotNetAssembly");
		}

		DotNetProjectExtension projectExtension;
		DotNetProjectExtension ProjectExtension {
			get {
				if (projectExtension == null)
					AssertExtensionChainCreated ();
				return projectExtension;
			}
		}

		protected override IEnumerable<WorkspaceObjectExtension> CreateDefaultExtensions ()
		{
			return base.CreateDefaultExtensions ().Concat (Enumerable.Repeat (new DefaultDotNetProjectExtension (), 1));
		}

		protected override ProjectItem OnCreateProjectItem (IMSBuildItemEvaluated item)
		{
			if (item.Name == "Reference" || item.Name == "ProjectReference")
				return new ProjectReference ();

			return base.OnCreateProjectItem (item);
		}

		private string languageName;
		public string LanguageName {
			get { return languageName; }
		}

		protected override string[] OnGetSupportedLanguages ()
		{
			return new [] { "", languageName };
		}

		public bool IsLibraryBasedProjectType {
			get { return (flags & DotNetProjectFlags.IsLibrary) != 0; }
		}

		public virtual bool IsPortableLibrary {
			get { return GetService<PortableDotNetProjectFlavor> () != null; }
		}

		public bool GeneratesDebugInfoFile {
			get { return (flags & DotNetProjectFlags.GeneratesDebugInfoFile) != 0; }
		}

		public bool SupportsRoslyn { get; protected set; }

		protected virtual DotNetProjectFlags OnGetDotNetProjectFlags ()
		{
			return DotNetProjectFlags.GeneratesDebugInfoFile;
		}

		protected string GetDefaultTargetPlatform (ProjectCreateInformation projectCreateInfo)
		{
			return ProjectExtension.OnGetDefaultTargetPlatform (projectCreateInfo);
		}

		protected virtual string OnGetDefaultTargetPlatform (ProjectCreateInformation projectCreateInfo)
		{
			if (CompileTarget == CompileTarget.Library)
				return string.Empty;

			// Guess a good default platform for the project
			if (projectCreateInfo.ParentFolder != null && projectCreateInfo.ParentFolder.ParentSolution != null) {
				ItemConfiguration conf = projectCreateInfo.ParentFolder.ParentSolution.GetConfiguration (projectCreateInfo.ActiveConfiguration);
				if (conf != null)
					return conf.Platform;
				else {
					string curName, curPlatform, bestPlatform = null;
					string sconf = projectCreateInfo.ActiveConfiguration.ToString ();
					ItemConfiguration.ParseConfigurationId (sconf, out curName, out curPlatform);
					foreach (ItemConfiguration ic in projectCreateInfo.ParentFolder.ParentSolution.Configurations) {
						if (ic.Platform == curPlatform)
							return curPlatform;
						if (ic.Name == curName)
							bestPlatform = ic.Platform;
					}
					if (bestPlatform != null)
						return bestPlatform;
				}
			}
			return Services.ProjectService.DefaultPlatformTarget;
		}

		public ProjectReferenceCollection References {
			get { return projectReferences; }
		}

		/// <summary>
		/// Checks the status of references. To be called when referenced files may have been deleted or created.
		/// </summary>
		public void RefreshReferenceStatus ()
		{
			for (int n = 0; n < References.Count; n++) {
				var cp = References [n].GetRefreshedReference ();
				if (cp != null)
					References [n] = cp;
			}
		}

		public bool CanReferenceProject (DotNetProject targetProject, out string reason)
		{
			return ProjectExtension.OnGetCanReferenceProject (targetProject, out reason);
		}
		bool CheckCanReferenceProject (DotNetProject targetProject, out string reason)
		{
			if (!TargetFramework.CanReferenceAssembliesTargetingFramework (targetProject.TargetFramework)) {
				reason = GettextCatalog.GetString ("Incompatible target framework: {0}", targetProject.TargetFramework.Id);
				return false;
			}

			reason = null;

			return true;
		}

		public LanguageBinding LanguageBinding {
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
			if (DefaultNamespaceIsImplicit) {
				if (DefaultNamespace.Length > 0 && ns.StartsWith (DefaultNamespace + "."))
					return ns.Substring (DefaultNamespace.Length + 1);
				else if (DefaultNamespace == ns)
					return string.Empty;
			}
			return ns;
		}

		public bool DefaultNamespaceIsImplicit { get; set; }

		TargetFramework targetFramework;

		public TargetFramework TargetFramework {
			get {
				if (targetFramework == null) {
					var id = GetDefaultTargetFrameworkId ();
					targetFramework = Runtime.SystemAssemblyService.GetTargetFramework (id);
				}
				return targetFramework;
			}
			set {
				if (!SupportsFramework (value))
					throw new ArgumentException ("Project does not support framework '" + value.Id.ToString () +"'");
				if (value == null)
					value = Runtime.SystemAssemblyService.GetTargetFramework (GetDefaultTargetFrameworkForFormat (ToolsVersion));
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
		public TargetFrameworkMoniker GetDefaultTargetFrameworkId ()
		{
			return ProjectExtension.OnGetDefaultTargetFrameworkId ();
		}

		protected virtual TargetFrameworkMoniker OnGetDefaultTargetFrameworkId ()
		{
			return Services.ProjectService.DefaultTargetFramework.Id;
		}

		/// <summary>
		/// Returns the default framework for a given format
		/// </summary>
		/// <returns>
		/// The default target framework for the format.
		/// </returns>
		/// <param name='toolsVersion'>
		/// MSBuild tools version for which to get the default format
		/// </param>
		/// <remarks>
		/// This method is used to determine what's the correct target framework for a project
		/// deserialized using a specific format.
		/// </remarks>
		public TargetFrameworkMoniker GetDefaultTargetFrameworkForFormat (string toolsVersion)
		{
			return ProjectExtension.OnGetDefaultTargetFrameworkForFormat (toolsVersion);
		}

		protected virtual TargetFrameworkMoniker OnGetDefaultTargetFrameworkForFormat (string toolsVersion)
		{
			// If GetDefaultTargetFrameworkId has been overriden to return something different than the
			// default framework, but OnGetDefaultTargetFrameworkForFormat has not been overriden, then
			// the framework most likely to be correct is the one returned by GetDefaultTargetFrameworkId.

			var fxid = GetDefaultTargetFrameworkId ();
			if (fxid == Services.ProjectService.DefaultTargetFramework.Id) {
				switch (toolsVersion) {
				case "2.0":
					return TargetFrameworkMoniker.NET_2_0;
				case "4.0":
					return TargetFrameworkMoniker.NET_4_0;
				}
			}
			return fxid;
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

		public bool SupportsFramework (TargetFramework framework)
		{
			return ProjectExtension.OnGetSupportsFramework (framework);
		}

		protected virtual bool OnSupportsFramework (TargetFramework framework)
		{
			// DotNetAssemblyProject can only generate assemblies for the regular framework.
			// Special frameworks such as Moonlight or MonoTouch must override SupportsFramework.
			if (!framework.CanReferenceAssembliesTargetingFramework (TargetFrameworkMoniker.NET_1_1))
				return false;
			if (LanguageBinding == null)
				return false;
			ClrVersion[] versions = OnGetSupportedClrVersions ();
			if (versions != null && versions.Length > 0 && framework != null) {
				foreach (ClrVersion v in versions) {
#pragma warning disable CS0618 // Type or member is obsolete
					if (v == framework.ClrVersion)
#pragma warning restore CS0618 // Type or member is obsolete
						return true;
				}
			}
			return false;
		}

		public bool UsePartialTypes {
			get { return usePartialTypes; }
			set { usePartialTypes = value; }
		}

		protected override void OnDispose ()
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

			base.OnDispose ();
		}

		public bool SupportsPartialTypes {
			get { return LanguageBinding.SupportsPartialTypes; }
		}

		void CheckReferenceChange (FilePath updatedFile)
		{
			for (int n=0; n<References.Count; n++) {
				ProjectReference pr = References [n];
				if (pr.ReferenceType == ReferenceType.Assembly && DefaultConfiguration != null) {
					if (pr.GetReferencedFileNames (DefaultConfiguration.Selector).Any (f => f == updatedFile)) {
						SetFastBuildCheckDirty ();
						pr.NotifyStatusChanged ();
					}
				} else if (pr.HintPath == updatedFile) {
					SetFastBuildCheckDirty ();
					var nr = pr.GetRefreshedReference ();
					if (nr != null)
						References [n] = nr;
				}
			}

			// If a referenced assembly changes, dirtify the project.
/*			if (DefaultConfiguration != null) {
				foreach (var asm in await GetReferencedAssemblies (DefaultConfiguration.Selector))
					if (asm == updatedFile) {
						SetFastBuildCheckDirty ();
						break;
					}
			}
			Removed for now since it can be a very slow operation
			*/
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
			List<ProjectReference> toBeRenamed = new List<ProjectReference> ();

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
			if (conf == null)
				return;

			// Debug info file

			if (conf.DebugSymbols) {
				string mdbFile = GetAssemblyDebugInfoFile (conf.Selector, conf.CompiledOutputName);
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
					DotNetProject p = projectReference.ResolveProject (ParentSolution) as DotNetProject;

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
					var visitedAssemblies = new HashSet<string> ();
					var referencedFiles = projectReference.GetReferencedFileNames (configuration);
					foreach (string file in referencedFiles.SelectMany (ar => GetAssemblyRefsRec (ar, visitedAssemblies))) {
						// Indirectly referenced assemblies are only copied if a newer copy doesn't exist. This avoids overwritting directly referenced assemblies
						// by indirectly referenced stale copies of the same assembly. See bug #655566.
						bool copyIfNewer = !referencedFiles.Contains (file);
						list.Add (file, copyIfNewer);
						if (File.Exists (file + ".config"))
							list.Add (file + ".config", copyIfNewer);
						string debugFile = file + ".mdb";
						if (File.Exists (debugFile))
							list.Add (debugFile, copyIfNewer);
						debugFile = Path.ChangeExtension (file, ".pdb");
						if (File.Exists (debugFile))
							list.Add (debugFile, copyIfNewer);
					}
				}
				else {
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
				string ext = Path.GetExtension (fileName);
				if (string.Equals (ext, ".dll", StringComparison.OrdinalIgnoreCase) || string.Equals (ext, ".exe", StringComparison.OrdinalIgnoreCase))
					yield break;
				string dllFileName = fileName + ".dll";
				if (File.Exists (dllFileName))
					fileName = dllFileName;
				else {
					string exeFileName = fileName + ".exe";
					if (File.Exists (exeFileName))
						fileName = exeFileName;
					else
						yield break;
				}
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
			ProjectReference newReferenceInformation = ProjectReference.CreateAssemblyFileReference (filename);
			References.Add (newReferenceInformation);
			return newReferenceInformation;
		}

		protected override IEnumerable<SolutionItem> OnGetReferencedItems (ConfigurationSelector configuration)
		{
			var items = new List<SolutionItem> (base.OnGetReferencedItems (configuration));
			if (ParentSolution == null)
				return items;

			var ctx = new ProjectParserContext (this, (DotNetProjectConfiguration)GetConfiguration (configuration));
			foreach (ProjectReference pref in References) {
				if (pref.ReferenceType == ReferenceType.Project &&
				    (string.IsNullOrEmpty (pref.Condition) || ConditionParser.ParseAndEvaluate (pref.Condition, ctx))) {
					Project rp = pref.ResolveProject (ParentSolution);
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
		public Task<IEnumerable<AssemblyReference>> GetReferencedAssemblies (ConfigurationSelector configuration)
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
		public Task<IEnumerable<AssemblyReference>> GetReferencedAssemblies (ConfigurationSelector configuration, bool includeProjectReferences)
		{
			return BindTask<IEnumerable<AssemblyReference>> (async ct => {
				var res = await ProjectExtension.OnGetReferencedAssemblies (configuration);
				
				if (includeProjectReferences) {
					foreach (ProjectReference pref in References.Where (pr => pr.ReferenceType == ReferenceType.Project)) {
						foreach (var asm in pref.GetReferencedFileNames (configuration))
							res.Add (CreateProjectAssemblyReference (asm, pref));
					}
				}
				return res;
			});
		}

		public Task<List<AssemblyReference>> GetReferences (ConfigurationSelector configuration)
		{
			return BindTask (async ct => {
				return await ProjectExtension.OnGetReferences (configuration, ct);
			});
		}

		public Task<List<AssemblyReference>> GetReferences (ConfigurationSelector configuration, CancellationToken token)
		{
			return BindTask (ct => {
				var tokenSource = CancellationTokenSource.CreateLinkedTokenSource (ct, token);
				return ProjectExtension.OnGetReferences (configuration, tokenSource.Token);
			});
		}

		/// <summary>
		/// Gets the referenced assembly projects, but only projects which output are actually referenced
		/// for example references with ReferenceOutputAssembly=false are excluded
		/// </summary>
		/// <param name="configuration">Configuration.</param>
		public IEnumerable<DotNetProject> GetReferencedAssemblyProjects (ConfigurationSelector configuration)
		{
			return ProjectExtension.OnGetReferencedAssemblyProjects (configuration);
		}

		internal protected virtual async Task<List<AssemblyReference>> OnGetReferencedAssemblies (ConfigurationSelector configuration)
		{
			List<AssemblyReference> result = new List<AssemblyReference> ();
			if (CheckUseMSBuildEngine (configuration)) {
				// Get the references list from the msbuild project
				using (Counters.ResolveMSBuildReferencesTimer.BeginTiming (GetProjectEventMetadata (configuration)))
					result.AddRange (await RunResolveAssemblyReferencesTarget (configuration));
			} else {
				foreach (ProjectReference pref in References) {
					if (pref.ReferenceType != ReferenceType.Project) {
						foreach (string asm in pref.GetReferencedFileNames (configuration))
							result.Add (new AssemblyReference (asm, pref.Aliases));
					}
				}
				var mscorlib = AssemblyContext.GetAssemblyFullName ("mscorlib", TargetFramework);
				var mscorlibPath = AssemblyContext.GetAssemblyLocation (mscorlib, TargetFramework);
				if (!result.Any (ar => ar.FilePath == mscorlibPath))
					result.Add (new AssemblyReference (mscorlibPath));

				var core = AssemblyContext.GetAssemblyFullName ("System.Core", TargetFramework);
				var corePath = AssemblyContext.GetAssemblyLocation (core, TargetFramework);
				if (!string.IsNullOrEmpty (corePath)) {
					if (!result.Any (ar => ar.FilePath == corePath))
						result.Add (new AssemblyReference (corePath));
				}
			}

			var config = (DotNetProjectConfiguration)GetConfiguration (configuration);
			bool noStdLib = false;
			if (config != null)
				noStdLib = config.CompilationParameters.NoStdLib;

			// System.Core is an implicit reference
			if (!noStdLib) {
				var sa = AssemblyContext.GetAssemblies (TargetFramework).FirstOrDefault (a => a.Name == "System.Core" && a.Package.IsFrameworkPackage);
				if (sa != null) {
					var ar = new AssemblyReference (sa.Location);
					if (!result.Contains (ar))
						result.Add (ar);
				}
			}
			var addFacadeAssemblies = false;
			foreach (var r in GetReferencedAssemblyProjects (configuration)) {
				// Facade assemblies need to be referenced if this project is referencing a PCL or .NET Standard project.
				if (r.IsPortableLibrary || r.TargetFramework.Id.Identifier == ".NETStandard") {
					addFacadeAssemblies = true;
					break;
				}
			}
			if (!addFacadeAssemblies) {
				foreach (var refFilename in result) {
					string fullPath = null;
					if (!Path.IsPathRooted (refFilename.FilePath)) {
						fullPath = Path.Combine (Path.GetDirectoryName (FileName), refFilename.FilePath);
					} else {
						fullPath = Path.GetFullPath (refFilename.FilePath);
					}
					if (await SystemAssemblyService.ContainsReferenceToSystemRuntimeAsync (fullPath)) {
						addFacadeAssemblies = true;
						break;
					}
				}
			}

			if (addFacadeAssemblies) {
				var runtime = TargetRuntime ?? MonoDevelop.Core.Runtime.SystemAssemblyService.DefaultRuntime;
				var facades = runtime.FindFacadeAssembliesForPCL (TargetFramework);
				foreach (var facade in facades) {
					if (!File.Exists (facade))
						continue;
					var ar = new AssemblyReference (facade);
					if (!result.Contains (ar))
						result.Add (ar);
				}
			}
			return result;
		}

		AsyncCriticalSection referenceCacheLock = new AsyncCriticalSection ();
		ImmutableDictionary<string, List<AssemblyReference>> referenceCache = ImmutableDictionary<string, List<AssemblyReference>>.Empty;
		bool referenceCacheNeedsRefresh;

		async Task<List<AssemblyReference>> RunResolveAssemblyReferencesTarget (ConfigurationSelector configuration)
		{
			List<AssemblyReference> refs = null;
			var confId = (GetConfiguration (configuration) ?? DefaultConfiguration)?.Id ?? "";

			// Check the cache before waiting for the lock, which may be very slow
			if (!referenceCacheNeedsRefresh && referenceCache.TryGetValue (confId, out refs))
				return refs;

			using (await referenceCacheLock.EnterAsync ().ConfigureAwait (false)) {
				// Check again the cache before starting the task
				if (!referenceCacheNeedsRefresh && referenceCache.TryGetValue (confId, out refs))
					return refs;

				var monitor = new ProgressMonitor ();

				var context = new TargetEvaluationContext ();
				context.ItemsToEvaluate.Add ("ReferencePath");
				context.BuilderQueue = BuilderQueue.ShortOperations;
				context.LoadReferencedProjects = false;
				context.LogVerbosity = MSBuildVerbosity.Quiet;

				var result = await RunTarget (monitor, "ResolveAssemblyReferences", configuration, context);

				refs = result.Items.Select (i => new AssemblyReference (i.Include, i.Metadata)).ToList ();

				referenceCache = referenceCache.SetItem (confId, refs);
				referenceCacheNeedsRefresh = false;
			}
			return refs;
		}

		public Task<IEnumerable<PackageDependency>> GetPackageDependencies (ConfigurationSelector configuration, CancellationToken cancellationToken)
		{
			return BindTask<IEnumerable<PackageDependency>> (async ct => {
				var tokenSource = CancellationTokenSource.CreateLinkedTokenSource (ct, cancellationToken);
				return await OnGetPackageDependencies (configuration, tokenSource.Token);
			});
		}

		internal protected virtual async Task<List<PackageDependency>> OnGetPackageDependencies (ConfigurationSelector configuration, CancellationToken cancellationToken)
		{
			var result = new List<PackageDependency> ();
			if (CheckUseMSBuildEngine (configuration)) {
				// Get the references list from the msbuild project
				using (Counters.ResolveMSBuildReferencesTimer.BeginTiming (GetProjectEventMetadata (configuration)))
					return await RunResolvePackageDependenciesTarget (configuration, cancellationToken);
			} else
				return new List<PackageDependency> ();
		}

		ImmutableDictionary<string, List<PackageDependency>> packageDependenciesCache = ImmutableDictionary<string, List<PackageDependency>>.Empty;
		AsyncCriticalSection packageDependenciesCacheLock = new AsyncCriticalSection ();
		bool packageDependenciesNeedRefresh;

		async Task<List<PackageDependency>> RunResolvePackageDependenciesTarget (ConfigurationSelector configuration, CancellationToken cancellationToken)
		{
			List<PackageDependency> packageDependencies = null;
			var confId = (GetConfiguration (configuration) ?? DefaultConfiguration)?.Id ?? "";

			// Check the cache before entering the lock, which may be slow
			if (!packageDependenciesNeedRefresh && packageDependenciesCache.TryGetValue (confId, out packageDependencies))
				return packageDependencies;
			
			using (await packageDependenciesCacheLock.EnterAsync ().ConfigureAwait (false)) {
				// Check the cache before starting the task
				if (!packageDependenciesNeedRefresh && packageDependenciesCache.TryGetValue (confId, out packageDependencies))
					return packageDependencies;

				var monitor = new ProgressMonitor ().WithCancellationToken (cancellationToken);

				var context = new TargetEvaluationContext ();
				context.ItemsToEvaluate.Add ("_DependenciesDesignTime");
				context.BuilderQueue = BuilderQueue.ShortOperations;
				context.LoadReferencedProjects = false;
				context.LogVerbosity = MSBuildVerbosity.Quiet;

				var result = await RunTarget (monitor, "ResolvePackageDependenciesDesignTime", configuration, context);

				if (result == null)
					return new List<PackageDependency> ();

				packageDependencies = result.Items.Select (i => PackageDependency.Create (i)).Where (dependency => dependency != null).ToList ();

				packageDependenciesCache = packageDependenciesCache .SetItem (confId, packageDependencies);
				packageDependenciesNeedRefresh = false;
			}

			return packageDependencies;
		}

		internal protected virtual IEnumerable<DotNetProject> OnGetReferencedAssemblyProjects (ConfigurationSelector configuration)
		{
			if (ParentSolution == null) {
				yield break;
			}
			var ctx = new ProjectParserContext (this, (DotNetProjectConfiguration)GetConfiguration (configuration));
			foreach (ProjectReference pref in References) {
				if (pref.ReferenceType == ReferenceType.Project && pref.ReferenceOutputAssembly &&
					(string.IsNullOrEmpty (pref.Condition) || ConditionParser.ParseAndEvaluate (pref.Condition, ctx))) {
					var rp = pref.ResolveProject (ParentSolution) as DotNetProject;
					if (rp != null)
						yield return rp;
				}
			}
		}

		protected override async Task OnClearCachedData ()
		{
			// Clean the reference and package cache

			referenceCacheNeedsRefresh = true;
			packageDependenciesNeedRefresh = true;

			await base.OnClearCachedData ();
		}

		internal protected virtual async Task<List<AssemblyReference>> OnGetReferences (ConfigurationSelector configuration, CancellationToken token)
		{
			var result = await OnGetReferencedAssemblies (configuration);

			foreach (ProjectReference pref in References.Where (pr => pr.ReferenceType == ReferenceType.Project)) {
				foreach (var asm in pref.GetReferencedFileNames (configuration))
					result.Add (CreateProjectAssemblyReference (asm, pref));
			}

			return result;
		}

		/// <summary>
		/// This should be removed once the project reference information is retrieved from MSBuild.
		/// </summary>
		AssemblyReference CreateProjectAssemblyReference (string path, ProjectReference reference)
		{
			var metadata = new MSBuildPropertyGroupEvaluated (MSBuildProject);
			SetProperty (metadata, "Aliases", reference.Aliases);
			SetProperty (metadata, "CopyLocal", reference.LocalCopy.ToString ());
			SetProperty (metadata, "Project", reference.ProjectGuid);
			SetProperty (metadata, "MSBuildSourceProjectFile", GetProjectFileName (reference));
			SetProperty (metadata, "ReferenceOutputAssembly", reference.ReferenceOutputAssembly.ToString ());
			SetProperty (metadata, "ReferenceSourceTarget", "ProjectReference");

			return new AssemblyReference (path, metadata);
		}

		void SetProperty (MSBuildPropertyGroupEvaluated metadata, string name, string value)
		{
			var property = new MSBuildPropertyEvaluated (MSBuildProject, name, value, value);
			metadata.SetProperty (name, property);
		}

		static string GetProjectFileName (ProjectReference reference)
		{
			if (reference.OwnerProject?.ParentSolution == null)
				return null;

			Project project = reference.ResolveProject (reference.OwnerProject.ParentSolution);
			return project?.FileName;
		}

		protected override Task<BuildResult> DoBuild (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var handler = new MD1DotNetProjectHandler (this);
			return handler.RunTarget (monitor, "Build", configuration);
		}

		protected override Task<BuildResult> DoClean (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var handler = new MD1DotNetProjectHandler (this);
			return handler.RunTarget (monitor, "Clean", configuration);
		}

		protected internal override Task OnSave (ProgressMonitor monitor)
		{
			// Make sure the fx version is sorted out before saving
			// to avoid changes in project references while saving
			if (targetFramework == null)
				targetFramework = Runtime.SystemAssemblyService.GetTargetFramework (GetDefaultTargetFrameworkForFormat (ToolsVersion));
			return base.OnSave (monitor);
		}

		LanguageBinding FindLanguage (string name)
		{
			return LanguageBindingService.GetBindingPerLanguageName (languageName);
		}

		protected override SolutionItemConfiguration OnCreateConfiguration (string name, ConfigurationKind kind)
		{
			DotNetProjectConfiguration conf = new DotNetProjectConfiguration (name);
			string dir;
			if (conf.Platform.Length == 0)
				dir = Path.Combine ("bin", conf.Name);
			else
				dir = Path.Combine (Path.Combine ("bin", conf.Platform), conf.Name);

			conf.OutputDirectory = String.IsNullOrEmpty (BaseDirectory) ? dir : Path.Combine (BaseDirectory, dir);
			conf.OutputAssembly = Name;

			if (kind == ConfigurationKind.Debug) {
				conf.DebugSymbols = true;
				conf.DebugType = "full";
			} else {
				conf.DebugSymbols = false;
			}

			if (LanguageBinding != null)
				conf.CompilationParameters = OnCreateCompilationParameters (conf, kind);

			return conf;
		}

		protected override ProjectRunConfiguration OnCreateRunConfiguration (string name)
		{
			return new AssemblyRunConfiguration (name);
		}

		protected override FilePath OnGetOutputFileName (ConfigurationSelector configuration)
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
			if (GeneratesDebugInfoFile && conf != null && conf.DebugSymbols) {
				string file = GetOutputFileName (configuration);
				if (file != null) {
					file = GetAssemblyDebugInfoFile (configuration, file);
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

		public FilePath GetAssemblyDebugInfoFile (ConfigurationSelector configuration)
		{
			return GetAssemblyDebugInfoFile (configuration, GetOutputFileName (configuration));
		}

		public FilePath GetAssemblyDebugInfoFile (ConfigurationSelector configuration, FilePath exeFile)
		{
			if (CheckUseMSBuildEngine (configuration)) {
				var mono = TargetRuntime as MonoTargetRuntime;
				if (mono != null) {
					var version = mono.MonoRuntimeInfo?.RuntimeVersion;
					if (version == null || (version < new Version (4, 9, 0)))
						return exeFile + ".mdb";
				}
				return exeFile.ChangeExtension (".pdb");
			} else
				return exeFile + ".mdb";
		}

		public IList<string> GetUserAssemblyPaths (ConfigurationSelector configuration)
		{
			if (ParentSolution == null)
				return null;
			//return all projects in the sln in case some are loaded dynamically
			//FIXME: should we do this for the whole workspace?
			return ParentSolution.RootFolder.GetAllBuildableEntries (configuration).OfType<DotNetProject> ()
				.Select (d => (string) d.GetOutputFileName (configuration))
				.Where (d => !string.IsNullOrEmpty (d)).ToList ();
		}

		[Obsolete("Use the overload that takes a RunConfiguration")]
		public ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration)
		{
			return CreateExecutionCommand (configSel, configuration, GetDefaultRunConfiguration () as ProjectRunConfiguration);
		}

		public ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			return ProjectExtension.OnCreateExecutionCommand (configSel, configuration, runConfiguration);
		}

		internal protected virtual ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration)
		{
			DotNetExecutionCommand cmd = new DotNetExecutionCommand (configuration.CompiledOutputName);
			cmd.Arguments = configuration.CommandLineParameters;
			cmd.WorkingDirectory = Path.GetDirectoryName (configuration.CompiledOutputName);
			cmd.EnvironmentVariables = configuration.GetParsedEnvironmentVariables ();
			cmd.TargetRuntime = TargetRuntime;
			return cmd;
		}

		internal protected virtual ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			ExecutionCommand rcmd;
			var rc = runConfiguration as AssemblyRunConfiguration;
			if (rc != null && rc.StartAction == AssemblyRunConfiguration.StartActions.Program) {
				var tagModel = GetStringTagModel (configSel);
				var pcmd = Runtime.ProcessService.CreateCommand (StringParserService.Parse (rc.StartProgram, tagModel));
				pcmd.Arguments = StringParserService.Parse (rc.StartArguments, tagModel);
				pcmd.WorkingDirectory = StringParserService.Parse (rc.StartWorkingDirectory, tagModel);
				pcmd.EnvironmentVariables = StringParserService.Parse (rc.EnvironmentVariables, tagModel);
				rcmd = pcmd;
			} else {
#pragma warning disable 618 // Type or member is obsolete
				rcmd = ProjectExtension.OnCreateExecutionCommand (configSel, configuration);
#pragma warning restore 618 // Type or member is obsolete
			}

			var cmd = rcmd as DotNetExecutionCommand;
			if (cmd == null)
				return rcmd;

			if (rc != null) {
				// Don't directly overwrite the settings, since those may have been set by the OnCreateExecutionCommand
				// overload that doesn't take a runConfiguration.

				var tagModel = GetStringTagModel (configSel);

				string monoOptions;
				rc.MonoParameters.GenerateOptions (StringParserService.Parse (cmd.EnvironmentVariables, tagModel), out monoOptions);
				cmd.RuntimeArguments = monoOptions;
				if (!string.IsNullOrEmpty (rc.StartArguments))
					cmd.Arguments = StringParserService.Parse (rc.StartArguments, tagModel);
				if (!rc.StartWorkingDirectory.IsNullOrEmpty)
					cmd.WorkingDirectory = StringParserService.Parse (rc.StartWorkingDirectory, tagModel);
				if (cmd.EnvironmentVariables != rc.EnvironmentVariables) {
					foreach (var env in rc.EnvironmentVariables)
						cmd.EnvironmentVariables [env.Key] = StringParserService.Parse (env.Value, tagModel);
				}
				cmd.PauseConsoleOutput = rc.PauseConsoleOutput;
				cmd.ExternalConsole = rc.ExternalConsole;
				cmd.TargetRuntime = Runtime.SystemAssemblyService.GetTargetRuntime (rc.TargetRuntimeId);
			}
			return cmd;
		}

		protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			DotNetProjectConfiguration config = (DotNetProjectConfiguration)GetConfiguration (configuration);
			if (config == null)
				return false;
			
			var runConfig = runConfiguration as ProjectRunConfiguration;
			if (runConfig == null)
				return false;

			var asmRunConfig = runConfiguration as AssemblyRunConfiguration;
		
			ExecutionCommand executionCommand;

			if (asmRunConfig != null && asmRunConfig.StartAction == AssemblyRunConfiguration.StartActions.Program) {
				executionCommand = Runtime.ProcessService.CreateCommand (asmRunConfig.StartProgram);
				// If it is command for executing an assembly, add runtime options
				var dcmd = executionCommand as DotNetExecutionCommand;
				if (dcmd != null) {
					string monoOptions;
					asmRunConfig.MonoParameters.GenerateOptions (dcmd.EnvironmentVariables, out monoOptions);
					dcmd.RuntimeArguments = monoOptions;
				}
				// If it is command for executing a process, add arguments, work directory and env vars
				var pcmd = executionCommand as ProcessExecutionCommand;
				if (pcmd != null) {
					pcmd.Arguments = asmRunConfig.StartArguments;
					pcmd.WorkingDirectory = asmRunConfig.StartWorkingDirectory;

					foreach (var env in asmRunConfig.EnvironmentVariables)
						pcmd.EnvironmentVariables [env.Key] = env.Value;
				}
			} else {
				executionCommand = CreateExecutionCommand (configuration, config, runConfig);
				if (context.ExecutionTarget != null)
					executionCommand.Target = context.ExecutionTarget;
			}

			return executionCommand != null && context.ExecutionHandler.CanExecute (executionCommand);
		}

		protected override ProjectFeatures OnGetSupportedFeatures ()
		{
			var sf = base.OnGetSupportedFeatures ();

			// Libraries are not executable by default, unless the project has a custom execution command
			if (compileTarget == CompileTarget.Library
			    && !Configurations.OfType<ProjectConfiguration> ().Any (c => c.CustomCommands.HasCommands (CustomCommandType.Execute))
			    && !GetRunConfigurations ().Any ()
			   )
				sf &= ~ProjectFeatures.Execute;
			
			return sf;
		}

		protected override IEnumerable<SolutionItemRunConfiguration> OnGetRunConfigurations ()
		{
			var configs = base.OnGetRunConfigurations ();
			if (compileTarget == CompileTarget.Library) {
				// A library project can't run by itself, so discard configurations which have "Project" as startup action
				foreach (var c in configs) {
					var dc = c as DotNetProjectRunConfiguration;
					if (dc != null && !dc.CanRunLibrary)
						continue;
					yield return c;
				}
			} else
				foreach (var c in configs)
					yield return c;
		}

		protected override IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			var baseFiles = base.OnGetItemFiles (includeReferencedFiles);
			if (includeReferencedFiles) {
				List<FilePath> col = new List<FilePath> ();
				foreach (ProjectReference pref in References) {
					if (pref.ReferenceType == ReferenceType.Assembly) {
						foreach (var f in pref.GetReferencedFileNames (DefaultConfiguration.Selector))
							col.Add (f);
					}
				}
				foreach (DotNetProjectConfiguration c in Configurations) {
					if (c.SignAssembly && !c.AssemblyKeyFile.IsNullOrEmpty)
						col.Add (c.AssemblyKeyFile);
				}
				baseFiles = baseFiles.Concat (col);
			}
			return baseFiles;
		}

		internal Task<BuildResult> Compile (ProgressMonitor monitor, BuildData buildData)
		{
			return ProjectExtension.OnCompile (monitor, buildData);
		}

		protected virtual Task<BuildResult> OnCompile (ProgressMonitor monitor, BuildData buildData)
		{
			return MD1DotNetProjectHandler.Compile (monitor, this, buildData);
		}

		protected override bool OnGetIsCompileable (string fileName)
		{
			if (LanguageBinding == null)
				return false;
			return LanguageBinding.IsSourceCodeFile (fileName);
		}

		/// <summary>
		/// Gets the default namespace for the file, according to the naming policy.
		/// </summary>
		/// <remarks>Always returns a valid namespace, even if the fileName is null.</remarks>
		public string GetDefaultNamespace (string fileName, bool useVisualStudioNamingPolicy = false)
		{
			return OnGetDefaultNamespace (fileName, useVisualStudioNamingPolicy);
		}

		/// <summary>
		/// Gets the default namespace for the file, according to the naming policy.
		/// </summary>
		/// <remarks>Always returns a valid namespace, even if the fileName is null.</remarks>
		protected virtual string OnGetDefaultNamespace (string fileName, bool useVisualStudioNamingPolicy = false)
		{
			return GetDefaultNamespace (this, DefaultNamespace, fileName, useVisualStudioNamingPolicy);
		}

		/// <summary>
		/// Gets the default namespace for the file, according to the naming policy.
		/// </summary>
		/// <remarks>Always returns a valid namespace, even if the fileName is null.</remarks>
		internal static string GetDefaultNamespace (Project project, string defaultNamespace, string fileName, bool useVisualStudioNamingPolicy = false)
		{
			DirectoryNamespaceAssociation association = useVisualStudioNamingPolicy
				? DirectoryNamespaceAssociation.PrefixedHierarchical
				: project.Policies.Get<DotNetNamingPolicy> ().DirectoryNamespaceAssociation;

			string root = null;
			string dirNamespc = null;
			string defaultNmspc = !string.IsNullOrEmpty (defaultNamespace)
				? SanitisePotentialNamespace (defaultNamespace)
				: SanitisePotentialNamespace (project.Name) ?? "Application";

			if (string.IsNullOrEmpty (fileName)) {
				return defaultNmspc;
			}

			string dirname = Path.GetDirectoryName (fileName);
			string relativeDirname = null;
			if (!String.IsNullOrEmpty (dirname)) {
				relativeDirname = project.GetRelativeChildPath (dirname);
				if (string.IsNullOrEmpty (relativeDirname) || relativeDirname.StartsWith("..", StringComparison.Ordinal))
					relativeDirname = null;
			}

			if (relativeDirname != null) {
				try {
					switch (association) {
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

		static string GetHierarchicalNamespace (string relativePath)
		{
			StringBuilder sb = new StringBuilder (relativePath);
			for (int i = 0; i < sb.Length; i++) {
				if (sb[i] == Path.DirectorySeparatorChar)
					sb[i] = '.';
			}
			return sb.ToString ();
		}

		static string SanitisePotentialNamespace (string potential)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (char c in potential) {
				if (char.IsLetter (c) || c == '_' || (sb.Length > 0 && (char.IsLetterOrDigit (sb[sb.Length - 1]) || sb[sb.Length - 1] == '_') && (c == '.' || char.IsNumber (c)))) {
					sb.Append (c);
				}
			}
			if (sb.Length > 0) {
				if (sb[sb.Length - 1] == '.')
					sb.Remove (sb.Length - 1, 1);

				return sb.ToString ();
			} else
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
					// package name is only relevant if it's not a framework package
					var pkg = pref.Package;
					string packageName = pkg != null && !pkg.IsFrameworkPackage? pkg.Name : null;
					// find the version of the assembly that's valid for the new framework
					var newAsm = AssemblyContext.GetAssemblyForVersion (pref.Reference, packageName, TargetFramework);
					// if it changed, clear assembly resolution caches and update reference
					if (newAsm == null) {
						pref.ResetReference ();
					} else if (newAsm.FullName != pref.Reference) {
						pref.Reference = newAsm.FullName;
					} else if (!pref.IsValid || newAsm.Package != pref.Package) {
						pref.ResetReference ();
					}
				}
			}
		}

		protected override IEnumerable<string> OnGetStandardBuildActions ()
		{
			return BuildAction.DotNetActions;
		}

		protected override IList<string> OnGetCommonBuildActions ()
		{
			return BuildAction.DotNetCommonActions;
		}

		protected override void OnEndLoad ()
		{
			// Just after loading, the resource Ids are using the file format's policy.
			// They have to be converted to the new policy
			MigrateResourceIds (ResourceNamePolicy.FileFormatDefault, Policies.Get<DotNetNamingPolicy>().ResourceNamePolicy);

			base.OnEndLoad ();
		}

		protected abstract DotNetCompilerParameters OnCreateCompilationParameters (DotNetProjectConfiguration config, ConfigurationKind kind);

		internal protected virtual BuildResult OnCompileSources (ProjectItemCollection items, DotNetProjectConfiguration configuration, ConfigurationSelector configSelector, ProgressMonitor monitor)
		{
			throw new NotSupportedException ();
		}

		protected abstract ClrVersion[] OnGetSupportedClrVersions ();

		internal string GetDefaultResourceId (ProjectFile projectFile)
		{
			DotNetNamingPolicy pol = Policies.Get<DotNetNamingPolicy> ();
			return GetDefaultResourceIdForPolicy (projectFile, pol.ResourceNamePolicy);
		}

		internal string GetDefaultResourceIdForPolicy (ProjectFile projectFile, ResourceNamePolicy policy)
		{
			if (policy == ResourceNamePolicy.FileFormatDefault || policy == ResourceNamePolicy.MSBuild)
				return GetDefaultMSBuildResourceId (projectFile);
			else
				return projectFile.FilePath.FileName;
		}

		internal string GetDefaultMSBuildResourceId (ProjectFile projectFile)
		{
			return ProjectExtension.OnGetDefaultResourceId (projectFile);
		}

		/// <summary>
		/// Returns the resource id that the provided file will have if none is explicitly set
		/// </summary>
		/// <param name="projectFile">Project file.</param>
		/// <remarks>The algorithm for getting the resource id is usually language-specific.</remarks>
		protected virtual string OnGetDefaultResourceId (ProjectFile projectFile)
		{
			string fname = projectFile.ProjectVirtualPath;
			fname = FileService.NormalizeRelativePath (fname);
			fname = Path.Combine (Path.GetDirectoryName (fname).Replace (' ','_'), Path.GetFileName (fname));

			if (String.Compare (Path.GetExtension (fname), ".resx", true) == 0) {
				fname = Path.ChangeExtension (fname, ".resources");
			} else {
				string only_filename, culture, extn;
				if (MSBuildProjectService.TrySplitResourceName (fname, out only_filename, out culture, out extn)) {
					//remove the culture from fname
					//foo.it.bmp -> foo.bmp
					fname = only_filename + "." + extn;
				}
			}

			string rname = fname.Replace (Path.DirectorySeparatorChar, '.');

			DotNetProject dp = projectFile.Project as DotNetProject;

			if (dp == null || String.IsNullOrEmpty (dp.DefaultNamespace))
				return rname;
			else
				return dp.DefaultNamespace + "." + rname;
		}

		/// <summary>
		/// Migrates resource identifiers from a policy to the current policy of the project.
		/// </summary>
		/// <param name="oldPolicy">Old policy.</param>
		public void MigrateResourceIds (ResourceNamePolicy oldPolicy)
		{
			MigrateResourceIds (oldPolicy, Policies.Get<DotNetNamingPolicy>().ResourceNamePolicy);
		}

		void MigrateResourceIds (ResourceNamePolicy oldPolicy, ResourceNamePolicy newPolicy)
		{
			if (oldPolicy != newPolicy) {
				// If the file format has a default resource handler different from the one
				// choosen for this project, then all resource ids must be converted
				foreach (ProjectFile file in Files.Where (f => f.BuildAction == BuildAction.EmbeddedResource)) {
					if (file.Subtype == Subtype.Directory)
						continue;
					string oldId = file.GetResourceId (oldPolicy);
					string newId = file.GetResourceId (newPolicy);
					string newDefault = GetDefaultResourceIdForPolicy (file, newPolicy);
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

		protected override void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			foreach (var pref in objs.OfType<ProjectReference> ())
				pref.SetOwnerProject (this);

			base.OnItemsAdded (objs);

			// Notify that references have been added after the owner project has been set for all references.
			// Otherwise the subscriber of the reference added event may try to access reference information
			// while it is not yet properly set.

			foreach (var pref in objs.OfType<ProjectReference> ())
				ProjectExtension.OnReferenceAddedToProject (new ProjectReferenceEventArgs (this, pref));
			
			NotifyReferencedAssembliesChanged ();
		}

		protected override void OnItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			foreach (var pref in objs.OfType<ProjectReference> ())
				pref.SetOwnerProject (null);

			base.OnItemsRemoved (objs);

			// Notify that references have been removed after the owner project has been set for all references.
			// Otherwise the subscriber of the reference removed event may try to access reference information
			// while it is not yet properly set.

			foreach (var pref in objs.OfType<ProjectReference> ())
				ProjectExtension.OnReferenceRemovedFromProject (new ProjectReferenceEventArgs (this, pref));
			
			NotifyReferencedAssembliesChanged ();
		}

		internal void NotifyReferencedAssembliesChanged ()
		{
			NotifyModified ("References");
			ProjectExtension.OnReferencedAssembliesChanged ();
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

		protected virtual void OnReferencedAssembliesChanged ()
		{
			if (ReferencedAssembliesChanged != null) {
				ReferencedAssembliesChanged (this, EventArgs.Empty);
			}
		}

		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
		public event ProjectReferenceEventHandler ReferenceAddedToProject;

		/// <summary>
		/// Raised when the list of assemblies that this project references changes
		/// </summary>
		public event EventHandler ReferencedAssembliesChanged;

		private void OnFileRemoved (Object o, FileEventArgs e)
		{
			foreach (FileEventInfo ei in e)
				CheckReferenceChange (ei.FileName);
		}

		protected async override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			DotNetProjectConfiguration dotNetProjectConfig = GetConfiguration (configuration) as DotNetProjectConfiguration;
			if (dotNetProjectConfig == null) {
				monitor.ReportError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration, Name), null);
				return;
			}

			monitor.Log.WriteLine (GettextCatalog.GetString ("Running {0} ...", dotNetProjectConfig.CompiledOutputName));

			ExecutionCommand executionCommand = CreateExecutionCommand (configuration, dotNetProjectConfig, runConfiguration as ProjectRunConfiguration);
			if (context.ExecutionTarget != null)
				executionCommand.Target = context.ExecutionTarget;

			if (!context.ExecutionHandler.CanExecute (executionCommand)) {
				monitor.ReportError (GettextCatalog.GetString ("Can not execute \"{0}\". The selected execution mode is not supported for .NET projects.", dotNetProjectConfig.CompiledOutputName), null);
				return;
			}

			try {
				await ProjectExtension.OnExecuteCommand (monitor, context, configuration, executionCommand);
			} catch (Exception ex) {
				LoggingService.LogError (string.Format ("Cannot execute \"{0}\"", dotNetProjectConfig.CompiledOutputName), ex);
				monitor.ReportError (GettextCatalog.GetString ("Cannot execute \"{0}\"", dotNetProjectConfig.CompiledOutputName), ex);
			}
		}

		protected virtual async Task OnExecuteCommand (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, ExecutionCommand executionCommand)
		{
			bool externalConsole = false, pauseConsole = false;

			var dotNetExecutionCommand = executionCommand as DotNetExecutionCommand;
			if (dotNetExecutionCommand != null) {
				dotNetExecutionCommand.UserAssemblyPaths = GetUserAssemblyPaths (configuration);
				externalConsole = dotNetExecutionCommand.ExternalConsole;
				pauseConsole = dotNetExecutionCommand.PauseConsoleOutput;
			}

			var console = externalConsole ? context.ExternalConsoleFactory.CreateConsole (!pauseConsole, monitor.CancellationToken)
												   : context.ConsoleFactory.CreateConsole (OperationConsoleFactory.CreateConsoleOptions.Default.WithTitle (Name), monitor.CancellationToken);
		
			using (console) {
				ProcessAsyncOperation asyncOp = context.ExecutionHandler.Execute (executionCommand, console);

				using (var stopper = monitor.CancellationToken.Register (asyncOp.Cancel))
					await asyncOp.Task;

				monitor.Log.WriteLine (GettextCatalog.GetString ("The application exited with code: {0}", asyncOp.ExitCode));
			}
		}


		protected override void OnReadProjectHeader (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnReadProjectHeader (monitor, msproject);

			compileTarget = msproject.EvaluatedProperties.GetValue<CompileTarget> ("OutputType");
			defaultNamespace = msproject.EvaluatedProperties.GetValue ("RootNamespace", string.Empty);
			usePartialTypes = msproject.EvaluatedProperties.GetValue ("UsePartialTypes", true);

			string frameworkIdentifier = msproject.EvaluatedProperties.GetValue ("TargetFrameworkIdentifier");
			string frameworkVersion = msproject.EvaluatedProperties.GetValue ("TargetFrameworkVersion");
			string frameworkProfile = msproject.EvaluatedProperties.GetValue ("TargetFrameworkProfile");

			//determine the default target framework from the project type's default
			//overridden by the components in the project
			var def = GetDefaultTargetFrameworkForFormat (ToolsVersion);
			var targetFx = new TargetFrameworkMoniker (
				string.IsNullOrEmpty (frameworkIdentifier)? def.Identifier : frameworkIdentifier,
				string.IsNullOrEmpty (frameworkVersion)? def.Version : frameworkVersion,
				string.IsNullOrEmpty (frameworkProfile)? def.Profile : frameworkProfile);


			string fx = ExtendedProperties ["InternalTargetFrameworkVersion"] as string;
			if (!string.IsNullOrEmpty (fx)) {
				targetFx = TargetFrameworkMoniker.Parse (fx);
				ExtendedProperties.Remove ("InternalTargetFrameworkVersion");
			}

			TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (targetFx);
		}

		protected override void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnReadProject (monitor, msproject);

			// Load legacy configurations

			var addedConfigs = new List<CustomCommand> ();
			int count = 1;
			foreach (var c in Configurations) {
				foreach (var cmd in c.CustomCommands.Where (cc => cc.Type == CustomCommandType.Execute)) {
					if (addedConfigs.Any (cc => cc.Equals (cmd)))
						continue;
					string exe, args;
					cmd.ParseCommand (out exe, out args);

					//if the executable name matches an executable in the project directory, use that, for back-compat
					//else fall back and let the execution handler handle it via PATH, working directory, etc.
					if (!Path.IsPathRooted (exe)) {
						string localPath = ((FilePath)exe).ToAbsolute (BaseDirectory).FullPath;
						if (File.Exists (localPath))
							exe = localPath;
					}

					// If the project doesn't have any default run configuration, use the custom command as default configuration
					AssemblyRunConfiguration rc = RunConfigurations.OfType<AssemblyRunConfiguration> ().FirstOrDefault (co => co.IsDefaultConfiguration && co.IsEmpty);

					if (rc == null) {
						// There is already a default configuration. Use a custom command.
						var name = "Custom Command";
						if (count++ > 1)
							name += " " + count;
						rc = new AssemblyRunConfiguration (name);
					}
					rc.StartAction = AssemblyRunConfiguration.StartActions.Program;
					rc.StartProgram = exe ?? "";
					rc.StartArguments = args ?? "";
					rc.StartWorkingDirectory = cmd.WorkingDir ?? "";
					rc.PauseConsoleOutput = cmd.PauseExternalConsole;
					rc.ExternalConsole = cmd.ExternalConsole;
					rc.EnvironmentVariables.CopyFrom (cmd.EnvironmentVariables);
					rc.StoreInUserFile = false;
					if (!rc.IsDefaultConfiguration)
						RunConfigurations.Add (rc);
					addedConfigs.Add (cmd);
				}
				c.CustomCommands.RemoveAll (cc => cc.Type == CustomCommandType.Execute);
			}
		}

		protected override void OnWriteProjectHeader (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnWriteProjectHeader (monitor, msproject);

			IMSBuildPropertySet globalGroup = msproject.GetGlobalPropertyGroup ();

			globalGroup.SetValue ("OutputType", compileTarget);
			globalGroup.SetValue ("RootNamespace", defaultNamespace, string.Empty);
			globalGroup.SetValue ("UsePartialTypes", usePartialTypes, true);
		}

		protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnWriteProject (monitor, msproject);

			var moniker = TargetFramework.Id;
			bool supportsMultipleFrameworks = true; // All supported formats support multiple frameworks. // toolsFormat.SupportsMonikers || toolsFormat.SupportedFrameworks.Length > 0;
			var def = GetDefaultTargetFrameworkForFormat (ToolsVersion);

			IMSBuildPropertySet globalGroup = msproject.GetGlobalPropertyGroup ();

			// If the format only supports one fx version, or the version is the default, there is no need to store it.
			// However, is there is already a value set, do not remove it.
			if (supportsMultipleFrameworks) {
				globalGroup.SetValue ("TargetFrameworkVersion", "v" + moniker.Version, "v" + def.Version, true);
			}

			if (MSBuildFileFormat.ToolsSupportMonikers (ToolsVersion)) {
				globalGroup.SetValue ("TargetFrameworkIdentifier", moniker.Identifier, def.Identifier, true);
				globalGroup.SetValue ("TargetFrameworkProfile", moniker.Profile, def.Profile, true);
			}
		}

		protected override void OnWriteConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IPropertySet pset)
		{
			base.OnWriteConfiguration (monitor, config, pset);
			if (MSBuildProject.IsNewProject)
				pset.SetValue ("ErrorReport", "prompt");
			
		}

		protected override async Task OnReevaluateProject (ProgressMonitor monitor)
		{
			await base.OnReevaluateProject (monitor);
			NotifyReferencedAssembliesChanged ();
		}

		internal class DefaultDotNetProjectExtension: DotNetProjectExtension
		{
			internal protected override DotNetProjectFlags OnGetDotNetProjectFlags ()
			{
				return Project.OnGetDotNetProjectFlags ();
			}

			internal protected override bool OnGetCanReferenceProject (DotNetProject targetProject, out string reason)
			{
				return Project.CheckCanReferenceProject (targetProject, out reason);
			}

			internal protected override string OnGetDefaultTargetPlatform (ProjectCreateInformation projectCreateInfo)
			{
				return Project.OnGetDefaultTargetPlatform (projectCreateInfo);
			}

			internal protected override Task<List<AssemblyReference>> OnGetReferences (ConfigurationSelector configuration, CancellationToken token)
			{
				return Project.OnGetReferences (configuration, token);
			}

			internal protected override Task<List<AssemblyReference>> OnGetReferencedAssemblies (ConfigurationSelector configuration)
			{
				return Project.OnGetReferencedAssemblies (configuration);
			}

			internal protected override IEnumerable<DotNetProject> OnGetReferencedAssemblyProjects (ConfigurationSelector configuration)
			{
				return Project.OnGetReferencedAssemblyProjects (configuration);
			}

#pragma warning disable 672 // Member overrides obsolete member
			internal protected override ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration)
			{
				return Project.OnCreateExecutionCommand (configSel, configuration);
			}
#pragma warning restore 672 // Member overrides obsolete member

			internal protected override ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
			{
				return Project.OnCreateExecutionCommand (configSel, configuration, runConfiguration);
			}

			internal protected override void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
			{
				Project.OnReferenceRemovedFromProject (e);
			}

			internal protected override void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
			{
				Project.OnReferenceAddedToProject (e);
			}

			internal protected override void OnReferencedAssembliesChanged ()
			{
				Project.OnReferencedAssembliesChanged ();
			}

			internal protected override Task<BuildResult> OnCompile (ProgressMonitor monitor, BuildData buildData)
			{
				return Project.OnCompile (monitor, buildData);
			}

			internal protected override string OnGetDefaultResourceId (ProjectFile projectFile)
			{
				return Project.OnGetDefaultResourceId (projectFile);
			}

			internal protected override Task OnExecuteCommand (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, ExecutionCommand executionCommand)
			{
				return Project.OnExecuteCommand (monitor, context, configuration, executionCommand);
			}

			internal protected override string[] SupportedLanguages {
				get {
					return Project.OnGetSupportedLanguages ();
				}
			}

			#region Framework management

			internal protected override TargetFrameworkMoniker OnGetDefaultTargetFrameworkId ()
			{
				return Project.OnGetDefaultTargetFrameworkId ();
			}

			internal protected override TargetFrameworkMoniker OnGetDefaultTargetFrameworkForFormat (string toolsVersion)
			{
				return Project.OnGetDefaultTargetFrameworkForFormat (toolsVersion);
			}

			internal protected override bool OnGetSupportsFramework (TargetFramework framework)
			{
				return Project.OnSupportsFramework (framework);
			}

			#endregion
		}
	}

	[Flags]
	public enum DotNetProjectFlags
	{
		None = 0,
		GeneratesDebugInfoFile = 1,
		IsLibrary = 2
	}
}
