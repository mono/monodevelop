//
// TextMateLanguage.cs
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
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.TextMate
{
	public class TextMateLanguage
	{
		readonly ScopeStack scope;

		Dictionary<string, string> shellVariables;
		Dictionary<string, string> ShellVariables {
			get {
				if (shellVariables != null)
					return shellVariables;
				shellVariables = new Dictionary<string, string> ();
				foreach (var setting in SyntaxHighlightingService.GetSettings (scope).Where (s => s.Settings.ContainsKey ("shellVariables"))) {
					var vars = (PArray)setting.Settings ["shellVariables"];
					foreach (var d in vars.OfType<PDictionary> ()) {
						var name = d.Get<PString> ("name").Value;
						shellVariables [name] = d.Get<PString> ("value").Value;
					}
				}
				return shellVariables;
			}
		}


		internal IEnumerable<TmSnippet> Snippets {
			get {
				return SyntaxHighlightingService.GetSnippets (scope);
			}
		}

		string GetCommentStartString (int num)
		{
			if (num > 0)
				return "TM_COMMENT_START_" + (num + 1);
			return "TM_COMMENT_START";
		}

		string GetCommentEndString (int num)
		{
			if (num > 0)
				return "TM_COMMENT_END_" + (num + 1);
			return "TM_COMMENT_END";
		}

		List<string> lineComments;
		public IReadOnlyList<string> LineComments {
			get {
				if (lineComments != null)
					return lineComments;
				ExtractComments ();
				return lineComments;
			}
		}

		List<Tuple<string, string>> blockComments;
		public IReadOnlyList<Tuple<string, string>> BlockComments {
			get {
				if (blockComments != null)
					return blockComments;
				ExtractComments ();
				return blockComments;
			}
		}

		List<Tuple<string, string>> highlightPairs;
		public IReadOnlyList<Tuple<string, string>> HighlightPairs {
			get {
				if (highlightPairs != null)
					return highlightPairs;
				highlightPairs = new List<Tuple<string, string>> ();
				foreach (var setting in SyntaxHighlightingService.GetSettings (scope)) {
					PObject val;
					if (setting.TryGetSetting ("highlightPairs", out val)) {
						var arr = val as PArray;
						if (arr == null)
							continue;
						foreach (var pair in arr.OfType<PArray> ()) {
							if (pair.Count != 2)
								continue;
							try {
								highlightPairs.Add (Tuple.Create (((PString)pair [0]).Value, ((PString)pair [1]).Value));
							} catch (Exception e) {
								LoggingService.LogError ("Error while loading highlight pairs from :" + setting);
							}
						}
					}
				}
				return highlightPairs;
			}
		}

		Lazy<Regex> cancelCompletion;
		internal Regex CancelCompletion { get { return cancelCompletion.Value; } }

		Lazy<Regex> increaseIndentPattern;
		internal Regex IncreaseIndentPattern { get { return increaseIndentPattern.Value; } }

		Lazy<Regex> decreaseIndentPattern;
		internal Regex DecreaseIndentPattern { get { return decreaseIndentPattern.Value; } }

		Lazy<Regex> indentNextLinePattern;
		internal Regex IndentNextLinePattern { get { return indentNextLinePattern.Value; } }

		Lazy<Regex> unIndentedLinePattern;
		internal Regex UnIndentedLinePattern { get { return unIndentedLinePattern.Value; } }

		Lazy<Regex> foldingStartMarkerPattern;
		internal Regex FoldingStartMarker { get { return foldingStartMarkerPattern.Value; } }

		Lazy<Regex> foldingStopMarkerPattern;
		internal Regex FoldingStopMarker { get { return foldingStopMarkerPattern.Value; } }

		void ExtractComments ()
		{
			lineComments = new List<string> ();
			blockComments = new List<Tuple<string, string>> ();
			int i = 0;
			while (true) {
				string start, end;
				if (!ShellVariables.TryGetValue (GetCommentStartString (i), out start))
					break;
				if (ShellVariables.TryGetValue (GetCommentEndString (i), out end)) {
					blockComments.Add (Tuple.Create (start, end));
				} else {
					lineComments.Add (start);
				}
				i++;
			}
		}

		TextMateLanguage (ScopeStack scope)
		{
			this.scope = scope;
			cancelCompletion = new Lazy<Regex> (() => ReadSetting ("cancelCompletion"));
			increaseIndentPattern = new Lazy<Regex> (() => ReadSetting ("increaseIndentPattern"));
			decreaseIndentPattern = new Lazy<Regex> (() => ReadSetting ("decreaseIndentPattern"));
			indentNextLinePattern = new Lazy<Regex> (() => ReadSetting ("indentNextLinePattern"));
			unIndentedLinePattern = new Lazy<Regex> (() => ReadSetting ("unIndentedLinePattern"));
			foldingStartMarkerPattern = new Lazy<Regex> (() => ReadSetting ("foldingStartMarker"));
			foldingStopMarkerPattern = new Lazy<Regex> (() => ReadSetting ("foldingStopMarker"));
		}

		Regex ReadSetting (string settingName)
		{
			foreach (var setting in SyntaxHighlightingService.GetSettings (scope)) {
				PObject val;
				if (setting.TryGetSetting (settingName, out val)) {
					try {
						
						return new Regex (Sublime3Format.CompileRegex (((PString)val).Value));
					} catch (Exception e) {
						LoggingService.LogError ("Error while parsing " + settingName + ": " + val, e);
					}
				}
			}
			return null;
		}

		public static TextMateLanguage Create (ScopeStack scope) => new TextMateLanguage (scope);
	}
}