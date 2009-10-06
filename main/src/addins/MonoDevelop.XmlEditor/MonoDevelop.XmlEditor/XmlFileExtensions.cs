//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
