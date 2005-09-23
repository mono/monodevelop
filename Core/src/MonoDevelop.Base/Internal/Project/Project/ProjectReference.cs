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
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using System.ComponentModel;
using MonoDevelop.Gui.Components;
using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Serialization;

namespace MonoDevelop.Internal.Project
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
	public class ProjectReference : LocalizedObject, ICloneable, ICustomDataItem
	{
		[ItemProperty ("type")]
		ReferenceType referenceType;
		
		Project ownerProject;
		
		string reference = String.Empty;
		
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
		}
		
		public ProjectReference (Project referencedProject)
		{
			referenceType = ReferenceType.Project;
			reference = referencedProject.Name;
		}
		
		[ReadOnly(true)]
		[LocalizedProperty("${res:MonoDevelop.Internal.Project.ProjectReference.ReferenceType}",
		                   Description ="${res:MonoDevelop.Internal.Project.ProjectReference.ReferenceType.Description})")]
		public ReferenceType ReferenceType {
			get {
				return referenceType;
			}
			set {
				referenceType = value;
			}
		}
		
		[ReadOnly(true)]
		[LocalizedProperty("${res:MonoDevelop.Internal.Project.ProjectReference.Reference}",
		                   Description = "${res:MonoDevelop.Internal.Project.ProjectReference.Reference.Description}")]
		public string Reference {
			get {
				return reference;
			}
			set {
				reference = value;
				OnReferenceChanged(EventArgs.Empty);
			}
		}
		
		[LocalizedProperty("${res:MonoDevelop.Internal.Project.ProjectReference.LocalCopy}",
		                   Description = "${res:MonoDevelop.Internal.Project.ProjectReference.LocalCopy.Description}")]
		[DefaultValue(true)]
		public bool LocalCopy {
			get {
				return localCopy;
			}
			set {
				localCopy = value;
				Runtime.ProjectService.SaveCombine();
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
			}
			data.Add (new DataValue ("refto", refto));
			return data;
		}
		
		void ICustomDataItem.Deserialize (ITypeSerializer handler, DataCollection data)
		{
			DataValue refto = data.Extract ("refto") as DataValue;
			handler.Deserialize (this, data);
			if (refto != null) {
				reference = refto.Value;
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
