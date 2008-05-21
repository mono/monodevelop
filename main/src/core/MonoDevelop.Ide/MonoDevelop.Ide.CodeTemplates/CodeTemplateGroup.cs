//
// CodeTemplateGroup.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.CodeTemplates
{
	/// <summary>
	/// This class reperesents a single Code Template
	/// </summary>
	public class CodeTemplateGroup
	{
		List<string>       extensions = new List<string> ();
		List<CodeTemplate> templates  = new List<CodeTemplate> ();
		
		public List<string> Extensions {
			get {
				return extensions;
			}
		}
		
		public List<CodeTemplate> Templates {
			get {
				return templates;
			}
		}
		
		public string[] ExtensionStrings {
			get {
				return extensions.ToArray ();
			}
			set {
				extensions.Clear();
				extensions.AddRange (value);
			}
		}
		
		public CodeTemplateGroup ()
		{
		}
		
		public CodeTemplateGroup (string extensions)
		{
			if (!String.IsNullOrEmpty (extensions))
				this.ExtensionStrings = extensions.Split (';');
		}

#region I/O
		public const string Node = "CodeTemplateGroup";
		const string extensionsAttribute = "extensions";
		
		public static CodeTemplateGroup Read (XmlReader reader)
		{
			Debug.Assert (reader.LocalName == Node);
			
			CodeTemplateGroup result = new CodeTemplateGroup ();
			if (!String.IsNullOrEmpty (reader.GetAttribute (extensionsAttribute)))
				result.ExtensionStrings = reader.GetAttribute (extensionsAttribute).Split (';');
			
			while (reader.Read ()) {
				if (reader.IsStartElement ()) {
					switch (reader.LocalName) {
					case CodeTemplate.Node:
						result.templates.Add (CodeTemplate.Read (reader));
						break;
					}
				}
				else
					break;
			}
			return result;
		}
		
		public void Write (XmlWriter writer)
		{
			writer.WriteStartElement (Node);
			writer.WriteAttributeString (extensionsAttribute, String.Join(";", ExtensionStrings));
			foreach (CodeTemplate template in this.templates)
				template.Write (writer);
			writer.WriteEndElement (); // Node
		}
#endregion
	}
}
