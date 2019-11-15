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
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Wizard;
using Xwt.Drawing;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class ScaffolderDialogController : WizardDialogControllerBase
	{
		// We have 2 pages, the first contains a list of templates
		// and the 2nd is an entry form based on the selection
		// in the first page.
		readonly ScaffolderArgs args;

		readonly IWizardDialogPage firstPage;
		readonly Dictionary<ScaffolderArgs, ScaffolderTemplateConfigurePage> cachedPages
			= new Dictionary<ScaffolderArgs, ScaffolderTemplateConfigurePage> ();

		public override bool CanGoBack {
			get {
				return CurrentPage is ScaffolderTemplateConfigurePage;
			}
		}

		public override bool CurrentPageIsLast {
			get { return CanGoBack; }
		}

		public ScaffolderDialogController (string title, IWizardDialogPage firstPage, ScaffolderArgs args)
			: base (title, null, null, firstPage)
		{
			this.firstPage = firstPage;
			this.args = args;
		}

		ScaffolderTemplateConfigurePage GetConfigurePage (ScaffolderArgs args, CancellationToken token)
		{
			// we want to return the same instance for the same args
			if (cachedPages.ContainsKey (args)) {
				return cachedPages [args];
			} else {
				var page = new ScaffolderTemplateConfigurePage (args, token);
				cachedPages.Add (args, page);
				return page;
			}
		}

		protected override Task<IWizardDialogPage> OnGoNext (CancellationToken token)
		{
			switch (CurrentPage) {
			case ScaffolderTemplateSelectPage _:
				IWizardDialogPage configPage = GetConfigurePage (args, token);
				return Task.FromResult (configPage);
			}
			return Task.FromException<IWizardDialogPage> (new InvalidOperationException ());
		}

		protected override Task<IWizardDialogPage> OnGoBack (CancellationToken token)
		{
			return Task.FromResult (firstPage);
		}
	}
}
