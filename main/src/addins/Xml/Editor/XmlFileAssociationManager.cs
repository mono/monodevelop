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
using System;
using System.Collections.Generic;

namespace MonoDevelop.Xml.Editor
{
	static class XmlFileAssociationManager
	{
		static Dictionary<string,XmlFileAssociation> map;
		
		static XmlFileAssociationManager ()
		{
			map = new Dictionary<string,XmlFileAssociation> (StringComparer.OrdinalIgnoreCase);
			
			foreach (XmlFileAssociationNode node in AddinManager.GetExtensionNodes ("/MonoDevelop/Xml/Editor/XmlFileAssociations")) {
				var assoc = node.GetAssociation ();
				map[assoc.Extension] = assoc;
			}
		}
		
		/// <summary>File extensions that have XML associations.</summary>
		/// <returns>LowerInvariant file extensions.</returns>
		public static IEnumerable<string> GetExtensions ()
		{
			foreach (string prop in XmlEditorOptions.GetFileExtensions ())
				if (!map.ContainsKey (prop))
					yield return prop;
			
			foreach (var item in map)
				yield return item.Key;
		}
		
		public static IEnumerable<XmlFileAssociation> GetAssociations ()
		{
			var returned = new HashSet<string> ();
			
			foreach (var assoc in XmlEditorOptions.GetFileAssociations ()) {
				returned.Add (assoc.Extension);
				yield return assoc;
			}
			
			foreach (var item in map)
				if (!returned.Contains (item.Key))
					yield return item.Value;
		}
		
		public static XmlFileAssociation GetAssociationForFileName (string filename)
		{
			foreach (var extension in GetFileExtensions (filename)) {
				var assoc = XmlEditorOptions.GetFileAssociation (extension);
				if (assoc != null)
					return assoc;
				if (map.TryGetValue (extension, out assoc))
					return assoc;
			}
			return null;
		}
		
		public static bool IsXmlFileName (string filename)
		{
			foreach (var extension in GetFileExtensions (filename)) {
				if (string.IsNullOrEmpty (extension))
					return false;
				
				if (map.ContainsKey (extension))
					return true;
				
				if (XmlEditorOptions.GetFileAssociation (extension) != null)
					return true;
			}

			return false;
		}

		static IEnumerable<string> GetFileExtensions (string filename)
		{
			int idx = 0;
			while ((idx = filename.IndexOf ('.', idx)) > -1 && (filename.Length - idx) > 1) {
				yield return filename.Substring (idx);
				idx++;
			}
		}
	}
}