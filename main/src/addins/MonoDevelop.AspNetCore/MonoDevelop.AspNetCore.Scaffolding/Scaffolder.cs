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
		public IScaffolder Scaffolder { get; set; }

		public override int GetHashCode ()
		{
			return Scaffolder.Name.GetHashCode ();
		}
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
			return new XwtControl (mainBox);
		}

		protected abstract Widget GetMainControl ();
	}

	abstract class ScaffolderField
	{
		public string CommandLineName { get; }
		public string DisplayName { get; }
		public string SelectedValue { get; set; }

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

	class ComboField : ScaffolderField
	{
		public ComboField (string commandLineName, string displayName, string [] options) : base (commandLineName, displayName)
		{
			Options = options;
		}

		public string [] Options { get; }
	}

	abstract class IScaffolder
	{
		public virtual string Name { get; }
		public virtual string CommandLineName { get; }
		public virtual string [] DefaultArgs => Array.Empty<string> ();
		public virtual IEnumerable<ScaffolderField> Fields { get; }
	}

	class EmptyMvcControllerScaffolder : IScaffolder
	{
		//Generator Options:
		//--controllerName|-name              : Name of the controller
		//--useAsyncActions|-async            : Switch to indicate whether to generate async controller actions
		//--noViews|-nv                       : Switch to indicate whether to generate CRUD views
		//--restWithNoViews|-api              : Specify this switch to generate a Controller with REST style API, noViews is assumed and any view related options are ignored
		//--readWriteActions|-actions         : Specify this switch to generate Controller with read/write actions when a Model class is not used
		//--model|-m                          : Model class to use
		//--dataContext|-dc                   : DbContext class to use
		//--referenceScriptLibraries|-scripts : Switch to specify whether to reference script libraries in the generated views
		//--layout|-l                         : Custom Layout page to use
		//--useDefaultLayout|-udl             : Switch to specify that default layout should be used for the views
		//--force|-f                          : Use this option to overwrite existing files
		//--relativeFolderPath|-outDir        : Specify the relative output folder path from project where the file needs to be generated, if not specified, file will be generated in the project folder
		//--controllerNamespace|-namespace    : Specify the name of the namespace to use for the generated controller


		public override string Name => "MVC Controller - Empty";
		public override string CommandLineName => "controller";

		static StringField [] stringField = new [] { new StringField ("-name", "Name") };
		public override IEnumerable<ScaffolderField> Fields => stringField;
	}

	class MvcControllerWithActionsScaffolder : IScaffolder
	{
		public override string Name => "MVC Controller with read / write actions";
		public override string CommandLineName => "controller";
		public override string [] DefaultArgs => new [] { "--readWriteActions" };
		static StringField [] stringField = new [] { new StringField ("-name", "Name") };
		public override IEnumerable<ScaffolderField> Fields => stringField;
	}

	class EmptyApiControllerScaffolder : IScaffolder
	{
		public override string Name => "API Controller - Empty";
		public override string CommandLineName => "controller";
		public override string [] DefaultArgs => new [] { "--restWithNoViews" };
		static StringField [] stringField = new [] { new StringField ("-name", "Name") };
		public override IEnumerable<ScaffolderField> Fields => stringField;
	}

	class ApiControllerWithActionsScaffolder : IScaffolder
	{
		public override string Name => "API Controller with read / write actions";
		public override string CommandLineName => "controller";
		public override string [] DefaultArgs => new [] { "--restWithNoViews", "--readWriteActions" };
		static StringField [] stringField = new [] { new StringField ("-name", "Name") };
		public override IEnumerable<ScaffolderField> Fields => stringField;
	}

	class ApiControllerEntityFrameworkScaffolder : IScaffolder
	{
		public override string Name => "API Controller with actions using Entity Framework";
		public override string CommandLineName => "controller";

		public override IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class RazorPageScaffolder : IScaffolder
	{

		//		Generator Arguments:
		//  razorPageName : Name of the Razor Page
		//  templateName  : The template to use, supported view templates: 'Empty|Create|Edit|Delete|Details|List'

		//Generator Options:
		//  --model|-m                          : Model class to use
		//  --dataContext|-dc                   : DbContext class to use
		//  --referenceScriptLibraries|-scripts : Switch to specify whether to reference script libraries in the generated views
		//  --layout|-l                         : Custom Layout page to use
		//  --useDefaultLayout|-udl             : Switch to specify that default layout should be used for the views
		//  --force|-f                          : Use this option to overwrite existing files
		//  --relativeFolderPath|-outDir        : Specify the relative output folder path from project where the file needs to be generated, if not specified, file will be generated in the project folder
		//  --namespaceName|-namespace          : Specify the name of the namespace to use for the generated PageModel
		//  --partialView|-partial              : Generate a partial view, other layout options (-l and -udl) are ignored if this is specified
		//  --noPageModel|-npm                  : Switch to not generate a PageModel class for Empty template

		public override string Name => "Razor Page";
		public override string CommandLineName => "razorpage";

		static string [] viewTemplateOptions = new [] { "Empty", "Create", "Edit", "Delete", "Details", "List" };
		static ScaffolderField [] fields =
			new ScaffolderField[] {
				new StringField ("", "Name of the Razor Page"),
				new ComboField ("", "The template to use, supported view templates", viewTemplateOptions)
			 };

		public override IEnumerable<ScaffolderField> Fields => fields;
	}

	class RazorPageEntityFrameworkScaffolder : IScaffolder
	{
		public override string Name => "Razor Page using Entity Framework";
		public override string CommandLineName => "controller";

		public override IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class RazorPageEntityFrameworkCrudScaffolder : IScaffolder
	{
		public override string Name => "Razor Page using Entity Framework (CRUD)";
		public override string CommandLineName => "controller";

		public override IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class IdentityScaffolder : IScaffolder
	{
		public override string Name => "Identity";
		public override string CommandLineName => "controller";

		public override IEnumerable<ScaffolderField> Fields =>
			new [] { new StringField ("name", "Name") };
	}

	class LayoutScaffolder : IScaffolder
	{
		public override string Name => "Layout";
		public override string CommandLineName => "controller";

		public override IEnumerable<ScaffolderField> Fields =>
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
				var hbox = new HBox ();
				var label = new Label ();
                    
				switch (field) {
				case StringField s:
					var input = new TextEntry ();
					input.HeightRequest = 30;
					hbox.PackEnd (input);
					label.Font = label.Font.WithSize (15);
					label.Text = s.DisplayName;
					hbox.PackEnd (label);
					vbox.PackStart (hbox);
					input.Changed += (sender, args) => s.SelectedValue = input.Text;
					break;
				case ComboField comboField:
					var comboBox = new ComboBox ();

					foreach(var option in comboField.Options) {
						comboBox.Items.Add (option);
                    }

					comboBox.HeightRequest = 30;
					hbox.PackEnd (comboBox);
					label.Font = label.Font.WithSize (15);
					label.Text = comboField.DisplayName;
					hbox.PackEnd (label);
					vbox.PackStart (hbox);
					comboBox.SelectionChanged += (sender, args) => comboField.SelectedValue = comboBox.SelectedText;
					comboBox.SelectedIndex = 0;
					break;
				}
			}
			return vbox;
		}

		public override int GetHashCode ()
		{
			// Pages are used as dictionary keys in WizardDialog.cs
			return unchecked(
				base.GetHashCode () + 37 * Args.GetHashCode ()
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

		static Dictionary<ScaffolderArgs, ScaffolderTemplateConfigurePage> cachedPages
			= new Dictionary<ScaffolderArgs, ScaffolderTemplateConfigurePage> ();
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

		ScaffolderTemplateConfigurePage GetConfigurePage (ScaffolderArgs args)
		{
			// we want to return the same instance for the same args
			if (cachedPages.ContainsKey (args)) {
				return cachedPages [args];
			} else {
				var page = new ScaffolderTemplateConfigurePage (args);
				cachedPages.Add (args, page);
				return page;
			}
		}

		protected override Task<IWizardDialogPage> OnGoNext (CancellationToken token)
		{
			switch (CurrentPage) {
			case ScaffolderTemplateSelectPage _:
				IWizardDialogPage configPage = GetConfigurePage (args);
				return Task.FromResult (configPage);
			}
			return Task.FromException<IWizardDialogPage> (new InvalidOperationException ());
		}

		protected override Task<IWizardDialogPage> OnGoBack (CancellationToken token)
		{
			IWizardDialogPage firstPage = pages [0];
			return Task.FromResult (firstPage);
		}
	}

	class ScaffolderWizard : ScaffolderDialogController
	{
		static readonly ScaffolderArgs args = new ScaffolderArgs ();
		readonly DotNetProject project;
		readonly FilePath parentFolder;

		public ScaffolderWizard (DotNetProject project, FilePath parentFolder) : base ("Add New Scaffolded Item", StockIcons.Information, null, GetPages (), args)
		{
			this.DefaultPageSize = new Size (600, 500);

			var rightSideImage = new Xwt.ImageView (Image.FromResource ("aspnet-wizard-page.png"));
			var rightSideWidget = new FrameBox (rightSideImage);
			rightSideWidget.BackgroundColor = Styles.Wizard.PageBackgroundColor;
			this.RightSideWidget = new XwtControl (rightSideWidget);
			this.Completed += (sender, e) => Task.Run (() => OnCompletedAsync ());
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

			foreach (var field in args.Scaffolder.Fields) {
				argBuilder.Add (field.CommandLineName);
				argBuilder.Add (field.SelectedValue);
			}

			argBuilder.Add ("--no-build"); //TODO: when do we need to build?
			argBuilder.Add ("-outDir");
			argBuilder.AddQuoted (parentFolder);
			//TODO: does this apply to every scaffolder or just Controller?
			argBuilder.Add ("-namespace", project.GetDefaultNamespace (parentFolder.Combine ("file.cs")));

			foreach (var arg in args.Scaffolder.DefaultArgs) {
				argBuilder.Add (arg);
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
				true,
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
