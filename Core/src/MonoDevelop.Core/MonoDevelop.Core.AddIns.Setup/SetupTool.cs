//
// mdsetup.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.AddIns.Setup;
using System.IO;
using System.Collections;

namespace MonoDevelop.Core.AddIns.Setup
{
	class SetupTool: IApplication
	{
		Hashtable options = new Hashtable ();
		string[] arguments;
			
		public int Run (string[] args)
		{
			Console.WriteLine ("MonoDevelop Add-In Setup Utility");
			
			if (args.Length == 0) {
				PrintHelp ();
				return 0;
			}
			
			string[] parms = new string [args.Length - 1];
			Array.Copy (args, 1, parms, 0, args.Length - 1);
			
			try {
				ReadOptions (parms);
				return RunCommand (args [0], parms);
			} catch (InstallException ex) {
				Console.WriteLine (ex.Message);
				return -1;
			}
		}
		
		public int RunCommand (string cmd, string[] parms)
		{
			switch (cmd) {
				case "install":
				case "i":
					Install (parms);
					break;
					
				case "uninstall":
				case "u":
					Uninstall (parms);
					break;
					
				case "update":
				case "up":
					Update (parms);
					break;
					
				case "list":
				case "l":
					ListInstalled (parms);
					break;
	
				case "list-av":
				case "la":
					ListAvailable (parms);
					break;
					
				case "list-update":
				case "lu":
					ListUpdates (parms);
					break;
					
				case "rep-add":
				case "ra":
					AddRepository (parms);
					break;
					
				case "rep-remove":
				case "rr":
					RemoveRepository (parms);
					break;
	
				case "rep-update":
				case "ru":
					UpdateAvailableAddins (parms);
					break;
	
				case "rep-list":
				case "rl":
					ListRepositories (parms);
					break;
	
	
				case "rep-build":
				case "rb":
					BuildRepository (parms);
					break;
					
				case "pack":
				case "p":
					BuildPackage (parms);
					break;
					
				case "dir-link":
					LinkDirectory (parms);
					break;
					
				case "dir-unlink":
					UnlinkDirectory (parms);
					break;
					
				case "dir-list":
					ListDirectories (parms);
					break;
				
				case "help":
				case "h":
					PrintHelp (parms);
					break;
					
				default:
					Console.WriteLine ("Unknown command: " + cmd);
					return 1;
			}
			
			return 0;
		}
		
		void Install (string[] args)
		{
			if (args.Length < 1) {
				PrintHelp ("install");
				return;
			}
			
			PackageCollection packs = new PackageCollection ();
			for (int n=0; n<args.Length; n++) {
				if (File.Exists (args [n])) { 
					packs.Add (AddinPackage.FromFile (args [n]));
				} else {
					string[] aname = args[n].Split ('/');
					AddinRepositoryEntry[] ads = Runtime.SetupService.GetAvailableAddin (aname[0], null);
					if (ads.Length == 0)
						throw new InstallException ("The addin '" + args[n] + "' is not available for install.");
					if (ads.Length > 1) {
						if (aname.Length < 2) {
							Console.WriteLine (args[n] + ": the addin version is required because there are several versions of the same addin available.");
							return;
						}
						ads = Runtime.SetupService.GetAvailableAddin (aname[0], aname[1]);
						if (ads.Length == 0)
							throw new InstallException ("The addin " + aname[0] + " v" + aname[1] + " is not available.");
					}
				
					packs.Add (AddinPackage.FromRepository (ads[0]));
				}
			}
			Install (packs);
		}
		
		void Install (PackageCollection packs)
		{
			PackageCollection toUninstall;
			PackageDependencyCollection unresolved;
			
			IProgressMonitor m = new ConsoleProgressMonitor ();
			int n = packs.Count;
			if (!Runtime.SetupService.ResolveDependencies (m, packs, out toUninstall, out unresolved))
				throw new InstallException ("Not all dependencies could be resolved.");

			bool ask = false;
			if (packs.Count != n || toUninstall.Count != 0) {
				Console.WriteLine ("The following packages will be installed:");
				foreach (Package p in packs)
					Console.WriteLine (" - " + p.Name);
				ask = true;
			}
			if (toUninstall.Count != 0) {
				Console.WriteLine ("The following packages need to be uninstalled:");
				foreach (Package p in toUninstall)
					Console.WriteLine (" - " + p.Name);
				ask = true;
			}
			if (ask) {
				Console.WriteLine ();
				Console.Write ("Are you sure you want to continue? (y/N): ");
				string res = Console.ReadLine ();
				if (res != "y" && res != "Y")
					return;
			}
			
			if (!Runtime.SetupService.Install (m, packs)) {
				Console.WriteLine ("Install operation failed.");
			}
		}
		
		void Uninstall (string[] args)
		{
			if (args.Length < 1)
				throw new InstallException ("The addin id is required.");
			
			AddinSetupInfo ads = Runtime.SetupService.GetInstalledAddin (args[0]);
			if (ads == null)
				throw new InstallException ("The addin '" + args[0] + "' is not installed.");
			
			Console.WriteLine ("The following add-ins will be uninstalled:");
			Console.WriteLine (" - " + ads.Addin.Name);
			foreach (AddinSetupInfo si in Runtime.SetupService.GetDependentAddins (args[0], true))
				Console.WriteLine (" - " + si.Addin.Name);
			
			Console.WriteLine ();
			Console.Write ("Are you sure you want to continue? (y/N): ");
			string res = Console.ReadLine ();
			if (res == "y" || res == "Y")
				Runtime.SetupService.Uninstall (new ConsoleProgressMonitor (), ads.Addin);
		}
		
		void ListInstalled (string[] args)
		{
			Console.WriteLine ("Installed add-ins:");
			AddinSetupInfo[] addins = Runtime.SetupService.GetInstalledAddins ();
			foreach (AddinSetupInfo addin in addins) {
				Console.WriteLine (" - " + addin.Addin.Id + " " + addin.Addin.Version);
			}
		}
		
		void ListAvailable (string[] args)
		{
			Console.WriteLine ("Available add-ins:");
			AddinRepositoryEntry[] addins = Runtime.SetupService.GetAvailableAddins ();
			foreach (AddinRepositoryEntry addin in addins) {
				Console.WriteLine (" - " + addin.Addin.Id + " " + addin.Addin.Version + " (" + addin.Repository.Name + ")");
			}
		}
		
		void ListUpdates (string[] args)
		{
			Console.WriteLine ("Looking for updates...");
			Runtime.SetupService.UpdateRepositories (new NullProgressMonitor ());
			Console.WriteLine ("Available add-in updates:");
			AddinRepositoryEntry[] addins = Runtime.SetupService.GetAvailableAddins ();
			bool found = false;
			foreach (AddinRepositoryEntry addin in addins) {
				AddinSetupInfo sinfo = Runtime.SetupService.GetInstalledAddin (addin.Addin.Id);
				if (sinfo != null && AddinInfo.CompareVersions (sinfo.Addin.Version, addin.Addin.Version) == 1) {
					Console.WriteLine (" - " + addin.Addin.Id + " " + addin.Addin.Version + " (" + addin.Repository.Name + ")");
					found = true;
				}
			}
			if (!found)
				Console.WriteLine ("No updates found.");
		}
		
		void Update (string [] args)
		{
			Console.WriteLine ("Looking for updates...");
			Runtime.SetupService.UpdateRepositories (new NullProgressMonitor ());
			
			PackageCollection packs = new PackageCollection ();
			AddinRepositoryEntry[] addins = Runtime.SetupService.GetAvailableAddins ();
			foreach (AddinRepositoryEntry addin in addins) {
				AddinSetupInfo sinfo = Runtime.SetupService.GetInstalledAddin (addin.Addin.Id);
				if (sinfo != null && AddinInfo.CompareVersions (sinfo.Addin.Version, addin.Addin.Version) == 1)
					packs.Add (AddinPackage.FromRepository (addin));
			}
			if (packs.Count > 0)
				Install (packs);
			else
				Console.WriteLine ("No updates found.");
		}
		
		void UpdateAvailableAddins (string[] args)
		{
			Runtime.SetupService.UpdateRepositories (new ConsoleProgressMonitor ());
		}
		
		void AddRepository (string[] args)
		{
			foreach (string rep in args)
				Runtime.SetupService.RegisterRepository (new ConsoleProgressMonitor (), rep);
		}
		
		void RemoveRepository (string[] args)
		{
			foreach (string rep in args)
				Runtime.SetupService.UnregisterRepository (rep);
		}
		
		void ListRepositories (string[] args)
		{
			RepositoryRecord[] reps = Runtime.SetupService.GetRepositories ();
			if (reps.Length == 0) {
				Console.WriteLine ("No repositories have been registered.");
				return;
			}
			Console.WriteLine ("Registered repositories:");
			foreach (RepositoryRecord rep in reps) {
				Console.WriteLine (" - " + rep.Title);
			}
		}
		
		void BuildRepository (string[] args)
		{
			if (args.Length < 1)
				throw new InstallException ("A directory name is required.");
			Runtime.SetupService.BuildRepository (new ConsoleProgressMonitor (), args[0]);
		}
		
		void BuildPackage (string[] args)
		{
			if (args.Length < 1)
				throw new InstallException ("A file name is required.");
				
			Runtime.SetupService.BuildPackage (new ConsoleProgressMonitor (), GetOption ("d", "."), GetArguments ());
		}
		
		void LinkDirectory (string[] args)
		{
			if (args.Length < 1)
				throw new InstallException ("A path is required.");
				
			try {
				Runtime.SetupService.AddAddinPath (args[0]);
				Console.WriteLine ("Directory '{0}' registered.", args [0]);
			} catch (System.UnauthorizedAccessException) {
				throw new InstallException ("The directory can't be registered with the current user permissions.");
			}
		}

		void UnlinkDirectory (string[] args)
		{
			if (args.Length < 1)
				throw new InstallException ("A path is required.");
			try {
				Runtime.SetupService.RemoveAddinPath (args[0]);
				Console.WriteLine ("Directory '{0}' removed.", args [0]);
			} catch (System.UnauthorizedAccessException) {
				throw new InstallException ("The directory can't be unregistered with the current user permissions.");
			}
		}
		
		void ListDirectories (string[] args)
		{
			Console.WriteLine ("Registered add-in directories:");
			foreach (string dir in Runtime.SetupService.GetAddinPaths ()) {
				Console.WriteLine (dir);
			}
		}
		
		string[] GetArguments ()
		{
			return arguments;
		}
		
		string GetOption (string key, string defValue)
		{
			object val = options [key];
			if (val == null || val == (object) this)
				return defValue;
			else
				return (string) val;
		}
		
		void ReadOptions (string[] args)
		{
			options = new Hashtable ();
			ArrayList list = new ArrayList ();
			
			foreach (string arg in args) {
				if (arg.StartsWith ("-")) {
					int i = arg.IndexOf (':');
					if (i == -1)
						options [arg.Substring (1)] = this;
					else
						options [arg.Substring (1, i-1)] = arg.Substring (i+1);
				} else
					list.Add (arg);
			}
			
			arguments = (string[]) list.ToArray (typeof(string));
		}
		
		void PrintHelp (params string[] parms)
		{
			if (parms.Length == 0) {
				Console.WriteLine ();
				Console.WriteLine ("Add-in Commands:");
				Console.WriteLine ("  install (i)      Installs add-ins");
				Console.WriteLine ("  uninstall (u)    Unistalls add-ins");
				Console.WriteLine ("  update (up)      Updates installed add-ins");
				Console.WriteLine ("  list (l)         Lists installed add-ins");
				Console.WriteLine ("  list-av (la)     Lists add-ins available in registered repositories");
				Console.WriteLine ("  list-update (lu) Lists available add-in updates in registered repositories");
				Console.WriteLine ("  dir-link         Adds a directory to the add-in search path list");
				Console.WriteLine ("  dir-unlink       Removes a directory from the add-in search path list");
				Console.WriteLine ("  dir-list         Lists all directories in the add-in search path list");
				Console.WriteLine ();
				Console.WriteLine ("Repository Commands:");
				Console.WriteLine ("  rep-add (ra)     Registers repositories");
				Console.WriteLine ("  rep-remove (rr)  Unregisters repositories");
				Console.WriteLine ("  rep-list (rl)    Lists registered repositories");
				Console.WriteLine ("  rep-update (ru)  Updates the lists of addins available in repositories");
				Console.WriteLine ();
				Console.WriteLine ("Packaging Commands:");
				Console.WriteLine ("  rep-build (rb)   Creates a repository index file for a directory structure");
				Console.WriteLine ("  pack (p)         Creates a package from an add-in configuration file");
				Console.WriteLine ();
				Console.WriteLine ("Run 'setup help <command>' to get help about a specific command.");
				Console.WriteLine ();
				return;
			}
			
			Console.WriteLine ();
			switch (parms[0]) {
				case "install":
				case "i":
					Console.WriteLine ("install (i): Installs add-ins.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup install [package-name|package-file] ...");
					Console.WriteLine ();
					Console.WriteLine ("Installs an add-in or set of addins. The command argument is a list");
					Console.WriteLine ("of files and/or package names. If a package name is provided");
					Console.WriteLine ("the package will be looked out in the registered repositories.");
					Console.WriteLine ("A specific add-in version can be specified by appending it to.");
					Console.WriteLine ("the package name using '/' as a separator, like in this example:");
					Console.WriteLine ("MonoDevelop.SourceEditor/0.9.1");
					break;
					
				case "uninstall":
				case "u":
					Console.WriteLine ("uninstall (u): Uninstalls add-ins.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup uninstall <package-name>");
					Console.WriteLine ();
					Console.WriteLine ("Uninstalls an add-in. The command argument is the name");
					Console.WriteLine ("of the add-in to uninstall.");
					break;
					
				case "update":
				case "up":
					Console.WriteLine ("update (up): Updates installed add-ins.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup update");
					Console.WriteLine ();
					Console.WriteLine ("Downloads and installs available updates for installed add-ins.");
					break;
				
				case "list":
				case "l":
					Console.WriteLine ("list (l): Lists installed add-ins.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup list");
					Console.WriteLine ();
					Console.WriteLine ("Prints a list of all installed add-ins.");
					break;
	
				case "list-av":
				case "la":
					Console.WriteLine ("list-av (la): Lists add-ins available in registered repositories.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup list-av");
					Console.WriteLine ();
					Console.WriteLine ("Prints a list of add-ins available to install in the");
					Console.WriteLine ("registered repositories.");
					break;
					
				case "list-update":
				case "lu":
					Console.WriteLine ("list-update (lu): Lists available add-in updates.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup list-lu");
					Console.WriteLine ();
					Console.WriteLine ("Prints a list of available add-in updates in the registered repositories.");
					break;
					
				case "rep-add":
				case "ra":
					Console.WriteLine ("rep-add (ra): Registers repositories.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup rep-add <url> ...");
					Console.WriteLine ();
					Console.WriteLine ("Registers an add-in repository. Several URLs can be provided.");
					break;
					
				case "rep-remove":
				case "rr":
					Console.WriteLine ("rep-remove (rr): Unregisters repositories.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup rep-remove <url> ...");
					Console.WriteLine ();
					Console.WriteLine ("Unregisters an add-in repository. Several URLs can be provided.");
					break;
	
				case "rep-update":
				case "ru":
					Console.WriteLine ("rep-update (ru): Updates the lists of available addins.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup rep-update");
					Console.WriteLine ();
					Console.WriteLine ("Updates the lists of addins available in all registered repositories.");
					break;
	
				case "rep-list":
				case "rl":
					Console.WriteLine ("rep-list (ru): Lists registered repositories.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup rep-list");
					Console.WriteLine ();
					Console.WriteLine ("Shows a list of all registered repositories.");
					break;
	
				case "rep-build":
				case "rb":
					Console.WriteLine ("rep-build (rb): Creates a repository index file for a directory structure.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup rep-build <path>");
					Console.WriteLine ();
					Console.WriteLine ("Scans the provided directory and generates a set of index files with entries");
					Console.WriteLine ("for all add-in packages found in the directory tree. The resulting file");
					Console.WriteLine ("structure is an add-in repository that can be published in a web site or a");
					Console.WriteLine ("shared directory.");
					break;
					
				case "pack":
				case "p":
					Console.WriteLine ("pack (p): Creates a package from an add-in configuration file.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup pack <file-path>");
					Console.WriteLine ();
					Console.WriteLine ("Creates an add-in package (.mpack file) which includes all files ");
					Console.WriteLine ("needed to deploy an add-in. The command parameter is the path to");
					Console.WriteLine ("the add-in's configuration file.");
					break;
					
				case "dir-link":
					Console.WriteLine ("dir-link: Adds a directory to the add-in search path list.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup link-dir <directory-path>");
					Console.WriteLine ();
					Console.WriteLine ("Adds a directory to the add-in search path. All add-ins in");
					Console.WriteLine ("the directory and subdirectories are automatically installed.");
					break;
					
				case "dir-unlink":
					Console.WriteLine ("dir-unlink: Removes a directory from the add-in search path list.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup unlink-dir <directory-path>");
					Console.WriteLine ();
					Console.WriteLine ("Removes a directory from the add-in search path. All add-ins in");
					Console.WriteLine ("the directory and subdirectories are automatically uninstalled.");
					break;
					
				case "dir-list":
					Console.WriteLine ("dir-list: Lists all directories in the add-in search path list.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup dir-list");
					Console.WriteLine ();
					Console.WriteLine ("Lists all directories in the add-in search path list. Directories");
					Console.WriteLine ("can be added using the dir-link command and removed with dir-unlink.");
					break;
				
				case "help":
				case "h":
					Console.WriteLine ("help: Shows help about a command.");
					Console.WriteLine ();
					Console.WriteLine ("Usage: setup help <command>");
					Console.WriteLine ();
					break;
					
				default:
					Console.WriteLine ("Unknown command: " + parms[0]);
					break;
			}
			Console.WriteLine ();
		} 
	}
}

