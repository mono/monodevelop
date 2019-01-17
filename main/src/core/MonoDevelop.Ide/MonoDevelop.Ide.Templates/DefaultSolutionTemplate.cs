//
// DefaultSolutionTemplate.cs
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

namespace MonoDevelop.Ide.Templates
{
	internal class DefaultSolutionTemplate : SolutionTemplate
	{
		readonly ProjectTemplate template;

		internal DefaultSolutionTemplate (ProjectTemplate template)
			: base (template.Id, template.Name, template.Icon.ToString ())
		{
			this.template = template;

			Description = template.Description;
			Category = template.Category;
			Language = template.LanguageName;
			GroupId = template.GroupId;
			Condition = template.Condition;
			ProjectFileExtension = template.FileExtension;
			Wizard = template.WizardPath;
			SupportedParameters = template.SupportedParameters;
			DefaultParameters = template.DefaultParameters;
			ImageId = template.ImageId;
			ImageFile = template.ImageFile;
			Visibility = GetVisibility (template.Visibility);

			HasProjects = (template.SolutionDescriptor.EntryDescriptors.Length > 0);
		}

		SolutionTemplateVisibility GetVisibility (string value)
		{
			if (!String.IsNullOrEmpty (value)) {
				SolutionTemplateVisibility visibility;
				if (Enum.TryParse (value, true, out visibility)) {
					return visibility;
				}
			}
			return SolutionTemplateVisibility.All;
		}

		internal ProjectTemplate Template {
			get { return template; }
		}
	}
}

