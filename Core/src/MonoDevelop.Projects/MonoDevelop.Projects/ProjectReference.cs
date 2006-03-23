// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
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
		Typelib
	}
	
	/// <summary>
	/// This class represent a reference information in an Project object.
	/// </summary>
	[DataItemAttribute ("Reference")]
	public class ProjectReference : ICloneable, ICustomDataItem
	{
		[ItemProperty ("type")]
		ReferenceType referenceType;
		
		Project ownerProject;
		
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
		
		internal void SetOwnerProject (Project project)
		{
			ownerProject = project;
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
		
		[ReadOnly(true)]
		public ReferenceType ReferenceType {
			get {
				return referenceType;
			}
		}
		
		[ReadOnly(true)]
		public string Reference {
			get {
				return reference;
			}
			set {
				reference = value;
				UpdateGacReference ();
				OnReferenceChanged(EventArgs.Empty);
			}
		}
		
		[DefaultValue(true)]
		public bool LocalCopy {
			get {
				return localCopy;
			}
			set {
				localCopy = value;
			}
		}
		
		/// <summary>
		/// Returns the file name to an assembly, regardless of what 
		/// type the assembly is.
		/// </summary>
		public string GetReferencedFileName ()
		{
			switch (ReferenceType) {
				case ReferenceType.Typelib:
					return String.Empty;
				case ReferenceType.Assembly:
					return reference;
				
				case ReferenceType.Gac:
					string file = Runtime.SystemAssemblyService.GetAssemblyLocation (GetPathToGACAssembly (this));
					return file == null ? reference : file;
				case ReferenceType.Project:
					if (ownerProject != null) {
						Combine c = ownerProject.RootCombine;
						if (c != null) {
							Project p = c.FindProject (reference);
							if (p != null) return p.GetOutputFileName ();
						}
					}
					Console.WriteLine ("Reference not found for project " + reference);
					return null;
				
				default:
					throw new NotImplementedException("unknown reference type : " + ReferenceType);
			}
		}
		
		DataCollection ICustomDataItem.Serialize (ITypeSerializer handler)
		{
			DataCollection data = handler.Serialize (this);
			string refto = reference;
			if (referenceType == ReferenceType.Assembly) {
				string basePath = Path.GetDirectoryName (handler.SerializationContext.BaseFile);
				refto = Runtime.FileUtilityService.AbsoluteToRelativePath (basePath, refto);
			} else if (referenceType == ReferenceType.Gac && loadedReference != null)
				refto = loadedReference;

			data.Add (new DataValue ("refto", refto));
			return data;
		}
		
		void ICustomDataItem.Deserialize (ITypeSerializer handler, DataCollection data)
		{
			DataValue refto = data.Extract ("refto") as DataValue;
			handler.Deserialize (this, data);
			if (refto != null) {
				reference = refto.Value;
				UpdateGacReference ();
				if (referenceType == ReferenceType.Assembly) {
					string basePath = Path.GetDirectoryName (handler.SerializationContext.BaseFile);
					reference = Runtime.FileUtilityService.RelativeToAbsolutePath (basePath, reference);
				}
			}
		}
		
		/// <summary>
		/// This method returns the absolute path to an GAC assembly.
		/// </summary>
		/// <param name ="refInfo">
		/// The reference information containing a GAC reference information.
		/// </param>
		/// <returns>
		/// the absolute path to the GAC assembly which refInfo points to.
		/// </returns>
		static string GetPathToGACAssembly(ProjectReference refInfo)
		{ // HACK : Only works on windows.
			Debug.Assert(refInfo.ReferenceType == ReferenceType.Gac);
			string[] info = refInfo.Reference.Split(',');
			
			//if (info.Length < 4) {
			return info[0];
			//	}
			
			/*string aName      = info[0];
			string aVersion   = info[1].Substring(info[1].LastIndexOf('=') + 1);
			string aPublicKey = info[3].Substring(info[3].LastIndexOf('=') + 1);
			
			return System.Environment.GetFolderPath(Environment.SpecialFolder.System) + 
			       Path.DirectorySeparatorChar + ".." +
			       Path.DirectorySeparatorChar + "assembly" +
			       Path.DirectorySeparatorChar + "GAC" +
			       Path.DirectorySeparatorChar + aName +
			       Path.DirectorySeparatorChar + aVersion + "__" + aPublicKey +
			       Path.DirectorySeparatorChar + aName + ".dll";*/
		}
		
		void UpdateGacReference ()
		{
			if (referenceType == ReferenceType.Gac) {
				string cref = Runtime.SystemAssemblyService.FindInstalledAssembly (reference);
				if (cref != null && cref != reference) {
					loadedReference = reference;
					reference = cref;
				} else
					loadedReference = null;
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
		
		protected virtual void OnReferenceChanged(EventArgs e) 
		{
			if (ReferenceChanged != null) {
				ReferenceChanged(this, e);
			}
		}
		
		public event EventHandler ReferenceChanged;
	}
}
