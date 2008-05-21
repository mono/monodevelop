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
using MonoDevelop.Projects.Serialization;

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
	public class ProjectReference : ICloneable, IExtendedDataItem
	{
		Hashtable extendedProperties;
		
		[ItemProperty ("type")]
		ReferenceType referenceType;
		
		DotNetProject ownerProject;
		
		string reference = String.Empty;
		
		// A project may reference assemblies which are not available
		// in the system where it is opened. For example, opening
		// a project that references gtk# 2.4 in a system with gtk# 2.6.
		// In this case the reference will be upgraded to 2.6, but for
		// consistency reasons the reference will still be saved as 2.4.
		// The loadedReference stores the reference initially loaded,
		// so it can be saved again.
		string loadedReference;
		
		[ItemProperty ("localcopy")]
		bool localCopy = true;
		
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
			this.referenceType = referenceType;
			this.reference     = reference;
			UpdateGacReference ();
		}
		
		public ProjectReference (Project referencedProject)
		{
			referenceType = ReferenceType.Project;
			reference = referencedProject.Name;
		}
		
		public IDictionary ExtendedProperties {
			get {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
			}
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
				if (loadedReference != null)
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
					string file = Runtime.SystemAssemblyService.GetAssemblyLocation (Reference);
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
				string cref = Runtime.SystemAssemblyService.FindInstalledAssembly (reference);
				if (ownerProject != null) {
					if (cref == null)
						cref = reference;
					cref = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (cref, ownerProject.ClrVersion);
				}
				if (cref != null && cref != reference) {
					if (loadedReference == null) {
						loadedReference = reference;
					}
					reference = cref;
				}
			}
		}
		
		public object Clone()
		{
			return MemberwiseClone();
		}
		
		public override bool Equals (object other)
		{
			ProjectReference oref = other as ProjectReference;
			if (oref == null) return false;
			
			return reference == oref.reference && referenceType == oref.referenceType;
		}
		
		public override int GetHashCode ()
		{
			return reference.GetHashCode ();
		}
	}
	
	public class UnknownProjectReference: ProjectReference, IExtendedDataItem
	{
		Hashtable props;
		
		IDictionary IExtendedDataItem.ExtendedProperties {
			get {
				if (props == null)
					props = new Hashtable ();
				return props;
			}
		}
	}
}
