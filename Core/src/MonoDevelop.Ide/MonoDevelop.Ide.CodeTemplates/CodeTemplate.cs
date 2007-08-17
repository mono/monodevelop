//
// CodeTemplate.cs
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
using System.Xml;

namespace MonoDevelop.Ide.CodeTemplates
{
	public class CodeTemplate
	{
		string shortcut;
		string description;
		string text;
		
		public string Shortcut {
			get {
				return shortcut;
			}
			set {
				Debug.Assert (!String.IsNullOrEmpty (value));
				shortcut = value;
			}
		}
		
		public string Description {
			get {
				return description;
			}
			set {
				Debug.Assert (!String.IsNullOrEmpty (value));
				description = value;
			}
		}
		
		public string Text {
			get {
				return text;
			}
			set {
				Debug.Assert (!String.IsNullOrEmpty (value));
				text = value;
			}
		}
		
		public CodeTemplate()
		{
		}
		
		public CodeTemplate (string shortcut, string description, string text)
		{
			this.shortcut = shortcut;
			this.description = description;
			this.text = text;
		}
		
		public override string ToString ()
		{
			return String.Format ("[CodeTemplate: Shortcut={0}, Description={1}, Text={2}]", this.shortcut, this.description, this.text);
		}

#region I/O
		public const string Node          = "CodeTemplate";
		const string shortcutAttribute    = "template";
		const string descriptionAttribute = "description";
		
		public void Write (XmlWriter writer)
		{
			writer.WriteStartElement (Node);
			writer.WriteAttributeString (shortcutAttribute, this.shortcut);
			writer.WriteAttributeString (descriptionAttribute, this.description);
			writer.WriteString (this.text);
			writer.WriteEndElement (); // Node
		}
		
		public static CodeTemplate Read (XmlReader reader)
		{
			Debug.Assert (reader.LocalName == Node);
			
			CodeTemplate result = new CodeTemplate ();
			result.shortcut    = reader.GetAttribute (shortcutAttribute);
			result.description = reader.GetAttribute (descriptionAttribute);
			result.text        = reader.ReadString ();
			return result;
		}
#endregion
	}
}
