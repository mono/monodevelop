//
// SearchAndReplaceOptions.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.SourceEditor
{
	class SearchAndReplaceOptions
    {
		static string searchPattern;
		public static string SearchPattern {
			get {
				return searchPattern;
			}
			set {
				if (searchPattern == value)
					return;
				searchPattern = value;
				OnSearchPatternChanged (EventArgs.Empty);
			}
		}

		public static event EventHandler SearchPatternChanged;

		static void OnSearchPatternChanged (EventArgs e)
		{
			var handler = SearchPatternChanged;
			if (handler != null)
				handler (null, e);
		}

		static string replacePattern;
		public static string ReplacePattern {
			get {
				return replacePattern;
			}
			set {
				if (replacePattern == value)
					return;
				replacePattern = value;
				OnReplacePatternChanged (EventArgs.Empty);
			}
		}

		public static event EventHandler ReplacePatternChanged;

		static void OnReplacePatternChanged (EventArgs e)
		{
			var handler = ReplacePatternChanged;
			if (handler != null)
				handler (null, e);
		}
	}
}

