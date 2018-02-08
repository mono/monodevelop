// 
// ProjectReference.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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



using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml;
using MonoDevelop.Core;
using System.ComponentModel;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public enum ReferenceType {
		Assembly,
		Project,
		Package,
		Custom,
		[Obsolete ("Use Package")]
		Gac = Package
	}
	
	/// <summary>
	/// This class represent a reference information in an Project object.
	/// </summary>
	public class ProjectReference : ProjectItem, ICloneable
	{
		ReferenceType referenceType = ReferenceType.Custom;
		DotNetProject ownerProject;
		string reference = String.Empty;
		bool? localCopy;
		string projectGuid;
		
		// A project may reference assemblies which are not available
		// in the system where it is opened. For example, opening
		// a project that references gtk# 2.4 in a system with gtk# 2.6.
		// In this case the reference will be upgraded to 2.6, but for
		// consistency reasons the reference will still be saved as 2.4.
		// The loadedReference stores the reference initially loaded,
		// so it can be saved again.
		string loadedReference;
		bool specificVersion = true;
		bool notFound;
		string package;
		SystemPackage cachedPackage;
		string customError;
		FilePath hintPath;
		bool hasBeenRead;

		string originalMSBuildReferenceHintPath;

		public event EventHandler StatusChanged;
		
		[ItemProperty ("Package", DefaultValue=null)]
		internal string packageName {
			get {
				if (!string.IsNullOrEmpty (package))
					return package;
				SystemPackage sp = Package;
				if (sp != null && !sp.IsGacPackage)
					return sp.Name;
				else
					return null;
			}
			set {
				package = value;
			}
		}

		public string ProjectGuid { get => projectGuid; }
		
		public ProjectReference ()
		{
		}
		
		internal void SetOwnerProject (DotNetProject project)
		{
			ownerProject = project;
			UpdatePackageReference ();
		}

		public sealed override string Include {
			get {
				if (referenceType == ReferenceType.Project && OwnerProject != null) {
					Project refProj = OwnerProject.ParentSolution != null ? ResolveProject (OwnerProject.ParentSolution) : null;
					if (refProj != null)
						return MSBuildProjectService.ToMSBuildPath (OwnerProject.ItemDirectory, refProj.FileName);
					else
						return Reference;
				}
				return base.Include;
			}
			protected set { base.Include = value; }
		}
		
		ProjectReference (ReferenceType referenceType, string reference, string hintPath)
		{
			Init (referenceType, reference, hintPath);
		}
		
		ProjectReference (Project referencedProject)
		{
			Init (ReferenceType.Project, referencedProject.Name, null, referencedProject.ItemId);
			specificVersion = true;
		}

		ProjectReference (SystemAssembly asm)
		{
			Init (ReferenceType.Package, asm.FullName, null);
			if (asm.Package.IsFrameworkPackage)
				specificVersion = false;
			if (!asm.Package.IsGacPackage)
				package = asm.Package.Name;
			UpdatePackageReference ();
		}

		public static ProjectReference CreateCustomReference (ReferenceType referenceType, string reference, string hintPath = null)
		{
			return new ProjectReference (referenceType, reference, hintPath);
		}

		public static ProjectReference CreateAssemblyReference (SystemAssembly asm)
		{
			return new ProjectReference (asm);
		}

		public static ProjectReference CreateAssemblyReference (string assemblyName, string hintPath = null)
		{
			return new ProjectReference (ReferenceType.Package, assemblyName, hintPath);
		}

		public static ProjectReference CreateAssemblyFileReference (FilePath path)
		{
			return new ProjectReference (ReferenceType.Assembly, path, null);
		}

		public static ProjectReference CreateProjectReference (Project project)
		{
			return new ProjectReference (project);
		}

		public static ProjectReference CreateProjectReference (FilePath projectFile)
		{
			return new ProjectReference (ReferenceType.Project, projectFile.FileNameWithoutExtension, null);
		}

		void Init (ReferenceType referenceType, string reference, string hintPath, string projectGuid = null)
		{
			if (referenceType == ReferenceType.Assembly) {
				specificVersion = false;
				if (hintPath == null) {
					hintPath = reference;
					reference = Path.GetFileNameWithoutExtension (reference);
				}

				if (Include == null) {
					if (File.Exists (HintPath)) {
						try {
							var aname = System.Reflection.AssemblyName.GetAssemblyName (HintPath);
							if (SpecificVersion) {
								Include = aname.FullName;
							} else {
								Include = aname.Name;
							}
						} catch (Exception ex) {
							string msg = string.Format ("Could not get full name for assembly '{0}'.", Reference);
							LoggingService.LogError (msg, ex);
						}
					}
					if (Include == null)
						Include = Path.GetFileNameWithoutExtension (hintPath);
				}
			}

			switch (referenceType) {
			case ReferenceType.Package: 
			case ReferenceType.Assembly: 
				ItemName = "Reference";
				break;
			case ReferenceType.Project:
				ItemName = "ProjectReference";
				break;
			}

			this.referenceType = referenceType;
			this.reference = reference.Trim ();
			this.hintPath = hintPath;
			this.projectGuid = projectGuid;
			UpdatePackageReference ();

			if (Include == null)
				Include = reference;
		}

		internal protected override void Read (Project project, IMSBuildItemEvaluated buildItem)
		{
			base.Read (project, buildItem);

			if (buildItem.Name == "Reference")
				ReadReference (project, buildItem);
			else if (buildItem.Name == "ProjectReference")
				ReadProjectReference (project, buildItem);

			localCopy = buildItem.Metadata.GetValue<bool?> ("Private", null);
			ReferenceOutputAssembly = buildItem.Metadata.GetValue ("ReferenceOutputAssembly", true);
		}

		void ReadReference (Project project, IMSBuildItemEvaluated buildItem)
		{
			if (buildItem.Metadata.HasProperty ("HintPath")) {
				FilePath path;
				var p = buildItem.Metadata.GetProperty ("HintPath");
				if (p != null)
					originalMSBuildReferenceHintPath = p.UnevaluatedValue;
				if (!buildItem.Metadata.TryGetPathValue ("HintPath", out path)) {
					var hp = buildItem.Metadata.GetValue ("HintPath");
					Init (ReferenceType.Assembly, hp, null);
					SetInvalid (GettextCatalog.GetString ("Invalid file path"));
				} else {
					var type = File.Exists (path) ? ReferenceType.Assembly : ReferenceType.Package;
					Init (type, buildItem.Include, path);
				}
			} else {
				string asm = buildItem.Include;
				// This is a workaround for a VS bug. Looks like it is writing this assembly incorrectly
				if (asm == "System.configuration")
					asm = "System.Configuration";
				else if (asm == "System.XML")
					asm = "System.Xml";
				else if (asm == "system")
					asm = "System";
				Init (ReferenceType.Package, asm, null);
			}

			string specificVersion = buildItem.Metadata.GetValue ("SpecificVersion");
			if (string.IsNullOrWhiteSpace (specificVersion)) {
				// If the SpecificVersion element isn't present, check if the Assembly Reference specifies a Version
				SpecificVersion = ReferenceStringHasVersion (buildItem.Include);
			}
			else {
				bool value;
				// if we can't parse the value, default to false which is more permissive
				SpecificVersion = bool.TryParse (specificVersion, out value) && value;
			}
			hasBeenRead = true;
		}

		void ReadProjectReference (Project project, IMSBuildItemEvaluated buildItem)
		{
			// Get the project name from the path, since the Name attribute may other stuff other than the name
			string path = MSBuildProjectService.FromMSBuildPath (project.ItemDirectory, buildItem.Include);
			string name = buildItem.Metadata.GetValue ("Name", Path.GetFileNameWithoutExtension (path));
			string projectGuid = buildItem.Metadata.GetValue ("Project");
			Init (ReferenceType.Project, name, null, projectGuid);
		}

		internal protected override void Write (Project project, MSBuildItem buildItem)
		{
			// If the project is not supported, don't try to update any metadata of the property,
			// just leave what was read
			if (OwnerProject.IsUnsupportedProject)
				return;

			base.Write (project, buildItem);

			if (ReferenceType == ReferenceType.Assembly) {
				if (!hasBeenRead && !HintPath.IsNullOrEmpty)
					buildItem.Metadata.SetValue ("HintPath", HintPath);

				buildItem.Metadata.SetValue ("SpecificVersion", SpecificVersion || !ReferenceStringHasVersion (Include), true);
			}
			else if (ReferenceType == ReferenceType.Package) {
				buildItem.Metadata.SetValue ("SpecificVersion", SpecificVersion || !ReferenceStringHasVersion (Include), true);

				//RequiredTargetFramework is undocumented, maybe only a hint for VS. Only seems to be used for .NETFramework
				var dnp = OwnerProject as DotNetProject;
				IList supportedFrameworks = project.FileFormat.SupportedFrameworks;
				if (supportedFrameworks != null && dnp != null && Package != null
					&& dnp.TargetFramework.Id.Identifier == TargetFrameworkMoniker.ID_NET_FRAMEWORK
					&& Package.IsFrameworkPackage && supportedFrameworks.Contains (Package.TargetFramework)
					&& Package.TargetFramework.Version != "2.0" && supportedFrameworks.Count > 1)
				{
					TargetFramework fx = Runtime.SystemAssemblyService.GetTargetFramework (Package.TargetFramework);
					buildItem.Metadata.SetValue ("RequiredTargetFramework", fx.Id.Version);
				} else {
					buildItem.Metadata.RemoveProperty ("RequiredTargetFramework");
				}
			}
			else if (ReferenceType == ReferenceType.Project) {
				Project refProj = OwnerProject.ParentSolution != null ? ResolveProject (OwnerProject.ParentSolution) : null;
				if (refProj != null) {
					buildItem.Metadata.SetValue ("Project", refProj.ItemId, preserveExistingCase:true);
					buildItem.Metadata.SetValue ("Name", refProj.Name);
					buildItem.Metadata.SetValue ("ReferenceOutputAssembly", ReferenceOutputAssembly, true);
				}
			}

			buildItem.Metadata.SetValue ("Private", LocalCopy, DefaultLocalCopy);
		}

		bool ReferenceStringHasVersion (string asmName)
		{
			int commaPos = asmName.IndexOf (',');
			return commaPos >= 0 && asmName.IndexOf ("Version", commaPos) >= 0;
		}

		protected void InitCustomReference (string reference)
		{
			Reference = reference;
			referenceType = ReferenceType.Custom;
		}
		
		public static ProjectReference RenameReference (ProjectReference pref, string newReference)
		{
			ProjectReference newRef = (ProjectReference) pref.MemberwiseClone ();
			newRef.reference = newReference;
			newRef.Include = newReference;
			return newRef;
		}
		
		public Project OwnerProject {
			get { return ownerProject; }
		}

		// This property is used by the serializer. It ensures that the obsolete Gac value is not serialized
		internal ReferenceType internalReferenceType {
			get { return referenceType; }
			set { referenceType = value; }
		}

		public ReferenceType ReferenceType {
			get {
				return referenceType;
			}
		}
		
		public string Reference {
			get {
				return reference;
			}
			internal set {
				reference = value;
				UpdatePackageReference ();
			}
		}
		
		public string StoredReference {
			get {
				if (!string.IsNullOrEmpty (loadedReference))
					return loadedReference;
				else
					return reference;
			}
		}
		
		public bool LocalCopy {
			get {
				// When not explicitly set, the default value of LocalCopy depends on the type of reference.
				// For project and file references the default is true. For framework and package assemblies
				// (including GAC) the default is false

				if (localCopy.HasValue)
					return localCopy.Value;
				return DefaultLocalCopy;
			}
			set {
				localCopy = value;
				if (ownerProject != null)
					ownerProject.NotifyModified (null);
			}
		}

		bool referenceOutputAssembly = true;
		public bool ReferenceOutputAssembly {
			get { return referenceOutputAssembly; }
			set {
				if (referenceOutputAssembly != value) {
					referenceOutputAssembly = value;
					OnStatusChanged ();
				}
			}
		}
		
		internal bool DefaultLocalCopy {
			get {
				return referenceType != ReferenceType.Package;
			}
		}

		public bool CanSetLocalCopy {
			get {
				return true;
			}
		}

		public bool SpecificVersion {
			get {
				return specificVersion;
			}
			set {
				if (specificVersion != value) {
					specificVersion = value;
					OnStatusChanged ();
				}
			}
		}
		
		public bool CanSetSpecificVersion {
			get {
				if (ReferenceType == ReferenceType.Project || ReferenceType == ReferenceType.Custom)
					return false;
				if (ReferenceType == ReferenceType.Package && Package != null && Package.IsFrameworkPackage)
					return false;
				return true;
			}
		}
		string aliases = "";

		[ItemProperty ("Aliases", DefaultValue = "")]
		public string Aliases {
			get {
				return aliases;
			}
			set {
				aliases = value;
				if (ownerProject != null)
					ownerProject.NotifyModified ("References");
			}
		}

		public bool IsValid {
			get { return string.IsNullOrEmpty (ValidationErrorMessage); }
		}
		
		// Returns the validation error message, or an empty string if everything is ok
		public virtual string ValidationErrorMessage {
			get {
				if (customError != null)
					return customError;
				if (ReferenceType == ReferenceType.Package) {
					if (!IsExactVersion && SpecificVersion)
						return GettextCatalog.GetString ("Specified version not found: expected {0}, found {1}", GetVersionNum (StoredReference), GetVersionNum (Reference));
					if (notFound) {
						if (ownerProject != null) {
							bool isDefaultRuntime = Runtime.SystemAssemblyService.DefaultRuntime == TargetRuntime;
							bool probablyFrameworkAssembly = string.IsNullOrEmpty (originalMSBuildReferenceHintPath);

							if (TargetRuntime.IsInstalled (TargetFramework) || !probablyFrameworkAssembly) {
								if (isDefaultRuntime)
									return GettextCatalog.GetString ("Assembly not found for framework {0}", TargetFramework.Name);

								return GettextCatalog.GetString ("Assembly not found for framework {0} (in {1})", TargetFramework.Name, TargetRuntime.DisplayName);
							}

							if (isDefaultRuntime)
								return GettextCatalog.GetString ("Framework {0} is not installed", TargetFramework.Name);

							return GettextCatalog.GetString ("Framework {0} is not installed (in {1})", TargetFramework.Name, TargetRuntime.DisplayName);
						}

						return GettextCatalog.GetString ("Assembly not found");
					}
				} else if (ReferenceType == ReferenceType.Project) {
					if (ownerProject != null && ownerProject.ParentSolution != null && ReferenceOutputAssembly) {
						var p = ResolveProject (ownerProject.ParentSolution);
						var dotNetProject = p as DotNetProject;
						if (dotNetProject != null) {
							string reason;

							if (!ownerProject.CanReferenceProject (dotNetProject, out reason))
								return reason;
						} else if (p == null)
							return GettextCatalog.GetString ("Project not found");
					}
				} else if (ReferenceType == ReferenceType.Assembly) {
					if (!File.Exists (hintPath))
						return GettextCatalog.GetString ("File not found");
				}
				return string.Empty;
			}
		}
		
		public void SetInvalid (string message)
		{
			customError = message;
			OnStatusChanged ();
		}
		
		public bool IsExactVersion {
			get {
				if (ReferenceType == ReferenceType.Package) {
					string r1 = MonoDevelop.Core.Assemblies.AssemblyContext.NormalizeAsmName (StoredReference);
					string r2 = MonoDevelop.Core.Assemblies.AssemblyContext.NormalizeAsmName (Reference);
					return r1 == r2;
				}
				return true;
			}
		}

		public FilePath HintPath {
			get { return hintPath; }
		}
		
		string GetVersionNum (string asmName)
		{
			int i = asmName.IndexOf (',');
			if (i != -1) {
				i++;
				int j = asmName.IndexOf (',', i);
				if (j == -1)
					j = asmName.Length;
				string ver = asmName.Substring (i, j - i).Trim ();
				if (ver.Length > 0)
					return ver;
			}
			return "0.0.0.0";
		}

		internal ProjectReference GetRefreshedReference ()
		{
			if (customError != null)
				return null;
			if (ReferenceType == ReferenceType.Package) {
				if (!string.IsNullOrEmpty (hintPath) && File.Exists (hintPath)) {
					var res = (ProjectReference) MemberwiseClone ();
					res.referenceType = ReferenceType.Assembly;
					res.Project = null;
					return res;
				}
			} else if (ReferenceType == ReferenceType.Assembly) {
				if (!string.IsNullOrEmpty (hintPath) && !File.Exists (hintPath)) {
					var res = (ProjectReference) MemberwiseClone ();
					res.referenceType = ReferenceType.Package;
					res.Project = null;
					return res;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Returns the file name to an assembly, regardless of what 
		/// type the assembly is.
		/// </summary>
		string GetReferencedFileName (ConfigurationSelector configuration)
		{
			switch (ReferenceType) {
				case ReferenceType.Assembly:
					return hintPath;
				
				case ReferenceType.Package:
					string file = AssemblyContext.GetAssemblyLocation (Reference, package, ownerProject != null? ownerProject.TargetFramework : null);
					return file == null ? reference : file;
				case ReferenceType.Project:
					if (ownerProject != null && ownerProject.ParentSolution != null) {
						var p = ResolveProject (ownerProject.ParentSolution);
						if (p != null) {
							return p.GetOutputFileName (configuration);
						}
					}
					return null;
				
				default:
					return null;
			}
		}
		
		public virtual string[] GetReferencedFileNames (ConfigurationSelector configuration)
		{
			string s = GetReferencedFileName (configuration);
	/*		if (referenceType == ReferenceType.Package) {
				List<string> result = new List<string> ();
				result.Add (s);
				AddRequiredPackages (result, Package);
				return result.ToArray ();
			}*/
			
			if (s != null)
				return new string[] { s };
			return new string [0];
		}
		/*
		void AddRequiredPackages (List<string> result, SystemPackage fromPackage)
		{
			if (fromPackage == null || string.IsNullOrEmpty (fromPackage.Requires))
				return;
			foreach (string requiredPackageName in fromPackage.Requires.Split (' ')) {
				SystemPackage package = AssemblyContext.GetPackage (requiredPackageName);
				if (package == null)
					continue;
				foreach (SystemAssembly assembly in package.Assemblies) {
					if (assembly == null)
						continue;
					string location = AssemblyContext.GetAssemblyLocation (assembly.FullName, ownerProject != null ? ownerProject.TargetFramework : null);
					result.Add (location);
				}
				AddRequiredPackages (result, package);
			}
		}*/
		
		void UpdatePackageReference ()
		{
			if (referenceType == ReferenceType.Package && ownerProject != null) {
				notFound = false;
				string cref = AssemblyContext.FindInstalledAssembly (reference, package, ownerProject.TargetFramework);
				if (cref == null)
					cref = reference;
				cref = AssemblyContext.GetAssemblyNameForVersion (cref, package, ownerProject.TargetFramework);
				notFound = (cref == null);
				if (cref != null && cref != reference) {
					SystemAssembly asm = AssemblyContext.GetAssemblyFromFullName (cref, package, ownerProject.TargetFramework);
					bool isFrameworkAssembly = asm != null && asm.Package.IsFrameworkPackage;
					if (loadedReference == null && !isFrameworkAssembly) {
						loadedReference = reference;
					}
					reference = cref;
				}
				cachedPackage = null;

				SystemPackage pkg = Package;
				if (pkg != null && pkg.IsFrameworkPackage) {
					int i = Include.IndexOf (',');
					if (i != -1)
						Include = Include.Substring (0, i).Trim ();
				}

				OnStatusChanged ();
			}
		}
		
		IAssemblyContext AssemblyContext {
			get {
				if (ownerProject != null)
					return ownerProject.AssemblyContext;
				else
					return Runtime.SystemAssemblyService.DefaultAssemblyContext;
			}
		}
		
		TargetRuntime TargetRuntime {
			get {
				if (ownerProject != null)
					return ownerProject.TargetRuntime;
				else
					return Runtime.SystemAssemblyService.DefaultRuntime;
			}
		}
		
		TargetFramework TargetFramework {
			get {
				if (ownerProject != null)
					return ownerProject.TargetFramework;
				else
					return null;
			}
		}
		
		public SystemPackage Package {
			get {
				if (referenceType == ReferenceType.Package) {
					if (cachedPackage != null)
						return cachedPackage;

					if (!string.IsNullOrEmpty (package)) {
						var p = AssemblyContext.GetPackage (package);
						if (p != null)
							return p;
					}

					// No package is specified, get any of the registered assemblies, giving priority to gaced assemblies
					// (because non-gac assemblies should have a package name set)
					TargetFramework fx = ownerProject == null? null : ownerProject.TargetFramework;
					var best = AssemblyContext.GetAssemblyFromFullName (reference, null, fx);
					if (best != null)
						return cachedPackage = best.Package;
				}
				return null;
			}
		}
		
		internal void ResetReference ()
		{
			cachedPackage = null;
			if (loadedReference != null) {
				reference = loadedReference;
				loadedReference = null;
				UpdatePackageReference ();
			} else
				UpdatePackageReference ();
		}
		
		public object Clone()
		{
			return MemberwiseClone();
		}
		
		public override bool Equals (object obj)
		{
			return Equals (obj as ProjectReference);
		}
		
		public bool Equals (ProjectReference other)
		{
			return other != null
				&& StoredReference == other.StoredReference
				&& referenceType == other.referenceType
				&& package == other.package;
		}
		
		public override int GetHashCode ()
		{
			int result = 0;
			if (StoredReference != null)
				result ^= StoredReference.GetHashCode ();
			if (package != null)
				result ^= package.GetHashCode ();
			return result;
		}
		
		internal void NotifyStatusChanged ()
		{
			OnStatusChanged ();
		}
		
		protected virtual void OnStatusChanged ()
		{
			if (StatusChanged != null)
				StatusChanged (this, EventArgs.Empty);
		}

		/// <summary>
		/// Resolves a project for a ReferenceType.Project reference type in a given solution.
		/// </summary>
		/// <returns>The project, or <c>null</c> if it couldn't be resolved.</returns>
		/// <param name="inSolution">The solution the project is in.</param>
		/// <exception cref="T:System.ArgumentNullException">Thrown if inSolution == null</exception>
		/// <exception cref="T:System.InvalidOperationException">Thrown if ReferenceType != ReferenceType.Project</exception>
		public Project ResolveProject (Solution inSolution)
		{
			if (inSolution == null)
				throw new ArgumentNullException ("inSolution");
			if (ReferenceType != ReferenceType.Project)
				throw new InvalidOperationException ("ResolveProject is only definied for Project reference type.");
			if (!string.IsNullOrEmpty (projectGuid)) {
				var project = inSolution.GetSolutionItem (projectGuid) as Project;
				if (project != null) {
					return project;
				}
			}
			return inSolution.FindProjectByName (Reference);
		}
	}
	
	public class UnknownProjectReference: ProjectReference
	{
	}
}
