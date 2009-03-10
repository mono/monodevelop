// 
// CodeFormatDescription.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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

namespace MonoDevelop.Projects.Text
{
	public class CodeFormatType
	{
		public string Name {
			get;
			set;
		} 
		
		List<string> values = new List<string> ();
		public IEnumerable<string> Values {
			get {
				return values;
			}
		}
		internal const string Node = "Type";
		
		public static CodeFormatType Read (XmlReader reader)
		{
			CodeFormatType result = new CodeFormatType ();
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case "Value":
					result.values.Add (reader.ReadElementString ());
					return true;
				}
				return false;
			});
			return result;
		}
	}

	public class CodeFormatOption
	{
		public string Name {
			get;
			set;
		} 
		
		public string DisplayName {
			get;
			set;
		}
		
		public string Type {
			get;
			set;
		}
		internal static string Node = "Option";
		
		public static CodeFormatOption Read (XmlReader reader)
		{
			CodeFormatOption result = new CodeFormatOption ();
			result.Name = reader.ReadElementString ();
			return result;
		}
	}
	
	public class CodeFormatCategory
	{
		public string Name {
			get;
			set;
		}
		public string Example {
			get;
			set;
		}
		
		protected List<CodeFormatCategory> subCategories = new List<CodeFormatCategory> ();
		public IEnumerable<CodeFormatCategory> SubCategories {
			get {
				return subCategories;
			}
		}
		
		protected List<CodeFormatOption> options = new List<CodeFormatOption> ();
		public IEnumerable<CodeFormatOption> Options {
			get {
				return options;
			}
		}
		internal const string Node = "Category";
		
		public static CodeFormatCategory Read (XmlReader reader)
		{
			CodeFormatCategory result = new CodeFormatCategory ();
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case "Option":
					result.options.Add (CodeFormatOption.Read (reader));
					return true;
				case CodeFormatCategory.Node:
					result.subCategories.Add (CodeFormatCategory.Read (reader));
					return true;
				}
				return false;
			});
			return result;
		}
	}
	
	public class CodeFormatDescription : CodeFormatCategory
	{
		List<CodeFormatType> types = new List<CodeFormatType> ();
		
		public CodeFormatDescription()
		{
		}
		
		public static CodeFormatDescription Load (string fileName)
		{
			CodeFormatDescription result = new CodeFormatDescription ();
			XmlReader reader = XmlTextReader.Create (fileName);
			try {
				result.Read (reader);
			} finally {
				reader.Close ();
			}
			return result;
		}
		
		const string Version          = "1.0";
		new const string Node             = "CodeFormat";
		const string VersionAttribute = "version";

		public new void Read (XmlReader reader)
		{
			while (reader.Read ()) {
				if (reader.IsStartElement ()) {
					switch (reader.LocalName) {
					case Node:
						string fileVersion = reader.GetAttribute (VersionAttribute);
						if (fileVersion != Version) 
							return;
						break;
					case CodeFormatType.Node:
						types.Add (CodeFormatType.Read (reader));
						break;
					case CodeFormatCategory.Node:
						subCategories.Add (CodeFormatCategory.Read (reader));
						break;
					}
				}
			}
		}

	}
}
