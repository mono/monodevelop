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

		public bool TryGetColor (string key, out HslColor color)
		{
			string value;
			if (!settings.TryGetValue (key, out value)) {
				color = new HslColor (0, 0, 0);
				return false;
			}
			try {
				color = HslColor.Parse (value);
			} catch (Exception e) {
				LoggingService.LogError ("Error while parsing color " + key, e);
				color = new HslColor (0, 0, 0);
				return false;
			}
			return true;
		}

		public override string ToString ()
		{
			return string.Format ("[ThemeSetting: Name={0}]", Name);
		}
	}

	public sealed class EditorTheme
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

		HslColor GetColor (string key, ImmutableStack<string> scopeStack)
		{
			HslColor result = default (HslColor);
			string found = null;
			foreach (var setting in settings) {
				string compatibleScope = null;
				if (setting.Scopes.Count == 0 || setting.Scopes.Any (s => IsCompatibleScope (s, scopeStack, ref compatibleScope))) {
					if (found != null && found.Length > compatibleScope.Length)
						continue;
					HslColor tryC;
					if (setting.TryGetColor (key, out tryC)) {
						found = compatibleScope;
						result = tryC;
					}
				}
			}
			if (found != null) {
				return result;
			}
			return result;
		}

		string GetSetting (string key, ImmutableStack<string> scopeStack)
		{
			string result = null;
			string found = null;
			foreach (var setting in settings) {
				string compatibleScope = null;
				if (setting.Scopes.Count == 0 || setting.Scopes.Any (s => IsCompatibleScope (s, scopeStack, ref compatibleScope))) {
					if (found != null && found.Length > compatibleScope.Length)
						continue;

					string tryC;
					if (setting.TryGetSetting (key, out tryC)) {
						found = compatibleScope;
						result = tryC;
					}
				}
			}
			if (found != null) {
				return result;
			}
			return result;
		}

		public bool TryGetColor (string scope, string key, out HslColor result)
		{
			string found = null;
			var foundColor = default (HslColor);
			var stack = ImmutableStack<string>.Empty.Push (scope);
			foreach (var setting in settings) {
				string compatibleScope = null;
				if (setting.Scopes.Count == 0 || setting.Scopes.Any (s => IsCompatibleScope (s, stack, ref compatibleScope))) {
					if (found != null && found.Length > compatibleScope.Length)
						continue;
				
					if (setting.TryGetColor (key, out foundColor))
						found = compatibleScope;
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

		internal static bool IsCompatibleScope (StackMatchExpression expr, ImmutableStack<string> scope, ref string matchingKey)
		{
			while (!scope.IsEmpty) {
				var result = expr.MatchesStack (scope);
				if (result.Item1) 
					return true;
				scope = scope.Pop ();
			}
			return false;
		}

		internal ChunkStyle GetChunkStyle (ImmutableStack<string> scope)
		{
			var fontStyle = GetSetting ("fontStyle", scope);

			return new ChunkStyle () {
				ScopeStack = scope,
				Foreground = GetColor (EditorThemeColors.Foreground, scope),
				Background = GetColor (EditorThemeColors.Background, scope),
				FontStyle = ConvertFontStyle (fontStyle)
			};
		}

		FontStyle ConvertFontStyle (string fontStyle)
		{
			if (fontStyle == "italic")
				return FontStyle.Italic;
			return FontStyle.Normal;
		}

		internal Cairo.Color GetForeground (ChunkStyle chunkStyle)
		{
			if (chunkStyle.TransparentForeground)
				return GetColor (EditorThemeColors.Foreground, ImmutableStack<string>.Empty.Push (""));
			return chunkStyle.Foreground;
		}

		internal EditorTheme CloneWithName (string newName)
		{
			var result = (EditorTheme)this.MemberwiseClone ();
			result.Name = newName;
			return result;
		}
	}
}