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
using System.Linq;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class RazorPageScaffolderBase : ScaffolderBase
	{
		protected InvertedBoolField PageModelField { get { return new InvertedBoolField ("--noPageModel", "Generate PageModel class", isChecked: true); } }
		protected BoolField PartialViewField { get { return new BoolField ("--partialView", "Create as a partial view"); } }
		protected BoolField ReferenceScriptLibrariesField { get { return new BoolField ("--referenceScriptLibraries", "Reference script libraries", isChecked: true, enabled: true); } }
		protected BoolField LayoutPageField { get { return new InvertedBoolField ("--useDefaultLayout", "Use a layout page", isChecked: true); } }
		protected FileField CustomLayoutField { get { return new FileField ("--layout", "Leave empty if is set in a Razor _viewstart file", "*.cshtml"); } }
		protected StringField NameField { get { return new StringField ("", "Name of the Razor Page"); } }

		public override string CommandLineName => "razorpage";

		IEnumerable<CommandLineArg> commandLineArgs;
		protected IEnumerable<ScaffolderField> fields;

		public override IEnumerable<CommandLineArg> DefaultArgs => commandLineArgs;

		public RazorPageScaffolderBase (ScaffolderArgs args)
		{
			var defaultNamespace = args.ParentFolder.Combine ("file.cs");

			commandLineArgs = base.DefaultArgs.Append (
	new CommandLineArg ("--namespaceName", args.Project.GetDefaultNamespace (defaultNamespace))
);
		}
	}
}
