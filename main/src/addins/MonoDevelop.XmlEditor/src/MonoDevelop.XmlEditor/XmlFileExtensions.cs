//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

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
					XmlFileExtensionCodon[] xmlFileExtensionCodons = (XmlFileExtensionCodon[])Runtime.AddInService.GetTreeItems("/MonoDevelop/XmlEditor/XmlFileExtensions", typeof(XmlFileExtensionCodon));
					extensions = new StringCollection();
					foreach (XmlFileExtensionCodon codon in xmlFileExtensionCodons) {
						extensions.Add(codon.FileExtension);
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
