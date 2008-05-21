//
// GettextFeature.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Gtk;

using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Gettext
{
	public class GettextFeature : ISolutionItemFeature
	{
		public string Title {
			get { return GettextCatalog.GetString ("Translation"); }
		}
		
		public string Description {
			get { return GettextCatalog.GetString ("Add a Translation Project to the solution that will use gettext to generate a set of PO files for the new project."); }
		}
		
		public bool SupportsSolutionItem (SolutionFolder parentCombine, SolutionItem entry)
		{
			return ((entry is Project) || (entry is TranslationProject)) && parentCombine != null;
		}
		
		public Widget CreateFeatureEditor (SolutionFolder parentCombine, SolutionItem entry)
		{
			return new GettextFeatureWidget ();
		}

		public void ApplyFeature (SolutionFolder parentCombine, SolutionItem entry, Widget editor)
		{
			((GettextFeatureWidget)editor).ApplyFeature (parentCombine, entry);
		}
		
		public string Validate (SolutionFolder parentCombine, SolutionItem entry, Gtk.Widget editor)
		{
			return null;
		}
		
		public bool IsEnabled (SolutionFolder parentCombine, SolutionItem entry) 
		{
			return entry is TranslationProject;
		}
	}
}
