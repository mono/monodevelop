using System;

using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core;
using MonoDevelop.Deployment;
using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{
	public class MakefileGeneratorTool : IApplication
	{
		bool generateAutotools = true;
		string defaultConfig = null;
		string filename = null;

		public int Run (string [] arguments)
		{
			Console.WriteLine ("MonoDevelop Makefile generator");
			if (arguments.Length == 0) {
				ShowUsage ();
				return 0;
			}

			// Parse arguments
			foreach (string s in arguments) {
				if (s == "--simple-makefiles") {
					generateAutotools = false;
				} else if (s.StartsWith ("-d:")) {
					if (s.Length > 3)
						defaultConfig = s.Substring (3);
				} else if (s [0] == '-') {
					Console.WriteLine ("Error: Unknown option {0}", s);
					return 1;
				} else {
					if (filename != null) {
						Console.WriteLine ("Error: Filename already specified - {0}, another filename '{1}' cannot be specified.", filename, s);
						return 1;
					}

					filename = s;
				}
			}

			if (filename == null) {
				Console.WriteLine ("Error: Solution file not specified.");
				ShowUsage ();
				return 1;
			}

			Console.WriteLine ("Loading solution file {0}", filename);
			ConsoleProgressMonitor monitor = new ConsoleProgressMonitor ();
			CombineEntry centry = Services.ProjectService.ReadCombineEntry (filename, monitor);
			Combine combine = centry as Combine;
			if (combine == null) {
				Console.WriteLine ("Error: Makefile generation supported only for solutions.\n");
				return 1;
			}

			if (defaultConfig == null || !CheckValidConfig (combine, defaultConfig)) {
				Console.WriteLine ("\nInvalid configuration {0}. Valid configurations : ", defaultConfig);
				for (int i = 0; i < combine.Configurations.Count; i ++) {
					CombineConfiguration cc = (CombineConfiguration) combine.Configurations [i];
					Console.WriteLine ("\t{0}. {1}", i + 1, cc.Name);
				}

				int configCount = combine.Configurations.Count;
				int op = 0;
				do {
					Console.Write ("Select configuration : ");
					string s = Console.ReadLine ();
					if (s.Length == 0)
						return 1;
					if (int.TryParse (s, out op)) {
						if (op > 0 && op <= configCount)
							break;
					}
				} while (true);

				defaultConfig = combine.Configurations [op - 1].Name;

			}

			SolutionDeployer deployer = new SolutionDeployer (generateAutotools);
			if (deployer.HasGeneratedFiles (combine)) {
				string msg = GettextCatalog.GetString ( "{0} already exist for this solution.  Would you like to overwrite them? (Y/N)",
						generateAutotools ? "Autotools files" : "Makefiles" );
				bool op = false;
				do {
					Console.Write (msg);
					string line = Console.ReadLine ();
					if (line.Length == 0)
						return 1;

					if (line.Length == 1) {
						if (line [0] == 'Y' || line [0] == 'y')
							op = true;
						else if (line [0] == 'N' || line [0] == 'n')
							op = false;
						else
							continue;
					} else {
						if (String.Compare (line, "YES", true) == 0)
							op = true;
						else if (String.Compare (line, "NO", true) == 0)
							op = false;
						else
							continue;
					}
					break;
				} while (true);
				if (!op)
					return 0;
			}

			DeployContext ctx = new DeployContext (new TarballDeployTarget (), "Linux", null);
			try {
				deployer.GenerateFiles (ctx, combine, defaultConfig, monitor);
			}
			finally {
				ctx.Dispose ();
				monitor.Dispose ();
			}

			return 0;
		}

		void ShowUsage ()
		{
			Console.WriteLine ("generate-makefiles <solution-file> [--simple-makefiles] [-d:default-config]");
			Console.WriteLine ();
			Console.WriteLine ("Options");
			Console.WriteLine (" --simple-makefiles\n\tGenerates set of Makefiles with the most common targets, and a configuration script that does a basic check of package dependencies. Default is to generate Makefile structure based on Autotools with the standard targets and configuration scripts.");
			Console.WriteLine ();
			Console.WriteLine (" -d:default-config\n\tConfiguration that the Makefile will build by default. Other configurations can be selected via the '--config' or '--enable-*' option of the generated configure script.");
			Console.WriteLine ();
		}

		bool CheckValidConfig (Combine combine, string config)
		{
			foreach (IConfiguration iconf in combine.Configurations)
				if (String.Compare (iconf.Name, config, true) == 0)
					return true;

			return false;
		}

	}
}
