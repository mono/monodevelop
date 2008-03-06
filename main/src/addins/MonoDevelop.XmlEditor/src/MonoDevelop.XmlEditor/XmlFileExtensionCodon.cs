//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Core.AddIns;
using System.Collections;

namespace MonoDevelop.XmlEditor
{
	[CodonNameAttribute("XmlFileExtension")]
	internal class XmlFileExtensionCodon : AbstractCodon
	{
		[XmlMemberAttribute("extension", IsRequired = true)]
		string fileExtension;
		
		public string FileExtension {
			get {
				return fileExtension;
			}
			set {
				fileExtension = value;
			}
		}
		
		public override object BuildItem(object owner, ArrayList subItems, ConditionCollection conditions)
		{
			return this;
		}
	}
}