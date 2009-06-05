// 
// CodeFormatDescription.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Reflection;


namespace MonoDevelop.Projects.Text
{
	
	public class CodeFormatType
	{
		public string Name {
			get;
			set;
		} 
		
		List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>> ();
		public IList<KeyValuePair<string, string>> Values {
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
		
		public string Example {
			get;
			set;
		}
		
		internal static string Node = "Option";
		
		public override string ToString ()
		{
			return string.Format("[CodeFormatOption: Name={0}, DisplayName={1}]", Name, DisplayName);
		}
		
		public static CodeFormatOption Read (CodeFormatDescription descr, XmlReader reader)
		{
			CodeFormatOption result = new CodeFormatOption ();
			result.Name        = reader.GetAttribute ("name");
			result.DisplayName = reader.GetAttribute ("_displayName");
			string example    = reader.GetAttribute ("example");
			if (!string.IsNullOrEmpty (example))
				result.Example = descr.GetExample (example);
			if (!reader.IsEmptyElement) {
				reader.Read ();
				result.Example = reader.ReadElementString ();
				reader.Read ();
			}
			return result;
		}
	}
	
	public class CodeFormatCategory
	{
		public bool IsOptionCategory {
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
			return string.Format("[CodeFormatCategory: DisplayName={3}, Example={0}, #SubCategories={1}, #Options={2}]", Example, subCategories.Count, options.Count, DisplayName);
		}
		
		internal const string Node = "Category";
		internal const string OptionCategoryNode = "OptionCategory";
		
		public static CodeFormatCategory Read (CodeFormatDescription descr, XmlReader reader)
		{
			CodeFormatCategory result = new CodeFormatCategory ();
			result.IsOptionCategory = reader.LocalName == OptionCategoryNode;
			result.DisplayName = reader.GetAttribute ("_displayName");
			XmlReadHelper.ReadList (reader, result.IsOptionCategory ? OptionCategoryNode : Node, delegate () {
				switch (reader.LocalName) {
				case "Option":
					result.options.Add (CodeFormatOption.Read (descr, reader));
					return true;
				case CodeFormatCategory.OptionCategoryNode:
				case CodeFormatCategory.Node:
					result.subCategories.Add (CodeFormatCategory.Read (descr, reader));
					return true;
				}
				return false;
			});
			return result;
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
		
		public string GetExample (string name)
		{
			string result;
			if (!examples.TryGetValue (name, out result))
				System.Console.WriteLine ("Example:" + name + " not found.");
			return result;
		}
		
		static CodeFormatType codeFormatTypeBool = new CodeFormatType ("Bool", new KeyValuePair <string, string> ("True", "True"), new KeyValuePair <string, string> ("False", "False"));
			
		public CodeFormatType GetCodeFormatType (object settings, CodeFormatOption option)
		{
			PropertyInfo info = settings.GetType ().GetProperty (option.Name);
			if (info.PropertyType == typeof (bool))
				return codeFormatTypeBool;
			return types.FirstOrDefault (t => t.Name == info.PropertyType.Name);
		}
		
		
		const string Version          = "1.0";
		new const string Node         = "CodeStyle";
		const string VersionAttribute = "version";

		// returns value / display name
		public KeyValuePair<string, string> GetValue (object settings, CodeFormatOption option)
		{
			PropertyInfo info = settings.GetType ().GetProperty (option.Name);
			string value = info.GetValue (settings, null).ToString ();
			CodeFormatType type = GetCodeFormatType (settings, option);
			return new KeyValuePair<string, string> (value, type.GetValue (value).Value);
		}
		
		public void SetValue (object settings, CodeFormatOption option, string value)
		{
			PropertyInfo info = settings.GetType ().GetProperty (option.Name);
			object val;
			if (typeof (System.Enum).IsAssignableFrom (info.PropertyType)) {
				val = Enum.Parse (info.PropertyType, value);
			} else {
				val = Convert.ChangeType (value, info.PropertyType);
			}
			info.SetValue (settings, val, null);
		}
		
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
}
