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
using Mono.Addins;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Core
{
	public static class StringParserService
	{
		static Dictionary<string, object> properties = new Dictionary<string, object> (StringComparer.InvariantCultureIgnoreCase);
		static Dictionary<string, GenerateString> stringGenerators = new Dictionary<string, GenerateString> (StringComparer.InvariantCultureIgnoreCase);
		static StringTagModel DefaultStringTagModel = new StringTagModel ();
		
		delegate string GenerateString (string tag, string format);
		
		public static Dictionary<string, object> Properties {
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
			return Parse (input, DefaultStringTagModel);
		}
		
		public static void Parse (ref string[] inputs)
		{
			for (int i = inputs.GetLowerBound (0); i <= inputs.GetUpperBound (0); ++i) 
				inputs[i] = Parse (inputs[i], (string[,])null);
		}
		
		public static IDictionary<string,string> Parse (IDictionary<string, string> input, IStringTagModel customTags)
		{
			Dictionary<string, string> res = new Dictionary<string, string> ();
			foreach (var e in input)
				res [e.Key] = Parse (e.Value, customTags);
			return res;
		}

		static string Replace (string tag, IStringTagModel customTags)
		{
			string tname, tformat;
			int n = tag.IndexOf (':');
			if (n != -1) {
				tname = tag.Substring (0, n);
				tformat = tag.Substring (n + 1);
			} else {
				tname = tag;
				tformat = string.Empty;
			}
			
			tname = tname.ToUpperInvariant ();
			object val = customTags.GetValue (tname);
			if (val != null)
				return FormatValue (val, tformat);
			
			if (properties.ContainsKey (tname))
				return FormatValue (properties [tname], tformat);
		
			GenerateString genString;

			if (stringGenerators.TryGetValue (tname, out genString))
				return genString (tname, tformat);
			
			if (tformat.Length > 0) {
				switch (tname) {
				case "ENV":
					foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables ()) {
						if (string.Equals (variable.Key.ToString (), tformat, StringComparison.OrdinalIgnoreCase))
							return variable.Value.ToString ();
					}
					break;
				case "PROPERTY":
					return PropertyService.Get<string> (tformat);
				}
			}
			return null;
		}
		
		static string FormatValue (object val, string format)
		{
			if (format.Length == 0)
				return val.ToString ();
			if (val is DateTime)
				return ((DateTime)val).ToString (format);
			else if (val is int)
				return ((int)val).ToString (format);
			else if (val is uint)
				return ((uint)val).ToString (format);
			else if (val is long)
				return ((long)val).ToString (format);
			else if (val is ulong)
				return ((ulong)val).ToString (format);
			else if (val is short)
				return ((short)val).ToString (format);
			else if (val is ushort)
				return ((ushort)val).ToString (format);
			else if (val is byte)
				return ((byte)val).ToString (format);
			else if (val is sbyte)
				return ((sbyte)val).ToString (format);
			else if (val is decimal)
				return ((decimal)val).ToString (format);
			else if (val is float)
				return ((float)val).ToString (format);
			else if (val is double)
				return ((double)val).ToString (format);
			else if (val is string) {
				if (format.Equals ("UPPER", StringComparison.OrdinalIgnoreCase))
					return val.ToString ().ToUpper ();
				if (format.Equals ("LOWER", StringComparison.OrdinalIgnoreCase))
					return val.ToString ().ToLower ();
				if (format.Equals ("HTMLENCODE", StringComparison.OrdinalIgnoreCase))
					return System.Net.WebUtility.HtmlEncode (val.ToString ());
			}
			return val.ToString ();
		}
		
		public static string Parse<T> (string input, Dictionary<string,T> customTags)
		{
			return Parse (input, new DictionaryStringTagModel<T> (customTags));
		}
		
		public static string Parse (string input, string[,] customTags)
		{
			Dictionary<string, object> tags = new Dictionary<string, object> (StringComparer.InvariantCultureIgnoreCase);
			if (customTags != null) {
				for (int i = 0; i < customTags.GetLength (0); ++i) {
					tags.Add (customTags[i, 0].ToUpper (), customTags[i, 1]);
				}
			}
			return Parse (input, tags);
		}
		
		static string OpenBraces  = "{(";
		static string CloseBraces = "})";
		
		public static string Parse (string input, IStringTagModel customTags)
		{
			StringBuilder result = StringBuilderCache.Allocate ();
			int brace;
			int i = 0;
			
			while (i < input.Length) {
				if (input [i] == '$') {
					i++;
					
					if (i >= input.Length || (brace = OpenBraces.IndexOf (input[i])) == -1) {
						result.Append ('$');
						continue;
					}
					
					i++;
					int start = i;
					while (i < input.Length && input [i] != CloseBraces[brace])
						i++;
					
					string tag      = input.Substring (start, i - start);
					char close      = CloseBraces[brace];
					char open       = OpenBraces[brace];
					
					string tagValue = Replace (tag, customTags) ?? string.Format ("${0}{1}{2}", open, tag, i < input.Length ? close.ToString () : "");
					result.Append (tagValue);
				} else {
					result.Append (input [i]);
				}
				i++;
			}
			return StringBuilderCache.ReturnAndFree (result);
		}
		
		public static IEnumerable<IStringTagProvider> GetProviders ()
		{
			foreach (IStringTagProvider provider in AddinManager.GetExtensionObjects (typeof(IStringTagProvider)))
				yield return provider;
			foreach (IStringTagProvider provider in customProviders)
				yield return provider;
		}

		static List<IStringTagProvider> customProviders = new List<IStringTagProvider> ();

		public static void RegisterStringTagProvider (IStringTagProvider provider)
		{
			customProviders.Add (provider);
		}

		public static void UnregisterStringTagProvider (IStringTagProvider provider)
		{
			customProviders.Remove (provider);
		}
	}
}
