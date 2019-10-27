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
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Wizard;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class ScaffolderArgs
	{

	}

	abstract class ScaffolderWizardPageBase : WizardDialogPageBase
	{
		string subSubTitle;
		protected ScaffolderWizardPageBase (ScaffolderArgs args)
		{
			Args = args;
		}

		public string SubSubTitle {
			get => subSubTitle;
			protected set => subSubTitle = value;
		}

		public ScaffolderArgs Args { get; }

		protected override Control CreateControl ()
		{
			var icon = new Xwt.ImageView (StockIcons.Information);
			var mainBox = new VBox { Spacing = 0 };
			var label = new Label (subSubTitle);
			label.Font = label.Font.WithSize (18);
			mainBox.PackStart (label, margin: 30);
			var separator = new HSeparator ();
			mainBox.PackStart (separator);

			mainBox.PackStart (GetMainControl (), margin: 20);
			//mainBox.ExpandVertical = true;
			return new XwtControl (mainBox);
		}

		protected abstract Widget GetMainControl ();
	}

	class ScaffolderField
	{
		string CommandLineName { get; }
		Type Type { get; }
		string DisplayName { get; }

		public ScaffolderField (string commandLineName, string displayName, Type type)
		{
			CommandLineName = commandLineName;
			DisplayName = displayName;
			Type = type;
		}
	}

	interface IScaffolder
	{
		string Name { get; }
		string CommandLineName { get; }
		IEnumerable<ScaffolderField> Fields { get; }
	}

	class EmptyMvcControllerScaffolder : IScaffolder
	{
		public string Name => "MVC Controller - Empty";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new ScaffolderField ("name", "Name", typeof (string)) };
	}

	class MvcControllerWithActionsScaffolder : IScaffolder
	{
		public string Name => "MVC Controller with read / write actions";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new ScaffolderField ("name", "Name", typeof (string)) };
	}

	class EmptyApiControllerScaffolder : IScaffolder
	{
		public string Name => "API Controller - Empty";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new ScaffolderField ("name", "Name", typeof (string)) };
	}

	class ApiControllerWithActionsScaffolder : IScaffolder
	{
		public string Name => "API Controller with read / write actions";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new ScaffolderField ("name", "Name", typeof (string)) };
	}

	class ApiControllerEntityFrameworkScaffolder : IScaffolder
	{
		public string Name => "API Controller with actions using Entity Framework";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new ScaffolderField ("name", "Name", typeof (string)) };
	}

	class RazorPageScaffolder : IScaffolder
	{
		public string Name => "Razor Page";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new ScaffolderField ("name", "Name", typeof (string)) };
	}

	class RazorPageEntityFrameworkScaffolder : IScaffolder
	{
		public string Name => "Razor Page using Entity Framework";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new ScaffolderField ("name", "Name", typeof (string)) };
	}

	class RazorPageEntityFrameworkCrudScaffolder : IScaffolder
	{
		public string Name => "Razor Page using Entity Framework (CRUD)";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new ScaffolderField ("name", "Name", typeof (string)) };
	}

	class IdentityScaffolder : IScaffolder
	{
		public string Name => "Identity";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new ScaffolderField ("name", "Name", typeof (string)) };
	}

	class LayoutScaffolder : IScaffolder
	{
		public string Name => "Layout";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new ScaffolderField ("name", "Name", typeof (string)) };
	}

	class ScaffolderTemplateSelect : ScaffolderWizardPageBase
	{
		public ScaffolderTemplateSelect () : base (new ScaffolderArgs ())
		{
			this.CanGoBack = true;
			this.CanGoNext = true;
			this.PageSubtitle = "Select Scaffolder";
			this.PageIcon = StockIcons.Question;
			this.SubSubTitle = "Select Scaffolder SUB";
		}

		private Lazy<IScaffolder []> GetScaffolders ()
		{
			var scaffolders = new IScaffolder [] {
				new EmptyMvcControllerScaffolder(),
				new MvcControllerWithActionsScaffolder(),
				new EmptyApiControllerScaffolder(),
				new ApiControllerWithActionsScaffolder(),
				new ApiControllerEntityFrameworkScaffolder(),
				new RazorPageScaffolder(),
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

			foreach (var scaffolder in GetScaffolders ().Value) {
				var row = listStore.AddRow ();
				var png = Image.FromResource ("file-web-32.png");
				listStore.SetValue (row, icon, png);
				listStore.SetValue (row, name, scaffolder.Name);
			}

			var listBox = new ListBox ();
			listBox.Views.Add (new ImageCellView (icon));
			listBox.Views.Add (new TextCellView (name));

			listBox.DataSource = listStore;
			//listBox.Items.Add ("MVC Controller – Empty");
			//listBox.Items.Add ("MVC Controller with read / write actions");
			//listBox.Items.Add ("API Controller – Empty");
			//listBox.Items.Add ("API Controller with read / write actions");
			//listBox.Items.Add ("API Controller with actions using Entity Framework");
			//listBox.Items.Add ("Razor Page");
			//listBox.Items.Add ("Razor Page using Entity Framework");
			//listBox.Items.Add ("Razor Page using Entity Framework (CRUD)");
			//listBox.Items.Add ("Identity");
			//listBox.Items.Add ("Layout ");
			listBox.HeightRequest = 400;
			listBox.WidthRequest = 300;
			//listBox.Font = MonoDevelop.Ide.Gui.Styles.DefaultFont;
			//listBox.ExpandVertical = true;
			//mainBox. = new WidgetSpacing (20, 20, 20, 20);
			return listBox;
		}
	}

	class ScaffolderWizard : WizardDialogController
	{
		public ScaffolderWizard (string title, IWizardDialogPage firstPage) : base (title, StockIcons.Information, null, firstPage)
		{
			this.DefaultPageSize = new Size (600, 500);

			var rightSideImage = new Xwt.ImageView (Xwt.Drawing.Image.FromResource ("aspnet-wizard-page.png"));
			var rightSideWidget = new Xwt.FrameBox (rightSideImage);
			rightSideWidget.BackgroundColor = MonoDevelop.Ide.Gui.Styles.Wizard.PageBackgroundColor;
			////rightSideWidget.ExpandHorizontal = true;
			//rightSideWidget.ExpandVertical = true;
			this.RightSideWidget = new XwtControl (rightSideWidget);
		}
	}
}
