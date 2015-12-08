//
// TextLink.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Core.Text;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor
{
	public sealed class TextLink : IListDataProvider<string>
	{
		public ISegment PrimaryLink {
			get {
				if (links.Count == 0)
					return TextSegment.Invalid;
				return links [0];
			}
		}

		List<ISegment> links = new List<ISegment> ();

		public List<ISegment> Links {
			get {
				return links;
			}
			set {
				links = value;
			}
		}

		public bool IsIdentifier {
			get;
			set;
		}

		public bool IsEditable {
			get;
			set;
		}

		public string Name {
			get;
			set;
		}

		public string CurrentText {
			get;
			set;
		}

		public string Tooltip {
			get;
			set;
		}

		public IListDataProvider<string> Values {
			get;
			set;
		}

		public Func<Func<string, string>, IListDataProvider<string>> GetStringFunc {
			get;
			set;
		}

		public TextLink (string name)
		{
			IsEditable = true;
			this.Name = name;
			this.IsIdentifier = false;
		}

		public override string ToString ()
		{
			return string.Format ("[TextLink: Name={0}, Links={1}, IsEditable={2}, Tooltip={3}, CurrentText={4}, Values=({5})]", 
				Name, 
				Links.Count, 
				IsEditable, 
				Tooltip, 
				CurrentText, 
				Values.Count);
		}

		public void AddLink (ISegment segment)
		{
			links.Add (segment);
		}
		#region IListDataProvider implementation
		public string GetText (int n)
		{
			return Values != null ? Values.GetText (n) : "";
		}

		public string this [int n] {
			get {
				return Values != null ? Values [n] : "";
			}
		}

		public Xwt.Drawing.Image GetIcon (int n)
		{
			return Values != null ? Values.GetIcon (n) : null;
		}

		public int Count {
			get {
				return Values != null ? Values.Count : 0;
			}
		}
		#endregion
	}
}

