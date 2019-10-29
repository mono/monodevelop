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
using Microsoft.CodeAnalysis.FindSymbols;
using MonoDevelop.Ide;

namespace MonoDevelop.AspNetCore.Scaffolding
{
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

		private IEnumerable<CommandLineArg> commandLineArgs;
		public override IEnumerable<CommandLineArg> DefaultArgs => commandLineArgs;

		public RazorPageScaffolder (ScaffolderArgs args)
		{
			this.args = args;
			var defaultNamespace = args.ParentFolder.Combine ("file.cs");

			commandLineArgs = base.DefaultArgs.Append (
	new CommandLineArg ("--namespaceName", args.Project.GetDefaultNamespace (defaultNamespace))
);

		}

		public override IEnumerable<ScaffolderField> Fields => GetFields ();

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
			var dbModelField = new ComboField ("--model", "Model class to use", dbModels.ToArray ());
			return fields.Append (dbContextField).Append (dbModelField);
		}
	}
}
