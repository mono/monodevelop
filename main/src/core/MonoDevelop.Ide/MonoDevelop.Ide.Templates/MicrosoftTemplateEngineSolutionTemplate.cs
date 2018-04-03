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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Settings;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Ide.Templates
{
	class MicrosoftTemplateEngineSolutionTemplate : SolutionTemplate
	{
		internal readonly ITemplateInfo templateInfo;

		internal MicrosoftTemplateEngineSolutionTemplate (TemplateExtensionNode template, ITemplateInfo templateInfo)
			: base (template.Id, template.OverrideName ?? templateInfo.Name, template.Icon)
		{
			this.templateInfo = templateInfo;
			Description = ParseDescription (template.OverrideDescription) ?? templateInfo.Description;
			Category = template.Category;
			Language = MicrosoftTemplateEngine.GetLanguage (templateInfo);
			GroupId = template.GroupId ?? templateInfo.GroupIdentity;
			//TODO: Support all this params
			Condition = template.Condition;
			//ProjectFileExtension = template.FileExtension;
			Wizard = template.Wizard;
			SupportedParameters = template.SupportedParameters;
			DefaultParameters = MicrosoftTemplateEngine.MergeDefaultParameters (template.DefaultParameters, templateInfo);
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

		/// <summary>
		/// Replaces \n in description with new lines unless escaped with an extra backslash.
		/// </summary>
		static internal string ParseDescription (string description)
		{
			if (string.IsNullOrEmpty (description)) {
				return description;
			}

			int index = description.IndexOf ("\\n", StringComparison.Ordinal);
			if (index == -1) {
				return description;
			}

			var textBuilder = new StringBuilder (description.Length);

			index = 0;
			while (index < description.Length) {
				char ch = description [index];

				if (ch == '\\') {
					index++;
					if (index >= description.Length) {
						textBuilder.Append (ch);
					} else if (description [index] == 'n') {
						textBuilder.Append (Environment.NewLine);
					} else if (description [index] == '\\') {
						textBuilder.Append ('\\');
						index++;
						if (index < description.Length && description [index] == 'n') {
							textBuilder.Append ("n");
						} else {
							textBuilder.Append ("\\");
						}
					}
				} else {
					textBuilder.Append (ch);
				}

				index++;
			}

			return textBuilder.ToString ();
		}
	}
}
