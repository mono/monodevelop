//
// SearchPopupSearchPattern.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Collections.Generic;
using Gtk;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.Components.MainToolbar
{
	public struct SearchPopupSearchPattern
	{
		public string Tag;
		public string Pattern;
		public int    LineNumber;

		public bool HasLineNumber {
			get {
				return LineNumber >= 0;
			}
		}

		public SearchPopupSearchPattern (string tag, string pattern, int lineNumber)
		{
			Tag = tag;
			Pattern = pattern;
			LineNumber = lineNumber;
		}

		void SetPart (int part, string tag)
		{
			switch (part) {
			case 0:
				Tag = tag;
				break;
			case 1:
				Pattern = tag;
				break;
			case 2:
				if (string.IsNullOrEmpty (tag)) {
					LineNumber = 0;
					return;
				}
				try {
					LineNumber = int.Parse (tag);
				} catch (Exception) {
					LineNumber = 0;
				}
				break;
			}
		}
		
		public static SearchPopupSearchPattern ParsePattern (string searchPattern)
		{
			var result = new SearchPopupSearchPattern ();
			result.LineNumber = -1;
			const int maxTag = 3;
			string[] parts = new string[maxTag];
			int foundTags = 0;
			int idx = 0;
			for (int i = 0; i < searchPattern.Length; i++) {
				if (searchPattern[i] == ':') {
					parts[foundTags++] = searchPattern.Substring (idx, i - idx);
					idx = i + 1;
					if (foundTags >= maxTag)
						break;
				}
			}
			if (foundTags < maxTag)
				parts[foundTags++] = searchPattern.Substring (idx,searchPattern.Length - idx);
			switch (foundTags) {
			case 1:
				result.SetPart (1, parts[0]);
				break;
			case 2:
				try {
					int.Parse (parts[1]);
					if (!string.IsNullOrEmpty (parts[0]))
						result.SetPart (1, parts[0]);
					result.SetPart (2, parts[1]);
				} catch (Exception) {
					result.SetPart (0, parts[0]);
					result.SetPart (1, parts[1]);
				}
				break;
			case 3:
				result.SetPart (0, parts[0]);
				result.SetPart (1, parts[1]);
				result.SetPart (2, parts[2]);
				break;
			}
			return result;
		}

		public override int GetHashCode ()
		{
			return (Tag != null ? Tag.GetHashCode () : 0) ^ (Pattern != null ? Pattern.GetHashCode () : 0) ^ LineNumber.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			if (!(obj is SearchPopupSearchPattern))
				return false;
			var other = (SearchPopupSearchPattern)obj;
			return Tag == other.Tag && Pattern == other.Pattern && LineNumber == other.LineNumber;
		}

		public override string ToString ()
		{
			return string.Format ("[SearchPopupSearchPattern: Tag={0}, Pattern={1}, LineNumber={2}]", Tag, Pattern, LineNumber);
		}
	}
}