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

namespace MonoDevelop.Projects.MSBuild
{
	public class MSBuildItemGroup: MSBuildElement
	{
		internal override void ReadChildElement (MSBuildXmlReader reader)
		{
			var item = new MSBuildItem ();
			item.ParentNode = this;
			item.Read (reader);
			ChildNodes = ChildNodes.Add (item);
		}

		internal override string GetElementName ()
		{
			return "ItemGroup";
		}

		public bool IsImported {
			get;
			set;
		}
		
		public MSBuildItem AddNewItem (string name, string include)
		{
			return AddNewItem (name, include, null);
		}

		public MSBuildItem AddNewItem (string name, string include, MSBuildItem beforeItem)
		{
			AssertCanModify ();
			var it = new MSBuildItem (name);
			it.Include = include;
			AddItem (it, beforeItem);
			return it;
		}

		public void AddItem (MSBuildItem item)
		{
			AssertCanModify ();
			item.ParentNode = this;
			ChildNodes = ChildNodes.Add (item);
			item.ResetIndent (false);
			if (ParentProject != null)
				ParentProject.NotifyChanged ();
		}

		public void AddItem (MSBuildItem item, MSBuildItem beforeItem)
		{
			AssertCanModify ();
			item.ParentNode = this;

			int i;
			if (beforeItem != null && (i = ChildNodes.IndexOf (beforeItem)) != -1)
				ChildNodes = ChildNodes.Insert (i, item);
			else
				ChildNodes = ChildNodes.Add (item);
			
			item.ResetIndent (false);
			if (ParentProject != null)
				ParentProject.NotifyChanged ();
		}

		public IEnumerable<MSBuildItem> Items {
			get {
				return ChildNodes.OfType<MSBuildItem> ();
			}
		}

		internal void RemoveItem (MSBuildItem item)
		{
			AssertCanModify ();
			if (ChildNodes.Contains (item)) {
				item.RemoveIndent ();
				ChildNodes = ChildNodes.Remove (item);
				item.ParentNode = null;
				NotifyChanged ();
			}
		}
	}
	
}
