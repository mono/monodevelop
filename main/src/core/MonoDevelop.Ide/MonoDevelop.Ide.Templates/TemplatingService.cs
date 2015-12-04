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
using Mono.Addins;
using MonoDevelop.Components;
using MonoDevelop.Ide.Codons;
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

		public TemplatingService ()
		{
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

		public ProcessedTemplateResult ProcessTemplate (SolutionTemplate template, NewProjectConfiguration config, SolutionFolder parentFolder)
		{
			IProjectTemplatingProvider provider = GetTemplatingProviderForTemplate (template);
			if (provider != null) {
				return provider.ProcessTemplate (template, config, parentFolder);
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
	}
}

