//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//

using Mono.Addins;
using System.Collections;

namespace MonoDevelop.XmlEditor
{
	public class XmlFileExtensionNode : ExtensionNode
	{
		[NodeAttribute("extension", true)]
		string fileExtension;
		
		public string FileExtension {
			get {
				return fileExtension;
			}
			set {
				fileExtension = value;
			}
		}
	}
}