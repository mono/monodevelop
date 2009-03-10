//  ProjectReference.cs
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml;
using MonoDevelop.Core;
using System.ComponentModel;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

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
			set {
				loadedReference = value;
			}
		}

		public bool SpecificVersion {
			get {
				return specificVersion;
			}
			set {
				specificVersion = value;
			}
		}

		public bool IsValid {
			get { return string.IsNullOrEmpty (ValidationErrorMessage); }
		}
		
		// Returns the validation error message, or an empty string if everything is ok
		public virtual string ValidationErrorMessage {
			get {
				if (ReferenceType == ReferenceType.Gac) {
					if (notFound)
						return GettextCatalog.GetString ("Assembly not found");
					if (!IsExactVersion && SpecificVersion)
						return GettextCatalog.GetString ("Specified version not found: expected {0}, found {1}", GetVersionNum (StoredReference), GetVersionNum (Reference));
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
						GettextCatalog.GetString ("File not found");
				}
				return string.Empty;
			}
		}
		
		public bool IsExactVersion {
			get {
				if (ReferenceType == ReferenceType.Gac) {
					if (StoredReference != Reference && !Reference.StartsWith (StoredReference + ","))
						return false;
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
					string file = Runtime.SystemAssemblyService.GetAssemblyLocation (Reference, package);
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
			if (s != null)
				return new string[] { s };
			else
				return new string [0];
		}
		
		void UpdateGacReference ()
		{
			if (referenceType == ReferenceType.Gac) {
				notFound = false;
				string cref = Runtime.SystemAssemblyService.FindInstalledAssembly (reference, package);
				if (ownerProject != null) {
					if (cref == null)
						cref = reference;
					cref = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (cref, package, ownerProject.TargetFramework);
					notFound = (cref == null);
				}
				if (cref != null && cref != reference) {
					if (loadedReference == null) {
						loadedReference = reference;
					}
					reference = cref;
				}
				cachedPackage = null;
			}
		}
		
		public SystemPackage Package {
			get {
				if (referenceType == ReferenceType.Gac) {
					if (cachedPackage != null)
						return cachedPackage;
					if (package != null)
						return Runtime.SystemAssemblyService.GetPackage (package);

					// No package is specified, get any of the registered assemblies, giving priority to gaced assemblies
					// (because non-gac assemblies should have a package name set)
					SystemAssembly best = null;
					foreach (SystemAssembly asm in Runtime.SystemAssemblyService.GetAssembliesFromFullName (reference)) {
						if (asm.Package.IsGacPackage) {
							best = asm;
							break;
						} else if (best == null)
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
			} else if (notFound)
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
			
			return reference == oref.reference && referenceType == oref.referenceType && package == oref.package;
		}
		
		public override int GetHashCode ()
		{
			return reference.GetHashCode ();
		}
	}
	
	public class UnknownProjectReference: ProjectReference
	{
	}
}
