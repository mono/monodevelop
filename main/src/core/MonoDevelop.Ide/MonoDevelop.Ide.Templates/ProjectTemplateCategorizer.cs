//
// ProjectTemplateCategorizer.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Templates
{
	public class ProjectTemplateCategorizer
	{
		List<TemplateCategory> categories;
		TemplateCategory defaultCategory;

		public ProjectTemplateCategorizer (IEnumerable<TemplateCategory> categories)
		{
			this.categories = categories.Select (category => category.Clone ()).ToList ();
			defaultCategory = GetDefaultCategory ();
		}

		TemplateCategory GetDefaultCategory ()
		{
			foreach (TemplateCategory topLevelCategory in categories) {
				foreach (TemplateCategory secondLevelCategory in topLevelCategory.Categories) {
					foreach (TemplateCategory thirdLevelCategory in secondLevelCategory.Categories) {
						if (thirdLevelCategory.IsDefault) {
							return thirdLevelCategory;
						}
					}
				}
			}

			return null;
		}

		public IEnumerable<TemplateCategory> GetCategorizedTemplates ()
		{
			return categories;
		}

		public void CategorizeTemplates (IEnumerable<SolutionTemplate> templates)
		{
			foreach (SolutionTemplate template in templates) {
				TemplateCategory category = GetCategory (template);
				if (category != null) {
					category.AddTemplate (template);
				} else {
					LogNoCategoryMatch (template);
				}
			}
		}

		TemplateCategory GetCategory (SolutionTemplate template)
		{
			TemplateCategory match = GetCategory (template, categories);

			if (match != null) {
				return match;
			} else if (defaultCategory != null) {
				LogUsingDefaultCategory (template);

				return defaultCategory;
			}
			return null;
		}

		static TemplateCategory GetCategory (SolutionTemplate template, IEnumerable<TemplateCategory> categories)
		{
			TemplateCategory match = null;
			IEnumerable<TemplateCategory> currentCategories = categories;

			var path = new TemplateCategoryPath (template.Category);
			foreach (string part in path.GetParts ()) {
				match = currentCategories.FirstOrDefault (category => category.IsMatch (part));

				if (match != null) {
					currentCategories = match.Categories;
				} else {
					return null;
				}
			}

			return match;
		}

		void LogUsingDefaultCategory (SolutionTemplate template)
		{
			string message = String.Format (
				"Invalid category using default category. Template id='{0}', category='{1}'.",
				template.Id,
				template.Category);
			LogWarning (message);
		}

		void LogNoCategoryMatch (SolutionTemplate template)
		{
			string message = String.Format (
				"Invalid category. Template id='{0}', category='{1}'.",
				template.Id,
				template.Category);
			LogWarning (message);
		}

		protected virtual void LogWarning (string message)
		{
			LoggingService.LogWarning (message);
		}
	}
}

