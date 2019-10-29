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
using Microsoft.CodeAnalysis.FindSymbols;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.DotNetCore.GlobalTools;
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

		public DotNetProject Project { get; internal set; }
		public FilePath ParentFolder { get; internal set; }
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
		public virtual IEnumerable<CommandLineArg> DefaultArgs => Enumerable.Empty<CommandLineArg> ();
		public virtual IEnumerable<ScaffolderField> Fields { get; }
	}

	class CommandLineArg
	{
		public string CommandLineName { get; }
		public string Value { get; }

		public CommandLineArg (string commandLineName)
		{
			CommandLineName = commandLineName;
		}

		public CommandLineArg (string commandLineName, string value)
		{
			CommandLineName = commandLineName;
			Value = value;
		}

		public override string ToString ()
		{
			return $"{CommandLineName} {Value}";
		}
	}

	abstract class ControllerScaffolder : IScaffolder
	{
		protected static StringField [] stringField = new [] { new StringField ("-name", "Name") };

		public override string CommandLineName => "controller";

		private IEnumerable<CommandLineArg> commandLineArgs;
		public override IEnumerable<CommandLineArg> DefaultArgs => commandLineArgs;

		public ControllerScaffolder (ScaffolderArgs args) : this (args, null)
		{
		}

		public ControllerScaffolder (ScaffolderArgs args, string controllerTypeArgument)
		{
			var defaultNamespace = args.ParentFolder.Combine ("file.cs");
			commandLineArgs = base.DefaultArgs.Append(
				new CommandLineArg ("-namespace", args.Project.GetDefaultNamespace (defaultNamespace))
			);

			if (controllerTypeArgument != null)
				commandLineArgs.Append (new CommandLineArg (controllerTypeArgument));

		}
	}

	class EmptyMvcControllerScaffolder : ControllerScaffolder
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

		public EmptyMvcControllerScaffolder (ScaffolderArgs args) : base (args) { }

		public override IEnumerable<ScaffolderField> Fields => stringField;
	}


	class MvcControllerWithActionsScaffolder : ControllerScaffolder
	{
		public override string Name => "MVC Controller with read / write actions";
		public override string CommandLineName => "controller";
		public override IEnumerable<CommandLineArg> DefaultArgs => new [] { new CommandLineArg ("--readWriteActions") };

		public MvcControllerWithActionsScaffolder (ScaffolderArgs args) : base (args) { }

		public override IEnumerable<ScaffolderField> Fields => stringField;
	}

	class EmptyApiControllerScaffolder : ControllerScaffolder
	{
		public override string Name => "API Controller - Empty";
		public override string CommandLineName => "controller";
		public override IEnumerable<CommandLineArg> DefaultArgs => new [] { new CommandLineArg ("--restWithNoViews") };

		public EmptyApiControllerScaffolder (ScaffolderArgs args) : base (args) { }

		public override IEnumerable<ScaffolderField> Fields => stringField;
	}

	class ApiControllerWithActionsScaffolder : ControllerScaffolder
	{
		public override string Name => "API Controller with read / write actions";
		public override string CommandLineName => "controller";
		public override IEnumerable<CommandLineArg> DefaultArgs => new [] { new CommandLineArg ("--restWithNoViews", "--readWriteActions") };

		public ApiControllerWithActionsScaffolder (ScaffolderArgs args) : base (args) { }

		public override IEnumerable<ScaffolderField> Fields => stringField;
	}

	class ApiControllerEntityFrameworkScaffolder : ControllerScaffolder
	{
		public ApiControllerEntityFrameworkScaffolder (ScaffolderArgs args) : base (args) { }

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

        const string DbContextTypeName = "System.Data.Entity.DbContext";
        const string EF7DbContextTypeName = "Microsoft.Data.Entity.DbContext";
		const string EFCDbContextTypeName = "Microsoft.EntityFrameworkCore.DbContext";

		readonly ScaffolderArgs args;

		public override string Name => "Razor Page";
		public override string CommandLineName => "razorpage";

		static string [] viewTemplateOptions = new [] { "Empty", "Create", "Edit", "Delete", "Details", "List" };
		static ScaffolderField [] fields =
			new ScaffolderField [] {
				new StringField ("", "Name of the Razor Page"),
				new ComboField ("", "The template to use, supported view templates", viewTemplateOptions)
			 };

		public RazorPageScaffolder(ScaffolderArgs args)
		{
			this.args = args;
		}

		public override IEnumerable<ScaffolderField> Fields => GetFields();

		private IEnumerable<string> GetDbContextClasses ()
		{
			//TODO: make async
			var compilation = IdeApp.TypeSystemService.GetCompilationAsync (args.Project).Result;
			var dbContext = compilation.GetTypeByMetadataName (EFCDbContextTypeName)
						 ?? compilation.GetTypeByMetadataName (DbContextTypeName)
						 ?? compilation.GetTypeByMetadataName (EF7DbContextTypeName);


			if (dbContext != null) {
				var s = SymbolFinder.FindDerivedClassesAsync (dbContext, IdeApp.TypeSystemService.Workspace.CurrentSolution).Result;
				return s.Select (c => c.MetadataName);
			}
			return Enumerable.Empty<string> ();
		}

		private IEnumerable<string> GetModelClasses ()
		{
			//TODO: make async
			var compilation = IdeApp.TypeSystemService.GetCompilationAsync (args.Project).Result;
			var modelTypes = DbSetModelVisitor.FindModelTypes (compilation.Assembly);
			return modelTypes.Select (t => t.MetadataName);
		}

		private IEnumerable<ScaffolderField> GetFields ()
		{
			var dbContexts = GetDbContextClasses ();
			var dbContextField = new ComboField ("--dataContext", "DBContext class to use", dbContexts.ToArray ());
			var dbModels = GetModelClasses ();
			var dbModelField = new ComboField ("--model", "Model class to use", dbModels.ToArray());
			return fields.Append(dbContextField).Append(dbModelField);
		}
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

					foreach (var option in comboField.Options) {
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

	class ScaffolderDialogController : WizardDialogControllerBase
	{
		// We have 2 pages, the first contains a list of templates
		// and the 2nd is an entry form based on the selection
		// in the first page.
		readonly ScaffolderArgs args;

		static Dictionary<ScaffolderArgs, ScaffolderTemplateConfigurePage> cachedPages
			= new Dictionary<ScaffolderArgs, ScaffolderTemplateConfigurePage> ();

		public override bool CanGoBack {
			get {
				return CurrentPage is ScaffolderTemplateConfigurePage;
			}
		}

		public override bool CurrentPageIsLast {
			get { return CanGoBack; }
		}

		public ScaffolderDialogController (string title, Image icon, Control rightSideWidget, ScaffolderArgs args)
			: base (title, icon, null, new ScaffolderTemplateSelectPage(args))
		{
			this.args = args;
		}

		ScaffolderTemplateConfigurePage GetConfigurePage (ScaffolderArgs args)
		{
			var page2 = new ScaffolderTemplateConfigurePage (args);
			return page2;
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
			IWizardDialogPage firstPage = new ScaffolderTemplateSelectPage (args); //TODO: we should return the same instance
			return Task.FromResult (firstPage);
		}
	}

	class ScaffolderWizard : ScaffolderDialogController
	{
		static readonly ScaffolderArgs args = new ScaffolderArgs ();
		readonly DotNetProject project;
		readonly FilePath parentFolder;

		public ScaffolderWizard (DotNetProject project, FilePath parentFolder) : base ("Add New Scaffolded Item", StockIcons.Information, null, args)
		{
			this.DefaultPageSize = new Size (600, 500);

			var rightSideImage = new Xwt.ImageView (Image.FromResource ("aspnet-wizard-page.png"));
			var rightSideWidget = new FrameBox (rightSideImage);
			rightSideWidget.BackgroundColor = Styles.Wizard.PageBackgroundColor;
			this.RightSideWidget = new XwtControl (rightSideWidget);
			this.Completed += (sender, e) => Task.Run (() => OnCompletedAsync ());
			this.project = project;
			this.parentFolder = parentFolder;
			args.Project = project;
			args.ParentFolder = parentFolder;

		}

		const string toolName = "dotnet-aspnet-codegenerator";

		async Task OnCompletedAsync ()
		{
			using var progressMonitor = CreateProgressMonitor ();

			// Install the tool
			if (!DotNetCoreGlobalToolManager.IsInstalled (toolName)) {
				await DotNetCoreGlobalToolManager.Install (toolName, progressMonitor.CancellationToken);
			}

			// Run the tool
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

			foreach (var arg in args.Scaffolder.DefaultArgs) {
				argBuilder.Add (arg.ToString ());
			}

			var commandLineArgs = argBuilder.ToString ();

			var msg = $"Running {dotnet} {commandLineArgs}\n";
			progressMonitor.Console.Debug (0, "", msg);

			try {
				var process = Runtime.ProcessService.StartConsoleProcess (
					dotnet,
					commandLineArgs,
					parentFolder,
					progressMonitor.Console
				);

				await process.Task;
			} catch (Exception ex) {
				await progressMonitor.Log.WriteLineAsync (ex.Message);
				LoggingService.LogError ($"Failed to run {dotnet} {commandLineArgs}", ex);
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
	}
}
