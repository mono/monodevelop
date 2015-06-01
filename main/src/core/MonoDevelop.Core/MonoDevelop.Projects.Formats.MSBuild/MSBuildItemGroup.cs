//
// MSBuildItemGroup.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildItemGroup: MSBuildObject
	{
		List<MSBuildItem> items = new List<MSBuildItem> ();

		internal override void ReadChildElement (MSBuildXmlReader reader)
		{
			var item = new MSBuildItem ();
			item.ParentObject = this;
			item.Read (reader);
			items.Add (item);
		}

		internal override string GetElementName ()
		{
			return "ItemGroup";
		}

		internal override IEnumerable<MSBuildObject> GetChildren ()
		{
			return items;
		}

		public bool IsImported {
			get;
			set;
		}
		
		public MSBuildItem AddNewItem (string name, string include)
		{
			var it = new MSBuildItem (name);
			it.ParentObject = this;
			it.Include = include;
			items.Add (it);
			if (ParentProject != null)
				ParentProject.NotifyChanged ();
			return it;
		}

		public void AddItem (MSBuildItem item)
		{
			items.Add (item);
			if (ParentProject != null)
				ParentProject.NotifyChanged ();
		}

		public IEnumerable<MSBuildItem> Items {
			get {
				return items;
			}
		}

		internal void RemoveItem (MSBuildItem item)
		{
			if (items.Contains (item)) {
				item.RemoveIndent ();
				items.Remove (item);
				NotifyChanged ();
			}
		}
	}
	
}
