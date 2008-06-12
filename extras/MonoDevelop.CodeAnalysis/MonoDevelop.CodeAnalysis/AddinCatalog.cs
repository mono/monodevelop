// Gettext.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
// Copyright (c) 2007 Ben Motmans
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
//
//

using System;
using Mono.Addins;

namespace MonoDevelop.CodeAnalysis {
	public static class AddinCatalog {
		static AddinLocalizer localizer;
		
		public static string GetString (string str)
		{
			if (localizer == null)
				localizer = AddinManager.CurrentLocalizer;
			return localizer.GetString (str);
		}
		
		public static string GetString (string str, params object[] pars)
		{
			return string.Format (GetString (str), pars);
		}
		
		public static string GetPluralString (string msgid, string defaultPlural, int n)
		{
			if (localizer == null)
				localizer = AddinManager.CurrentLocalizer;
			return localizer.GetPluralString (msgid, defaultPlural, n);
		}
		
		public static string GetPluralString (string msgid, string defaultPlural, int n, params string[] args)
		{
			if (localizer == null)
				localizer = AddinManager.CurrentLocalizer;
			return localizer.GetPluralString (msgid, defaultPlural, n, args);
		}
	}
}