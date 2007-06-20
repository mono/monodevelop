
using System;
using System.IO;
using System.Collections;
using Mono.Addins;

namespace MonoDevelop.Core.AddIns
{
	[ExtensionNode ("Package")]
	[ExtensionNodeChild (typeof(AssemblyExtensionNode))]
	public class PackageExtensionNode: TypeExtensionNode
	{
		[NodeAttribute ("version", Required=true)]
		string version;
		
		[NodeAttribute("clrVersion")]
		ClrVersion clrVersion = ClrVersion.Default;
		
		[NodeAttribute ("gacRoot")]
		bool hasGacRoot;
		
		string[] assemblies;
		
		public string[] Assemblies {
			get {
				if (assemblies == null) {
					assemblies = new string [ChildNodes.Count];
					for (int n=0; n<ChildNodes.Count; n++) {
						string file = ((AssemblyExtensionNode)ChildNodes [n]).FileName;
						file = base.Addin.GetFilePath (file);
						assemblies [n] = file;
					}
				}
				return assemblies; 
			}
		}
		
		public string Version {
			get { return version; }
		}
		
		public ClrVersion TargetClrVersion {
			get { return clrVersion; }
		}
		
		public string GacRoot {
			get {
				if (hasGacRoot)
					return Addin.GetFilePath (".");
				else
					return null;
			}
		}
	}
}
