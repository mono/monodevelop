//
// FileTemplateTests.cs
//
// Author:
//       Vincent Dondain <vincent.dondain@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc
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
using NUnit.Framework;
using System.Xml;

namespace MonoDevelop.Ide.Templates
{
	[TestFixture]
	public class FileTemplateTests
	{
		const string NonEmptyCategoryMessage = @"The ""Categories"" dictionary shouldn't include any category that doesn't match a project type defined in the ""ProjectType"" list.";

		FileTemplate fileTemplate;

		[SetUp]
		public void Init ()
		{
			fileTemplate = null;
		}

		public void InitializeTest (string template)
		{
			var document = new XmlDocument ();
			document.LoadXml (template);
			fileTemplate = FileTemplate.LoadFileTemplate (null, document);
		}

		[Test]
		public void LoadFileTemplate_Simple ()
		{
			InitializeTest (FileTemplateSnippets.Template_Simple);

			// TemplateConfiguration
			Assert.AreEqual ("TemplateName", fileTemplate.Name);
			Assert.AreEqual ("iOS", fileTemplate.Categories [FileTemplate.DefaultCategoryKey]);
			Assert.AreEqual ("C#", fileTemplate.LanguageName);
			Assert.AreEqual ("XamarinIOS", fileTemplate.ProjectTypes [0]);
			Assert.AreEqual ("Description", fileTemplate.Description);
		}

		[Test]
		public void LoadFileTemplate_MultiProjectType ()
		{
			InitializeTest (FileTemplateSnippets.Template_Multi_Project_Type);

			// TemplateConfiguration
			Assert.AreEqual ("TemplateName", fileTemplate.Name);
			Assert.AreEqual ("iOS", fileTemplate.Categories [FileTemplate.DefaultCategoryKey]);
			Assert.AreEqual ("C#", fileTemplate.LanguageName);
			Assert.AreEqual ("XamarinIOS", fileTemplate.ProjectTypes [0]);
			Assert.AreEqual ("WatchOS", fileTemplate.ProjectTypes [1]);
			Assert.AreEqual ("TVOS", fileTemplate.ProjectTypes [2]);
			Assert.AreEqual ("Description", fileTemplate.Description);
		}

		[Test]
		public void LoadFileTemplate_MultiCategories_OneMatch ()
		{
			InitializeTest (FileTemplateSnippets.Template_Multi_Categories_One_Match);

			Assert.AreEqual ("watchOS", fileTemplate.Categories ["WatchOS"]);
			Assert.IsFalse (fileTemplate.Categories.ContainsKey ("XamarinIOS"), NonEmptyCategoryMessage);
			Assert.IsFalse (fileTemplate.Categories.ContainsKey ("TVOS"), NonEmptyCategoryMessage);
			Assert.AreEqual ("WatchOS", fileTemplate.ProjectTypes [0]);
		}

		[Test]
		public void LoadFileTemplate_MultiCategories_FullMatch ()
		{
			InitializeTest (FileTemplateSnippets.Template_Multi_Categories_Full_Match);

			Assert.AreEqual ("iOS", fileTemplate.Categories ["XamarinIOS"]);
			Assert.AreEqual ("watchOS", fileTemplate.Categories ["WatchOS"]);
			Assert.AreEqual ("tvOS", fileTemplate.Categories ["TVOS"]);
			Assert.AreEqual ("XamarinIOS", fileTemplate.ProjectTypes [0]);
			Assert.AreEqual ("WatchOS", fileTemplate.ProjectTypes [1]);
			Assert.AreEqual ("TVOS", fileTemplate.ProjectTypes [2]);
		}

		[Test]
		public void LoadFileTemplate_MultiCategories_DefaultFirst ()
		{
			LoadFileTemplate_MultiCategories_DefaultFirst_Core (FileTemplateSnippets.Template_Multi_Categories_Default_First);
		}

		[Test]
		public void LoadFileTemplate_MultiCategories_DefaultFirst2 ()
		{
			LoadFileTemplate_MultiCategories_DefaultFirst_Core (FileTemplateSnippets.Template_Multi_Categories_Default_First_2);
		}

		public void LoadFileTemplate_MultiCategories_DefaultFirst_Core (string template)
		{
			InitializeTest (template);

			Assert.AreEqual ("iOS", fileTemplate.Categories [FileTemplate.DefaultCategoryKey]);
			Assert.IsFalse (fileTemplate.Categories.ContainsKey ("XamarinIOS"), NonEmptyCategoryMessage);
			Assert.IsFalse (fileTemplate.Categories.ContainsKey ("WatchOS"), NonEmptyCategoryMessage);
			Assert.IsFalse (fileTemplate.Categories.ContainsKey ("TVOS"), NonEmptyCategoryMessage);
			Assert.AreEqual ("XamarinIOS", fileTemplate.ProjectTypes [0]);
		}
	}
}

