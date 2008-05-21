// TranslationCollection.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace MonoDevelop.Gettext
{
	public class TranslationCollection: Collection<Translation>
	{
		TranslationProject project;
		
		public TranslationCollection ()
		{
		}
		
		public Translation[] ToArray ()
		{
			Translation[] array = new Translation [Count];
			CopyTo (array, 0);
			return array;
		}
		
		internal TranslationCollection (TranslationProject project)
		{
			this.project = project;
		}
		
		protected override void InsertItem (int index, Translation item)
		{
			base.InsertItem (index, item);
			if (project != null) {
				item.ParentProject = project;
				project.NotifyTranslationAdded (item);
			}
		}
		
		protected override void SetItem (int index, Translation item)
		{
			Translation old = this[index];
			if (project != null)
				old.ParentProject = null;
			base.SetItem (index, item);
			if (project != null) {
				item.ParentProject = project;
				project.NotifyTranslationRemoved (old);
				project.NotifyTranslationAdded (item);
			}
		}
		
		protected override void RemoveItem (int index)
		{
			Translation old = this[index];
			base.RemoveItem (index);
			if (project != null) {
				old.ParentProject = null;
				project.NotifyTranslationRemoved (old);
			}
		}
		
		protected override void ClearItems ()
		{
			List<Translation> copy = new List<Translation> (this);
			base.ClearItems ();
			foreach (Translation t in copy) {
				t.ParentProject = null;
				project.NotifyTranslationRemoved (t);
			}
		}
	}
}
