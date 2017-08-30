//
// TemplateCategoryCodon.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using Mono.Addins;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Core;
using System;
using System.Linq;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode (Description="A template category.")]
	internal class TemplateCategoryCodon : ExtensionNode
	{
		// OBSOLETE: This member is ignored when generating translations.
		[NodeAttribute ("name", "Name of the category.", Localizable=true)]
		string name;

		[NodeAttribute ("_name", "Name of the category.", Localizable=true)]
		string _name;

		[NodeAttribute("icon", "Icon for the category.")]
		string icon;

		[NodeAttribute ("default", "Category is the default for templates.")]
		string isDefault;

		[NodeAttribute ("mappedCategories", "Legacy categories that will be used for mapping templates.")]
		string mappedCategories;

		public TemplateCategory ToTemplateCategory ()
		{
			var category = new TemplateCategory (Id, _name ?? name, icon);
			category.MappedCategories = mappedCategories;
			category.IsDefault = IsDefaultCategory ();

			AddChildren (category);
			return category;
		}

		public TemplateCategory ToTopLevelTemplateCategory ()
		{
			TemplateCategory category = ToTemplateCategory ();
			category.IsTopLevel = true;
			return category;
		}

		bool IsDefaultCategory ()
		{
			bool result = false;
			if (bool.TryParse (isDefault, out result)) {
				return result;
			}
			return false;
		}

		void AddChildren (TemplateCategory category)
		{
			foreach (var childCodon in ChildNodes.OfType<TemplateCategoryCodon> ()) {
				category.AddCategory (childCodon.ToTemplateCategory ());
			}
		}
	}
}

