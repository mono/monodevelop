//
// TreeStoreExtensions.cs
//
// Author:
//       Aleksandr Shevchenko <alexandre.shevchenko@gmail.com>
//
// Copyright (c) 2014 Aleksandr Shevchenko
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
using Xwt;
using System.Collections.Generic;
using Mono.TextEditor.Highlighting;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public static class TreeStoreExtensions
	{
		public static TreeNavigator GetGroupParentNode (this TreeStore colorStore, string groupName, IDataField<string> nameField)
		{
			var name = string.Empty;
			var navigator = colorStore.GetFirstNode ();
			if (navigator.CurrentPosition == null)
				return AddNewGroup (colorStore, groupName, nameField);

			name = navigator.GetValue (nameField);
			while (name != groupName && navigator.MoveNext ())
				name = navigator.GetValue (nameField);

			if (name != groupName)
				navigator = AddNewGroup (colorStore, groupName, nameField);

			return navigator;
		}

		static TreeNavigator AddNewGroup (TreeStore colorStore, string groupName, IDataField<string> nameField)
		{
			var navigator = colorStore.AddNode ();
			navigator.SetValue (nameField, groupName);
			return navigator;
		}

		public static TreeNavigator GetNodeFromStyleName (this TreeStore colorStore, string styleName, IDataField<ColorScheme.PropertyDecsription> propertyField)
		{
			var navigator = colorStore.GetFirstNode ();
			if (navigator == null)
				return null;

			do {
				if (!navigator.MoveToChild ())
					return null;

				do {
					ColorScheme.PropertyDecsription data;

					try {
						data = navigator.GetValue (propertyField);	
					} catch (Exception) {
						return null;
					}

					if (data != null && data.Attribute != null && data.Attribute.Name == styleName)
						return navigator;
				} while (navigator.MoveNext ());

				navigator.MoveToParent ();
			} while (navigator.MoveNext ());

			return null;
		}
	}
}

