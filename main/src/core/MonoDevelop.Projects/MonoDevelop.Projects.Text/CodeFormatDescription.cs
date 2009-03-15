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
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects.Text
{
	
	public class CodeFormatType
	{
		public string Name {
			get;
			set;
		} 
		
		List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>> ();
		public IEnumerable<KeyValuePair<string, string>> Values {
			get {
				return values;
			}
		}
		
		internal const string Node = "Type";
		
		public CodeFormatType ()
		{
		}
		public KeyValuePair<string, string> GetValue (string name)
		{
			return values.Find (x => x.Key == name);
		}
		
		public CodeFormatType (string name)
		{
			this.Name = name;
		}
		
		public CodeFormatType (string name, params KeyValuePair<string, string>[] values)
		{
			this.Name = name;
			this.values.AddRange (values);
		}
		
		public override string ToString ()
		{
			return string.Format("[CodeFormatType: Name={0}, #Values={1}]", Name, values.Count);
		}
		
		public static CodeFormatType Read (XmlReader reader)
		{
			CodeFormatType result = new CodeFormatType ();
			result.Name = reader.GetAttribute ("name");
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case "Value":
					string displayName = reader.GetAttribute ("_displayName");
					string name        = reader.ReadElementString ();
					result.values.Add (new KeyValuePair <string, string> (name, displayName ?? name));
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
		
		public string Example {
			get;
			set;
		}
		
		internal static string Node = "Option";
		
		public override string ToString ()
		{
			return string.Format("[CodeFormatOption: Name={0}, DisplayName={1}, Type={2}]", Name, DisplayName, Type);
		}
		
		public static CodeFormatOption Read (CodeFormatDescription descr, XmlReader reader)
		{
			CodeFormatOption result = new CodeFormatOption ();
			result.Name        = reader.GetAttribute ("name");
			result.DisplayName = reader.GetAttribute ("_displayName");
			result.Type        = reader.GetAttribute ("type");
			string example    = reader.GetAttribute ("example");
			if (!string.IsNullOrEmpty (example))
				result.Example = descr.GetExample (example);
			if (!reader.IsEmptyElement) {
				reader.Read ();
				result.Example = reader.ReadElementString ();
			}
			return result;
		}
	}
	
	public class CodeFormatCategory
	{
		public string Name {
			get;
			set;
		}
		
		public string DisplayName {
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
		
		public IEnumerable<CodeFormatOption> AllOptions {
			get {
				foreach (CodeFormatOption option in Options) {
					yield return option;
				}
				foreach (CodeFormatCategory cat in SubCategories) {
					foreach (CodeFormatOption option in cat.AllOptions) {
						yield return option;
					}
				}
			}
		}
		
		public override string ToString ()
		{
			return string.Format("[CodeFormatCategory: Name={0}, DisplayName={4}, Example={1}, #SubCategories={2}, #Options={3}]", Name, Example, subCategories.Count, options.Count, DisplayName);
		}
		
		internal const string Node = "Category";
		
		public static CodeFormatCategory Read (CodeFormatDescription descr, XmlReader reader)
		{
			CodeFormatCategory result = new CodeFormatCategory ();
			result.DisplayName = reader.GetAttribute ("_displayName");
			result.Name        = reader.GetAttribute ("name");
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case "Option":
					result.options.Add (CodeFormatOption.Read (descr, reader));
					return true;
				case CodeFormatCategory.Node:
					result.subCategories.Add (CodeFormatCategory.Read (descr, reader));
					return true;
				}
				return false;
			});
			return result;
		}
	}
	
	public class CodeFormatSettings
	{
		public string Name {
			get;
			set;
		}
		
		Dictionary<string, string> properties = new Dictionary<string, string> ();
		public Dictionary<string, string> Properties {
			get {
				return properties;
			}
		}
		
		public CodeFormatSettings (string name)
		{
			this.Name = name;
		}
		
		public CodeFormatSettings (CodeFormatSettings copyFrom, string name)
		{
			this.Name = name;
			if (copyFrom != null) {
				foreach (KeyValuePair<string, string> item in copyFrom.Properties) {
					properties.Add (item.Key, item.Value);
				}
			}
		}
		
		public void SetValue (CodeFormatOption option, string value)
		{
			properties[option.Name] = value;
		}
		
		public KeyValuePair<string, string> GetValue (CodeFormatDescription descr, CodeFormatOption option)
		{
			string result;
			CodeFormatType type = descr.GetCodeFormatType (option.Type);
			if (properties.TryGetValue (option.Name, out result))
				return type.GetValue (result);
			return type.Values.FirstOrDefault ();
		}
	}
	
	public class CodeFormatDescription : CodeFormatCategory
	{
		Dictionary<string, string> examples = new Dictionary<string, string> ();
		List<CodeFormatType> types = new List<CodeFormatType> ();
		
		public string MimeType {
			get;
			set;
		}
	
		public IEnumerable<CodeFormatType> Types {
			get {
				return types;
			}
		}
		
		public CodeFormatDescription()
		{
		}
		
		public void ExportSettings (CodeFormatSettings settings, string fileName)
		{
			// todo
		}
		
		public CodeFormatSettings ImportSettings (string fileName)
		{
			// todo
			return new CodeFormatSettings (System.IO.Path.GetFileNameWithoutExtension (fileName));
		}
		
		public string GetExample (string name)
		{
			string result;
			if (!examples.TryGetValue (name, out result))
				System.Console.WriteLine ("Example:" + name + " not found.");
			return result;
		}
		
		static CodeFormatType codeFormatTypeBool = new CodeFormatType ("Bool", new KeyValuePair <string, string> ("True", "True"), new KeyValuePair <string, string> ("False", "False"));
			
		public CodeFormatType GetCodeFormatType (string name)
		{
			if (name == "Bool")
				return codeFormatTypeBool;
			return types.FirstOrDefault (t => t.Name == name);
		}
		
		
		const string Version          = "1.0";
		new const string Node         = "CodeStyle";
		const string VersionAttribute = "version";

		public static CodeFormatDescription Load (string fileName)
		{
			using (XmlReader reader = XmlTextReader.Create (fileName)) {
				return Read (reader);
			}
		}
		public override string ToString ()
		{
			return string.Format("[CodeFormatDescription: MimeType={0}, #Types={1}, #Categories={2}]", MimeType, types.Count, subCategories.Count);
		}

	 	public static CodeFormatDescription Read (XmlReader reader)
		{
			CodeFormatDescription result = new CodeFormatDescription ();
			while (reader.Read ()) {
				if (reader.IsStartElement ()) {
					switch (reader.LocalName) {
					case Node:
						string fileVersion = reader.GetAttribute (VersionAttribute);
						if (fileVersion != Version) 
							return result;
						result.MimeType = reader.GetAttribute ("mimeType");
						break;
					case "Example":
						string name = reader.GetAttribute ("name");
						result.examples[name] = reader.ReadElementString ();
						break;
					case CodeFormatType.Node:
						result.types.Add (CodeFormatType.Read (reader));
						break;
					case CodeFormatCategory.Node:
						result.subCategories.Add (CodeFormatCategory.Read (result, reader));
						break;
					}
				}
			}
			return result;
		}
	}
	
	[DataItem ("CodeFormat")]
	public class CodeFormattingPolicy  : IEquatable<CodeFormattingPolicy>
	{
		[ItemProperty]
		public string MimeType { 
			get; 
			set; 
		}
		
		[ItemProperty]
		public string CodeStyle { 
			get; 
			set; 
		}
		
		public virtual CodeFormatSettings GetSettings ()
		{
			return TextFileService.GetSettings (MimeType, CodeStyle);
		}
	
		#region IEquatable<CodeFormattingPolicy> implementation
		public bool Equals (CodeFormattingPolicy other)
		{
			return other != null && CodeStyle == other.CodeStyle && MimeType == other.MimeType;
		}
		#endregion
	}
}
