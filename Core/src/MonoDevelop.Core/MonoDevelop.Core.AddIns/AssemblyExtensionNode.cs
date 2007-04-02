
using System;
using System.Collections;

namespace MonoDevelop.Core.AddIns
{
	[CodonNameAttribute("Assembly")]
	class AssemblyExtensionNode: AbstractCodon
	{
		[XmlMemberAttribute ("file", IsRequired=true)]
		string file;
		
		public string FileName {
			get { return file; }
		}
		
		public override object BuildItem(object owner, ArrayList subItems, ConditionCollection conditions)
		{
			return this;
		}
	}
}
