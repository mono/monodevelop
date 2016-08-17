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
using MonoDevelop.Core.Text;
using System.Xml.Linq;
using System.Xml.XPath;

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
					themeSetting = CalculateMissingDefaultColors (themeSetting);
				settings.Add (themeSetting);
			}

			var uuid = (PString)dictionary ["uuid"];

			var methodDecl = GetSetting (settings, EditorThemeColors.UserMethodDeclaration);
			var methodUsage = GetSetting (settings, EditorThemeColors.UserMethodUsage);
			if (methodUsage == null && methodDecl != null) {
				settings.Add (new ThemeSetting (
					"User Method(Usage)",
					new List<string> { EditorThemeColors.UserMethodUsage },
					settings [0].Settings
				));
			}
			ConvertSetting (settings, "storage.type", EditorThemeColors.UserTypesInterfaces);
			ConvertSetting (settings, "entity.name", EditorThemeColors.UserTypes);
			ConvertSetting (settings, "entity.name", EditorThemeColors.UserTypesEnums);
			ConvertSetting (settings, "entity.name", EditorThemeColors.UserTypesMutable);
			ConvertSetting (settings, "entity.name", EditorThemeColors.UserTypesDelegates);
			ConvertSetting (settings, "entity.name", EditorThemeColors.UserTypesValueTypes);
			ConvertSetting (settings, "entity.name", EditorThemeColors.UserTypesTypeParameters);
			ConvertSetting (settings, "support.constant", "markup.other");
			ConvertSetting (settings, "constant.character", "meta.preprocessor");
			settings.Add (new ThemeSetting ("", new List<string> { "meta.preprocessor.region.name" }, settings [0].Settings));

			// set all remaining semantic colors to default.
			var semanticColors = new [] {
				EditorThemeColors.UserTypes,
				EditorThemeColors.UserTypesValueTypes,
				EditorThemeColors.UserTypesInterfaces,
				EditorThemeColors.UserTypesEnums,
				EditorThemeColors.UserTypesTypeParameters,
				EditorThemeColors.UserTypesDelegates,
				EditorThemeColors.UserTypesMutable,
				EditorThemeColors.UserFieldDeclaration,
				EditorThemeColors.UserFieldUsage,
				EditorThemeColors.UserPropertyDeclaration,
				EditorThemeColors.UserPropertyUsage,
				EditorThemeColors.UserEventDeclaration,
				EditorThemeColors.UserEventUsage,
				EditorThemeColors.UserMethodDeclaration,
				EditorThemeColors.UserMethodUsage,
				EditorThemeColors.UserParameterDeclaration,
				EditorThemeColors.UserParameterUsage,
				EditorThemeColors.UserVariableDeclaration,
				EditorThemeColors.UserVariableUsage
			};
			foreach (var semanticColor in semanticColors) {
				if (GetSetting (settings, semanticColor) == null) {
					settings.Add (new ThemeSetting ("", new List<string> { semanticColor }, settings [0].Settings));
				}
			}

			return new EditorTheme (name, settings, uuid);
		}

		static void ConvertSetting (List<ThemeSetting> settings, string fromSetting, string toSetting)
		{
			var fs = GetSetting (settings, fromSetting, false);
			var ts = GetSetting (settings, toSetting);
			if (ts == null && fs != null) {
				settings.Add (new ThemeSetting (
					"Copied From (" + fs + ")",
					new List<string> { toSetting },
					fs.Settings
				));
			}
		}

		static ThemeSetting GetSetting (List<ThemeSetting> settings, string scope, bool exact = true)
		{
			ThemeSetting result = null;
			foreach (var s in settings.Skip (1)) {
				if (s.Scopes.Any (a => exact ? a == scope : EditorTheme.IsCompatibleScope (a, scope))) {
					if (result == null || result.Scopes.Last ().Length < s.Scopes.Last ().Length)
						result = s;
				}
			}
			
			return result;
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
				scopes.AddRange (((PString)val).Value.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select (s => s.Trim ()));
			}
			if (dict.TryGetValue ("settings", out val)) {
				var settingsDictionary = val as PDictionary;
				foreach (var setting in settingsDictionary) {
					settings.Add (setting.Key, ((PString)setting.Value).Value);
				}
			}

			return new ThemeSetting (name, scopes, settings);
		}

		internal static TmSetting ReadPreferences (Stream stream)
		{
			var dict = PDictionary.FromStream (stream);

			string name = null;
			var scopes = new List<string> ();
			var settings = new Dictionary<string, PObject> ();

			PObject val;
			if (dict.TryGetValue ("name", out val))
				name = ((PString)val).Value;
			if (dict.TryGetValue ("scope", out val)) {
				scopes.AddRange (((PString)val).Value.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries));
			}
			if (dict.TryGetValue ("settings", out val)) {
				var settingsDictionary = val as PDictionary;
				foreach (var setting in settingsDictionary) {
					settings.Add (setting.Key, setting.Value);
				}
			}

			return new TmSetting (name, scopes, settings);
		}

		internal static TmSnippet ReadSnippet (Stream stream)
		{
			var dict = PDictionary.FromStream (stream);

			string name = null;
			string content = null;
			string tabTrigger = null;
			var scopes = new List<string> ();

			PObject val;
			if (dict.TryGetValue ("name", out val))
				name = ((PString)val).Value;
			if (dict.TryGetValue ("content", out val))
				content = ((PString)val).Value;
			if (dict.TryGetValue ("tabTrigger", out val))
				tabTrigger = ((PString)val).Value;
			if (dict.TryGetValue ("scope", out val)) {
				scopes.AddRange (((PString)val).Value.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries));
			}

			return new TmSnippet (name, scopes, content, tabTrigger);
		}

		static ThemeSetting CalculateMissingDefaultColors (ThemeSetting themeSetting)
		{
			var settings = (Dictionary<string, string>)themeSetting.Settings;
			var bgColor = HslColor.Parse (settings [EditorThemeColors.Background]);
			var darkModificator = HslColor.Brightness (bgColor) < 0.5 ? 1 : -1;

			// do some better best-fit calculations
			if (!settings.ContainsKey (EditorThemeColors.LineNumbersBackground))
				settings [EditorThemeColors.LineNumbersBackground] = bgColor.AddLight (0.01 * darkModificator).ToPangoString ();
			if (!settings.ContainsKey (EditorThemeColors.IndicatorMarginSeparator))
				settings [EditorThemeColors.IndicatorMarginSeparator] = bgColor.AddLight (0.03 * darkModificator).ToPangoString ();
			if (!settings.ContainsKey (EditorThemeColors.LineNumbers))
				settings [EditorThemeColors.LineNumbers] = HslColor.Parse (settings [EditorThemeColors.Foreground]).AddLight (-0.1 * darkModificator).ToPangoString ();
			if (!settings.ContainsKey (EditorThemeColors.IndicatorMargin))
				settings [EditorThemeColors.IndicatorMargin] = bgColor.AddLight (0.02 * darkModificator).ToPangoString ();

			// copy all missing settings from main template theme
			var templateTheme = SyntaxHighlightingService.GetEditorTheme (darkModificator == 1 ? EditorTheme.DefaultDarkThemeName : EditorTheme.DefaultThemeName);
			foreach (var kv in templateTheme.Settings [0].Settings) {
				if (settings.ContainsKey (kv.Key))
					continue;
				settings.Add (kv.Key, kv.Value);
			}

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
					includesAndMatches.Add (incl.TrimStart ('#'));
					continue;
				}
				var newMatch = ReadMatch (dict);
				if (newMatch != null)
					includesAndMatches.Add (newMatch);
			}
		}

		static SyntaxMatch ReadMatch (PDictionary dict)
		{
			List<string> matchScope  = new List<string> ();
			Sublime3Format.ParseScopes (matchScope, (dict ["name"] as PString)?.Value);

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
				List<string> endScope = new List<string> ();
				if (end != null) {
					captureDict = dict ["endCaptures"] as PDictionary;
					if (captureDict != null)
						endCaptures = ReadCaptureDictionary (captureDict);

					var list = new List<object> ();
					if (end != null)
						list.Add (new SyntaxMatch (Sublime3Format.CompileRegex (end), endScope, endCaptures, null, true, null));
					var patternsArray = dict ["patterns"] as PArray;
					if (patternsArray != null) {
						ReadPatterns (patternsArray, list);
					}
					pushContext = new AnonymousMatchContextReference (new SyntaxContext ("__generated begin/end capture context", list));
				}

				return new SyntaxMatch (Sublime3Format.CompileRegex (begin), matchScope, beginCaptures ?? captures, pushContext, false, null);
			}

			var match = (dict ["match"] as PString)?.Value;
			if (match == null)
				return null;
			return new SyntaxMatch (Sublime3Format.CompileRegex (match), matchScope, captures, pushContext, false, null);
		}

		static List<Tuple<int, string>> ReadCaptureDictionary (PDictionary captureDict)
		{
			var captures = new List<Tuple<int, string>> ();
			foreach (var kv in captureDict) {
				var g = int.Parse (kv.Key);
				var s = ((kv.Value as PDictionary) ["name"] as PString).Value;
				/*	if (g == 0) {
						scope = s;
						continue;
					}*/
				captures.Add (Tuple.Create (g, s));
			}
			return captures;
		}
		#endregion

		#region JSon Format
		internal static SyntaxHighlightingDefinition ReadHighlightingFromJson(Stream stream)
		{
			byte [] bytes;
			using (var sr = TextFileUtility.OpenStream (stream)) {
				bytes = System.Text.Encoding.UTF8.GetBytes (sr.ReadToEnd ());
			}

			var reader = System.Runtime.Serialization.Json.JsonReaderWriterFactory.CreateJsonReader (bytes, new System.Xml.XmlDictionaryReaderQuotas ());
			var root = XElement.Load (reader);

			string firstLineMatch = null;
			var extensions = new List<string> ();
			var contexts = new List<SyntaxContext> ();

			var name = root.XPathSelectElement ("name").Value;
			var scope = root.XPathSelectElement ("scopeName").Value;
			foreach (var type in root.XPathSelectElements ("fileTypes/*")) {
				extensions.Add (type.Value);
			}

			// Construct main context
			var includes = new List<object> ();
			foreach (var type in root.XPathSelectElements ("patterns/*")) {
				ReadJSonPattern (type, includes);
			}
			contexts.Add (new SyntaxContext ("main", includes));
			foreach (var kv in root.XPathSelectElements ("repository/*")) {
				string contextName = kv.Name.LocalName;
				includes = new List<object> ();
				foreach (var node in kv.Nodes ().OfType<XElement> ()) {
					if (node.Name.LocalName == "patterns") {
						foreach (var match in node.Nodes ().OfType<XElement> ()) {
							ReadJSonPattern (match, includes);
						}
					} else {
						var newMatch = ReadJSonMatch (node);	
						if (newMatch != null)
							includes.Add (newMatch);
					}
				}
				contexts.Add (new SyntaxContext (contextName, includes));
			}

			// var uuid = (dictionary ["uuid"] as PString)?.Value;*/
			var hideFromUser = root.XPathSelectElement ("hideFromUser")?.Value == "true";
			return new SyntaxHighlightingDefinition (name, scope, firstLineMatch, hideFromUser, extensions, contexts);
		}

		static void ReadJSonPattern (XElement type, List<object> includes)
		{
			var firstNode = ((XElement)type.FirstNode);
			if (firstNode.Name.LocalName == "include") {
				includes.Add (firstNode.Value.TrimStart ('#'));
				return;
			}
			var newMatch = ReadJSonMatch (type);
			if (newMatch != null)
				includes.Add (newMatch);
		}

		static SyntaxMatch ReadJSonMatch (XElement dict)
		{
			List<string> matchScope = new List<string> ();
			Sublime3Format.ParseScopes (matchScope, dict.XPathSelectElement ("name")?.Value);
			var captures = ReadJSonCaptureDictionary (dict.XPathSelectElement ("captures"));
			ContextReference pushContext = null;
			
			var begin = dict.XPathSelectElement ("begin")?.Value;
			if (begin != null) {
				List<Tuple<int, string>> beginCaptures = null;
				beginCaptures = ReadJSonCaptureDictionary (dict.XPathSelectElement ("beginCaptures"));

				var end = dict.XPathSelectElement ("end")?.Value;
				List<Tuple<int, string>> endCaptures = null;
				List<string> endScope = new List<string> ();
				if (end != null) {
					endCaptures = ReadJSonCaptureDictionary (dict.XPathSelectElement ("endCaptures"));


					var list = new List<object> ();
					if (end != null)
						list.Add (new SyntaxMatch (Sublime3Format.CompileRegex (end), endScope, endCaptures, null, true, null));
					var patternsArray = dict.XPathSelectElement ("patterns");
					if (patternsArray != null) {
						foreach (var match2 in patternsArray.Nodes ().OfType<XElement> ()) {
							ReadJSonPattern (match2, list);
						}
					}
					pushContext = new AnonymousMatchContextReference (new SyntaxContext ("__generated begin/end capture context", list));
				}

				return new SyntaxMatch (Sublime3Format.CompileRegex (begin), matchScope, beginCaptures ?? captures, pushContext, false, null);
			}

			var match = dict.XPathSelectElement ("match")?.Value;
			if (match == null)
				return null;
			return new SyntaxMatch (Sublime3Format.CompileRegex (match), matchScope, captures, pushContext, false, null);
		}

		static List<Tuple<int, string>> ReadJSonCaptureDictionary (XElement captureDict)
		{
			if (captureDict == null)
				return null;
			var captures = new List<Tuple<int, string>> ();
			foreach (var kv in captureDict.Nodes ().OfType<XElement> ()) {
				var g = int.Parse (kv.Attribute ("item").Value);
				var s = kv.XPathSelectElement ("name")?.Value;
				captures.Add (Tuple.Create (g, s));
			}
			return captures;
		}
		#endregion
	}
}