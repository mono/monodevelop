//
// TmSetting.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
	sealed class TmSetting 
	{
		public readonly string Name = ""; // not defined in vs.net

		IReadOnlyList<StackMatchExpression> scopes;
		public IReadOnlyList<StackMatchExpression> Scopes { get { return scopes; } }

		IReadOnlyDictionary<string, PObject> settings;

		internal IReadOnlyDictionary<string, PObject> Settings {
			get {
				return settings;
			}
		}

		internal TmSetting (string name, IReadOnlyList<StackMatchExpression> scopes, IReadOnlyDictionary<string, PObject> settings)
		{
			Name = name;
			this.scopes = scopes ?? new List<StackMatchExpression> ();
			this.settings = settings ?? new Dictionary<string, PObject> ();
		}

		public bool TryGetSetting (string key, out PObject value)
		{
			return settings.TryGetValue (key, out value);
		}

		public bool TryGetColor (string key, out HslColor color)
		{
			PObject value;
			if (!settings.TryGetValue (key, out value)) {
				color = new HslColor (0, 0, 0);
				return false;
			}
			try {
				color = HslColor.Parse (((PString)value).Value);
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

		internal static bool IsSettingMatch (ScopeStack scopes, StackMatchExpression expr)
		{
			string cs = null;
			int d = 0;
			if (EditorTheme.IsCompatibleScope (expr, scopes, ref cs, ref d)) {
				return true;
			}
			return false;
		}
	}
}