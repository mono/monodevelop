//
// EditorTheme.cs
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
using MonoDevelop.Components;
using Xwt.Drawing;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Linq;
using Cairo;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public sealed class ThemeSetting 
	{
		public readonly string Name = ""; // not defined in vs.net

		IReadOnlyList<StackMatchExpression> scopes;
		public IReadOnlyList<StackMatchExpression> Scopes { get { return scopes; } }

		IReadOnlyDictionary<string, string> settings;

		internal IReadOnlyDictionary<string, string> Settings {
			get {
				return settings;
			}
		}

		internal ThemeSetting (string name, IReadOnlyList<StackMatchExpression> scopes, IReadOnlyDictionary<string, string> settings)
		{
			Name = name;
			this.scopes = scopes ?? new List<StackMatchExpression> ();
			this.settings = settings ?? new Dictionary<string, string> ();
		}

		internal ThemeSetting (string name, IReadOnlyList<string> scopes, IReadOnlyDictionary<string, string> settings)
		{
			Name = name;
			var s = new List<StackMatchExpression> ();
			if (scopes != null) {
				foreach (var str in scopes)
					s.Add (StackMatchExpression.Parse (str));
			}
			this.scopes = s;
			this.settings = settings ?? new Dictionary<string, string> ();
		}

		public bool TryGetSetting (string key, out string value)
		{
			return settings.TryGetValue (key, out value);
		}

		ImmutableDictionary<string, HslColor> colorCache = ImmutableDictionary<string, HslColor>.Empty;
		
		public bool TryGetColor (string key, out HslColor color)
		{
			string value;
			if (colorCache.TryGetValue (key, out color))
				return true;
			if (!settings.TryGetValue (key, out value)) {
				color = new HslColor (0, 0, 0);
				return false;
			}
			try {
				color = HslColor.Parse (value);
				colorCache = colorCache.SetItem (key, color);
			} catch (Exception e) {
				LoggingService.LogError ("Error while parsing color " + key, e);
				color = new HslColor (0, 0, 0);
				colorCache = colorCache.SetItem (key, color);
				return false;
			}
			return true;
		}

		public override string ToString ()
		{
			return string.Format ("[ThemeSetting: Name={0}]", Name);
		}
	}

	public sealed class EditorTheme : IEditorThemeProvider
	{
		public readonly static string DefaultThemeName = "Light";
		public readonly static string DefaultDarkThemeName = "Dark";

		public string Name {
			get;
			private set;
		}

		public string Uuid {
			get;
			private set;
		}

		internal string FileName { get; set; }

		List<ThemeSetting> settings;
		internal object CollapsedText;

		public IReadOnlyList<ThemeSetting> Settings {
			get {
				return settings;
			}
		}

		internal EditorTheme (string name) : this (name, new List<ThemeSetting> ())
		{
		}

		internal EditorTheme (string name, List<ThemeSetting> settings) : this (name, settings, Guid.NewGuid ().ToString ())
		{
		}

		internal EditorTheme (string name, List<ThemeSetting> settings, string uuuid)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));
			if (settings == null)
				throw new ArgumentNullException (nameof (settings));
			if (uuuid == null)
				throw new ArgumentNullException (nameof (uuuid));
			Name = name;
			this.settings = settings;
			this.Uuid = uuuid;
		}


		readonly struct FontColorStyle {
			public readonly HslColor Foreground;
			public readonly HslColor Background;
			public readonly string FontStyle;

			public FontColorStyle (HslColor foregound, HslColor background, string fontStyle)
			{
				Foreground = foregound;
				Background = background;
				FontStyle = fontStyle;
			}
		}

		FontColorStyle GetColor (ScopeStack scopeStack)
		{
			HslColor foreground;
			HslColor background;
			string fontStyle;
			settings [0].TryGetColor (EditorThemeColors.Foreground, out foreground);
			settings [0].TryGetColor (EditorThemeColors.Background, out background);
			settings [0].TryGetSetting ("fontStyle", out fontStyle);

			string found = null;
			int foundDepth = int.MaxValue;
			if (scopeStack.Count > 1) {
				if (scopeStack.Peek () == scopeStack.FirstElement)
					return new FontColorStyle (foreground, background, fontStyle);
			}

			for (int i = 1; i < settings.Count; ++i) {
				var setting = settings[i];
				string compatibleScope = null;
				int depth = 0;
				if (IsValidScope (setting, scopeStack, ref compatibleScope, ref depth)) {
					if (found != null && (depth > foundDepth || depth == foundDepth && found.Length >= compatibleScope.Length))
						continue;
					HslColor tryC;
					if (setting.TryGetColor (EditorThemeColors.Foreground, out tryC)) {
						found = compatibleScope;
						foreground = tryC;
						foundDepth = depth;
					}
					if (setting.TryGetColor (EditorThemeColors.Background, out tryC)) {
						background = tryC;
					}
					string tryS;
					if (setting.TryGetSetting ("fontStyle", out tryS)) {
						fontStyle = tryS;
					}

				}
			}
			return new FontColorStyle (foreground, background, fontStyle);
		}

		bool IsValidScope (ThemeSetting setting, ScopeStack scopeStack, ref string compatibleScope, ref int depth)
		{
			if (setting.Scopes.Count == 0) {
				compatibleScope = "";
				depth = int.MaxValue - 1;
				return true;
			}
			for(int i = 0; i < setting.Scopes.Count; i++) {
				var s = setting.Scopes[i];
				if (IsCompatibleScope (s, scopeStack, ref compatibleScope, ref depth)) {
					return true;
				}
			}
			return false;
		}

		public bool TryGetColor (string scope, string key, out HslColor result)
		{
			string found = null;
			int foundDepth = int.MaxValue;
			var foundColor = default (HslColor);
			var stack = new ScopeStack (scope);
			foreach (var setting in settings) {
				string compatibleScope = null;
				int depth = 0;
				if (IsValidScope (setting, stack, ref compatibleScope, ref depth)) {
					if (found != null && (depth > foundDepth || depth == foundDepth && found.Length >= compatibleScope.Length))
						continue;

					if (setting.TryGetColor (key, out foundColor)) {
						found = compatibleScope;
						foundDepth = depth;
					}
				}
			}
			if (found != null) {
				result = foundColor;
				return true;
			}
			result = default (HslColor);
			return false;
		}

		public bool TryGetColor (string key, out HslColor color)
		{
			foreach (var setting in settings) {
				if (setting.TryGetColor (key, out color))
					return true;
			}
			color = default (HslColor);
			return false;
		}

		internal static bool IsCompatibleScope (StackMatchExpression expr, ScopeStack scope, ref string matchingKey, ref int depth)
		{
			depth = 0;
			if (scope.Count == 1)
			{
				var result = expr.MatchesStack (scope, ref matchingKey);
				if (result.Item1) {
					return true;
				}
				depth = 1;
			}
			else
			{
				depth = 0;
				while (!scope.IsEmpty) {
					var result = expr.MatchesStack (scope, ref matchingKey);
					if (result.Item1) {
						return true;
					}
					scope = scope.Pop ();
					depth++;
				}
			}
			return false;
		}

		internal ChunkStyle GetChunkStyle (ScopeStack scope)
		{
			var color = GetColor (scope);
			return new ChunkStyle () {
				ScopeStack = scope,
				Foreground = color.Foreground,
				Background = color.Background,
				FontStyle = ConvertFontStyle (color.FontStyle),
				FontWeight = ConvertFontWeight (color.FontStyle)
			};
		}

		private Xwt.Drawing.FontWeight ConvertFontWeight (string fontStyle)
		{
			if (fontStyle != null) {
				if (fontStyle.Contains ("bold"))
					return Xwt.Drawing.FontWeight.Bold;
			}
			return Xwt.Drawing.FontWeight.Normal;
		}

		FontStyle ConvertFontStyle (string fontStyle)
		{
			if (fontStyle != null) {
				if (fontStyle.Contains ("italic"))
					return FontStyle.Italic;
				if (fontStyle.Contains ("oblique"))
					return FontStyle.Oblique;
			}
			return FontStyle.Normal;
		}

		internal Cairo.Color GetForeground (ChunkStyle chunkStyle)
		{
			if (chunkStyle.TransparentForeground) {
				return GetColor (ScopeStack.Empty).Foreground;
			}
			return chunkStyle.Foreground;
		}

		internal EditorTheme CloneWithName (string newName)
		{
			var result = (EditorTheme)this.MemberwiseClone ();
			result.Name = newName;
			return result;
		}

		EditorTheme IEditorThemeProvider.GetEditorTheme () => this;
	}
}