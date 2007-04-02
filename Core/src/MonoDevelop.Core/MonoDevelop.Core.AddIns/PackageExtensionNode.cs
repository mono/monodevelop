
using System;
using System.IO;
using System.Collections;

namespace MonoDevelop.Core.AddIns
{
	[CodonNameAttribute ("Package")]
	public class PackageExtensionNode: AbstractCodon
	{
		[XmlMemberAttribute ("version", IsRequired=true)]
		string version;
		
		[XmlMemberAttribute("clrVersion")]
		ClrVersion clrVersion = ClrVersion.Default;
		
		string[] assemblies;
		
		public string[] Assemblies {
			get { return assemblies; }
		}
		
		public string Version {
			get { return version; }
		}
		
		public ClrVersion TargetClrVersion {
			get { return clrVersion; }
		}
		
		public override object BuildItem (object owner, ArrayList subItems, ConditionCollection conditions)
		{
			assemblies = new string [subItems.Count];
			string basePath = Path.GetDirectoryName (this.AddIn.FileName);
			for (int n=0; n<subItems.Count; n++) {
				string file = ((AssemblyExtensionNode)subItems [n]).FileName;
				file = Path.Combine (basePath, file);
				assemblies [n] = file;
			}
			return this;
		}
	}
}
