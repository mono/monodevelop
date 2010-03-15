//
// StringParserService.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Text;

using MonoDevelop.Core;

namespace MonoDevelop.Core
{
	public static class StringParserService
	{
		static Dictionary<string, string> properties = new Dictionary<string, string> (StringComparer.InvariantCultureIgnoreCase);
		static Dictionary<string, GenerateString> stringGenerators = new Dictionary<string, GenerateString> (StringComparer.InvariantCultureIgnoreCase);
		
		delegate string GenerateString (string tag, string format);
		
		public static Dictionary<string, string> Properties {
			get {
				return properties;
			}
		}
		
		static StringParserService ()
		{
			stringGenerators.Add ("DATE", delegate (string tag, string format) {
				if (format.Length == 0)
					return DateTime.Today.ToShortDateString ();
				else
					return DateTime.Today.ToString (format);
			}
			);
			stringGenerators.Add ("TIME", delegate (string tag, string format) {
				if (format.Length == 0)
					return DateTime.Now.ToShortTimeString (); 
				else
					return DateTime.Now.ToString (format);
			});
			
			stringGenerators.Add ("YEAR", delegate (string tag, string format) { return DateTime.Today.Year.ToString (format); });
			stringGenerators.Add ("MONTH", delegate (string tag, string format) { return DateTime.Today.Month.ToString (format); });
			stringGenerators.Add ("DAY", delegate (string tag, string format) { return DateTime.Today.Day.ToString (format); });
			stringGenerators.Add ("HOUR", delegate (string tag, string format) { return DateTime.Now.Hour.ToString (format); });
			stringGenerators.Add ("MINUTE", delegate (string tag, string format) { return DateTime.Now.Minute.ToString (format); });
			stringGenerators.Add ("SECOND", delegate (string tag, string format) { return DateTime.Now.Second.ToString (format); });
			stringGenerators.Add ("USER", delegate (string tag, string format) { return Environment.UserName; });
		}
		
		public static string Parse (string input)
		{
			return Parse (input, (string[,])null);
		}
		
		public static void Parse (ref string[] inputs)
		{
			for (int i = inputs.GetLowerBound (0); i <= inputs.GetUpperBound (0); ++i) 
				inputs[i] = Parse (inputs[i], (string[,])null);
		}
		
		public static void RegisterStringTagProvider (IStringTagProvider tagProvider)
		{
			foreach (string providedTag in tagProvider.Tags) { 
				stringGenerators [providedTag] = delegate (string tag, string format) {
					return tagProvider.Convert (tag, format);
				};
			}
		}
		
		static string Replace (string tag, Dictionary<string, string> customTags)
		{
			if (customTags.ContainsKey(tag))
				return customTags[tag];
			if (properties.ContainsKey (tag))
				return properties [tag];
		
			GenerateString genString;
			string tname, tformat;
			int n = tag.IndexOf (':');
			if (n != -1) {
				tname = tag.Substring (0, n);
				tformat = tag.Substring (n + 1);
			} else {
				tname = tag;
				tformat = string.Empty;
			}

			if (stringGenerators.TryGetValue (tname, out genString))
				return genString (tname, tformat);
			
			if (tformat.Length > 0) {
				switch (tname.ToUpper()) {
				case "ENV":
					foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables ()) {
						if (variable.Key.ToString ().ToUpper () == tformat.ToUpper ())
							return variable.Value.ToString ();
					}
					break;
				case "PROPERTY":
					return PropertyService.Get<string> (tformat);
				}
			}
			return null;
		}
		
		public static string Parse (string input, Dictionary<string, string> customTags)
		{
			StringBuilder result = new StringBuilder (input.Length);
			int i = 0;
			while (i < input.Length) {
				if (input [i] == '$') {
					i++;
					if (i >= input.Length || input[i] != '{') {
						result.Append ('$');
						continue;
					}
					i++;
					int start = i;
					while (i < input.Length && input [i] != '}')
						i++;
					string tag      = input.Substring (start, i - start);
					string tagValue = Replace (tag, customTags) ?? "${" + tag + (i < input.Length ? "}" : "");
					result.Append (tagValue);
				} else {
					result.Append (input [i]);
				}
				i++;
			}
			return result.ToString ();
		}
		
		public static string Parse (string input, string[,] customTags)
		{
			Dictionary<string, string> tags = new Dictionary<string, string> (StringComparer.InvariantCultureIgnoreCase);
			if (customTags != null) {
				for (int i = 0; i < customTags.GetLength (0); ++i) {
					tags.Add (customTags[i, 0].ToUpper (), customTags[i, 1]);
				}
			}
			return Parse (input, tags);
		}
		
		public static string Parse (string input, CustomTagStore customTags)
		{
			return Parse (input, customTags.ToDictionary ());
		}
		
		public interface IStringTagProvider 
		{
			IEnumerable<string> Tags {
				get;
			}
			string Convert (string tag, string format);
		}
	}
	
	public class CustomTagStore
	{
		Dictionary<string,CustomTag> tags = new Dictionary<string, CustomTag> ();
		Dictionary<string,string> aliases = new Dictionary<string, string> ();
		List<CustomTagStore> stores;
		
		public void Add (string tag, string value)
		{
			Add (tag, value);
		}
		
		public void Add (string tag, string value, string description)
		{
			tags [tag] = new CustomTag (tag, value, description);
		}
		
		public void AddAlias (string tag, params string[] aliases)
		{
			CustomTag ct;
			if (!tags.TryGetValue (tag, out ct))
				throw new InvalidOperationException ("Tag not registered: " + tag);
			foreach (string t in aliases)
				this.aliases [t] = ct.Value;
		}
		
		public void Add (CustomTagStore store)
		{
			if (stores == null)
				stores = new List<CustomTagStore> ();
			stores.Add (store);
		}
		
		public IEnumerable<CustomTag> Tags {
			get {
				foreach (CustomTag t in tags.Values)
					yield return t;
				if (stores != null) {
					foreach (CustomTagStore s in stores) {
						foreach (CustomTag t in s.Tags)
							yield return t;
					}
				}
			}
		}
		
		public Dictionary<string,string> ToDictionary ()
		{
			if (stores != null) {
				foreach (CustomTagStore s in stores) {
					foreach (KeyValuePair<string,string> ct in s.ToDictionary ()) {
						if (!aliases.ContainsKey (ct.Key))
							aliases [ct.Key] = ct.Value;
					}
				}
			}
			foreach (CustomTag t in tags.Values)
				aliases [t.Tag] = t.Value;
			return aliases;
		}
		
		public string Parse (string input)
		{
			return StringParserService.Parse (input, this);
		}
	}
	
	public class CustomTag
	{
		string tag;
		string value;
		string description;
		ValueGenerator valueGenerator;
		
		public delegate string ValueGenerator (string tag);
		
		public CustomTag (string tag, string value): this (tag, value, null)
		{
		}
		
		public CustomTag (string tag, string value, string description)
		{
			this.tag = tag;
			this.value = value;
			this.description = description;
		}
		
		public CustomTag (string tag, ValueGenerator valueGenerator): this (tag, valueGenerator, null)
		{
		}
		
		public CustomTag (string tag, ValueGenerator valueGenerator, string description)
		{
			this.tag = tag;
			this.valueGenerator = valueGenerator;
			this.description = description;
		}
		
		public string Tag {
			get { return this.tag; }
		}

		public string Value {
			get {
				if (valueGenerator != null)
					return valueGenerator (tag);
				else
					return this.value;
			}
		}

		public string Description {
			get { return this.description; }
		}
	}
}
