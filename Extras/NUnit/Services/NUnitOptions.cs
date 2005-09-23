//
// NUnitCategoryOptions.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using MonoDevelop.Internal.Serialization;
using MonoDevelop.Services;

namespace MonoDevelop.NUnit
{
	public class NUnitCategoryOptions: ICloneable
	{
		[ItemProperty ("Categories")]
		[ItemProperty ("Category", ValueType=typeof(string), Scope=1)]
		StringCollection categories = new StringCollection ();
		
		bool enableFilter;
		bool exclude;
		
		public StringCollection Categories {
			get { return categories; }
		}
		
		[ItemProperty]
		public bool EnableFilter {
			get { return enableFilter; }
			set { enableFilter = value; }
		}
		
		[ItemProperty]
		public bool Exclude {
			get { return exclude; }
			set { exclude = value; }
		}
		
		public object Clone ()
		{
			NUnitCategoryOptions op = new NUnitCategoryOptions ();
			op.enableFilter = enableFilter;
			op.exclude = exclude;
			op.categories = new StringCollection ();
			foreach (string s in categories)
				op.categories.Add (s);
			return op;
		}
		
		public override string ToString ()
		{
			if (EnableFilter && Categories.Count > 0) {
				StringBuilder s = new StringBuilder ();
				if (Exclude)
					s.Append (GettextCatalog.GetString ("Exclude the following categories: "));
				else
					s.Append (GettextCatalog.GetString ("Include the following categories: "));
				for (int n=0; n<Categories.Count; n++) {
					if (n > 0) s.Append (", ");
					s.Append (Categories [n]);
				}
				return s.ToString ();
			} else
				return "";
		}
	}
}

