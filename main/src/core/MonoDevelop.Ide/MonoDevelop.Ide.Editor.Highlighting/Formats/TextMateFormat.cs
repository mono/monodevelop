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
using MonoDevelop.Core;
using System.Collections.Immutable;

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
			string cs = null;
			int d = 0;
			var stack = new ScopeStack (scope);
			foreach (var s in settings.Skip (1)) {
				if (s.Scopes.Any (a => EditorTheme.IsCompatibleScope (a, stack, ref cs, ref d))) {
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
				var scope = ((PString)val).Value;
				scopes.Add (scope);
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
			return ReadPreferences (dict);
		}

		internal static TmSetting ReadPreferences (PDictionary dict)
		{
			string name = null;
			var scopes = new List<StackMatchExpression> ();
			var settings = new Dictionary<string, PObject> ();

			PObject val;
			if (dict.TryGetValue ("name", out val))
				name = ((PString)val).Value;
			if (dict.TryGetValue ("scope", out val)) {
				foreach (var scope in ((PString)val).Value.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
					scopes.Add (StackMatchExpression.Parse (scope));
				}
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
			var scopes = new List<StackMatchExpression> ();

			PObject val;
			if (dict.TryGetValue ("name", out val))
				name = ((PString)val).Value;
			if (dict.TryGetValue ("content", out val))
				content = ((PString)val).Value;
			if (dict.TryGetValue ("tabTrigger", out val))
				tabTrigger = ((PString)val).Value;
			if (dict.TryGetValue ("scope", out val)) {
				foreach (var scope in ((PString)val).Value.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
					scopes.Add (StackMatchExpression.Parse (scope));
				}
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
			return ReadHighlighting (dictionary);
		}

		internal static SyntaxHighlightingDefinition ReadHighlighting (PDictionary dictionary)
		{

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
						} else {
							patternsArray = contents ["patterns"] as PArray;
							if (patternsArray != null) {
								ReadPatterns (patternsArray, includes);
							}
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
					if (incl == "$base") {
						includesAndMatches.Add ("main");
					} else if (incl == "$self") {
						includesAndMatches.Add ("main");
					} else if (incl.StartsWith ("#", StringComparison.Ordinal)) {
						includesAndMatches.Add (incl.TrimStart ('#'));
					} else {
						includesAndMatches.Add ("scope:" + incl);
					}
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

			Captures captures = null;
			var captureDict = dict ["captures"] as PDictionary;
			if (captureDict != null)
				captures = ReadCaptureDictionary (captureDict);

			ContextReference pushContext = null;

			var begin = (dict ["begin"] as PString)?.Value;
			if (begin != null) {
				
				Captures beginCaptures = null;
				captureDict = dict ["beginCaptures"] as PDictionary;

				if (captureDict != null)
					beginCaptures = ReadCaptureDictionary (captureDict);

				var end = (dict ["end"] as PString)?.Value;
				Captures endCaptures = null;
				List<string> endScope = new List<string> ();
				if (end != null) {
					captureDict = dict ["endCaptures"] as PDictionary;
					if (captureDict != null)
						endCaptures = ReadCaptureDictionary (captureDict);

					var list = new List<object> ();
					if (end != null)
						list.Add (new SyntaxMatch (Sublime3Format.CompileRegex (end), endScope, endCaptures ?? captures, null, true, null, null));
					var patternsArray = dict ["patterns"] as PArray;
					if (patternsArray != null) {
						ReadPatterns (patternsArray, list);
					}

					List<string> metaContent = null;
					var contentScope = (dict ["contentName"] as PString)?.Value;
					if (contentScope != null) {
						metaContent = new List<string> { contentScope };
					}

					var ctx = new SyntaxContext ("__generated begin/end capture context", list, metaScope: metaContent);

					pushContext = new AnonymousMatchContextReference (ctx);

				}

				return new SyntaxMatch (Sublime3Format.CompileRegex (begin), matchScope, beginCaptures ?? captures, pushContext, false, null, null);
			}

			var match = (dict ["match"] as PString)?.Value;
			if (match == null)
				return null;
			return new SyntaxMatch (Sublime3Format.CompileRegex (match), matchScope, captures, pushContext, false, null, null);
		}

		static Captures ReadCaptureDictionary (PDictionary captureDict)
		{
			var group = new List<Tuple<int, string>> ();
			var named = new List<Tuple<string, string>> ();
			foreach (var kv in captureDict) {
				var s = ((kv.Value as PDictionary) ["name"] as PString)?.Value;
				if (s == null)
					continue;
				int g;
				try {
					g = int.Parse (kv.Key);
				} catch (Exception e) {
					named.Add (Tuple.Create (kv.Key, s));
					continue;
				}
				group.Add (Tuple.Create (g, s));
			}
			return new Captures(group, named);
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
			var name = root.XPathSelectElement ("name")?.Value;
			var scopeName = root.XPathSelectElement ("scopeName")?.Value;
			if (name == null || scopeName == null)
				return null; // no json textmate highlighting file

			var dict = (PDictionary)Convert (root);
			return ReadHighlighting (dict);
		}

		static PObject Convert (XElement f)
		{
			var type = f.Attribute ("type").Value;
			switch (type) {
			case "string":
				return new PString (f.Value);
			case "array":
				return new PArray (new List<PObject> (f.Elements ().Select (Convert)));
			case "object":
				var val = new PDictionary ();
				foreach (var subElement in f.Elements ()) {
					var name = subElement.Name.LocalName;
					if (name == "item")
						name = subElement.Attribute ("item").Value;
					if (!val.ContainsKey (name)) {
						val.Add (name, Convert (subElement));
					} else {
						LoggingService.LogWarning ("Warning while converting json highlighting to textmate 'key' " + name + " is duplicated in : " + f);
					}
				}
				return val;
			case "number":
				return new PNumber (int.Parse (f.Value));
			default:
				LoggingService.LogWarning ("Can't convert element of type: " + type +"/"+ f);
				break;

			}
			return null;
		}
		#endregion
	}
}