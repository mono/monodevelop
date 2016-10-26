//
// LanguageBundle.cs
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
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	class LanguageBundle
	{
		List<SyntaxHighlightingDefinition> highlightings = new List<SyntaxHighlightingDefinition> ();
		List<TmSetting> settings = new List<TmSetting> ();
		List<TmSnippet> snippets = new List<TmSnippet> ();
		List<EditorTheme> editorThemes = new List<EditorTheme> ();

		public IReadOnlyList<EditorTheme> EditorThemes {
			get {
				return editorThemes;
			}
		}

		public IReadOnlyList<SyntaxHighlightingDefinition> Highlightings {
			get {
				return highlightings;
			}
		}

		public IReadOnlyList<TmSetting> Settings {
			get {
				return settings;
			}
		}

		public IReadOnlyList<TmSnippet> Snippets {
			get {
				return snippets;
			}
		}

		public string Name { get; private set; }

		public string FileName { get; private set; }

		public LanguageBundle (string name, string fileName)
		{
			Name = name;
			FileName = fileName;
		}

		public void Add (EditorTheme theme)
		{
			editorThemes.Add (theme);
		}

		public void Remove (EditorTheme style)
		{
			editorThemes.Remove (style);
		}

		public void Add (TmSetting setting)
		{
			settings.Add (setting);
		}

		public void Add (TmSnippet snippet)
		{
			snippets.Add (snippet);
		}

		public void Add (SyntaxHighlightingDefinition highlighting)
		{
			highlightings.Add (highlighting);
		}
	}
}
