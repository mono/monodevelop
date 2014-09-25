//
// ProjectTemplateCategorizerTests.cs
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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MonoDevelop.Ide.Templates
{
	[TestFixture]
	public class ProjectTemplateCategorizerTests
	{
		TestableProjectTemplateCategorizer categorizer;
		List<TemplateCategory> categories;
		List<SolutionTemplate> templates;
		List<TemplateCategory> categorizedTemplates;

		[SetUp]
		public void Init ()
		{
			categories = new List<TemplateCategory> ();
			templates = new List<SolutionTemplate> ();
		}

		void CreateCategories (string topLevelCategoryName, string secondLevelCategoryName, string thirdLevelCategoryName)
		{
			TemplateCategory topLevelCategory = AddTemplateCategory (topLevelCategoryName);
			TemplateCategory secondLevelCategory = AddTemplateCategory (secondLevelCategoryName, topLevelCategory);
			AddTemplateCategory (thirdLevelCategoryName, secondLevelCategory);
		}

		void CreateCategorizer ()
		{
			categorizer = new TestableProjectTemplateCategorizer (categories);
		}

		TemplateCategory AddTemplateCategory (string categoryName)
		{
			var templateCategory = new TemplateCategory (categoryName, categoryName, "iconid");
			categories.Add (templateCategory);
			return templateCategory;
		}

		TemplateCategory AddTemplateCategory (string categoryName, TemplateCategory parent)
		{
			var templateCategory = new TemplateCategory (categoryName, categoryName, "iconid");
			parent.AddCategory (templateCategory);
			return templateCategory;
		}

		SolutionTemplate AddTemplate (string id, string category)
		{
			var template = new SolutionTemplate (id, id, "icon-id") {
				Category = category
			};
			templates.Add (template);
			return template;
		}

		void CategorizeTemplates ()
		{
			categorizer.CategorizeTemplates (templates);
			categorizedTemplates = categorizer.GetCategorizedTemplates ().ToList ();
		}

		void AssertWarningLogged (string expectedMessage)
		{
			Assert.That (categorizer.WarningsLogged, Contains.Item (expectedMessage));
		}

		void CreateCategoriesWithDefaultCategory (string topLevelCategoryName, string secondLevelCategoryName, string thirdLevelCategoryName)
		{
			CreateCategories (topLevelCategoryName, secondLevelCategoryName, thirdLevelCategoryName);
			categories.First ()
				.Categories
				.First ()
				.Categories
				.First ()
				.IsDefault = true;
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateOneCategoryNoCategoryMatch_LogsWarning ()
		{
			CreateCategories ("Android", "App", "General");
			CreateCategorizer ();
			AddTemplate ("template-id", "unknown-category");

			CategorizeTemplates ();

			AssertWarningLogged ("Invalid category. Template id='template-id', category='unknown-category'.");
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateOneCategoryWithDefaultNoCategoryMatch_LogsWarning ()
		{
			CreateCategoriesWithDefaultCategory ("Android", "App", "General");
			CreateCategorizer ();
			AddTemplate ("template-id", "unknown-category");

			CategorizeTemplates ();

			AssertWarningLogged ("Invalid category using default category. Template id='template-id', category='unknown-category'.");
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateOneCategoryWithDefaultNoCategoryMatch_TemplateAddedToDefaultCategory ()
		{
			CreateCategoriesWithDefaultCategory ("Android", "App", "General");
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "unknown-category");

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			Assert.That (generalCategory.Templates.ToList (), Contains.Item (template));
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateOneCategory_TemplateAddedToMatchingCategory ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "android/app/general");

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			Assert.That (generalCategory.Templates.ToList (), Contains.Item (template));
		}
	}
}

