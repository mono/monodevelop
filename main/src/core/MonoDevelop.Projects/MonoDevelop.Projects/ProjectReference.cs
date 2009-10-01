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

namespace MonoDevelop.Projects
{
	public enum ReferenceType {
		Assembly,
		Project,
		Gac,
		Custom
	}
	
	/// <summary>
	/// This class represent a reference information in an Project object.
	/// </summary>
	[DataItem (FallbackType=typeof(UnknownProjectReference))]
	public class ProjectReference : ProjectItem, ICloneable
	{
		ReferenceType referenceType = ReferenceType.Custom;
		DotNetProject ownerProject;
		string reference = String.Empty;
		bool localCopy = true;
		
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
		
		public event EventHandler StatusChanged;
		
		[ItemProperty ("Package", DefaultValue="")]
		internal string packageName {
			get {
				SystemPackage sp = Package;
				if (sp != null && !sp.IsGacPackage)
					return sp.Name;
				else
					return string.Empty;
			}
			set {
				package = value;
			}
		}
		
		public ProjectReference ()
		{
		}
		
		internal void SetOwnerProject (DotNetProject project)
		{
			ownerProject = project;
			UpdateGacReference ();
		}
		
		public ProjectReference(ReferenceType referenceType, string reference)
		{
			if (referenceType == ReferenceType.Assembly)
				specificVersion = false;
			this.referenceType = referenceType;
			this.reference     = reference;
			UpdateGacReference ();
		}
		
		public ProjectReference (Project referencedProject)
		{
			referenceType = ReferenceType.Project;
			reference = referencedProject.Name;
		}
		
		public ProjectReference (SystemAssembly asm)
		{
			referenceType = ReferenceType.Gac;
			reference = asm.FullName;
			if (!asm.Package.IsGacPackage)
				package = asm.Package.Name;
			UpdateGacReference ();
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
			return newRef;
		}
		
		public Project OwnerProject {
			get { return ownerProject; }
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
				UpdateGacReference ();
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
				return localCopy;
			}
			set {
				localCopy = value;
			}
		}

		internal string LoadedReference {
			get {
				return loadedReference;
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

		public bool IsValid {
			get { return string.IsNullOrEmpty (ValidationErrorMessage); }
		}
		
		// Returns the validation error message, or an empty string if everything is ok
		public virtual string ValidationErrorMessage {
			get {
				if (customError != null)
					return customError;
				if (ReferenceType == ReferenceType.Gac) {
					if (!IsExactVersion && SpecificVersion)
						return GettextCatalog.GetString ("Specified version not found: expected {0}, found {1}", GetVersionNum (StoredReference), GetVersionNum (Reference));
					if (notFound) {
						if (ownerProject != null)
							return GettextCatalog.GetString ("Assembly not available for {0} (in {1})", TargetFramework.Name, TargetRuntime.DisplayName);
						else
							return GettextCatalog.GetString ("Assembly not found");
					}
				} else if (ReferenceType == ReferenceType.Project) {
					if (ownerProject != null && ownerProject.ParentSolution != null) {
						DotNetProject p = ownerProject.ParentSolution.FindProjectByName (reference) as DotNetProject;
						if (p != null) {
							if (!ownerProject.TargetFramework.IsCompatibleWithFramework (p.TargetFramework.Id))
								return GettextCatalog.GetString ("Incompatible target framework ({0})", p.TargetFramework.Name);
						}
					}
				} else if (ReferenceType == ReferenceType.Assembly) {
					if (!File.Exists (reference))
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
				if (ReferenceType == ReferenceType.Gac) {
					string r1 = MonoDevelop.Core.Assemblies.AssemblyContext.NormalizeAsmName (StoredReference);
					string r2 = MonoDevelop.Core.Assemblies.AssemblyContext.NormalizeAsmName (Reference);
					return r1 == r2;
				}
				return true;
			}
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
		
		/// <summary>
		/// Returns the file name to an assembly, regardless of what 
		/// type the assembly is.
		/// </summary>
		string GetReferencedFileName (string configuration)
		{
			switch (ReferenceType) {
				case ReferenceType.Assembly:
					return reference;
				
				case ReferenceType.Gac:
					string file = AssemblyContext.GetAssemblyLocation (Reference, package, ownerProject != null? ownerProject.TargetFramework : null);
					return file == null ? reference : file;
				case ReferenceType.Project:
					if (ownerProject != null) {
						if (ownerProject.ParentSolution != null) {
							Project p = ownerProject.ParentSolution.FindProjectByName (reference);
							if (p != null) return p.GetOutputFileName (configuration);
						}
					}
					return null;
				
				default:
					Console.WriteLine ("pp: " + Reference + " " + OwnerProject.FileName);
					throw new NotImplementedException("unknown reference type : " + ReferenceType);
			}
		}
		
		public virtual string[] GetReferencedFileNames (string configuration)
		{
			string s = GetReferencedFileName (configuration);
	/*		if (referenceType == ReferenceType.Gac) {
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
		
		void UpdateGacReference ()
		{
			if (referenceType == ReferenceType.Gac && ownerProject != null) {
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
				if (referenceType == ReferenceType.Gac) {
					if (cachedPackage != null)
						return cachedPackage;
					
					if (package != null)
						return AssemblyContext.GetPackage (package);

					// No package is specified, get any of the registered assemblies, giving priority to gaced assemblies
					// (because non-gac assemblies should have a package name set)
					SystemAssembly best = null;
					foreach (SystemAssembly asm in AssemblyContext.GetAssembliesFromFullName (reference)) {
						//highest priority to framework packages
						if (ownerProject != null && asm.Package.IsFrameworkPackage && asm.Package.TargetFramework == ownerProject.TargetFramework.Id) {
							return cachedPackage = asm.Package;
						}
						if (asm.Package.IsGacPackage)
							best = asm;
						else if (best == null)
							best = asm;
					}
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
				UpdateGacReference ();
			} else
				UpdateGacReference ();
		}
		
		public object Clone()
		{
			return MemberwiseClone();
		}
		
		public override bool Equals (object other)
		{
			ProjectReference oref = other as ProjectReference;
			if (oref == null) return false;
			
			return StoredReference == oref.StoredReference && referenceType == oref.referenceType && package == oref.package;
		}
		
		public override int GetHashCode ()
		{
			return StoredReference.GetHashCode ();
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
	}
	
	public class UnknownProjectReference: ProjectReference
	{
	}
}
