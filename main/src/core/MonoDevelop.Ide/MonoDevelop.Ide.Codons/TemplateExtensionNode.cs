//
// TemplateExtensionNode.cs
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
using Mono.Addins;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode (Description = "Template informations.")]
	class TemplateExtensionNode : ExtensionNode
	{
		//these fields are assigned by reflection, suppress "never assigned" warning
		#pragma warning disable 649

		[NodeAttribute ("path", "Either .nupkg file or folder.")]
		string path;

		[NodeAttribute ("icon", "Icon to display in new project dialog.")]
		string icon;

		[NodeAttribute ("templateId", "Overrides the template id from the extension node id. Allows the same template to be used with different parameters.")]
		string templateId;

		#pragma warning restore 649

		[NodeAttribute ("category", "Category used to place template into correct category inside new project dialog.")]
		public string Category { get; private set; }

		[NodeAttribute ("imageId", "ImageId of image showed in new project dialog description of project.")]
		public string ImageId { get; private set; }

		[NodeAttribute ("_overrideName", "If template.json is outside AddIn creator control use this to change name.", Localizable = true)]
		public string OverrideName { get; private set; }

		[NodeAttribute ("_overrideDescription", "If template.json is outside AddIn creator control use this to change description.", Localizable = true)]
		public string OverrideDescription { get; private set; }

		[NodeAttribute ("overrideLanguage", "If template.json is outside AddIn creator control use this to change language", Localizable = false)]
		public string OverrideLanguage { get; private set; }

		[NodeAttribute ("defaultParameters", "Default parameters for project template.")]
		public string DefaultParameters { get; private set; }

		[NodeAttribute ("supportedParameters", "Parameters supported by the project template.")]
		public string SupportedParameters { get; private set; }

		[NodeAttribute ("groupId", "Overrides the group id defined in the template. Allows the same template to be grouped differently.")]
		public string GroupId { get; private set; }

		[NodeAttribute ("wizard", "Wizard identifier for this template.")]
		public string Wizard { get; private set; }

		[NodeAttribute ("condition", "Allows a template to be conditionally selected.")]
		public string Condition { get; private set; }

		[NodeAttribute ("formatExclude", "Project files that should not be formatted. For example: readme.txt|*.xml")]
		public string FileFormatExclude { get; private set; }


		public string ScanPath {
			get {
				// If the path starts with '${' then the path contains a placeholder
				// that the StringParserService will replace. The path is returned
				// without calling Addin.GetFilePath to prevent the addin directory
				// being prefixed to the path.
				if (path != null && path.StartsWith ("${", StringComparison.Ordinal)) {
					return path;
				}
				return Addin.GetFilePath (path);
			}
		}

		public string Icon => ImageService.GetStockId (Addin, icon, Gtk.IconSize.Dnd);
		public string TemplateId => templateId ?? Id;
	}
}
