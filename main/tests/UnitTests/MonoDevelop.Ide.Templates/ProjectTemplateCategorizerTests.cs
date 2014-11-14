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
using MonoDevelop.Projects;

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

		ProjectCreateParameters CreateParameters (string name, string value)
		{
			var parameters = new ProjectCreateParameters ();
			parameters [name] = value;
			return parameters;
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

		[Test]
		public void GetCategorizedTemplates_TwoConsoleProjectTemplatesWithDifferentLanguagesInSameCategory_TemplatesCombinedIntoOneGroup ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template1 = AddTemplate ("template-id1", "android/app/general");
			template1.GroupId = "console";
			template1.Language = "C#";
			SolutionTemplate template2 = AddTemplate ("template-id2", "android/app/general");
			template2.GroupId = "console";
			template2.Language = "F#";

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate template = generalCategory.Templates.FirstOrDefault ();
			Assert.That (template.AvailableLanguages, Contains.Item ("C#"));
			Assert.That (template.AvailableLanguages, Contains.Item ("F#"));
			Assert.AreEqual (template1, template.GetTemplate ("C#"));
			Assert.AreEqual (template2, template.GetTemplate ("F#"));
			Assert.AreEqual (1, generalCategory.Templates.Count ());
		}

		[Test]
		public void GetCategorizedTemplates_TwoConsoleProjectTemplatesWithSameLanguageInSameGroup_TemplatesCombinedIntoOneGroupWarningLoggedAboutDuplicateLanguageOneLanguageShown ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template1 = AddTemplate ("template-id1", "android/app/general");
			template1.GroupId = "console";
			template1.Language = "C#";
			SolutionTemplate template2 = AddTemplate ("template-id2", "android/app/general");
			template2.GroupId = "console";
			template2.Language = "C#";

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate template = generalCategory.Templates.FirstOrDefault ();
			Assert.That (template.AvailableLanguages, Contains.Item ("C#"));
			Assert.AreEqual (1, template.AvailableLanguages.Count);
			Assert.AreEqual (1, generalCategory.Templates.Count ());
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateOneCategoryWithGroupCondition_TemplateFilteredUsingTemplateParameters ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "android/app/general");
			template.GroupId = "console";
			template.Language = "C#";
			template.Condition = "Device=MyDevice";
			ProjectCreateParameters parameters = CreateParameters ("Device", "MyDevice");

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			SolutionTemplate matchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			parameters.Clear ();
			SolutionTemplate noMatchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.AreEqual (template, matchedTemplate);
			Assert.IsNull (noMatchedTemplate);
		}

		[Test]
		public void GetCategorizedTemplates_TwoTemplatesWithGroupCondition_TemplateFilteredUsingTemplateParameters ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template1 = AddTemplate ("template-id1", "android/app/general");
			template1.GroupId = "console";
			template1.Language = "C#";
			template1.Condition = "Device=IPhone";
			SolutionTemplate template2 = AddTemplate ("template-id2", "android/app/general");
			template2.GroupId = "console";
			template2.Language = "C#";
			template2.Condition = "Device=IPad";
			ProjectCreateParameters parameters = CreateParameters ("Device", "IPad");

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			SolutionTemplate matchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			parameters.Clear ();
			SolutionTemplate noMatchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.AreEqual (template2, matchedTemplate);
			Assert.IsNull (noMatchedTemplate);
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateOneCategoryWithGroupConditionContainingExtraWhitespace_TemplateFilteredUsingTemplateParameters ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "android/app/general");
			template.GroupId = "console";
			template.Language = "C#";
			template.Condition = " Device = MyDevice ";
			ProjectCreateParameters parameters = CreateParameters ("Device", "MyDevice");

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			SolutionTemplate matchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.AreEqual (template, matchedTemplate);
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateOneCategoryWithGroupConditionAndParameterIsBoolean_TemplateFilteredUsingTemplateParameters ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "android/app/general");
			template.GroupId = "console";
			template.Language = "C#";
			template.Condition = "SupportsSizeClasses=True";
			ProjectCreateParameters parameters = CreateParameters ("SupportsSizeClasses", true.ToString ());

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			SolutionTemplate matchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.AreEqual (template, matchedTemplate);
			parameters.Clear ();
			SolutionTemplate noMatchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.IsNull (noMatchedTemplate);
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateOneCategoryWithGroupConditionAndParameterNameAndValueHaveDifferentCase_TemplateFilteredUsingTemplateParametersIgnoringCase ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "android/app/general");
			template.GroupId = "console";
			template.Language = "C#";
			template.Condition = "Device=MyDevice";
			ProjectCreateParameters parameters = CreateParameters ("device", "mydevice");

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			SolutionTemplate matchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.AreEqual (template, matchedTemplate);
			parameters = CreateParameters ("device", "no-match");
			SolutionTemplate noMatchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.IsNull (noMatchedTemplate);
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateOneCategoryWithGroupConditionHavingMultipleParameterConditions_TemplateFilteredUsingAllParametersInCondition ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "android/app/general");
			template.GroupId = "console";
			template.Language = "C#";
			template.Condition = "Device=MyDevice;SupportsSizeClasses=true";
			ProjectCreateParameters parameters = CreateParameters ("Device", "MyDevice");
			parameters ["SupportsSizeClasses"] = true.ToString ();

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			SolutionTemplate matchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.AreEqual (template, matchedTemplate);
			parameters = CreateParameters ("Device", "MyDevice");
			parameters ["SupportsSizeClasses"] = false.ToString ();
			SolutionTemplate noMatchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.IsNull (noMatchedTemplate);
			parameters = CreateParameters ("Device", "UnknownDevice");
			parameters ["SupportsSizeClasses"] = true.ToString ();
			noMatchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.IsNull (noMatchedTemplate);
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateOneCategoryWithGroupConditionHavingMultipleParameterConditionsSeparatedByComma_TemplateFilteredUsingAllParametersInCondition ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "android/app/general");
			template.GroupId = "console";
			template.Language = "C#";
			template.Condition = "Device=MyDevice,SupportsSizeClasses=true";
			ProjectCreateParameters parameters = CreateParameters ("Device", "MyDevice");
			parameters ["SupportsSizeClasses"] = true.ToString ();

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			SolutionTemplate matchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.AreEqual (template, matchedTemplate);
			parameters = CreateParameters ("Device", "MyDevice");
			parameters ["SupportsSizeClasses"] = false.ToString ();
			SolutionTemplate noMatchedTemplate = firstTemplate.GetTemplate ("C#", parameters);
			Assert.IsNull (noMatchedTemplate);
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateTwoDifferentThirdLevelCategories_EmptyThirdLevelCategoryIsRemoved ()
		{
			TemplateCategory topLevelCategory = AddTemplateCategory ("android");
			TemplateCategory secondLevelCategory = AddTemplateCategory ("app", topLevelCategory);
			AddTemplateCategory ("general", secondLevelCategory);
			AddTemplateCategory ("tests", secondLevelCategory);
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "android/app/general");

			CategorizeTemplates ();

			TemplateCategory appCategory = categorizedTemplates.First ().Categories.First ();
			TemplateCategory generalCategory = appCategory.Categories.First ();
			Assert.That (generalCategory.Templates.ToList (), Contains.Item (template));
			Assert.AreEqual (1, categorizedTemplates.Count);
			Assert.AreEqual (1, appCategory.Categories.Count ());
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateTwoDifferentSecondLevelCategories_EmptySecondLevelCategoryIsRemoved ()
		{
			TemplateCategory topLevelCategory = AddTemplateCategory ("android");
			TemplateCategory secondLevelCategory = AddTemplateCategory ("app", topLevelCategory);
			AddTemplateCategory ("general", secondLevelCategory);
			secondLevelCategory = AddTemplateCategory ("tests", topLevelCategory);
			AddTemplateCategory ("general", secondLevelCategory);
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "android/app/general");

			CategorizeTemplates ();

			TemplateCategory androidCategory = categorizedTemplates.First ();
			TemplateCategory appCategory = androidCategory.Categories.First ();
			TemplateCategory generalCategory = appCategory.Categories.First ();
			Assert.That (generalCategory.Templates.ToList (), Contains.Item (template));
			Assert.AreEqual (1, categorizedTemplates.Count);
			Assert.AreEqual (1, androidCategory.Categories.Count ());
		}

		[Test]
		public void GetCategorizedTemplates_OneTemplateTwoTopLevelCategories_EmptyTopLevelCategoryIsRemoved ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategories ("ios", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "android/app/general");

			CategorizeTemplates ();

			TemplateCategory appCategory = categorizedTemplates.First ().Categories.First ();
			TemplateCategory generalCategory = appCategory.Categories.First ();
			Assert.That (generalCategory.Templates.ToList (), Contains.Item (template));
			Assert.AreEqual (1, categorizedTemplates.Count);
			Assert.AreEqual (1, appCategory.Categories.Count ());
		}
	}
}

