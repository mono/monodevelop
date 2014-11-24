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
using System.Linq;

namespace MonoDevelop.Ide.Templates
{
	public class TemplateCategory
	{
		List<TemplateCategory> categories = new List<TemplateCategory> ();
		List<SolutionTemplate> templates = new List<SolutionTemplate> ();

		public TemplateCategory (string id, string name, string iconId, bool isDefault = false)
		{
			Id = id;
			Name = name;
			IconId = iconId;
			IsDefault = isDefault;
		}

		public string Id { get; private set; }
		public string Name { get; private set; }
		public string IconId { get; private set; }
		public bool IsDefault { get; set; }
		public bool IsTopLevel { get; set; }
		public string MappedCategories { get; set; }

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
			SolutionTemplate groupTemplate = templates.Find (t => t.IsGroupMatch (template));
			if (groupTemplate != null) {
				groupTemplate.AddGroupTemplate (template);
			} else {
				templates.Add (template);
			}
		}

		public TemplateCategory Clone ()
		{
			var clone = new TemplateCategory (Id, Name, IconId) {
				IsDefault = IsDefault,
				IsTopLevel = IsTopLevel,
				MappedCategories = MappedCategories
			};
			foreach (TemplateCategory child in categories) {
				clone.AddCategory (child.Clone ());
			}
			return clone;
		}

		public bool IsMatch (string category)
		{
			return Id == category;
		}

		public void RemoveEmptyCategories ()
		{
			foreach (TemplateCategory child in categories) {
				child.RemoveEmptyCategories ();
			}

			categories.RemoveAll (category => !category.HasChildren ());
		}

		public bool HasChildren ()
		{
			return templates.Any () || categories.Any ();
		}
	}
}

