//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//

using Mono.Addins;
using MonoDevelop.Core;
using System;
using System.Collections.Specialized;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// The extensions that will be edited using the xml editor.
	/// </summary>
	public class XmlFileExtensions
	{
		static StringCollection extensions;

		XmlFileExtensions()
		{
		}
		
		public static StringCollection Extensions {
			get {
				if (extensions == null) {
					extensions = new StringCollection();
					foreach (ExtensionNode node in AddinManager.GetExtensionNodes("/MonoDevelop/XmlEditor/XmlFileExtensions")) {
						XmlFileExtensionNode xmlFileExtensionNode = node as XmlFileExtensionNode;
						if (xmlFileExtensionNode != null) {
							extensions.Add(xmlFileExtensionNode.FileExtension);
						}
					}
				}
				return extensions;
			}
		}
		
		public static bool IsXmlFileExtension(string extension)
		{
			if (extension == null) {
				return false;
			}
			
			foreach (string knownExtension in Extensions) {
				if (String.Compare(extension, knownExtension, true) == 0) {
					return true;
				}
			}
			return false;
		}
	}
	
}
