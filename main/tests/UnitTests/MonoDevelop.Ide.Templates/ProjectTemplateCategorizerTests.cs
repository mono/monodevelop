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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.Templates
{
	[TestFixture]
	public class ProjectTemplateCategorizerTests
	{
		TestableProjectTemplateCategorizer categorizer;
		List<TemplateCategory> categories;
		List<SolutionTemplate> templates;
		List<TemplateCategory> categorizedTemplates;
		string recentlyUsedTemplatesFile;

		[SetUp]
		public void Init ()
		{
			categories = new List<TemplateCategory> ();
			templates = new List<SolutionTemplate> ();
		}

		[TearDown]
		public void TearDown ()
		{
			if (File.Exists (recentlyUsedTemplatesFile))
				Util.ClearTmpDir ();
		}

		static string GetRecentlyUsedTemplatesFileName (string name)
		{
			return Path.Combine (Util.TmpDir, name);
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

		void CreateCategorizer (Predicate<SolutionTemplate> templateFilter)
		{
			categorizer = new TestableProjectTemplateCategorizer (categories, templateFilter);
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

		/// <summary>
		/// The RecentTemplates class uses the RecentFileStorage class. In the
		/// RecentFileStorage class constructor a task is started to create a file
		/// and may finish whilst the unit test is adding a new template. This task
		/// delay can cause the cached in-memory list in the RecentFileStorage to be
		/// cleared. This causes the test to fail since it will run before the
		/// in-memory list can be updated when the RecentFileStorage class saves the
		/// file to disk. This saving to file is delayed by a second. So we retry
		/// getting the templates from the RecentTemplates until a timeout occurs.
		/// Most of the time the RecentTemplates.GetTemplates method works first
		/// time and returns the expected templates.
		/// </summary>
		static async Task<IList<SolutionTemplate>> GetRecentTemplates (
			RecentTemplates recentTemplates,
			List<TemplateCategory> categorizedTemplates,
			int expectedTemplateCount)
		{
			const int MAX_WAIT_TIME = 5000;
			const int RETRY_WAIT = 100;

			int remainingTries = MAX_WAIT_TIME / RETRY_WAIT;
			while (remainingTries > 0) {
				var recentTemplatesList = recentTemplates.GetTemplates (categorizedTemplates);
				if (recentTemplatesList.Count == expectedTemplateCount)
					return recentTemplatesList;

				// Wait for recent file storage to be updated.
				await Task.Delay (RETRY_WAIT);
				remainingTries--;
			}
			return new List<SolutionTemplate> ();
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

		[Test]
		public void GetCategorizedTemplates_TemplateUsesGoogleGlassLegacyCategory_TemplateIsMappedToNewCategories ()
		{
			TemplateCategory topLevelCategory = AddTemplateCategory ("android");
			TemplateCategory secondLevelCategory = AddTemplateCategory ("app", topLevelCategory);
			TemplateCategory category = AddTemplateCategory ("general", secondLevelCategory);
			category.MappedCategories = "C#/Glass";
			CreateCategorizer ();
			SolutionTemplate template = AddTemplate ("template-id", "C#/Glass");

			CategorizeTemplates ();

			TemplateCategory appCategory = categorizedTemplates.First ().Categories.First ();
			TemplateCategory generalCategory = appCategory.Categories.First ();
			Assert.That (generalCategory.Templates.ToList (), Contains.Item (template));
		}

		[Test]
		public void GetCategorizedTemplates_TwoLegacyCategoriesMappedToNewCategory_TemplatesAreMappedToNewCategories ()
		{
			TemplateCategory topLevelCategory = AddTemplateCategory ("android");
			TemplateCategory secondLevelCategory = AddTemplateCategory ("app", topLevelCategory);
			TemplateCategory category = AddTemplateCategory ("general", secondLevelCategory);
			category.MappedCategories = "C#/Android;VBNet/Android";
			CreateCategorizer ();
			SolutionTemplate csharpTemplate = AddTemplate ("template-id", "C#/Android");
			SolutionTemplate vbnetTemplate = AddTemplate ("template-id2", "VBNet/Android");

			CategorizeTemplates ();

			TemplateCategory appCategory = categorizedTemplates.First ().Categories.First ();
			TemplateCategory generalCategory = appCategory.Categories.First ();
			Assert.That (generalCategory.Templates.ToList (), Contains.Item (csharpTemplate));
			Assert.That (generalCategory.Templates.ToList (), Contains.Item (vbnetTemplate));
		}

		[Test]
		public void GetCategorizedTemplates_TwoTemplatesAndFilterShouldRemoveOneTemplate_TemplatesFiltered ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer (solutionTemplate => {
				return solutionTemplate.Id == "template-id2";
			});
			SolutionTemplate template1 = AddTemplate ("template-id1", "android/app/general");
			template1.Language = "C#";
			SolutionTemplate template2 = AddTemplate ("template-id2", "android/app/general");
			template2.Language = "C#";

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			Assert.AreEqual (1, generalCategory.Templates.Count ());
			Assert.AreEqual ("template-id2", firstTemplate.Id);
		}

		[Test]
		public void GetCategorizedTemplates_TwoTemplatesAndFilterByNewProject_TemplatesFiltered ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer (ProjectTemplateCategorizer.MatchNewProjectTemplates);
			SolutionTemplate template1 = AddTemplate ("template-id1", "android/app/general");
			template1.Language = "C#";
			template1.Visibility = SolutionTemplateVisibility.NewProject;
			SolutionTemplate template2 = AddTemplate ("template-id2", "android/app/general");
			template2.Language = "C#";
			template2.Visibility = SolutionTemplateVisibility.NewSolution;

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			Assert.AreEqual (1, generalCategory.Templates.Count ());
			Assert.AreEqual ("template-id1", firstTemplate.Id);
		}

		[Test]
		public void GetCategorizedTemplates_TwoTemplatesAndFilterByNewSolution_TemplatesFiltered ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer (ProjectTemplateCategorizer.MatchNewSolutionTemplates);
			SolutionTemplate template1 = AddTemplate ("template-id1", "android/app/general");
			template1.Language = "C#";
			template1.Visibility = SolutionTemplateVisibility.NewProject;
			SolutionTemplate template2 = AddTemplate ("template-id2", "android/app/general");
			template2.Language = "C#";
			template2.Visibility = SolutionTemplateVisibility.All;

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			Assert.AreEqual (1, generalCategory.Templates.Count ());
			Assert.AreEqual ("template-id2", firstTemplate.Id);
		}

		[Test]
		public void GetCategorizedTemplates_TwoTemplateProvidersEachWithOneTemplateForTwoDifferentThirdLevelCategories_ThirdLevelCategoriresAreNotRemoved ()
		{
			TemplateCategory topLevelCategory = AddTemplateCategory ("android");
			TemplateCategory secondLevelCategory = AddTemplateCategory ("app", topLevelCategory);
			AddTemplateCategory ("general", secondLevelCategory);
			AddTemplateCategory ("tests", secondLevelCategory);
			CreateCategorizer ();
			SolutionTemplate firstTemplateProviderTemplate = AddTemplate ("first-provider-template-id", "android/app/general");
			categorizer.CategorizeTemplates (templates);
			templates.Clear ();
			SolutionTemplate secondTemplateProviderTemplate = AddTemplate ("second-provider-template-id", "android/app/tests");
			CategorizeTemplates ();

			TemplateCategory appCategory = categorizedTemplates.First ().Categories.First ();
			TemplateCategory generalCategory = appCategory.Categories.First (category => category.Id == "general");
			TemplateCategory testsCategory = appCategory.Categories.First (category => category.Id == "tests");
			Assert.That (generalCategory.Templates.ToList (), Contains.Item (firstTemplateProviderTemplate));
			Assert.That (testsCategory.Templates.ToList (), Contains.Item (secondTemplateProviderTemplate));
			Assert.AreEqual (2, appCategory.Categories.Count ());
		}

		/// <summary>
		/// Tests that the SolutionTemplate's GroupedTemplates are cleared when the template is
		/// categorized again. This allows the templating provider to cache the templates and
		/// prevents the grouped template list from increasing every time the new project dialog
		/// is opened.
		/// </summary>
		[Test]
		public void GetCategorizedTemplates_CategorizeTwoGroupedConsoleProjectTemplatesMultipleTimes_GroupedTemplatesDoesNotGrow ()
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

			// Categorize the templates again after re-creating the categorizer but not
			// recreating the templates.
			CreateCategorizer ();
			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate template = generalCategory.Templates.FirstOrDefault ();
			int templateCount = 0;
			template.GetTemplate (t => {
				templateCount++;
				return false;
			});
			Assert.AreEqual (2, templateCount);
		}

		[Test]
		public void GetCategorizedTemplates_TwoTemplatesWithGroupCondition_CanGetTemplateMatchingConditionFromAnyGroupedTemplate ()
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
			ProjectCreateParameters ipadParameters = CreateParameters ("Device", "IPad");
			ProjectCreateParameters iphoneParameters = CreateParameters ("Device", "IPhone");

			CategorizeTemplates ();

			TemplateCategory generalCategory = categorizedTemplates.First ().Categories.First ().Categories.First ();
			SolutionTemplate firstTemplate = generalCategory.Templates.FirstOrDefault ();
			SolutionTemplate matchedIPadTemplate = firstTemplate.GetTemplate ("C#", ipadParameters);
			SolutionTemplate matchedIPhoneTemplate = firstTemplate.GetTemplate ("C#", iphoneParameters);

			Assert.AreEqual (template2, matchedIPadTemplate);
			Assert.AreEqual (template1, matchedIPhoneTemplate);
			Assert.AreEqual (template2, matchedIPadTemplate.GetTemplate ("C#", ipadParameters));
			Assert.AreEqual (template1, matchedIPadTemplate.GetTemplate ("C#", iphoneParameters));
		}

		[Test]
		public async Task RecentTemplates_TwoTemplatesInGroupConditionSameLanguage_TreatedAsSameRecentTemplate ()
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
			recentlyUsedTemplatesFile = GetRecentlyUsedTemplatesFileName ("TwoTemplatesInGroupSameLanguage.xml");
			var recentTemplates = new RecentTemplates (recentlyUsedTemplatesFile);
			var initialRecentTemplatesList = recentTemplates.GetTemplates (categorizedTemplates);

			recentTemplates.AddTemplate (template1);
			recentTemplates.AddTemplate (template2);

			var recentTemplatesList = await GetRecentTemplates (recentTemplates, categorizedTemplates, 1);

			Assert.AreEqual (0, initialRecentTemplatesList.Count);
			Assert.AreEqual (1, recentTemplatesList.Count);
			Assert.AreEqual (template2, recentTemplatesList[0]);
		}

		[Test]
		public async Task RecentTemplates_TwoTemplatesInGroupDifferentLanguage_TreatedAsDifferentRecentTemplate ()
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
			recentlyUsedTemplatesFile = GetRecentlyUsedTemplatesFileName ("TwoTemplatesInGroupDifferentLanguage.xml");
			var recentTemplates = new RecentTemplates (recentlyUsedTemplatesFile);
			var initialRecentTemplatesList = recentTemplates.GetTemplates (categorizedTemplates);

			recentTemplates.AddTemplate (template1);
			recentTemplates.AddTemplate (template2);

			var recentTemplatesList = await GetRecentTemplates (recentTemplates, categorizedTemplates, 2);

			Assert.AreEqual (0, initialRecentTemplatesList.Count);
			Assert.AreEqual (2, recentTemplatesList.Count);
			Assert.AreEqual (template2, recentTemplatesList[0]);
			Assert.AreEqual (template1, recentTemplatesList[1]);
		}

		[Test]
		public void GetGroupedTemplates_TwoGroupedConsoleProjectTemplates_CanGetOtherTemplatesInGroupFromEitherTemplate ()
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

			var templatesInGroupForTemplate1 = template1.GetGroupedTemplates ().Single ();
			var templatesInGroupForTemplate2 = template2.GetGroupedTemplates ().Single ();

			Assert.AreEqual (template2, templatesInGroupForTemplate1);
			Assert.AreEqual (template1, templatesInGroupForTemplate2);
		}

		[Test]
		public void GetGroupedTemplates_ThreeGroupedConsoleProjectTemplates_CanGetOtherTemplatesInGroupFromEitherTemplate ()
		{
			CreateCategories ("android", "app", "general");
			CreateCategorizer ();
			SolutionTemplate template1 = AddTemplate ("template-id1", "android/app/general");
			template1.GroupId = "console";
			template1.Language = "C#";
			SolutionTemplate template2 = AddTemplate ("template-id2", "android/app/general");
			template2.GroupId = "console";
			template2.Language = "F#";
			SolutionTemplate template3 = AddTemplate ("template-id3", "android/app/general");
			template3.GroupId = "console";
			template3.Language = "VBNet";
			CategorizeTemplates ();

			var templatesInGroupForTemplate1 = template1.GetGroupedTemplates ().ToList ();
			var templatesInGroupForTemplate2 = template2.GetGroupedTemplates ().ToList ();
			var templatesInGroupForTemplate3 = template3.GetGroupedTemplates ().ToList ();

			Assert.That (templatesInGroupForTemplate1, Contains.Item (template2));
			Assert.That (templatesInGroupForTemplate1, Contains.Item (template3));
			Assert.That (templatesInGroupForTemplate2, Contains.Item (template1));
			Assert.That (templatesInGroupForTemplate2, Contains.Item (template3));
			Assert.That (templatesInGroupForTemplate3, Contains.Item (template1));
			Assert.That (templatesInGroupForTemplate3, Contains.Item (template2));
			Assert.AreEqual (2, templatesInGroupForTemplate1.Count);
			Assert.AreEqual (2, templatesInGroupForTemplate2.Count);
			Assert.AreEqual (2, templatesInGroupForTemplate3.Count);
		}
	}
}

