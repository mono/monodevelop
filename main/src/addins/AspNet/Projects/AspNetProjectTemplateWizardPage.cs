//
// AspNetProjectTemplateWizardPage.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
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

using MonoDevelop.Ide.Templates;
using MonoDevelop.Core;

namespace MonoDevelop.AspNet.Projects
{
	class AspNetProjectTemplateWizardPage : WizardPage
	{
		readonly string title = GettextCatalog.GetString ("Web Project Options");
		readonly AspNetProjectTemplateWizard wizard;
		AspNetProjectTemplateWizardPageWidget view;

		bool includeTestProject;

		public AspNetProjectTemplateWizardPage (AspNetProjectTemplateWizard wizard)
		{
			this.wizard = wizard;

			InitializeValues ();
		}

		const string aspNetMvc = "UsesAspNetMvc";
		const string aspNetWebForms = "UsesAspNetWebForms";
		const string aspNetWebApi = "UsesAspNetWebApi";

		void InitializeValues ()
		{
			IncludeTestProject = true;

			//ensure these don't have empty values, since that would result in "if" conditions being evaluated as true
			InitializeDefault (aspNetMvc, "false");
			InitializeDefault (aspNetWebForms, "false");
			InitializeDefault (aspNetWebApi, "false");
		}

		void InitializeDefault (string name, string defaultVal)
		{
			if (string.IsNullOrEmpty (wizard.Parameters[name]))
				wizard.Parameters[name] = defaultVal;
		}

		public bool IncludeTestProject {
			get { return includeTestProject; }
			set {
				includeTestProject = value;
				wizard.Parameters ["IncludeTestProject"] = value.ToString ();
				UpdateCanMoveNext ();
			}
		}

		public bool AspNetMvcEnabled {
			get { return wizard.Parameters.GetBoolean (aspNetMvc); }
			set {
				wizard.Parameters [aspNetMvc] = value.ToString ();
				UpdateCanMoveNext ();
			}
		}

		public bool AspNetMvcMutable {
			get { return wizard.IsSupportedParameter (aspNetMvc); }
		}

		public bool AspNetWebFormsEnabled {
			get { return wizard.Parameters.GetBoolean (aspNetWebForms); }
			set {
				wizard.Parameters [aspNetWebForms] = value.ToString ();
				UpdateCanMoveNext ();
			}
		}

		public bool AspNetWebFormsMutable {
			get { return wizard.IsSupportedParameter (aspNetWebForms); }
		}

		public bool AspNetWebApiEnabled {
			get { return wizard.Parameters.GetBoolean (aspNetWebApi); }
			set {
				wizard.Parameters [aspNetWebApi] = value.ToString ();
				UpdateCanMoveNext ();
			}
		}

		public bool AspNetWebApiMutable {
			get { return wizard.IsSupportedParameter (aspNetWebApi); }
		}

		void UpdateCanMoveNext ()
		{
			CanMoveToNextPage = true;
		}

		public override string Title {
			get { return title; }
		}

		protected override object CreateNativeWidget ()
		{
			return view ?? (view = new AspNetProjectTemplateWizardPageWidget (this));
		}
	}
}
