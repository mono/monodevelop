// 
// Section.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.DocFood
{
	class Section : Node
	{
		public string Name {
			get;
			set;
		}
		
		public string Documentation {
			get;
			set;
		}
		
		public bool Override {
			get;
			set;
		}
		
		public Section (string name)
		{
			this.Name = name;
		}
		
		public override void Run (DocGenerator generator, object member)
		{
			string str = StringParserService.Parse (Documentation, generator.tags).Trim ();
			if (!char.IsUpper (str[0]))
				str = char.ToUpper (str[0]) + str.Substring (1);
			if (!Override) {
				if (generator.curName != null) {
					if (generator.sections.Where (s => s.Name == Name) .Any (s => s.Attributes.Any (attr => attr.Value == generator.curName)))
						return;
				} else if (generator.sections.Any (s => s.Name == Name)) {
					return;
				}
			}
			generator.Set (Name, generator.curName, str);
		}
		
		public override string ToString ()
		{
			return string.Format ("[Section: Name={0}, Documentation={1}, Attributes={2}]", Name, Documentation, Attributes);
		}
		
		public const string XmlTag = "Section";
		public override void Write (XmlWriter writer)
		{
			writer.WriteStartElement (XmlTag);
			writer.WriteAttributeString ("name", Name);
			foreach (var pair in Attributes) {
				writer.WriteAttributeString (pair.Key, pair.Value);
			}
			writer.WriteString (Documentation);
			writer.WriteEndElement ();
		}
		
		public static Section Read (XmlReader reader)
		{
			Section result = new Section (reader.GetAttribute ("name"));
			result.Documentation = reader.ReadElementString ();
			return result;
		}
	}
}

