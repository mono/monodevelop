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
		static Dictionary<string, string> properties = new Dictionary<string, string> ();
		static Dictionary<string, GenerateString> stringTagProviders = new Dictionary<string, GenerateString> ();
		
		delegate string GenerateString (string propertyName);
		
		public static Dictionary<string, string> Properties {
			get {
				return properties;
			}
		}
		
		static StringParserService ()
		{
			stringTagProviders.Add ("DATE", delegate (string pn) { return DateTime.Today.ToShortDateString (); });
			stringTagProviders.Add ("YEAR", delegate (string pn) { return DateTime.Today.Year.ToString (); });
			stringTagProviders.Add ("MONTH", delegate (string pn) { return DateTime.Today.Month.ToString (); });
			stringTagProviders.Add ("DAY", delegate (string pn) { return DateTime.Today.Day.ToString (); });
			stringTagProviders.Add ("TIME", delegate (string pn) { return DateTime.Now.ToShortTimeString (); });
			stringTagProviders.Add ("HOUR", delegate (string pn) { return DateTime.Now.Hour.ToString (); });
			stringTagProviders.Add ("MINUTE", delegate (string pn) { return DateTime.Now.Minute.ToString (); });
			stringTagProviders.Add ("SECOND", delegate (string pn) { return DateTime.Now.Second.ToString (); });
			stringTagProviders.Add ("USER", delegate (string pn) { return Environment.UserName; });
		}
		
		public static string Parse (string input)
		{
			return Parse (input, null);
		}
		
		public static void Parse (ref string[] inputs)
		{
			for (int i = inputs.GetLowerBound (0); i <= inputs.GetUpperBound (0); ++i) 
				inputs[i] = Parse (inputs[i], null);
		}
		
		public static void RegisterStringTagProvider (IStringTagProvider tagProvider)
		{
			foreach (string tag in tagProvider.Tags) { 
				stringTagProviders [tag.ToUpper ()] = delegate (string propertyName) {
					return tagProvider.Convert (propertyName);
				};
			}
		}
		
		static string Replace (string tag, string[,] customTags)
		{
			if (customTags != null) {
				for (int i = 0; i < customTags.GetLength (0); ++i) {
					if (tag.ToUpper () == customTags[i, 0].ToUpper ()) 
						return customTags[i, 1];
				}
			}
			
			if (properties.ContainsKey (tag.ToUpper ()))
				return properties [tag.ToUpper ()];
		
			GenerateString genString;
			if (stringTagProviders.TryGetValue (tag.ToUpper (), out genString))
				return genString (tag);
			
			int idx = tag.IndexOf (':');
			if (idx > 0) {
				string descriptor = tag.Substring (0, idx);
				string value      = tag.Substring (idx + 1);
				switch (descriptor.ToUpper()) {
				case "ENV":
					foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables ()) {
						if (variable.Key.ToString ().ToUpper () == value.ToUpper ())
							return variable.Value.ToString ();
					}
					break;
				case "PROPERTY":
					return PropertyService.Get<string> (value);
				default:
					throw new Exception (String.Format ("Descriptor '{0}' unknown. Valid is 'env', 'property'.", descriptor));
				}
			}
			return null;
		}
			
		public static string Parse (string input, string [,] customTags)
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
		
		public interface IStringTagProvider 
		{
			IEnumerable<string> Tags {
				get;
			}
			string Convert (string tag);
		}
	}
}
