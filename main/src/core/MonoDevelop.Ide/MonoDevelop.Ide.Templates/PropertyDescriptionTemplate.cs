//
// PropertyDescriptionTemplate.cs
//
// Author:
//       Vincent Dondain <vincent.dondain@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	class PropertyDescriptionTemplate : FileDescriptionTemplate
	{
		XmlAttribute typeAtt;
		string propertyInnerText;
		string extension;

		public override string Name {
			get {
				return "Property";
			}
		}

		public override void Show ()
		{
		}

		public override void Load (XmlElement filenode, FilePath baseDirectory)
		{
			var extAtt = filenode.GetAttributeNode ("extension");
			if (extAtt != null)
				extension = extAtt.Value ?? "";

			propertyInnerText = filenode.InnerText;
			if (propertyInnerText == null)
				throw new InvalidOperationException ("Property is missing its inner text");

			typeAtt = filenode.GetAttributeNode ("type");
			if (typeAtt == null)
				throw new InvalidOperationException ("Property is missing the type attribute");

			if (String.IsNullOrEmpty (typeAtt.Value))
				throw new InvalidOperationException ("Property's type attribute is empty");

			if (String.IsNullOrEmpty (filenode.InnerText))
				throw new InvalidOperationException ("Property is empty");
		}

		public override Task<bool> AddToProjectAsync (SolutionFolderItem policyParent, Project project, string language, string directory, string name)
		{
			var model = CombinedTagModel.GetTagModel (ProjectTagModel, policyParent, project, language, name, null);
			var fileName = StringParserService.Parse (name, model);

			project.ProjectProperties.SetValue (typeAtt.Value, string.IsNullOrEmpty (fileName) ? propertyInnerText : string.Concat (fileName, extension));

			return Task.FromResult(true);
		}
	}
}

