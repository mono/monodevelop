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
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class ScaffolderTemplateSelectPage : ScaffolderWizardPageBase
	{
		ListBox listBox;
		ScaffolderArgs args;

		public ScaffolderTemplateSelectPage (ScaffolderArgs args) : base (args)
		{
			this.CanGoBack = true;
			this.CanGoNext = true;
			this.PageSubtitle = "Select Scaffolder";
			this.PageIcon = StockIcons.Question;
			this.SubSubTitle = "Select Scaffolder SUB";
			this.args = args;
		}

		Lazy<IScaffolder []> GetScaffolders ()
		{
			var scaffolders = new IScaffolder [] {
				new EmptyMvcControllerScaffolder(args),
				new MvcControllerWithActionsScaffolder(args),
				new EmptyApiControllerScaffolder(args),
				new ApiControllerWithActionsScaffolder(args),
				new ApiControllerEntityFrameworkScaffolder(args),
				new RazorPageScaffolder(args),
				new RazorPageEntityFrameworkScaffolder(),
				new RazorPageEntityFrameworkCrudScaffolder(),
				new IdentityScaffolder(),
				new LayoutScaffolder()
			};
			return new Lazy<IScaffolder []> (() => scaffolders);
		}

		protected override Widget GetMainControl ()
		{
			var icon = new DataField<Image> ();
			var name = new DataField<string> ();

			var listStore = new ListStore (icon, name);

			var scaffolders = GetScaffolders ().Value;

			foreach (var scaffolder in scaffolders) {
				var row = listStore.AddRow ();
				var png = Image.FromResource ("file-web-32.png");
				listStore.SetValue (row, icon, png);
				listStore.SetValue (row, name, scaffolder.Name);
			}

			listBox = new ListBox ();
			listBox.Views.Add (new ImageCellView (icon));
			listBox.Views.Add (new TextCellView (name));

			listBox.DataSource = listStore;
			listBox.HeightRequest = 400;
			listBox.WidthRequest = 300;
			listBox.SelectionChanged += (sender, e) => Args.Scaffolder = scaffolders [listBox.SelectedRow];
			return listBox;
		}
	}
}
