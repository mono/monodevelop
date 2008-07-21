//
// MakefileVar.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Autotools
{

	[DataItem ("MakefileVariable")]
	public class MakefileVar
	{
		List<string> extra = null;

		[ItemProperty (DefaultValue = false)]
		public bool Sync = false;

		[ItemProperty (DefaultValue = "")]
		public string Name = String.Empty;

		[ItemProperty (DefaultValue = "")]
		public string Prefix = String.Empty;

		public bool SaveEnabled = true;

		public MakefileVar ()
		{
		}

		public MakefileVar (MakefileVar var)
		{
			this.extra = new List<string> (var.Extra);
			this.Sync = var.Sync;
			this.Name = var.Name;
			this.Prefix = var.Prefix;
			this.SaveEnabled = var.SaveEnabled;
		}

		public List<string> Extra {
			get {
				if (extra == null)
					extra = new List<string> ();
				return extra;
			}
		}
	}
}
