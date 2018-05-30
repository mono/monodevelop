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
using System.Diagnostics;

namespace MonoDevelop.Ide.Editor.Highlighting
{

	public static class SyntaxHighlightingService
	{
		static LanguageBundle builtInBundle = new LanguageBundle ("default", null);
		static LanguageBundle extensionBundle = new LanguageBundle ("extensions", null);
		internal static LanguageBundle userThemeBundle = new LanguageBundle ("userThemes", null);
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

		public static EditorTheme GetIdeFittingTheme (EditorTheme userTheme = null)
		{
			try {
				if (userTheme == null || !userTheme.FitsIdeTheme (Ide.IdeApp.Preferences.UserInterfaceTheme))
					return GetDefaultColorStyle (Ide.IdeApp.Preferences.UserInterfaceTheme);
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting the color style : " + Ide.IdeApp.Preferences.ColorScheme + " in ide theme : " + Ide.IdeApp.Preferences.UserInterfaceTheme, e);
			}
			return userTheme;
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
						return style.GetEditorTheme ();
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
			userThemeBundle.Remove (style);
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
					LoadStyleOrMode (userThemeBundle, file);
				} catch (Exception ex) {
					LoggingService.LogError ($"Could not load file '{file}'", ex);
				}
			}
		}

		static void PrepareMatches ()
		{
			foreach (var bundle in languageBundles) {
				foreach (var h in bundle.Highlightings)
					if (h is SyntaxHighlightingDefinition def)
						def.PrepareMatches ();
			}
		}

		internal static object LoadStyleOrMode (LanguageBundle bundle, string file)
		{
			return LoadFile (bundle, file, () => File.OpenRead (file), () => new UrlStreamProvider (file));
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


		static object LoadFile (LanguageBundle bundle, string file, Func<Stream> openStream, Func<IStreamProvider> getStreamProvider)
		{
			if (file.EndsWith (".json", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					string styleName;
					JSonFormat format;
					if (TryScanJSonStyle (stream, out styleName, out format, out List<string> fileTypes)) {
						switch (format) {
						case JSonFormat.OldSyntaxTheme:
							var oldThemeProvider = AbstractThemeProvider.CreateProvider (EditorThemeFormat.XamarinStudio, styleName, getStreamProvider);
							bundle.Add (oldThemeProvider);
							return oldThemeProvider;
						case JSonFormat.TextMateJsonSyntax:
							var syntaxProvider = AbstractSyntaxHighlightingDefinitionProvider.CreateProvider (SyntaxHighlightingDefinitionFormat.TextMateJson, fileTypes, getStreamProvider);
							bundle.Add (syntaxProvider);
							return syntaxProvider;
						}
					}
				}
			} else if (file.EndsWith (".tmTheme", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					string styleName = ScanTextMateStyle (stream);
					if (!string.IsNullOrEmpty (styleName)) {
						var theme = AbstractThemeProvider.CreateProvider (EditorThemeFormat.TextMate, styleName, getStreamProvider);
						bundle.Add (theme);
						return theme;
					} else {
						LoggingService.LogError ("Invalid .tmTheme theme file : " + file);
					}
				}
			} else if (file.EndsWith (".vssettings", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					var theme = AbstractThemeProvider.CreateProvider (EditorThemeFormat.VisualStudio, file, getStreamProvider);
					bundle.Add (theme);
					return theme;
				}
			} else if (file.EndsWith (".tmLanguage", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					string styleName = ScanTextMateSyntax (stream, out List<string> fileTypes);
					if (!string.IsNullOrEmpty (styleName) && fileTypes != null) {
						var syntaxProvider = AbstractSyntaxHighlightingDefinitionProvider.CreateProvider (SyntaxHighlightingDefinitionFormat.TextMate, fileTypes, getStreamProvider);
						bundle.Add (syntaxProvider);
						return syntaxProvider;
					} else {
						LoggingService.LogError ("Invalid .tmLanguage file : " + file);
					}
				}
			} else if (file.EndsWith (".sublime-syntax", StringComparison.OrdinalIgnoreCase)) {
				using (var stream = openStream ()) {
					string styleName = ScanSublimeSyntax (stream, out List<string> fileTypes);
					if (!string.IsNullOrEmpty (styleName) && fileTypes != null) {
						var syntaxProvider = AbstractSyntaxHighlightingDefinitionProvider.CreateProvider (SyntaxHighlightingDefinitionFormat.Sublime3, fileTypes, getStreamProvider);
						bundle.Add (syntaxProvider);
						return syntaxProvider;
					} else {
						LoggingService.LogError ("Invalid .sublime-syntax file : " + file);
					}
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
		static System.Text.RegularExpressions.Regex fileTypesRegex = new System.Text.RegularExpressions.Regex ("\\s*\"fileTypes\"");
		static System.Text.RegularExpressions.Regex fileTypesEndRegex = new System.Text.RegularExpressions.Regex ("\\],");

		enum JSonFormat { Unknown, OldSyntaxTheme, TextMateJsonSyntax }

		static bool TryScanJSonStyle (Stream stream, out string name, out JSonFormat format, out List<string> fileTypes)
		{
			name = null;
			fileTypes = null;
			format = JSonFormat.Unknown;
			StreamReader file = null; 

			try {
				file = TextFileUtility.OpenStream (stream);
				file.ReadLine ();
				var nameLine = file.ReadLine ();
				var versionLine = file.ReadLine ();
				var match = jsonNameRegex.Match (nameLine);
				if (match.Success) {
					if (jsonVersionRegex.Match (versionLine).Success) {
						name = match.Groups [1].Value;
						format = JSonFormat.OldSyntaxTheme;
						return true;
					}
				}
				string line;
				bool readFileTypes = false;

				while ((line = file.ReadLine ()) != null) {
					if (fileTypesRegex.Match (line).Success) {
						readFileTypes = true;
						fileTypes = new List<string> ();
						continue;
					}
					if (readFileTypes && fileTypesEndRegex.Match (line).Success)
						break;
					if (readFileTypes) {
						line = line.Trim ();
						if (line.Length > 3) {
							var fileType = line.Substring (1, line.Length - 3);
							fileTypes.Add (fileType);
						}
					}
				}
				if (fileTypes == null)
					return false;
				format = JSonFormat.TextMateJsonSyntax;
				return true;
			} catch (Exception e) {
				Console.WriteLine ("Error while scanning json:");
				Console.WriteLine (e);
			} finally {
				file?.Close();
			}
			return false;
		}

		static string ScanTextMateSyntax (Stream stream, out List<string> fileTypes)
		{
			fileTypes = null;
			string name = null;
			System.Text.RegularExpressions.Match match;

			try {
				var file = TextFileUtility.OpenStream (stream);
				bool readName = false, readFileTypes = false;
				while (true) {
					var line = file.ReadLine ();
					if (line == null)
						return name;
					if (name == null) {
						if (readName) { // usually this is the line with the name
							line = line.Trim ();
							if (line.StartsWith ("<string>", StringComparison.Ordinal) && line.EndsWith("</string>", StringComparison.Ordinal)) {
								name = line.Substring ("<string>".Length, line.Length - "<string>".Length - "</string>".Length);
							}
						}
						if (line.IndexOf ("<key>name</key>", StringComparison.Ordinal) >= 0) {
							readName = true;
							continue;
						}
					}

					if (line.IndexOf ("<key>fileTypes</key>", StringComparison.Ordinal) >= 0) {
						fileTypes = new List<string> ();
						readFileTypes = true;
						continue;
					}
					if (readFileTypes) {
						if (line.IndexOf ("</array>", StringComparison.Ordinal) >= 0 || line.IndexOf ("<array/>", StringComparison.Ordinal) >= 0) {
							readFileTypes = false;
							continue;
						}
						match = textMateNameRegex.Match (line);
						if (match.Success)
							fileTypes.Add (match.Groups [1].Value);
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while sublime 3 json", e);
				return null;
			}
		}

		static System.Text.RegularExpressions.Regex sublime3NameRegex = new System.Text.RegularExpressions.Regex ("^\\s*name:\\s*(.*)\\s*$");
		static string ScanSublimeSyntax (Stream stream, out List<string> fileTypes)
		{
			fileTypes = null;
			string name = null;
			System.Text.RegularExpressions.Match match;

			try {
				var file = TextFileUtility.OpenStream (stream);
				bool readExtensions = true;
				while (true) {
					var line = file.ReadLine ();
					if (line == null)
						return name;
					if (name == null) {
						match = sublime3NameRegex.Match (line);
						if (match.Success)
							name = match.Groups [1].Value.Trim ('"');
						continue;
					}
					if (line.Trim () == "file_extensions:") {
						fileTypes = new List<string> ();
						readExtensions = true;
						continue;
					}
					if (readExtensions) {
						line = line.Trim ();
						if (line [0] != '-')
							return name;
						fileTypes.Add (line.Substring (1).Trim ());
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while sublime 3 json", e);
				return null;
			}
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
				return match.Groups [1].Value;
			} catch (Exception e) {
				LoggingService.LogError ("Error while scanning json", e);
				return null;
			}
		}

		internal static void AddStyle (IStreamProvider provider)
		{
			string styleName;
			JSonFormat format;
			using (var stream = provider.Open ()) {
				if (TryScanJSonStyle (stream, out styleName, out format, out List<string> fileTypes)) {
					switch (format) {
					case JSonFormat.OldSyntaxTheme:
						var oldThemeProvider = AbstractThemeProvider.CreateProvider (EditorThemeFormat.XamarinStudio, styleName, () => provider);
						builtInBundle.Add (oldThemeProvider);
						break;
					case JSonFormat.TextMateJsonSyntax:
						var syntaxProvider = AbstractSyntaxHighlightingDefinitionProvider.CreateProvider (SyntaxHighlightingDefinitionFormat.TextMateJson, fileTypes, () => provider);
						builtInBundle.Add (syntaxProvider);
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
			languageBundles.Add (userThemeBundle);
			languageBundles.Add (extensionBundle);
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
						LoadStyleOrMode (userThemeBundle, file); 
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
					var o = LoadFile (extensionBundle, codon.Name, () => codon.Open (), () => codon);
					if (o is SyntaxHighlightingDefinition)
						((SyntaxHighlightingDefinition)o).PrepareMatches ();
					var bundle = o as LanguageBundle;
					if (bundle != null) {
						foreach (var h in bundle.Highlightings)
							if (h is SyntaxHighlightingDefinition def)
								def.PrepareMatches ();
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
							return h.GetSyntaxHighlightingDefinition ();
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
							bestMatch = h.GetSyntaxHighlightingDefinition ();
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
								return h.GetSyntaxHighlightingDefinition ();
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
							scope = highlight.GetSyntaxHighlightingDefinition ().Scope;
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
