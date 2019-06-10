//
// Commands.cs
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
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Documents;
using System.Threading;

namespace MonoDevelop.RegexToolkit
{
	enum Commands
	{
		ShowRegexToolkit
	}
	
	class RegexToolkitController : DocumentController
	{
		RegexToolkitWidget regexToolkit;

		public RegexToolkitWidget RegexToolkit {
			get {
				if (regexToolkit == null)
					regexToolkit = new RegexToolkitWidget ();
				return regexToolkit;
			}
		}

		public RegexToolkitController ()
		{
			DocumentTitle = GettextCatalog.GetString ("Regex Toolkit");
		}

		protected override Task<DocumentView> OnInitializeView ()
		{
			var container = new DocumentViewContainer ();
			container.SupportedModes = DocumentViewContainerMode.Tabs;

			var regexView = new DocumentViewContent (() => RegexToolkit);
			regexView.Title = GettextCatalog.GetString ("Regex Toolkit");

			var elementHelpView = new DocumentViewContent (() => new ElementHelpWidget (regexView, RegexToolkit));
			elementHelpView.Title = GettextCatalog.GetString ("Elements");

			container.Views.Add (regexView);
			container.Views.Add (elementHelpView);
			return Task.FromResult<DocumentView> (container);
		}

		protected override bool ControllerIsViewOnly => true;
	}
	
	class ShowRegexToolkitHandler : CommandHandler
	{
		protected override void Run ()
		{
			OpenToolkit ();
		}

		public static async Task<RegexToolkitWidget> RunRegexWindow ()
		{
			var document = await OpenToolkit ();
			var controller = document.GetContent<RegexToolkitController> ();
			return controller.RegexToolkit;
		}

		public static async Task<Document> OpenToolkit ()
		{
			foreach (var document in IdeApp.Workbench.Documents) {
				var controller = document.GetContent<RegexToolkitController> ();
				if (controller != null) {
					document.Select ();
					return document;
				}
			}
			return await IdeApp.Workbench.OpenDocument (new RegexToolkitController (), true);
		}
	}
	
	
}
