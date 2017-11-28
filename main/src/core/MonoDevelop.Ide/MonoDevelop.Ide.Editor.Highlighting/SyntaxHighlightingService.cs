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
using MonoDevelop.Core.Text;
using MonoDevelop.Components;
using System.Collections.Immutable;
using ICSharpCode.NRefactory.MonoCSharp;

namespace MonoDevelop.Ide.Editor.Highlighting
{

	public static class SyntaxHighlightingService
	{
		static LanguageBundle builtInBundle = new LanguageBundle ("default", null);
		static List<LanguageBundle> languageBundles = new List<LanguageBundle> ();

		internal static IEnumerable<LanguageBundle> AllBundles {
			get {
				return languageBundles;
			}
		}

		internal static IEnumerable<LanguageBundle> LanguageBundles {
			get {
				return languageBundles.Skip (1);
			}
		}

		public static string[] Styles {
			get {
				var result = new List<string> ();
				foreach (var bundle in languageBundles) {
					for (int i = 0; i < bundle.EditorThemes.Count; ++i) {
						var style = bundle.EditorThemes[i];
						if (!result.Contains (style.Name))
							result.Add (style.Name);
					}
				}
				return result.ToArray ();
			}
		}

		public static bool ContainsStyle (string styleName)
		{
			foreach (var bundle in languageBundles) {
				for (int i = 0; i < bundle.EditorThemes.Count; ++i) {
					var style = bundle.EditorThemes[i];
					if (style.Name == styleName)
						return true;
				}
			}
			return false;
		}

		public static FilePath LanguageBundlePath {
			get {
				return UserProfile.Current.UserDataRoot.Combine ("LanguageBundles");
			}
		}

		public static EditorTheme DefaultColorStyle {
			get {
				return GetEditorTheme (EditorTheme.DefaultDarkThemeName);
			}
		}
		public static EditorTheme GetDefaultColorStyle (this Theme theme)
		{
			return GetEditorTheme (GetDefaultColorStyleName (theme));
		}

		public static string GetDefaultColorStyleName ()
		{
			return GetDefaultColorStyleName (IdeApp.Preferences.UserInterfaceTheme);
		}

		public static string GetDefaultColorStyleName (this Theme theme)
		{
			switch (theme) {
				case Theme.Light:
					return IdePreferences.DefaultLightColorScheme;
				case Theme.Dark:
					return IdePreferences.DefaultDarkColorScheme;
				default:
					throw new InvalidOperationException ();
			}
		}

		public static EditorTheme GetUserColorStyle (this Theme theme)
		{
			var schemeName = IdeApp.Preferences.ColorScheme.ValueForTheme (theme);
			return GetEditorTheme (schemeName);
		}

		public static bool FitsIdeTheme (this EditorTheme editorTheme, Theme theme)
		{
			Components.HslColor bgColor;
			editorTheme.TryGetColor (EditorThemeColors.Background, out bgColor);
			if (theme == Theme.Dark)
				return (bgColor.L <= 0.5);
			return (bgColor.L > 0.5);
		}

		internal static IEnumerable<TmSetting> GetSettings (ScopeStack scope)
		{
			foreach (var bundle in languageBundles) {
				foreach (var setting in bundle.Settings) {
					if (!setting.Scopes.Any (s => TmSetting.IsSettingMatch (scope, s)))
						continue;
					yield return setting;
				}
			}
		}
		internal static IEnumerable<TmSnippet> GetSnippets (ScopeStack scope)
		{
			foreach (var bundle in languageBundles) {
				foreach (var setting in bundle.Snippets) {
					if (!setting.Scopes.Any (s => TmSetting.IsSettingMatch (scope, s)))
						continue;
					yield return setting;
				}
			}
		}

		public static EditorTheme GetEditorTheme (string name)
		{
			foreach (var bundle in languageBundles) {
				for (int i = 0; i < bundle.EditorThemes.Count; ++i) {
					var style = bundle.EditorThemes[i];
					if (style.Name == name)
						return style;
				}
			}
			LoggingService.LogWarning ("Color style " + name + " not found, switching to default.");
			return GetEditorTheme (GetDefaultColorStyleName ());
		}

		static void LoadStyle (string name)
		{
		}

		internal static void Remove (EditorTheme style)
		{
			builtInBundle.Remove (style);
		}

		internal static void Remove (LanguageBundle style)
		{
			languageBundles.Remove (style);
		}


		static List<ValidationEventArgs> ValidateStyleFile (string fileName)
		{
			List<ValidationEventArgs> result = new List<ValidationEventArgs> ();
			return result;
		}


		internal static void LoadStylesAndModesInPath (string path)
		{
			foreach (string file in Directory.GetFiles (path)) {
				try {
					LoadStyleOrMode (file);
				} catch (Exception ex) {
					LoggingService.LogError ($"Could not load file '{file}'", ex);
				}
			}
		}

		static void PrepareMatches ()
		{
			foreach (var bundle in languageBundles) {
				foreach (var h in bundle.Highlightings)
					h.PrepareMatches ();
			}
		}

		internal static object LoadStyleOrMode (string file)
		{
			return LoadFile (builtInBundle, file, () => File.OpenRead (file), () => new UrlStreamProvider (file));
		}

		internal static bool IsValidTheme (string file)
		{
			var bundle = new LanguageBundle ("default", null);
			try {
				LoadFile (bundle, file, () => File.OpenRead (file), () => new UrlStreamProvider (file));
				return bundle.EditorThemes.Count > 0;
			} catch (Exception ex) {
				LoggingService.LogError ($"Invalid color theme file '{file}'.", ex);
			}
			return false;
		}

			/*
			 * 				if (provider.Name.EndsWith (".vssettings", StringComparison.Ordinal)) {
					styles [name] = OldFormat.ImportVsSetting (provider.Name, stream);
				} else if (provider.Name.EndsWith (".json", StringComparison.Ordinal)) {
					styles [name] = OldFormat.ImportColorScheme (stream);
				} else {
					styles [name] = TextMateFormat.LoadEditorTheme (stream);
				}
				styles [name].FileName = provider.Name;

			 * */

		static object LoadFile (LanguageBundle bundle, string file, Func<Stream> openStream, Func<IStreamProvider> getStreamProvider)
		{
			if (file.EndsWith (".json", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					string styleName;
					JSonFormat format;
					if (TryScanJSonStyle (stream, out styleName, out format)) {
						switch (format) {
						case JSonFormat.OldSyntaxTheme:
							var theme = OldFormat.ImportColorScheme (getStreamProvider ().Open ());
							if (theme != null)
								bundle.Add (theme);
							return theme;
						case JSonFormat.TextMateJsonSyntax:
							SyntaxHighlightingDefinition highlighting = TextMateFormat.ReadHighlightingFromJson (getStreamProvider().Open ());
							if (highlighting != null)
								bundle.Add (highlighting);
							return highlighting;
						}
					}
				}
			} else if (file.EndsWith (".tmTheme", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					string styleName = ScanTextMateStyle (stream);
					if (!string.IsNullOrEmpty (styleName)) {
						var theme = TextMateFormat.LoadEditorTheme (getStreamProvider ().Open ());
						if (theme != null)
							bundle.Add (theme);
						return theme;
					} else {
						LoggingService.LogError ("Invalid .tmTheme theme file : " + file);
					}
				}
			} else if (file.EndsWith (".vssettings", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					string styleName = Path.GetFileNameWithoutExtension (file);
					EditorTheme theme;
					try {
						theme = OldFormat.ImportVsSetting (styleName, getStreamProvider ().Open ());
					} catch (StyleImportException e) {
						switch (e.Reason) {
						case StyleImportException.ImportFailReason.Unknown:
							LoggingService.LogWarning ("Unknown error in theme file : " + file, e);
							break;
						case StyleImportException.ImportFailReason.NoValidColorsFound:
							LoggingService.LogWarning ("No colors defined in vssettings : " + file, e);
							break;
						}
						return null;
					} catch (Exception e) {
						LoggingService.LogWarning ("Invalid theme : " + file, e);
						return null;
					}
					if (theme != null)
						bundle.Add (theme);
					return theme;
				}
			} else if (file.EndsWith (".tmLanguage", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					var highlighting = TextMateFormat.ReadHighlighting (stream);
					if (highlighting != null)
						bundle.Add (highlighting);
					return highlighting;
				}
			} else if (file.EndsWith (".sublime-syntax", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = new StreamReader (openStream ())) {
					var highlighting = Sublime3Format.ReadHighlighting (stream);
					if (highlighting != null)
						bundle.Add (highlighting);
					return highlighting;
				}
			} else if (file.EndsWith (".sublime-package", StringComparison.OrdinalIgnoreCase) || file.EndsWith (".tmbundle", StringComparison.OrdinalIgnoreCase)) {
				try {
					using (var stream = new ICSharpCode.SharpZipLib.Zip.ZipInputStream (openStream ())) {
						var entry = stream.GetNextEntry ();
						var newBundle = new LanguageBundle (Path.GetFileNameWithoutExtension (file), file);
						while (entry != null) {
							if (entry.IsFile && !entry.IsCrypted) {
								if (stream.CanDecompressEntry) {
									byte [] data = new byte [entry.Size];
									stream.Read (data, 0, (int)entry.Size);
									LoadFile (newBundle, entry.Name, () => new MemoryStream (data), () => new MemoryStreamProvider (data, entry.Name));
								}
							} 
							entry = stream.GetNextEntry ();
						}
						languageBundles.Add (newBundle); 
						return newBundle;
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error while reading : " + file, e); 
				}
			} else if (file.EndsWith (".tmPreferences", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					var preference = TextMateFormat.ReadPreferences (stream);
					if (preference != null)
						bundle.Add (preference);
					return preference;
				}
			} else if (file.EndsWith (".tmSnippet", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					var snippet = TextMateFormat.ReadSnippet (stream);
					if (snippet != null)
						bundle.Add (snippet);
					return snippet;
				}
			} else if (file.EndsWith (".sublime-snippet", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					var snippet = Sublime3Format.ReadSnippet (stream);
					if (snippet != null)
						bundle.Add (snippet);
					return snippet;
				}
			}
			return null;
		}

		static void LoadStylesAndModes (Assembly assembly)
		{
			foreach (string resource in assembly.GetManifestResourceNames ()) {
				LoadFile (builtInBundle, resource, () => assembly.GetManifestResourceStream (resource), () => new ResourceStreamProvider (assembly, resource));
			}
		}

		static System.Text.RegularExpressions.Regex jsonNameRegex = new System.Text.RegularExpressions.Regex ("\\s*\"name\"\\s*:\\s*\"(.*)\"\\s*,");
		static System.Text.RegularExpressions.Regex jsonVersionRegex = new System.Text.RegularExpressions.Regex ("\\s*\"version\"\\s*:\\s*\"(.*)\"\\s*,");

		enum JSonFormat { Unknown, OldSyntaxTheme, TextMateJsonSyntax }

		static bool TryScanJSonStyle (Stream stream, out string name, out JSonFormat format)
		{
			name = null;
			format = JSonFormat.Unknown;

			try {
				var file = TextFileUtility.OpenStream (stream);
				file.ReadLine ();
				var nameLine = file.ReadLine ();
				var versionLine = file.ReadLine ();
				file.Close ();
				var match = jsonNameRegex.Match (nameLine);
				if (match.Success) {
					if (jsonVersionRegex.Match (versionLine).Success) {
						name = match.Groups [1].Value;
						format = JSonFormat.OldSyntaxTheme;
						return true;
					}
				}

				format = JSonFormat.TextMateJsonSyntax;
				return true;
			} catch (Exception e) {
				Console.WriteLine ("Error while scanning json:");
				Console.WriteLine (e);
			}
			return false;
			
		}


		static System.Text.RegularExpressions.Regex textMateNameRegex = new System.Text.RegularExpressions.Regex ("\\<string\\>(.*)\\<\\/string\\>");

		static string ScanTextMateStyle (Stream stream)
		{
			try {
				var file = TextFileUtility.OpenStream (stream);
				string keyString = "<key>name</key>";
				while (true) {
					var line = file.ReadLine ();
					if (line == null)
						return "";
					if (line.Contains (keyString))
						break;
				}

				var nameLine = file.ReadLine ();
				file.Close ();
				var match = textMateNameRegex.Match (nameLine);
				if (!match.Success)
					return null;
				return match.Groups[1].Value;
			} catch (Exception e) {
				Console.WriteLine ("Error while scanning json:");
				Console.WriteLine (e);
				return null;
			}
		}

		internal static void AddStyle (IStreamProvider provider)
		{
			string styleName;
			JSonFormat format;
			using (var stream = provider.Open ()) {
				if (TryScanJSonStyle (stream, out styleName, out format)) {
					switch (format) {
					case JSonFormat.OldSyntaxTheme:
						var theme = OldFormat.ImportColorScheme (provider.Open ());
						if (theme != null)
							builtInBundle.Add (theme);
						break;
					case JSonFormat.TextMateJsonSyntax:
						SyntaxHighlightingDefinition highlighting = TextMateFormat.ReadHighlightingFromJson (provider.Open ());
						if (highlighting != null)
							builtInBundle.Add (highlighting);
						break;
					}
				}
			}
		}

		internal static void RemoveStyle (IStreamProvider provider)
		{
		}

		static SyntaxHighlightingService ()
		{
			languageBundles.Add (builtInBundle);

			LoadStylesAndModes (typeof (SyntaxHighlightingService).Assembly);
			var textEditorAssembly = AppDomain.CurrentDomain.GetAssemblies ().FirstOrDefault (a => a.GetName ().Name.StartsWith ("MonoDevelop.SourceEditor", StringComparison.Ordinal));
			if (textEditorAssembly != null) {
				LoadStylesAndModes (textEditorAssembly);
			} else {
				LoggingService.LogError ("Can't lookup Mono.TextEditor assembly. Default styles won't be loaded.");
			}

			bool success = true;
			if (!Directory.Exists (LanguageBundlePath)) {
				try {
					Directory.CreateDirectory (LanguageBundlePath);
				} catch (Exception e) {
					success = false;
					LoggingService.LogError ("Can't create syntax mode directory", e);
				}
			}
			if (success) {
				foreach (string file in Directory.GetFiles (LanguageBundlePath)) {
					if (file.EndsWith (".sublime-package", StringComparison.OrdinalIgnoreCase) || file.EndsWith (".tmbundle", StringComparison.OrdinalIgnoreCase)) {
						LoadStyleOrMode (file); 
					}
				}
			}
			PrepareMatches ();
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/Editor/Bundles", OnSyntaxModeExtensionChanged);
		}

		static void OnSyntaxModeExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			var codon = (TemplateCodon)args.ExtensionNode;

			if (args.Change == ExtensionChange.Add) {
				try {
					var o = LoadFile (builtInBundle, codon.Name, () => codon.Open (), () => codon);
					if (o is SyntaxHighlightingDefinition)
						((SyntaxHighlightingDefinition)o).PrepareMatches ();
					var bundle = o as LanguageBundle;
					if (bundle != null) {
						foreach (var h in bundle.Highlightings)
							h.PrepareMatches ();
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error while loading custom editor extension file.", e);
				}
			}
		}


		public static HslColor GetColor (EditorTheme style, string key)
		{
			HslColor result;
			if (!style.TryGetColor (key, out result)) {
				DefaultColorStyle.TryGetColor (key, out result);
			}
			return result;
		}

		public static HslColor GetColorFromScope (EditorTheme style, string scope, string key)
		{
			HslColor result;
			if (!style.TryGetColor (scope, key, out result)) {
				DefaultColorStyle.TryGetColor (scope, key, out result);
			}
			return result;
		}

		internal static ChunkStyle GetChunkStyle (EditorTheme style, string key)
		{
			HslColor result;
			if (!style.TryGetColor (key, out result)) {
				DefaultColorStyle.TryGetColor (key, out result);
			}
			return new ChunkStyle() { Foreground = result };
		}

		static SyntaxHighlightingDefinition GetSyntaxHighlightingDefinitionByName (FilePath fileName)
		{
			SyntaxHighlightingDefinition bestMatch = null;

			string bestType = null;
			int bestPosition = 0;
			var fileNameStr = (string)fileName;
			var name = fileName.FileName;
			foreach (var bundle in languageBundles) {
				foreach (var h in bundle.Highlightings) {
					for (int i = 0; i < h.FileTypes.Count; i++) {
						var fileType = h.FileTypes [i];
						if (!fileNameStr.EndsWith (fileType, StringComparison.OrdinalIgnoreCase))
							continue;

						//if type is full match to filename e.g. ChangeLog, we're done
						if (name.Length == fileType.Length) {
							return h;
						}

						//if type didn't start with period, check filename has one in the right place
						//e.g type 'y' is not valid match for name 'ab.xy'
						if (fileType [0] != '.' && fileNameStr [fileNameStr.Length - fileType.Length - 1] != '.') {
							continue;
						}

						if (bestType == null || // 1st match we take anything
							bestType.Length < fileType.Length || // longer match is better, e.g. 'xy' matches 'ab.nm.xy', but 'nm.xy' matches better
							(bestType.Length == fileType.Length && bestPosition > i)) { //fileType is same... take higher on list(e.g. XAML specific will have .xaml at index 0, but XML general will have .xaml at index 68)
							bestType = fileType;
							bestPosition = i;
							bestMatch = h;
						}
					}
				}
			}

			return bestMatch;
		}

		static SyntaxHighlightingDefinition GetSyntaxHighlightingDefinitionByMimeType (string mimeType)
		{
			foreach (string mt in DesktopService.GetMimeTypeInheritanceChain (mimeType)) {
				if (mimeType == "application/octet-stream" || mimeType == "text/plain")
					return null;

				foreach (var bundle in languageBundles) {
					foreach (var h in bundle.Highlightings) {
						foreach (var fe in h.FileTypes) {
							var uri = fe.StartsWith (".", StringComparison.Ordinal) ? "a" + fe : "a." + fe;
							var mime = DesktopService.GetMimeTypeForUri (uri);
							if (mimeType == mime) {
								return h;
							}
						}
					}
				}
			}

			return null;
		}

		internal static SyntaxHighlightingDefinition GetSyntaxHighlightingDefinition (FilePath fileName, string mimeType)
		{
			if (!fileName.IsNullOrEmpty) {
				var def = GetSyntaxHighlightingDefinitionByName (fileName);
				if (def != null) {
					return def;
				}

				if (mimeType == null) {
					mimeType = DesktopService.GetMimeTypeForUri (fileName);
				}
			}

			if (mimeType == null) {
				return null;
			}

			return GetSyntaxHighlightingDefinitionByMimeType (mimeType);
		}

		internal static ScopeStack GetScopeForFileName (string fileName)
		{
			string scope = null;
			if (fileName != null) {
				foreach (var bundle in languageBundles) {
					foreach (var highlight in bundle.Highlightings) {
						if (highlight.FileTypes.Any (ext => fileName.EndsWith (ext, FilePath.PathComparison))) {
							scope = highlight.Scope;
							break;
						}
					}
				}
			}

			if (scope == null)
				return ScopeStack.Empty;

			return new ScopeStack (scope);
		}
	}
}
