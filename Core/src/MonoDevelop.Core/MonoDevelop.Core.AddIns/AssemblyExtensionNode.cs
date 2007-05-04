
using System;
using System.Collections;
using Mono.Addins;

namespace MonoDevelop.Core.AddIns
{
	[ExtensionNode("Assembly")]
	class AssemblyExtensionNode: TypeExtensionNode
	{
		[NodeAttribute ("file", Required=true)]
		string file;
		
		public string FileName {
			get { return file; }
		}
	}
}
