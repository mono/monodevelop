//
// TemplateCategory.cs
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
//
using System.Collections.Generic;

namespace MonoDevelop.Ide.Templates
{
	public class TemplateCategory
	{
		List<TemplateCategory> categories = new List<TemplateCategory> ();
		List<SolutionTemplate> templates = new List<SolutionTemplate> ();

		public TemplateCategory (string id, string name, string iconId)
		{
			Id = id;
			Name = name;
			IconId = iconId;

			AddDummyTemplates ();
		}

		void AddDummyTemplates ()
		{
			var template = new SolutionTemplate ("blank-app-portable", "Blank App (Xamarin.Forms Portable)", "md-project") { // FIXME: VV: Retina
				Description = "Blank App (Xamarin.Forms Portable). More text and some more. Blah, blah, blah, blah, more text that should wrap. More and more. More and even more",
				LargeImageId = "template-default-background-light.png",
				Wizard = "Xamarin.Forms.Template.Wizard"
			};
			AddTemplate (template);
			template = new SolutionTemplate ("blank-app-shared", "Blank App (Xamarin.Forms Shared)", "md-project") { // FIXME: VV: Retina
				Description = "Blank App (Xamarin.Forms Shared)",
				LargeImageId = "template-default-background-light.png"
			};
			AddTemplate (template);
		}

		public string Id { get; private set; }
		public string Name { get; private set; }
		public string IconId { get; private set; }

		public void AddCategory (TemplateCategory category)
		{
			categories.Add (category);
		}

		public IEnumerable<TemplateCategory> Categories {
			get { return categories; }
		}

		public IEnumerable<SolutionTemplate> Templates {
			get { return templates; }
		}

		public void AddTemplate (SolutionTemplate template)
		{
			templates.Add (template);
		}
	}
}

