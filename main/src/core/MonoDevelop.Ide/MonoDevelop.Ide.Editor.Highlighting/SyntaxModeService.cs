// SyntaxModeService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Linq;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public static class SyntaxModeService
	{
		static Dictionary<string, ColorScheme> styles      = new Dictionary<string, ColorScheme> ();
		static Dictionary<string, IStreamProvider> styleLookup      = new Dictionary<string, IStreamProvider> ();

		public static string[] Styles {
			get {
				List<string> result = new List<string> ();
				foreach (string style in styles.Keys) {
					if (!result.Contains (style))
						result.Add (style);
				}
				foreach (string style in styleLookup.Keys) {
					if (!result.Contains (style))
						result.Add (style);
				}
				return result.ToArray ();
			}
		}

		public static ColorScheme GetColorStyle (string name)
		{
			if (styles.ContainsKey (name))
				return styles [name];
			if (styleLookup.ContainsKey (name)) {
				LoadStyle (name);
				return GetColorStyle (name);
			}
			return GetColorStyle ("Default");
		}

		public static IStreamProvider GetProvider (ColorScheme style)
		{
			if (styleLookup.ContainsKey (style.Name)) 
				return styleLookup[style.Name];
			return null;
		}
		
		static void LoadStyle (string name)
		{
			if (!styleLookup.ContainsKey (name))
				throw new System.ArgumentException ("Style " + name + " not found", "name");
			var provider = styleLookup [name];
			styleLookup.Remove (name); 
			var stream = provider.Open ();
			try {
				if (provider is UrlStreamProvider) {
					var usp = provider as UrlStreamProvider;
					if (usp.Url.EndsWith (".vssettings", StringComparison.Ordinal)) {
						styles [name] = ColorScheme.Import (usp.Url, stream);
					} else {
						styles [name] = ColorScheme.LoadFrom (stream);
					}
					styles [name].FileName = usp.Url;
				} else {
					styles [name] = ColorScheme.LoadFrom (stream);
				}
			} catch (Exception e) {
				throw new IOException ("Error while loading style :" + name, e);
			} finally {
				stream.Close ();
			}
		}
		

		public static void Remove (ColorScheme style)
		{
			if (styleLookup.ContainsKey (style.Name))
				styleLookup.Remove (style.Name);

			foreach (var kv in styles) {
				if (kv.Value == style) {
					styles.Remove (kv.Key); 
					return;
				}
			}
		}
		

		public static List<ValidationEventArgs> ValidateStyleFile (string fileName)
		{
			List<ValidationEventArgs> result = new List<ValidationEventArgs> ();
			return result;
		}


		public static void LoadStylesAndModes (string path)
		{
			foreach (string file in Directory.GetFiles (path)) {
				if (file.EndsWith (".json", StringComparison.Ordinal)) {
					using (var stream = File.OpenRead (file)) {
						string styleName = ScanStyle (stream);
						if (!string.IsNullOrEmpty (styleName)) {
							styleLookup [styleName] = new UrlStreamProvider (file);
						} else {
							Console.WriteLine ("Invalid .json syntax sheme file : " + file);
						}
					}
				} else if (file.EndsWith (".vssettings", StringComparison.Ordinal)) {
					using (var stream = File.OpenRead (file)) {
						string styleName = Path.GetFileNameWithoutExtension (file);
						styleLookup [styleName] = new UrlStreamProvider (file);
					}
				}
			}
		}

		public static void LoadStylesAndModes (Assembly assembly)
		{
			foreach (string resource in assembly.GetManifestResourceNames ()) {
				if (resource.EndsWith ("Style.json", StringComparison.Ordinal)) {
					using (Stream stream = assembly.GetManifestResourceStream (resource)) {
						string styleName = ScanStyle (stream);
						styleLookup [styleName] = new ResourceStreamProvider (assembly, resource);
					}
				}
			}
		}
		static System.Text.RegularExpressions.Regex nameRegex = new System.Text.RegularExpressions.Regex ("\\s*\"name\"\\s*:\\s*\"(.*)\"\\s*,");

		static string ScanStyle (Stream stream)
		{
			try {
				var file = new StreamReader (stream);
				file.ReadLine ();
				var nameLine = file.ReadLine ();
				var match = nameRegex.Match (nameLine);
				if (!match.Success)
					return null;
				return match.Groups[1].Value;
			} catch (Exception e) {
				Console.WriteLine ("Error while scanning json:");
				Console.WriteLine (e);
				return null;
			}
		}

		public static void AddStyle (ColorScheme style)
		{
			styles [style.Name] = style;
		}

		public static void AddStyle (IStreamProvider provider)
		{
			using (var stream = provider.Open ()) {
				string styleName = ScanStyle (stream);
				styleLookup [styleName] = provider;
			}
		}

		public static void RemoveStyle (IStreamProvider provider)
		{
			using (var stream = provider.Open ()) {
				string styleName = ScanStyle (stream);
				styleLookup.Remove (styleName);
			}
		}
		
		public static ColorScheme DefaultColorStyle {
			get {
				return GetColorStyle ("Default");
			}
		}
		
		static SyntaxModeService ()
		{
			var textEditorAssembly = Assembly.Load ("Mono.TextEditor");
			if (textEditorAssembly != null) {
				LoadStylesAndModes (textEditorAssembly);
			} else {
				LoggingService.LogError ("Can't lookup Mono.TextEditor assembly. Default styles won't be loaded.");
			}
		}
	}
}