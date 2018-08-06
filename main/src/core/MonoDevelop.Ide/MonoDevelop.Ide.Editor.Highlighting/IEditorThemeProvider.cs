//
// IEditorThemeProvider.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.IO;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	interface IEditorThemeProvider
	{
		string Name { get; }
		EditorTheme GetEditorTheme ();
	}

	enum EditorThemeFormat
	{
		XamarinStudio,
		VisualStudio,
		TextMate,
		TextMateJson
	}

	abstract class AbstractThemeProvider : IEditorThemeProvider
	{
		readonly Func<IStreamProvider> getStreamProvider;
		public string Name { get; }

		AbstractThemeProvider (string name, Func<IStreamProvider> getStreamProvider)
		{
			this.Name = name;
			this.getStreamProvider = getStreamProvider;
		}

		protected abstract EditorTheme LoadTheme (Stream stream);

		EditorTheme theme;

		static HashSet<string> faultedThemes = new HashSet<string> ();

		public EditorTheme GetEditorTheme ()
		{
			if (theme != null)
				return theme;
			try {
				using (var stream = getStreamProvider ().Open ()) {
					theme = LoadTheme (stream);
				}
				return theme;
			} catch (Exception e) {
				if (faultedThemes.Add (Name)) {
					MessageService.ShowError (GettextCatalog.GetString ("Error while loading theme :" + Name), e);
					LoggingService.LogError ("Error while loading theme :" + Name, e);
				}
				return null;
			}
		}
		public static IEditorThemeProvider CreateProvider (EditorThemeFormat format, string name, Func<IStreamProvider> getStreamProvider)
		{
			switch (format) {
			case EditorThemeFormat.XamarinStudio:
				return new XamarinStudioFormatThemeProvider (name, getStreamProvider);
			case EditorThemeFormat.VisualStudio:
				return new VisualStudioThemeProvider (name, getStreamProvider);
			case EditorThemeFormat.TextMate:
				return new TextMateThemeProvider (name, getStreamProvider);
			default:
				throw new InvalidOperationException ("Unknown editor theme format " + format);
			}
		}

		#region Format implementations

		class XamarinStudioFormatThemeProvider : AbstractThemeProvider
		{
			public XamarinStudioFormatThemeProvider (string name, Func<IStreamProvider> getStreamProvider) : base (name, getStreamProvider)
			{
			}

			protected override EditorTheme LoadTheme (Stream stream) => OldFormat.ImportColorScheme (stream);
		}

		class TextMateThemeProvider : AbstractThemeProvider
		{
			public TextMateThemeProvider (string name, Func<IStreamProvider> getStreamProvider) : base (name, getStreamProvider)
			{
			}

			protected override EditorTheme LoadTheme (Stream stream) => TextMateFormat.LoadEditorTheme (stream);
		}

		class VisualStudioThemeProvider : AbstractThemeProvider
		{
			string file;

			public VisualStudioThemeProvider (string file, Func<IStreamProvider> getStreamProvider) : base (System.IO.Path.GetFileNameWithoutExtension (file), getStreamProvider)
			{
				this.file = file;
			}

			protected override EditorTheme LoadTheme (Stream stream)
			{
				try {
					return OldFormat.ImportVsSetting (Name, stream);
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
			}
		}
		#endregion
	}
}