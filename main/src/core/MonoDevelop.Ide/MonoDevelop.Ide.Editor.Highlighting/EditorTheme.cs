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

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public static class ThemeSettingColors
	{
		public static readonly string Background = "background";
		public static readonly string Foreground = "foreground";
		public static readonly string Caret = "caret";
		public static readonly string Invisibles = "invisibles";
		public static readonly string LineHighlight = "lineHighlight";
		public static readonly string Selection = "selection";
		public static readonly string FindHighlight = "findHighlight";
		public static readonly string FindHighlightForeground = "findHighlightForeground";
		public static readonly string SelectionBorder = "selectionBorder";
		public static readonly string BracketsForeground = "bracketsForeground";
		public static readonly string BracketsOptions = "bracketsOptions";
	}

	public sealed class ThemeSetting 
	{
		public string Name { get; private set; }

		List<string> scopes = new List<string> ();
		public IReadOnlyList<string> Scopes { get { return scopes; } }

		Dictionary<string, string> settings = new Dictionary<string, string> ();

		internal ThemeSetting (string name, List<string> scopes, Dictionary<string, string> settings)
		{
			Name = name;
			this.scopes = scopes;
			this.settings = settings;
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
	}

	public sealed class EditorTheme
	{
		public string Name {
			get;
			private set;
		}

		List<ThemeSetting> settings;
		public IReadOnlyList<ThemeSetting> Settings {
			get {
				return settings;
			}
		}

		internal EditorTheme (string name) : this (name, new List<ThemeSetting> ())
		{
		}

		internal EditorTheme (string name, List<ThemeSetting> settings)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));
			if (settings == null)
				throw new ArgumentNullException (nameof (settings));
			Name = name;
			this.settings = settings;
		}
	}
}
