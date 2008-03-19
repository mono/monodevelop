//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//

using Mono.Addins;
using MonoDevelop.Core;
using System;
using System.Collections.Generic;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// The extensions that will be edited using the xml editor.
	/// </summary>
	public static class XmlFileExtensions
	{
		//note: these are all stored as LowerInvariant strings
		static List<string> extensions;
		
		public static IEnumerable<string> Extensions {
			get {
				if (extensions == null) {
					extensions = new List<string> ();
					foreach (ExtensionNode node in AddinManager.GetExtensionNodes("/MonoDevelop/XmlEditor/XmlFileExtensions")) {
						XmlFileExtensionNode xmlFileExtensionNode = node as XmlFileExtensionNode;
						if (xmlFileExtensionNode != null)
							extensions.Add (xmlFileExtensionNode.FileExtension.ToLowerInvariant ());
					}
				}
				
				//get user-registered extensions stored in the properties service but don't duplicate built-in ones
				//note: XmlEditorAddInOptions returns LowerInvariant strings
				foreach (string prop in XmlEditorAddInOptions.RegisteredFileExtensions)
					if (!extensions.Contains (prop))
						yield return prop;
				
				foreach (string s in extensions)
					yield return s;
			}
		}
		
		public static bool IsXmlFileExtension (string extension)
		{
			if (string.IsNullOrEmpty (extension))
				return false;
			
			string ext = extension.ToLowerInvariant ();
			foreach (string knownExtension in Extensions)
				if (ext == knownExtension)
					return true;
			return false;
		}
	}
	
}
