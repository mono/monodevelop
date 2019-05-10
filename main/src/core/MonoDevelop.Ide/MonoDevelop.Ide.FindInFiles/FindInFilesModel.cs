// 
// FilterOptions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Extensions;

namespace MonoDevelop.Ide.FindInFiles
{
	class FindInFilesModel
	{
		static readonly char [] separators = { ';' };

		string file_mask;
		string [] split_file_masks;
		FileNameEvaluator evaluator;

		public string FileMask {
			get {
				return file_mask;
			}
			set {
				file_mask = value;
				if (file_mask == null) {
					split_file_masks = null;
				} else {
					split_file_masks = file_mask.Split (separators, StringSplitOptions.RemoveEmptyEntries);
				}

				evaluator = FileNameEvaluator.CreateFileNameEvaluator (split_file_masks);
				FileMaskChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		public event EventHandler FileMaskChanged;

		public bool InReplaceMode { get; set; }

		public bool CaseSensitive { get; set; }

		public bool WholeWordsOnly { get; set; }

		public bool RegexSearch { get; set; }

		public string FindPattern { get; set; }

		public string ReplacePattern { get; set; }

		SearchScope currentScope;
		public SearchScope SearchScope {
			get => currentScope;
			set {
				if (currentScope == value)
					return;
				currentScope = value;
				CurrentScopeChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		public event EventHandler CurrentScopeChanged;

		bool recurseSubdirectories;
		private string findInFilesPath;

		public bool RecurseSubdirectories {
			get => recurseSubdirectories;
			set {
				if (recurseSubdirectories == value)
					return;
				recurseSubdirectories = value;
				RecurseSubdirectoriesChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		public event EventHandler RecurseSubdirectoriesChanged;

		public string FindInFilesPath {
			get => findInFilesPath;
			internal set {
				if (findInFilesPath == value)
					return;
				findInFilesPath = value;
				FindInFilesPathChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		public event EventHandler FindInFilesPathChanged;

		public bool IncludeCodeBehind { get; internal set; }  // unused, may be added later

		public bool NameMatches (string name)
		{
			if (string.IsNullOrEmpty (FileMask) || FileMask == "*" || split_file_masks == null)
				return true;
			return evaluator.SupportsFile (name);
		}

		public static bool IsWordSeparator (char ch)
		{
			return !char.IsLetterOrDigit (ch) && ch != '_';
		}

		public static bool IsWholeWordAt (string text, int offset, int length)
		{
			return (offset <= 0 || IsWordSeparator (text [offset - 1])) &&
				   (offset + length >= text.Length || IsWordSeparator (text [offset + length]));
		}

		public override string ToString ()
		{
			return string.Format ("[FindInFilesModel: FindPattern={5}, ReplacePattern={6}, SearchScope={7}, FileMask={0}, InReplaceMode={1}, CaseSensitive={2}, WholeWordsOnly={3}, RegexSearch={4},  RecurseSubdirectories={8}, FindInFilesPath={9}]", FileMask, InReplaceMode, CaseSensitive, WholeWordsOnly, RegexSearch, FindPattern, ReplacePattern, SearchScope, RecurseSubdirectories, FindInFilesPath);
		}
	}
}