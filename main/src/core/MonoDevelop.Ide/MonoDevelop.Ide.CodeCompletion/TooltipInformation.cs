//
// TooltipInformation.cs
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
using System.Collections.Generic;

namespace MonoDevelop.Ide.CodeCompletion
{
	/// <summary>
	/// A Tooltip class provides the information required to display a styled language item tooltip. 
	/// A styled tooltip includes pango markups for the signature, for one or more categories and a summary.
	/// </summary>
	public class TooltipInformation
	{
		string signatureMarkup;
		string summaryMarkup;
		List<Tuple<string, string>> categories = new List<Tuple<string, string>> ();

		/// <summary>
		/// Gets or sets the signature markup. The signature is never null.
		/// </summary>
		public string SignatureMarkup {
			get {
				return signatureMarkup ?? "";
			}
			set {
				signatureMarkup = value;
			}
		}

		/// <summary>
		/// Gets the categories. This is never null, but may be empty.
		/// </summary>
		public IEnumerable<Tuple<string, string>> Categories { 
			get {
				return categories;
			}
		}

		/// <summary>
		/// Gets or sets the summary markup. The summary markup is never null.
		/// </summary>
		public string SummaryMarkup {
			get {
				return summaryMarkup ?? "";
			}
			set {
				summaryMarkup = value;
			}
		}

		string footerMarkup;

		/// <summary>
		/// Gets or sets the footer markup.
		/// </summary>
		public string FooterMarkup {
			get {
				return footerMarkup ?? "";
			}
			set {
				footerMarkup = value;
			}
		}

		/// <summary>
		/// Adds a new category to the tooltip.
		/// </summary>
		/// <param name='categoryLabel'>
		/// The category label as non escaped text.
		/// </param>
		/// <param name='categoryMarkup'>
		/// The pango markup of the category contents.
		/// </param>
		public void AddCategory (string categoryLabel, string categoryMarkup)
		{
			categories.Add (Tuple.Create (categoryLabel, categoryMarkup));
		}
	}
}
