// 
// CodeTemplateListDataProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor.PopupWindow;

namespace MonoDevelop.Ide.CodeTemplates
{
	public class CodeTemplateListDataProvider : IListDataProvider<string>
	{
		List<KeyValuePair<string, string>> itemList;
		
		public CodeTemplateListDataProvider (List<KeyValuePair<string, string>> itemList)
		{
			this.itemList = itemList;
		}
		
		public CodeTemplateListDataProvider (string s)
		{
			itemList = new List<KeyValuePair<string, string>> ();
			itemList.Add (new KeyValuePair<string, string> (null, s));
		}

		#region IListDataProvider implementation
		public string GetText (int index)
		{
			return itemList[index].Value;
		}
		
		public string this [int index] {
			get {
				return GetText (index);
			}
		}
		
		public Gdk.Pixbuf GetIcon (int index)
		{
			string iconName = itemList[index].Key;
			if (string.IsNullOrEmpty (iconName))
				return null;
			return MonoDevelop.Core.Gui.PixbufService.GetPixbuf (iconName, Gtk.IconSize.Menu);
		}
		
		public int Count {
			get {
				return itemList.Count;
			}
		}
		#endregion
		
	}
}
