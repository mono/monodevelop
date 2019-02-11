//
// AddinRegistryExtensions.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using Mono.Addins;

namespace MonoDevelop.ExtensionTools
{
	static class AddinRegistryExtensions
	{
		public static Addin[] GetAllAddins (this AddinRegistry registry, Func<Addin, string> sortItemSelector = null)
		{
			if (sortItemSelector == null)
				sortItemSelector = x => x.Id;

			var array = registry.GetModules (AddinSearchFlags.IncludeAll | AddinSearchFlags.LatestVersionsOnly);

			var comparer = new NameComparer (sortItemSelector);
			Array.Sort (array, comparer);

			return array;
		}

		class NameComparer : IComparer<Addin>
		{
			readonly Func<Addin, string> selector;

			public NameComparer (Func<Addin, string> selector)
			{
				this.selector = selector;
			}

			public int Compare (Addin x, Addin y)
			{
				var xString = selector (x);
				var yString = selector (y);
				return string.Compare (xString, yString, StringComparison.Ordinal);
			}
		}
	}
}
