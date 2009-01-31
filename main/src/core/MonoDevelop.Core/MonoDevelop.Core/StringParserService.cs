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
				stringGenerators [providedTag.ToUpper ()] = delegate (string tag, string format) {
					return tagProvider.Convert (tag, format);
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
			string tname, tformat;
			int n = tag.IndexOf (':');
			if (n != -1) {
				tname = tag.Substring (0, n);
				tformat = tag.Substring (n + 1);
			} else {
				tname = tag;
				tformat = string.Empty;
			}

			if (stringGenerators.TryGetValue (tname.ToUpper (), out genString))
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
			string Convert (string tag, string format);
		}
	}
}
