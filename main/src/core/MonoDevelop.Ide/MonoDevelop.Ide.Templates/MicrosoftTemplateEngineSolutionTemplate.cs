//
// MicrosoftTemplateEngineSolutionTemplate.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Settings;
using System.Linq;
using MonoDevelop.Ide.Codons;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Templates
{
	class MicrosoftTemplateEngineSolutionTemplate : SolutionTemplate
	{
		internal readonly ITemplateInfo templateInfo;

		internal MicrosoftTemplateEngineSolutionTemplate (TemplateExtensionNode template, ITemplateInfo templateInfo)
			: base (template.Id, template.OverrideName ?? templateInfo.Name, template.Icon)
		{
			this.templateInfo = templateInfo;
			Description = template.OverrideDescription ?? templateInfo.Description;
			Category = template.Category;
			ICacheTag languageTag;
			if (templateInfo.Tags.TryGetValue ("language", out languageTag))
				Language = languageTag.DefaultValue;
			else
				Language = string.Empty;
			GroupId = template.GroupId ?? templateInfo.GroupIdentity;
			//TODO: Support all this params
			Condition = template.Condition;
			//ProjectFileExtension = template.FileExtension;
			Wizard = template.Wizard;
			SupportedParameters = template.SupportedParameters;
			DefaultParameters = MergeDefaultParameters (template.DefaultParameters);
			ImageId = template.ImageId;
			FileFormattingExclude = template.FileFormatExclude;
			//ImageFile = template.ImageFile;
			//Visibility = GetVisibility (template.Visibility);

			//HasProjects = (template.SolutionDescriptor.EntryDescriptors.Length > 0);
		}

		internal MicrosoftTemplateEngineSolutionTemplate (string id, string name, string iconId, ITemplateInfo templateInfo)
			: base (id, name, iconId)
		{
			this.templateInfo = templateInfo;
		}

		string MergeDefaultParameters (string defaultParameters)
		{
			List<TemplateParameter> priorityParameters = null;
			var parameters = new List<string> ();
			var cacheParameters = templateInfo.CacheParameters.Where (m => !string.IsNullOrEmpty (m.Value.DefaultValue));

			if (!cacheParameters.Any ())
				return defaultParameters;

			if (!string.IsNullOrEmpty (defaultParameters)) {
				priorityParameters = TemplateParameter.CreateParameters (defaultParameters).ToList ();
				defaultParameters += ",";
			}

			foreach (var p in cacheParameters) {
				if (priorityParameters != null && !priorityParameters.Exists (t => t.Name == p.Key))
					parameters.Add ($"{p.Key}={p.Value.DefaultValue}");
			}

			return defaultParameters += string.Join (",", parameters);
		}

		internal string FileFormattingExclude { get; set; }

		internal bool ShouldFormatFile (string fileName)
		{
			if (string.IsNullOrEmpty (FileFormattingExclude))
				return true;

			if (excludedFileEndings == null) {
				excludedFileEndings = GetFileEndings (FileFormattingExclude);
			}

			foreach (string ending in excludedFileEndings) {
				if (fileName.EndsWith (ending, StringComparison.OrdinalIgnoreCase)) {
					return false;
				}
			}

			return true;
		}

		List<string> excludedFileEndings;

		static List<string> GetFileEndings (string exclude)
		{
			var result = new List<string> ();
			foreach (string pattern in exclude.Split ('|')) {
				if (pattern.StartsWith ("*.", StringComparison.Ordinal)) {
					result.Add (pattern.Substring (1));
				} else {
					result.Add (pattern);
				}
			}
			return result;
		}
	}
}
