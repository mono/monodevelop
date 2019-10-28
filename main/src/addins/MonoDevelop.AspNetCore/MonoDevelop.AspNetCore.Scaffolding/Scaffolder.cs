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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Wizard;
using MonoDevelop.Projects;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class ScaffolderArgs
	{
		public ScaffolderArgs ()
		{
			//TODO: Get the scaffolder from the wizard
			Scaffolder = new EmptyMvcControllerScaffolder ();
		}
		public IScaffolder Scaffolder { get; set; }
	}

	abstract class ScaffolderWizardPageBase : WizardDialogPageBase
	{
		string subSubTitle;
		protected ScaffolderWizardPageBase (ScaffolderArgs args)
		{
			Args = args;
			CanGoBack = true;
			CanGoNext = true;
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

	abstract class ScaffolderField
	{
		public string CommandLineName { get; }
        public string DisplayName { get; }

        string val;
		public string SelectedValue {
			get { return val; }
			set {
				val = value;
				Console.WriteLine ("Value = " + value);
			}
		}

		public ScaffolderField (string commandLineName, string displayName)
		{
			CommandLineName = commandLineName;
			DisplayName = displayName;
		}
	}

	class StringField : ScaffolderField
	{
		public StringField (string commandLineName, string displayName) : base (commandLineName, displayName)
		{
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

		static StringField[] stringField = new [] { new StringField ("name", "Name") };
		public IEnumerable<ScaffolderField> Fields => stringField;
	}

	class MvcControllerWithActionsScaffolder : IScaffolder
	{
		public string Name => "MVC Controller with read / write actions";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class EmptyApiControllerScaffolder : IScaffolder
	{
		public string Name => "API Controller - Empty";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class ApiControllerWithActionsScaffolder : IScaffolder
	{
		public string Name => "API Controller with read / write actions";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class ApiControllerEntityFrameworkScaffolder : IScaffolder
	{
		public string Name => "API Controller with actions using Entity Framework";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class RazorPageScaffolder : IScaffolder
	{
		public string Name => "Razor Page";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class RazorPageEntityFrameworkScaffolder : IScaffolder
	{
		public string Name => "Razor Page using Entity Framework";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class RazorPageEntityFrameworkCrudScaffolder : IScaffolder
	{
		public string Name => "Razor Page using Entity Framework (CRUD)";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class IdentityScaffolder : IScaffolder
	{
		public string Name => "Identity";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class LayoutScaffolder : IScaffolder
	{
		public string Name => "Layout";
		public string CommandLineName => "controller";

		public IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class ScaffolderTemplateConfigurePage : ScaffolderWizardPageBase
	{
		IScaffolder scaffolder;

		public ScaffolderTemplateConfigurePage (ScaffolderArgs args) : base (args)
		{
			scaffolder = args.Scaffolder;
			this.SubSubTitle = scaffolder.Name;
		}

		protected override Widget GetMainControl ()
		{
			var vbox = new VBox ();
			foreach (var field in scaffolder.Fields) {
				switch (field) {
				case StringField s:
					var hbox = new HBox ();
					var input = new TextEntry ();
					input.HeightRequest = 30;
					hbox.PackEnd (input);
					var label = new Label ();
					label.Font = label.Font.WithSize (15);
					label.Text = s.DisplayName;
					hbox.PackEnd (label);
					vbox.PackStart (hbox);
					input.Changed += (sender, args) => s.SelectedValue = input.Text;
					break;
				}
			}
			return vbox;
		}

		public override int GetHashCode ()
		{
			// Pages are used as dictionary keys in WizardDialog.cs
			return unchecked(
				base.GetHashCode () + 37 * Args.Scaffolder.Name.GetHashCode ()
			);
		}
	}

	class ScaffolderTemplateSelectPage : ScaffolderWizardPageBase
	{
		ListBox listBox;

		public ScaffolderTemplateSelectPage (ScaffolderArgs args) : base (args)
		{
			this.CanGoBack = true;
			this.CanGoNext = true;
			this.PageSubtitle = "Select Scaffolder";
			this.PageIcon = StockIcons.Question;
			this.SubSubTitle = "Select Scaffolder SUB";
		}

		Lazy<IScaffolder []> GetScaffolders ()
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

	class ScaffolderDialogController : WizardDialogControllerBase
	{
		// We have 2 pages, the first contains a list of templates
		// and the 2nd is an entry form based on the selection
		// in the first page.
		ReadOnlyCollection<IWizardDialogPage> pages;
		readonly ScaffolderArgs args;

		public IReadOnlyCollection<IWizardDialogPage> Pages { get { return pages; } }

		public override bool CanGoBack {
			get {
				return CurrentPage is ScaffolderTemplateConfigurePage;
			}
		}

		public override bool CurrentPageIsLast {
			get { return CanGoBack; }
		}

		public ScaffolderDialogController (string title, Image icon, Control rightSideWidget, IWizardDialogPage page, ScaffolderArgs args)
			: this (title, icon, rightSideWidget, new IWizardDialogPage [] { page }, args)
		{
			this.args = args;
		}

		public ScaffolderDialogController (string title, Image icon, Control rightSideWidget, IEnumerable<IWizardDialogPage> pages, ScaffolderArgs args)
			: base (title, icon, rightSideWidget, pages.FirstOrDefault ())
		{
			this.pages = new ReadOnlyCollection<IWizardDialogPage> (pages.ToList ());
			if (this.pages.Count == 0)
				throw new ArgumentException ("pages must contain at least one page.", nameof (pages));
			this.args = args;
		}

		Lazy<ScaffolderTemplateConfigurePage> GetConfigurePage(ScaffolderArgs args)
		{
			// we want to return the same instance for the same args
			return new Lazy<ScaffolderTemplateConfigurePage>(() => new ScaffolderTemplateConfigurePage (args));
        }

		protected override Task<IWizardDialogPage> OnGoNext (CancellationToken token)
		{
			switch (CurrentPage) {
			case ScaffolderTemplateSelectPage _:
				IWizardDialogPage configPage = GetConfigurePage (args).Value;
				return Task.FromResult (configPage);
			}
			return Task.FromException<IWizardDialogPage>(new InvalidOperationException ());
			//var currentIndex = pages.IndexOf (CurrentPage);
			//if (currentIndex == pages.Count - 1)
			//	return Task.FromException<IWizardDialogPage>(new InvalidOperationException ());
			//else
			//	return Task.FromResult (pages [currentIndex + 1]);
		}

		protected override Task<IWizardDialogPage> OnGoBack (CancellationToken token)
		{
			IWizardDialogPage firstPage = pages [0];
			return Task.FromResult(firstPage);
			//var currentIndex = pages.IndexOf (CurrentPage);
			//return Task.FromResult (pages [currentIndex - 1]);
		}
	}

	class ScaffolderWizard : ScaffolderDialogController
	{
		static readonly ScaffolderArgs args = new ScaffolderArgs();
		readonly DotNetProject project;
		readonly string parentFolder;

		public ScaffolderWizard (DotNetProject project, string parentFolder) : base ("Add New Scaffolded Item", StockIcons.Information, null, GetPages (), args)
		{
			this.DefaultPageSize = new Size (600, 500);

			var rightSideImage = new Xwt.ImageView (Image.FromResource ("aspnet-wizard-page.png"));
			var rightSideWidget = new FrameBox (rightSideImage);
			rightSideWidget.BackgroundColor = Styles.Wizard.PageBackgroundColor;
			this.RightSideWidget = new XwtControl (rightSideWidget);
			this.Completed += (sender, e) => Task.Run(() => OnCompletedAsync());
			this.project = project;
			this.parentFolder = parentFolder;
		}

		async Task OnCompletedAsync ()
		{ 
			var dotnet = DotNetCoreRuntime.FileName;
			var argBuilder = new ProcessArgumentBuilder ();
			argBuilder.Add ("aspnet-codegenerator");
			argBuilder.Add ("--project");
			argBuilder.AddQuoted (project.FileName);
			argBuilder.Add (args.Scaffolder.CommandLineName);

			foreach(var field in args.Scaffolder.Fields) {
				argBuilder.Add ("-" + field.CommandLineName);
				argBuilder.Add (field.SelectedValue);
			}

			var commandLineArgs = argBuilder.ToString ();

			using (var progressMonitor = CreateProgressMonitor ()) {
				try {
					var process = Runtime.ProcessService.StartConsoleProcess (
						dotnet,
						commandLineArgs,
						parentFolder,
						progressMonitor.Console
					);

					await process.Task;
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception ex) {
					await progressMonitor.Log.WriteLineAsync (ex.Message);
					LoggingService.LogError ($"Failed to run {dotnet} {commandLineArgs}", ex);
				}
			}
        }

		static OutputProgressMonitor CreateProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				"AspNetCoreScaffolder",
				GettextCatalog.GetString ("ASP.NET Core Scaffolder"),
				Stock.Console,
				false,
				true);
		}

		static IReadOnlyCollection<IWizardDialogPage> GetPages ()
		{
			//TODO: Get this from wizard
			args.Scaffolder = new EmptyMvcControllerScaffolder ();
			return new IWizardDialogPage [] {
					new ScaffolderTemplateSelectPage(args),
					new ScaffolderTemplateConfigurePage(args)
				};

		}
	}
}
