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


namespace MonoDevelop.Projects.Formats.MSBuild
{
	
	public class MSBuildItemGroup: MSBuildObject
	{
		MSBuildProject parent;

		internal MSBuildItemGroup (MSBuildProject parent, XmlElement elem): base (elem)
		{
			this.parent = parent;
		}

		public bool IsImported {
			get;
			set;
		}
		
		public MSBuildItem AddNewItem (string name, string include)
		{
			XmlElement elem = AddChildElement (name);
			MSBuildItem it = parent.GetItem (elem);
			it.Include = include;
			XmlUtil.Indent (parent.TextFormat, elem, false);
			parent.NotifyChanged ();
			return it;
		}

		public void AddItem (MSBuildItem item)
		{
			XmlElement elem = item.Element;
			Element.AppendChild (elem);
			XmlUtil.Indent (parent.TextFormat, elem, false);
			parent.AddToItemCache (item);
			parent.NotifyChanged ();
		}

		MSBuildItem[] items;
		public IEnumerable<MSBuildItem> Items {
			get {
				lock (parent.ReadLock) {
					if (items == null)
						items = Element.ChildNodes.OfType<XmlElement> ().Select (e => parent.GetItem (e)).ToArray ();
					return items;
				}
			}
		}

		internal void ResetItemCache ()
		{
			lock (parent.ReadLock)
				items = null;
		}
	}
	
}
