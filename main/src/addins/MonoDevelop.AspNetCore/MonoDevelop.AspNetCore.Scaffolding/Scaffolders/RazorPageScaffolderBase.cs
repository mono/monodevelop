//
// Scaffolder.cs
//
// Author:
//       jasonimison <jaimison@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class RazorPageScaffolderBase : ScaffolderBase
	{
		protected BoolField PageModelField { get { return new BoolField ("--noPageModel", GettextCatalog.GetString("Generate PageModel class"), isChecked: true, isInverted: true); } }
		protected BoolField PartialViewField { get { return new BoolField ("--partialView", GettextCatalog.GetString ("Create as a partial view")); } }
		protected BoolField ReferenceScriptLibrariesField { get { return new BoolField ("--referenceScriptLibraries", GettextCatalog.GetString ("Reference script libraries"), isChecked: true, enabled: true); } }
		protected BoolField LayoutPageField { get { return new BoolField ("--useDefaultLayout", GettextCatalog.GetString ("Use a layout page"), isChecked: true, isInverted: true); } }
		protected FileField CustomLayoutField { get { return new FileField ("", GettextCatalog.GetString ("Leave empty if is set in a Razor _viewstart file"), "*.cshtml"); } }
		protected StringField NameField { get { return new StringField ("", GettextCatalog.GetString ("Name of the Razor Page:")); } }

		public override string CommandLineName => "razorpage";

		protected IEnumerable<ScaffolderField> fields;

		public RazorPageScaffolderBase (ScaffolderArgs args)
		{
			var defaultNamespace = args.ParentFolder.Combine ("file.cs");

			DefaultArgs.Add (new CommandLineArg ("--namespaceName", args.Project.GetDefaultNamespace (defaultNamespace)));
		}
	}
}
