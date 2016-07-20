//
// TextMatePlistFormat.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using MonoDevelop.Components;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	static class TextMateFormat
	{
		#region Themes

		public static EditorTheme LoadEditorTheme (Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException (nameof (stream));
			var dictionary = PDictionary.FromStream (stream);
			var name = (PString)dictionary ["name"];
			var contentArray = dictionary ["settings"] as PArray;
			if (contentArray == null || contentArray.Count == 0)
				return new EditorTheme (name);

			var settings = new List<ThemeSetting> ();
			for (int i = 0; i < contentArray.Count; i++) {
				var dict = contentArray [i] as PDictionary;
				if (dict == null)
					continue;
				var themeSetting = LoadThemeSetting (dict);
				if (i == 0)
					themeSetting = CalculateMissingColors (themeSetting);
				settings.Add (themeSetting);
			}
			var uuid = (PString)dictionary ["uuid"];

			return new EditorTheme (name, settings, uuid);
		}

		public static void Save (TextWriter writer, EditorTheme theme)
		{
			writer.WriteLine ("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			writer.WriteLine ("<!DOCTYPE plist PUBLIC \"-//Apple Computer//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
			writer.WriteLine ("<plist version=\"1.0\">");
			writer.WriteLine ("<dict>");
			writer.WriteLine ("\t<key>name</key>");
			writer.WriteLine ("\t<string>" + Ambience.EscapeText (theme.Name) + "</string>");
			writer.WriteLine ("\t<key>settings</key>");
			writer.WriteLine ("\t<array>");
			foreach (var setting in theme.Settings) {
				writer.WriteLine ("\t\t<dict>");
				if (setting.Name != null) {
					writer.WriteLine ("\t\t\t<key>name</key>");
					writer.WriteLine ("\t\t\t<string>" + Ambience.EscapeText (setting.Name) + "</string>");
				}
				if (setting.Scopes.Count > 0) {
					writer.WriteLine ("\t\t\t<key>scope</key>");
					writer.WriteLine ("\t\t\t<string>" + Ambience.EscapeText (string.Join (", ", setting.Scopes)) + "</string>");
				}
				if (setting.Settings.Count > 0) {
					writer.WriteLine ("\t\t\t<key>settings</key>");
					writer.WriteLine ("\t\t\t<dict>");
					foreach (var kv in setting.Settings) {
						writer.WriteLine ("\t\t\t\t<key>" + Ambience.EscapeText (kv.Key) + "</key>");
						writer.WriteLine ("\t\t\t\t<string>" + Ambience.EscapeText (kv.Value) + "</string>");
					}
					writer.WriteLine ("\t\t\t</dict>");
				}
				writer.WriteLine ("\t\t</dict>");
			}
			writer.WriteLine ("\t</array>");
			writer.WriteLine ("\t<key>uuid</key>");
			writer.WriteLine ("\t<string>" + theme.Uuid + "</string>");
			writer.WriteLine ("</dict>");
			writer.WriteLine ("</plist>");
		}

		static ThemeSetting LoadThemeSetting (PDictionary dict)
		{
			string name = null;
			var scopes = new List<string> ();
			var settings = new Dictionary<string, string> ();

			PObject val;
			if (dict.TryGetValue ("name", out val))
				name = ((PString)val).Value;
			if (dict.TryGetValue ("scope", out val)) {
				scopes.AddRange (((PString)val).Value.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries));
			}
			if (dict.TryGetValue ("settings", out val)) {
				var settingsDictionary = val as PDictionary;
				foreach (var setting in settingsDictionary) {
					settings.Add (setting.Key, ((PString)setting.Value).Value);
				}
			}

			return new ThemeSetting (name, scopes, settings);
		}

		static ThemeSetting CalculateMissingColors (ThemeSetting themeSetting)
		{
			var settings = (Dictionary<string, string>)themeSetting.Settings;
			settings [ThemeSettingColors.LineNumbersBackground] = HslColor.Parse (settings [ThemeSettingColors.Background]).AddLight (0.01).ToPangoString ();
			settings [ThemeSettingColors.LineNumbers] = HslColor.Parse (settings [ThemeSettingColors.Foreground]).AddLight (-0.1).ToPangoString ();

			return new ThemeSetting (themeSetting.Name, themeSetting.Scopes, settings);
		}

		#endregion

		#region Syntax highlighting

		internal static SyntaxHighlightingDefinition ReadHighlighting (Stream stream)
		{
			var dictionary = PDictionary.FromStream (stream);

			string firstLineMatch = null;

			var extensions = new List<string> ();
			var contexts = new List<SyntaxContext> ();

			var name = (dictionary ["name"] as PString)?.Value;
			var scope = (dictionary ["scopeName"] as PString)?.Value;



			var fileTypesArray = dictionary ["fileTypes"] as PArray;
			if (fileTypesArray != null) {
				foreach (var type in fileTypesArray.OfType<PString> ()) {
					extensions.Add (type.Value);
				}
			}

			// Construct main context
			var patternsArray = dictionary ["patterns"] as PArray;
			if (patternsArray != null) {
				var includes = new List<object> ();
				ReadPatterns (patternsArray, includes);
				contexts.Add (new SyntaxContext ("main", includes));
			}

			var repository = dictionary ["repository"] as PDictionary;
			if (repository != null) {
				foreach (var kv in repository) {
					string contextName = kv.Key;
					var includes = new List<object> ();

					var contents = kv.Value as PDictionary;
					if (contents != null) {
						var newMatch = ReadMatch (contents);
						if (newMatch != null) {

							includes.Add (newMatch);

						}

						patternsArray = contents ["patterns"] as PArray;
						if (patternsArray != null) {
							ReadPatterns (patternsArray, includes);
						}
					}

					contexts.Add (new SyntaxContext (contextName, includes));
				}
			}

			// var uuid = (dictionary ["uuid"] as PString)?.Value;
			var hideFromUser = (dictionary ["hideFromUser"] as PBoolean)?.Value;
			return new SyntaxHighlightingDefinition (name, scope, firstLineMatch, hideFromUser == true, extensions, contexts);
		}

		static void ReadPatterns (PArray patternsArray, List<object> includesAndMatches)
		{
			foreach (var type in patternsArray) {
				var dict = type as PDictionary;
				if (dict == null)
					continue;
				var incl = (dict ["include"] as PString)?.Value;
				if (incl != null) {
					includesAndMatches.Add (incl);
					continue;
				}
				var newMatch = ReadMatch (dict);
				if (newMatch != null)
					includesAndMatches.Add (newMatch);
			}
		}

		static SyntaxMatch ReadMatch (PDictionary dict)
		{
			var match = (dict ["match"] as PString)?.Value;
			var matchScope = (dict ["name"] as PString)?.Value;
			if (match == null)
				return null;
			List<Tuple<int, string>> captures = null;
			var captureDict = dict ["captures"] as PDictionary;
			if (captureDict != null)
				captures = ReadCaptureDictionary (captureDict);

			ContextReference pushContext = null;

			var begin = (dict ["begin"] as PString)?.Value;
			if (begin != null) {

				List<Tuple<int, string>> beginCaptures = null;
				captureDict = dict ["beginCaptures"] as PDictionary;
				if (captureDict != null)
					beginCaptures = ReadCaptureDictionary (captureDict);

				var end = (dict ["end"] as PString)?.Value;
				List<Tuple<int, string>> endCaptures = null;
				if (end != null) {
					captureDict = dict ["endCaptures"] as PDictionary;
					if (captureDict != null)
						endCaptures = ReadCaptureDictionary (captureDict);
				}

				var list = new List<object> { new SyntaxMatch (begin, null, beginCaptures, null, false, null) };
				if (end != null)
					list.Add (new SyntaxMatch (end, null, endCaptures, null, true, null));
				pushContext = new AnonymousMatchContextReference (new SyntaxContext ("__generated begin/end capture context", list));
			}

			return new SyntaxMatch (match, matchScope, captures, pushContext, false, null);
		}

		static List<Tuple<int, string>> ReadCaptureDictionary (PDictionary captureDict)
		{
			var captures = new List<Tuple<int, string>> ();
			foreach (var kv in captureDict) {
				captures.Add (Tuple.Create (int.Parse (kv.Key), ((kv.Value as PDictionary) ["name"] as PString).Value));
			}
			return captures;
		}
		#endregion
	}
}