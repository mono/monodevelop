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
		static Dictionary<string, GenerateString> stringGenerators = new Dictionary<string, GenerateString> ();
		
		delegate string GenerateString (string tag);
		
		public static Dictionary<string, string> Properties {
			get {
				return properties;
			}
		}
		
		static StringParserService ()
		{
			stringGenerators.Add ("DATE", delegate (string tag) { return DateTime.Today.ToShortDateString (); });
			stringGenerators.Add ("YEAR", delegate (string tag) { return DateTime.Today.Year.ToString (); });
			stringGenerators.Add ("MONTH", delegate (string tag) { return DateTime.Today.Month.ToString (); });
			stringGenerators.Add ("DAY", delegate (string tag) { return DateTime.Today.Day.ToString (); });
			stringGenerators.Add ("TIME", delegate (string tag) { return DateTime.Now.ToShortTimeString (); });
			stringGenerators.Add ("HOUR", delegate (string tag) { return DateTime.Now.Hour.ToString (); });
			stringGenerators.Add ("MINUTE", delegate (string tag) { return DateTime.Now.Minute.ToString (); });
			stringGenerators.Add ("SECOND", delegate (string tag) { return DateTime.Now.Second.ToString (); });
			stringGenerators.Add ("USER", delegate (string tag) { return Environment.UserName; });
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
			foreach (string providedTag in tagProvider.Tags) { 
				stringGenerators [providedTag.ToUpper ()] = delegate (string tag) {
					return tagProvider.Convert (tag);
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
			if (stringGenerators.TryGetValue (tag.ToUpper (), out genString))
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
