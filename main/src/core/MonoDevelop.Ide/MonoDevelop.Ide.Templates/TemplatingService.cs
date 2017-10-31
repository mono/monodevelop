//
// TemplatingService.cs
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
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects;
using Xwt.Drawing;

namespace MonoDevelop.Ide.Templates
{
	public class TemplatingService
	{
		List<TemplateCategory> projectTemplateCategories = new List<TemplateCategory> ();
		List<IProjectTemplatingProvider> templateProviders = new List<IProjectTemplatingProvider> ();
		List<TemplateWizard> projectTemplateWizards = new List<TemplateWizard> ();
		List<ImageCodon> projectTemplateImages = new List<ImageCodon> ();

		MicrosoftTemplateEngineItemTemplatingProvider itemTemplatingProvider;

		public TemplatingService ()
		{
			RecentTemplates = new RecentTemplates ();
			itemTemplatingProvider = new MicrosoftTemplateEngineItemTemplatingProvider ();
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/ProjectTemplateCategories", OnTemplateCategoriesChanged);
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/ProjectTemplatingProviders", OnTemplatingProvidersChanged);
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/ProjectTemplateWizards", OnProjectTemplateWizardsChanged);
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/TemplateImages", OnTemplateImagesChanged);
		}

		void OnTemplateCategoriesChanged (object sender, ExtensionNodeEventArgs args)
		{
			var codon = (TemplateCategoryCodon)args.ExtensionNode;
			if (args.Change == ExtensionChange.Add) {
				projectTemplateCategories.Add (codon.ToTopLevelTemplateCategory ());
			} else {
				projectTemplateCategories.RemoveAll (category => category.Id == codon.Id);
			}
		}

		void OnTemplatingProvidersChanged (object sender, ExtensionNodeEventArgs args)
		{
			var provider = args.ExtensionObject as IProjectTemplatingProvider;
			if (args.Change == ExtensionChange.Add) {
				templateProviders.Add (provider);
			} else {
				templateProviders.Remove (provider);
			}
		}

		void OnProjectTemplateWizardsChanged (object sender, ExtensionNodeEventArgs args)
		{
			var wizard = args.ExtensionObject as TemplateWizard;
			if (args.Change == ExtensionChange.Add) {
				projectTemplateWizards.Add (wizard);
			} else {
				projectTemplateWizards.Remove (wizard);
			}
		}

		void OnTemplateImagesChanged (object sender, ExtensionNodeEventArgs args)
		{
			var codon = args.ExtensionNode as ImageCodon;
			if (args.Change == ExtensionChange.Add) {
				projectTemplateImages.Add (codon);
			} else {
				projectTemplateImages.Remove (codon);
			}
		}

		public IEnumerable<TemplateCategory> GetProjectTemplateCategories ()
		{
			return GetProjectTemplateCategories (solutionTemplate => true);
		}

		public IEnumerable<TemplateCategory> GetProjectTemplateCategories (Predicate<SolutionTemplate> match)
		{
			var templateCategorizer = new ProjectTemplateCategorizer (projectTemplateCategories, match);
			foreach (IProjectTemplatingProvider provider in templateProviders) {
				templateCategorizer.CategorizeTemplates (provider.GetTemplates ());
			}
			return templateCategorizer.GetCategorizedTemplates ();
		}

		internal static SolutionTemplate GetTemplate (IEnumerable<TemplateCategory> categories, string templateId)
		{
			return GetTemplate (
				categories,
				template => template.Id == templateId,
				category => true,
				category => true);
		}

		internal static SolutionTemplate GetTemplate (
			IEnumerable<TemplateCategory> categories,
			Func<SolutionTemplate, bool> isTemplateMatch,
			Func<TemplateCategory, bool> isTopLevelCategoryMatch,
			Func<TemplateCategory, bool> isSecondLevelCategoryMatch)
		{
			Predicate<SolutionTemplate> predicate = (t) => isTemplateMatch (t);
			foreach (TemplateCategory topLevelCategory in categories.Where (isTopLevelCategoryMatch)) {
				foreach (TemplateCategory secondLevelCategory in topLevelCategory.Categories.Where (isSecondLevelCategoryMatch)) {
					foreach (TemplateCategory thirdLevelCategory in secondLevelCategory.Categories) {
						foreach (SolutionTemplate template in thirdLevelCategory.Templates) {
							if (isTemplateMatch (template))
								return template;
							else {
								var groupedTemplate = template.GetTemplate (predicate);
								if (groupedTemplate != null)
									return groupedTemplate;
							}
						}
					}
				}
			}
			return null;
		}

		public async Task<ProcessedTemplateResult> ProcessTemplate (SolutionTemplate template, NewProjectConfiguration config, SolutionFolder parentFolder)
		{
			IProjectTemplatingProvider provider = GetTemplatingProviderForTemplate (template);
			if (provider != null) {
				var result = await provider.ProcessTemplate (template, config, parentFolder);
				if (result.WorkspaceItems.Any ())
					RecentTemplates.AddTemplate (template);
				return result;
			}
			return null;
		}

		IProjectTemplatingProvider GetTemplatingProviderForTemplate (SolutionTemplate template)
		{
			return templateProviders.FirstOrDefault (provider => provider.CanProcessTemplate (template));
		}

		public TemplateWizard GetWizard (string id)
		{
			return projectTemplateWizards.FirstOrDefault (wizard => wizard.Id == id);
		}

		public Image LoadTemplateImage (string imageId)
		{
			ImageCodon imageCodon = projectTemplateImages.FirstOrDefault (codon => codon.Id == imageId);

			if (imageCodon != null) {
				return imageCodon.Addin.GetImageResource (imageCodon.Resource);
			}
			return null;
		}

		public IEnumerable<ItemTemplate> GetItemTemplates ()
		{
			return itemTemplatingProvider.GetTemplates ();
		}

		public IEnumerable<ItemTemplate> GetItemTemplates (Predicate<ItemTemplate> match)
		{
			return GetItemTemplates ().Where (template => match (template));
		}

		public Task ProcessTemplate (ItemTemplate template, Project project, NewItemConfiguration config)
		{
			return itemTemplatingProvider.ProcessTemplate (template, project, config);
		}

		public RecentTemplates RecentTemplates { get; private set; }
	}

	public class RecentTemplates
	{
		RecentFileStorage recentTemplates;

		const string templateUriScheme = "monodevelop+template://";
		const string templateGroup = "MonoDevelop Templates";

		const int ItemLimit = 25;

		public RecentTemplates () : this (UserProfile.Current.LocalConfigDir.Combine ("RecentlyUsedTemplates.xml"))
		{
		}

		public RecentTemplates (string storageFile)
		{
			recentTemplates = new RecentFileStorage (storageFile);
		}

		public event EventHandler Changed {
			add { recentTemplates.RecentFilesChanged += value; }
			remove { recentTemplates.RecentFilesChanged -= value; }
		}

		public IList<SolutionTemplate> GetTemplates ()
		{
			var categories = IdeApp.Services.TemplatingService.GetProjectTemplateCategories ();
			return GetTemplates (categories);
		}

		internal IList<SolutionTemplate> GetTemplates (IEnumerable<TemplateCategory> categories)
		{
			try {
				var gp = recentTemplates.GetItemsInGroup (templateGroup);
				return gp.Select (item => FromRecentItem (categories, item)).Where (t => t != null).ToList ();
			} catch (Exception e) {
				LoggingService.LogError ("Can't get recent templates list.", e);
				return new List<SolutionTemplate> ();
			}
		}

		public void Clear ()
		{
			try {
				recentTemplates.ClearGroup (templateGroup);
			} catch (Exception e) {
				LoggingService.LogError ("Can't clear recent templates list.", e);
			}
		}

		public void AddTemplate (SolutionTemplate template)
		{
			try {
				if (template.HasGroupId)
					RemoveTemplateFromSameGroup (template);
				var recentItem = CreateRecentItem (template);
				recentTemplates.AddWithLimit (recentItem, templateGroup, ItemLimit);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to add item to recent templates list.", e);
			}
		}

		/// <summary>
		/// Removes any recent templates from the same group if it has the same language.
		/// Different languages for the same group can exist separately in the recent project
		/// templates list.
		/// </summary>
		void RemoveTemplateFromSameGroup (SolutionTemplate template)
		{
			foreach (var groupTemplate in template.GetGroupedTemplates ()) {
				if (groupTemplate.Language == template.Language) {
					var recentItem = CreateRecentItem (groupTemplate);
					recentTemplates.RemoveItem (recentItem);
				}
			}
		}

		RecentItem CreateRecentItem (SolutionTemplate template)
		{
			var mime = "application/vnd.monodevelop.template";
			var uri = templateUriScheme ;
			var categoryPath = template.Category;
			if (!string.IsNullOrEmpty (categoryPath))
				uri += categoryPath + "/";
			uri += template.Id;
			return new RecentItem (uri, mime, templateGroup) { Private = template.Name };
		}

		SolutionTemplate FromRecentItem (IEnumerable<TemplateCategory> categories, RecentItem item)
		{
			var templatePath = item.Uri.StartsWith (templateUriScheme, StringComparison.Ordinal) ? item.Uri.Substring (templateUriScheme.Length) : item.Uri;
			var parts = templatePath.Split ('/');
			var templateId = parts [parts.Length - 1];
			SolutionTemplate recentTemplate = null;

			if (parts.Length > 1)
				recentTemplate = TemplatingService.GetTemplate (
					categories,
					(template) => template.Id == templateId,
					(category) => parts.Length > 1 ? category.Id == parts[0] : true,
					(category) => parts.Length > 2 ? category.Id == parts[1] : true
				);

			// fallback to global template lookup if no category matched
			// in this case the category is not guaranteed if a template is listed in more than one category
			if (recentTemplate == null)
				recentTemplate = TemplatingService.GetTemplate (categories, templateId);
			return recentTemplate;
		}

		public void Dispose ()
		{
			recentTemplates.Dispose ();
			recentTemplates = null;
		}
	}
}

