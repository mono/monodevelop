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
	internal class TemplateExtensionNode : ExtensionNode
	{
		[NodeAttribute ("category", "Category used to place template into correct category inside new project dialog.")]
		string category;

		public string Category {
			get {
				return category;
			}
		}

		[NodeAttribute ("path", "Either .nupkg file or folder.")]
		string path;

		public string ScanPath {
			get {
				return Addin.GetFilePath (path);
			}
		}

		
		[NodeAttribute ("icon", "Icon to display in new project dialog.")]
		string icon;

		public string Icon {
			get {
				return ImageService.GetStockId (Addin, icon, Gtk.IconSize.Dnd);
			}
		}


		[NodeAttribute ("imageId", "ImageId of image showed in new project dialog description of project.")]
		string imageId;

		public string ImageId {
			get {
				return imageId;
			}
		}

		[NodeAttribute ("_overrideName", "If template.json is outside AddIn creator control use this to change name.", Localizable = true)]
		string overrideName;
		public string OverrideName {
			get {
				return overrideName;
			}
		}
		

		[NodeAttribute ("_overrideDescription", "If template.json is outside AddIn creator control use this to change description.", Localizable = true)]
		string overrideDescription;
		public string OverrideDescription {
			get {
				return overrideDescription;
			}
		}
	}
}
