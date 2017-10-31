//
// ItemTemplateExtensionNode.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
	[ExtensionNode (Description = "A file template")]
	class ItemTemplateExtensionNode : ExtensionNode
	{
		[NodeAttribute ("path", "A .nupkg file or a folder.")]
		string path;

		public string ScanPath {
			get {
				return Addin.GetFilePath (path);
			}
		}

		[NodeAttribute ("templateId", "Overrides the template id from the extension node id. Allows the same template to be used with different parameters.")]
		string templateId;

		public string TemplateId {
			get {
				return templateId ?? Id;
			}
		}

		[NodeAttribute ("_overrideName", "Override name used in template.json file.", Localizable = true)]
		string overrideName;

		public string OverrideName {
			get {
				return overrideName;
			}
		}
	}
}
