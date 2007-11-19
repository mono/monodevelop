//
// mdsetup.cs
//
// Author:
//   Lluis Sanchez Gual
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
using System.Collections;
using Mono.Addins;
using Mono.Addins.Setup.ProgressMonitoring;
using Mono.Addins.Setup;
using System.IO;
using Mono.Addins.Description;

namespace Mono.Addins.Setup
{
	public class SetupTool
	{
		Hashtable options = new Hashtable ();
		string[] arguments;
		string applicationName = "Mono";
		SetupService service;
		AddinRegistry registry;
		ArrayList commands = new ArrayList ();
		string setupAppName = "";
		
		bool verbose;
		
		public SetupTool (AddinRegistry registry)
		{
			this.registry = registry;
			service = new SetupService (registry);
			CreateCommands ();
		}
		
		public string ApplicationName {
			get { return applicationName; }
			set { applicationName = value; }
		}
		
		public string ApplicationNamespace {
			get { return service.ApplicationNamespace; }
			set { service.ApplicationNamespace = value; }
		}
		
		public bool VerboseOutput {
			get { return verbose; }
			set { verbose = value; }
		}

		public int Run (string[] args, int firstArgumentIndex)
		{
			string[] aa = new string [args.Length - firstArgumentIndex];
			Array.Copy (args, firstArgumentIndex, aa, 0, aa.Length);
			return Run (aa);
		}
		
		public int Run (string[] args)
		{
			if (args.Length == 0) {
				PrintHelp ();
				return 0;
			}
			
			string[] parms = new string [args.Length - 1];
			Array.Copy (args, 1, parms, 0, args.Length - 1);
			
			try {
				ReadOptions (parms);
				verbose = verbose || HasOption ("v");
				return RunCommand (args [0], parms);
			} catch (InstallException ex) {
				Console.WriteLine (ex.Message);
				return -1;
			}
		}
		
		int RunCommand (string cmd, string[] parms)
		{
			SetupCommand cc = FindCommand (cmd);
			if (cc != null) {
				cc.Handler (parms);
				return 0;
			}
			else {
				Console.WriteLine ("Unknown command: " + cmd);
				return 1;
			}
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
					string aname = Addin.GetIdName (GetFullId (args[n]));
					string aversion = Addin.GetIdVersion (args[n]);
					if (aversion.Length == 0) aversion = null;
					
					AddinRepositoryEntry[] ads = service.Repositories.GetAvailableAddin (aname, aversion);
					if (ads.Length == 0)
						throw new InstallException ("The addin '" + args[n] + "' is not available for install.");
					packs.Add (AddinPackage.FromRepository (ads[ads.Length-1]));
				}
			}
			Install (packs, true);
		}
		
		void CheckInstall (string[] args)
		{
			if (args.Length < 1) {
				PrintHelp ("check-install");
				return;
			}
			
			PackageCollection packs = new PackageCollection ();
			for (int n=0; n<args.Length; n++) {
				Addin addin = registry.GetAddin (GetFullId (args[n]));
				if (addin != null)
					continue;
				string aname = Addin.GetIdName (GetFullId (args[n]));
				string aversion = Addin.GetIdVersion (args[n]);
				if (aversion.Length == 0) aversion = null;
				
				AddinRepositoryEntry[] ads = service.Repositories.GetAvailableAddin (aname, aversion);
				if (ads.Length == 0)
					throw new InstallException ("The addin '" + args[n] + "' is not available for install.");
				packs.Add (AddinPackage.FromRepository (ads[ads.Length-1]));
			}
			Install (packs, false);
		}
		
		void Install (PackageCollection packs, bool prompt)
		{
			PackageCollection toUninstall;
			DependencyCollection unresolved;
			
			IProgressStatus m = new ConsoleProgressStatus (verbose);
			int n = packs.Count;
			if (!service.Store.ResolveDependencies (m, packs, out toUninstall, out unresolved))
				throw new InstallException ("Not all dependencies could be resolved.");

			bool ask = false;
			if (prompt && (packs.Count != n || toUninstall.Count != 0)) {
				Console.WriteLine ("The following packages will be installed:");
				foreach (Package p in packs)
					Console.WriteLine (" - " + p.Name);
				ask = true;
			}
			if (prompt && (toUninstall.Count != 0)) {
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
			
			if (!service.Store.Install (m, packs)) {
				Console.WriteLine ("Install operation failed.");
			}
		}
		
		void Uninstall (string[] args)
		{
			if (args.Length < 1)
				throw new InstallException ("The add-in id is required.");
			
			Addin ads = registry.GetAddin (GetFullId (args[0]));
			if (ads == null)
				throw new InstallException ("The add-in '" + args[0] + "' is not installed.");
			
			Console.WriteLine ("The following add-ins will be uninstalled:");
			Console.WriteLine (" - " + ads.Description.Name);
			foreach (Addin si in service.GetDependentAddins (args[0], true))
				Console.WriteLine (" - " + si.Description.Name);
			
			Console.WriteLine ();
			Console.Write ("Are you sure you want to continue? (y/N): ");
			string res = Console.ReadLine ();
			if (res == "y" || res == "Y")
				service.Uninstall (new ConsoleProgressStatus (verbose), ads.Id);
		}
		
		bool IsHidden (Addin ainfo)
		{
			return service.ApplicationNamespace != null && !(ainfo.Namespace + ".").StartsWith (service.ApplicationNamespace + ".");
		}
		
		bool IsHidden (AddinHeader ainfo)
		{
			return service.ApplicationNamespace != null && !(ainfo.Namespace + ".").StartsWith (service.ApplicationNamespace + ".");
		}
		
		string GetId (AddinHeader ainfo)
		{
			if (service.ApplicationNamespace != null && (ainfo.Namespace + ".").StartsWith (service.ApplicationNamespace + "."))
				return ainfo.Id.Substring (service.ApplicationNamespace.Length + 1);
			else
				return ainfo.Id;
		}
		
		string GetFullId (string id)
		{
			if (service.ApplicationNamespace != null)
				return service.ApplicationNamespace + "." + id;
			else
				return id;
		}
		
		void ListInstalled (string[] args)
		{
			IList alist = args;
			bool showAll = alist.Contains ("-a");
			Console.WriteLine ("Installed add-ins:");
			ArrayList list = new ArrayList ();
			list.AddRange (registry.GetAddins ());
			if (alist.Contains ("-r"))
				list.AddRange (registry.GetAddinRoots ());
			foreach (Addin addin in list) {
				if (!showAll && IsHidden (addin))
					continue;
				Console.Write (" - " + addin.Name + " " + addin.Version);
				if (showAll)
					Console.Write (" (" + addin.AddinFile + ")");
				Console.WriteLine ();
			}
		}
		
		void ListAvailable (string[] args)
		{
			bool showAll = args.Length > 0 && args [0] == "-a";
			Console.WriteLine ("Available add-ins:");
			AddinRepositoryEntry[] addins = service.Repositories.GetAvailableAddins ();
			foreach (PackageRepositoryEntry addin in addins) {
				if (!showAll && IsHidden (addin.Addin))
					continue;
				Console.WriteLine (" - " + GetId (addin.Addin) + " (" + addin.Repository.Name + ")");
			}
		}
		
		void ListUpdates (string[] args)
		{
			bool showAll = args.Length > 0 && args [0] == "-a";
			
			Console.WriteLine ("Looking for updates...");
			service.Repositories.UpdateAllRepositories (null);
			Console.WriteLine ("Available add-in updates:");
			AddinRepositoryEntry[] addins = service.Repositories.GetAvailableAddins ();
			bool found = false;
			foreach (PackageRepositoryEntry addin in addins) {
				Addin sinfo = registry.GetAddin (addin.Addin.Id);
				if (!showAll && IsHidden (sinfo))
					continue;
				if (sinfo != null && Addin.CompareVersions (sinfo.Version, addin.Addin.Version) == 1) {
					Console.WriteLine (" - " + addin.Addin.Id + " " + addin.Addin.Version + " (" + addin.Repository.Name + ")");
					found = true;
				}
			}
			if (!found)
				Console.WriteLine ("No updates found.");
		}
		
		void Update (string [] args)
		{
			bool showAll = args.Length > 0 && args [0] == "-a";
			
			Console.WriteLine ("Looking for updates...");
			service.Repositories.UpdateAllRepositories (null);
			
			PackageCollection packs = new PackageCollection ();
			AddinRepositoryEntry[] addins = service.Repositories.GetAvailableAddins ();
			foreach (PackageRepositoryEntry addin in addins) {
				Addin sinfo = registry.GetAddin (addin.Addin.Id);
				if (!showAll && IsHidden (sinfo))
					continue;
				if (sinfo != null && Addin.CompareVersions (sinfo.Version, addin.Addin.Version) == 1)
					packs.Add (AddinPackage.FromRepository (addin));
			}
			if (packs.Count > 0)
				Install (packs, true);
			else
				Console.WriteLine ("No updates found.");
		}
		
		void UpdateAvailableAddins (string[] args)
		{
			service.Repositories.UpdateAllRepositories (new ConsoleProgressStatus (verbose));
		}
		
		void AddRepository (string[] args)
		{
			foreach (string rep in args)
				service.Repositories.RegisterRepository (new ConsoleProgressStatus (verbose), rep);
		}
		
		void RemoveRepository (string[] args)
		{
			foreach (string rep in args)
				service.Repositories.RemoveRepository (rep);
		}
		
		void ListRepositories (string[] args)
		{
			AddinRepository[] reps = service.Repositories.GetRepositories ();
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
			service.BuildRepository (new ConsoleProgressStatus (verbose), args[0]);
		}
		
		void BuildPackage (string[] args)
		{
			if (args.Length < 1)
				throw new InstallException ("A file name is required.");
				
			service.BuildPackage (new ConsoleProgressStatus (verbose), GetOption ("d", "."), GetArguments ());
		}
		
		void UpdateRegistry (string[] args)
		{
			registry.Update (new ConsoleProgressStatus (verbose));
		}
		
		void RepairRegistry (string[] args)
		{
			registry.Rebuild (new ConsoleProgressStatus (verbose));
		}
		
		void DumpRegistryFile (string[] args)
		{
			if (args.Length < 1)
				throw new InstallException ("A file name is required.");
			registry.DumpFile (args[0]);
		}
		
		void PrintAddinInfo (string[] args)
		{
			if (args.Length == 0)
				throw new InstallException ("A file name or add-in ID is required.");
			
			AddinDescription desc = null;
			if (File.Exists (args[0]))
				desc = registry.GetAddinDescription (new Mono.Addins.ConsoleProgressStatus (verbose), args[0]);
			else {
				Addin addin = registry.GetAddin (args [0]);
				if (addin != null)
					desc = addin.Description;
			}
			
			if (desc == null)
				throw new InstallException ("Add-in not found.");
			
			Console.WriteLine ();
			Console.WriteLine ("Addin Header");
			Console.WriteLine ("------------");
			Console.WriteLine ();
			Console.WriteLine ("Name:      " + desc.Name);
			Console.WriteLine ("Id:        " + desc.LocalId);
			if (desc.Namespace.Length > 0)
				Console.WriteLine ("Namespace: " + desc.Namespace);

			Console.Write ("Version:   " + desc.Version);
			if (desc.CompatVersion.Length > 0)
				Console.WriteLine (" (compatible with: " + desc.CompatVersion + ")");
			else
				Console.WriteLine ();
			
			if (desc.AddinFile.Length > 0)
				Console.WriteLine ("File:      " + desc.AddinFile);
			if (desc.Author.Length > 0)
				Console.WriteLine ("Author:    " + desc.Author);
			if (desc.Category.Length > 0)
				Console.WriteLine ("Category:  " + desc.Category);
			if (desc.Copyright.Length > 0)
				Console.WriteLine ("Copyright: " + desc.Copyright);
			if (desc.Url.Length > 0)
				Console.WriteLine ("Url:       " + desc.Url);

			if (desc.Description.Length > 0) {
				Console.WriteLine ();
				Console.WriteLine ("Description: \n" + desc.Description);
			}
			
			if (desc.ExtensionPoints.Count > 0) {
				Console.WriteLine ();
				Console.WriteLine ("Extenstion Points");
				Console.WriteLine ("-----------------");
				foreach (ExtensionPoint ep in desc.ExtensionPoints)
					PrintExtensionPoint (desc, ep);
			}
		}
		
		void PrintExtensionPoint (AddinDescription desc, ExtensionPoint ep)
		{
			Console.WriteLine ();
			Console.WriteLine ("* Extension Point: " + ep.Path);
			if (ep.Description.Length > 0)
				Console.WriteLine (ep.Description);
			
			ArrayList list = new ArrayList ();
			Hashtable visited = new Hashtable ();
			
			Console.WriteLine ();
			Console.WriteLine ("  Extension nodes:");
			GetNodes (desc, ep.NodeSet, list, new Hashtable ());
			
			foreach (ExtensionNodeType nt in list)
				Console.WriteLine ("   - " + nt.Id + ": " + nt.Description);
			
			Console.WriteLine ();
			Console.WriteLine ("  Node description:");
			
			string sind = "    ";
			
			for (int n=0; n<list.Count; n++) {
				
				ExtensionNodeType nt = (ExtensionNodeType) list [n];
				if (visited.Contains (nt.Id + " " + nt.TypeName))
					continue;
				
				visited.Add (nt.Id + " " + nt.TypeName, nt);
				Console.WriteLine ();
				
				Console.WriteLine (sind + "- " + nt.Id + ": " + nt.Description);
				string nsind = sind + "    ";
				if (nt.Attributes.Count > 0) {
					Console.WriteLine (nsind + "Attributes:");
					foreach (NodeTypeAttribute att in nt.Attributes) {
						string req = att.Required ? " (required)" : "";
						Console.WriteLine (nsind + "  " + att.Name + " (" + att.Type + "): " + att.Description + req);
					}
				}
				
				if (nt.NodeTypes.Count > 0 || nt.NodeSets.Count > 0) {
					Console.WriteLine (nsind + "Child nodes:");
					ArrayList newList = new ArrayList ();
					GetNodes (desc, nt, newList, new Hashtable ());
					list.AddRange (newList);
					foreach (ExtensionNodeType cnt in newList)
						Console.WriteLine ("          " + cnt.Id + ": " + cnt.Description);
				}
			}
			Console.WriteLine ();
		}
		
		void GetNodes (AddinDescription desc, ExtensionNodeSet nset, ArrayList list, Hashtable visited)
		{
			if (visited.Contains (nset))
				return;
			visited.Add (nset, nset);

			foreach (ExtensionNodeType nt in nset.NodeTypes) {
				if (!visited.Contains (nt.Id + " " + nt.TypeName)) {
					list.Add (nt);
					visited.Add (nt.Id + " " + nt.TypeName, nt);
				}
			}
			
			foreach (string nsid in nset.NodeSets) {
				ExtensionNodeSet rset = desc.ExtensionNodeSets [nsid];
				if (rset != null)
					GetNodes (desc, rset, list, visited);
			}
		}
		
		string[] GetArguments ()
		{
			return arguments;
		}
		
		bool HasOption (string key)
		{
			return options.Contains (key);
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
		
		public void AddCommand (string category, string command, string shortName, string arguments, string description, string longDescription, SetupCommandHandler handler)
		{
			SetupCommand cmd = new SetupCommand (category, command, shortName, handler);
			cmd.Usage = arguments;
			cmd.Description = description;
			cmd.LongDescription = longDescription;
			
			int lastCatPos = -1;
			for (int n=0; n<commands.Count; n++) {
				SetupCommand ec = (SetupCommand) commands [n];
				if (ec.Category == category)
					lastCatPos = n;
			}
			if (lastCatPos == -1)
				commands.Add (cmd);
			else
				commands.Insert (lastCatPos+1, cmd);
		}
		
		SetupCommand FindCommand (string id)
		{
			foreach (SetupCommand cmd in commands)
				if (cmd.Command == id || cmd.ShortCommand == id)
					return cmd;
			return null;
		}

		public void PrintHelp (params string[] parms)
		{
			if (parms.Length == 0) {
				string lastCat = null;
				foreach (SetupCommand cmd in commands) {
					if (lastCat != cmd.Category) {
						Console.WriteLine ();
						Console.WriteLine (cmd.Category + ":");
						lastCat = cmd.Category;
					}
					string cc = cmd.CommandDesc;
					if (cc.Length < 16)
						cc += new string (' ', 16 - cc.Length);
					Console.WriteLine ("  " + cc + " " + cmd.Description);
				}
				Console.WriteLine ();
				Console.WriteLine ("Run '" + setupAppName + "help <command>' to get help about a specific command.");
				Console.WriteLine ();
				return;
			}
			else {
				Console.WriteLine ();
				SetupCommand cmd = FindCommand (parms [0]);
				if (cmd != null) {
					Console.WriteLine ("{0}: {1}", cmd.CommandDesc, cmd.Description);
					Console.WriteLine ();
					Console.WriteLine ("Usage: {0}{1}", setupAppName, cmd.Usage);
					Console.WriteLine ();
					Console.WriteLine (cmd.LongDescription);
				}
				else
					Console.WriteLine ("Unknown command: " + parms [0]);
				Console.WriteLine ();
			}
		}
			
		void CreateCommands ()
		{
			SetupCommand cmd;
			string cat = "Add-in commands";
			
			cmd = new SetupCommand (cat, "install", "i", new SetupCommandHandler (Install));
			cmd.Description = "Installs add-ins.";
			cmd.Usage = "[package-name|package-file] ...";
			cmd.AppendDesc ("Installs an add-in or set of addins. The command argument is a list");
			cmd.AppendDesc ("of files and/or package names. If a package name is provided");
			cmd.AppendDesc ("the package will be looked out in the registered repositories.");
			cmd.AppendDesc ("A specific add-in version can be specified by appending it to.");
			cmd.AppendDesc ("the package name using '/' as a separator, like in this example:");
			cmd.AppendDesc ("MonoDevelop.SourceEditor/0.9.1");
			commands.Add (cmd);
			
			cmd = new SetupCommand (cat, "uninstall", "u", new SetupCommandHandler (Uninstall));
			cmd.Description = "Uninstalls add-ins.";
			cmd.Usage = "<package-name>";
			cmd.AppendDesc ("Uninstalls an add-in. The command argument is the name");
			cmd.AppendDesc ("of the add-in to uninstall.");
			commands.Add (cmd);
			
			cmd = new SetupCommand (cat, "check-install", "ci", new SetupCommandHandler (CheckInstall));
			cmd.Description = "Checks installed add-ins.";
			cmd.Usage = "[package-name|package-file] ...";
			cmd.AppendDesc ("Checks if a package is installed. If it is not, it looks for");
			cmd.AppendDesc ("the package in the registered repositories, and if found");
			cmd.AppendDesc ("the package is downloaded and installed, including all");
			cmd.AppendDesc ("needed dependencies.");
			commands.Add (cmd);
			
			
			cmd = new SetupCommand (cat, "update", "up", new SetupCommandHandler (Update));
			cmd.Description = "Updates installed add-ins.";
			cmd.AppendDesc ("Downloads and installs available updates for installed add-ins.");
			commands.Add (cmd);
			
			cmd = new SetupCommand (cat, "list", "l", new SetupCommandHandler (ListInstalled));
			cmd.Description = "Lists installed add-ins.";
			cmd.AppendDesc ("Prints a list of all installed add-ins.");
			commands.Add (cmd);
					
			cmd = new SetupCommand (cat, "list-av", "la", new SetupCommandHandler (ListAvailable));
			cmd.Description = "Lists add-ins available in registered repositories.";
			cmd.AppendDesc ("Prints a list of add-ins available to install in the");
			cmd.AppendDesc ("registered repositories.");
			commands.Add (cmd);
					
			cmd = new SetupCommand (cat, "list-update", "lu", new SetupCommandHandler (ListUpdates));
			cmd.Description = "Lists available add-in updates.";
			cmd.AppendDesc ("Prints a list of available add-in updates in the registered repositories.");
			commands.Add (cmd);
			
			cat = "Repository Commands";

			cmd = new SetupCommand (cat, "rep-add", "ra", new SetupCommandHandler (AddRepository));
			cmd.Description = "Registers repositories.";
			cmd.Usage = "<url> ...";
			cmd.AppendDesc ("Registers an add-in repository. Several URLs can be provided.");
			commands.Add (cmd);

			cmd = new SetupCommand (cat, "rep-remove", "rr", new SetupCommandHandler (RemoveRepository));
			cmd.Description = "Unregisters repositories.";
			cmd.Usage = "<url> ...";
			cmd.AppendDesc ("Unregisters an add-in repository. Several URLs can be provided.");
			commands.Add (cmd);

			cmd = new SetupCommand (cat, "rep-update", "ru", new SetupCommandHandler (UpdateAvailableAddins));
			cmd.Description = "Updates the lists of available addins.";
			cmd.AppendDesc ("Updates the lists of addins available in all registered repositories.");
			commands.Add (cmd);

			cmd = new SetupCommand (cat, "rep-list", "rl", new SetupCommandHandler (ListRepositories));
			cmd.Description = "Lists registered repositories.";
			cmd.AppendDesc ("Shows a list of all registered repositories.");
			commands.Add (cmd);

			cat = "Add-in Registry Commands";

			cmd = new SetupCommand (cat, "reg-update", "rgu", new SetupCommandHandler (UpdateRegistry));
			cmd.Description = "Updates the add-in registry.";
			cmd.AppendDesc ("Looks for changes in add-in directories and updates the registry.");
			cmd.AppendDesc ("New add-ins will be added and deleted add-ins will be removed.");
			commands.Add (cmd);

			cmd = new SetupCommand (cat, "reg-build", "rgu", new SetupCommandHandler (RepairRegistry));
			cmd.Description = "Rebuilds the add-in registry.";
			cmd.AppendDesc ("Regenerates the add-in registry");
			commands.Add (cmd);

			cmd = new SetupCommand (cat, "info", null, new SetupCommandHandler (PrintAddinInfo));
			cmd.Description = "Prints information about an add-in.";
			cmd.AppendDesc ("Prints information about an add-in.");
			commands.Add (cmd);

			cat = "Packaging Commands";

			cmd = new SetupCommand (cat, "rep-build", "rb", new SetupCommandHandler (BuildRepository));
			cmd.Description = "Creates a repository index file for a directory structure.";
			cmd.Usage = "<path>";
			cmd.AppendDesc ("Scans the provided directory and generates a set of index files with entries");
			cmd.AppendDesc ("for all add-in packages found in the directory tree. The resulting file");
			cmd.AppendDesc ("structure is an add-in repository that can be published in a web site or a");
			cmd.AppendDesc ("shared directory.");
			commands.Add (cmd);
	
			cmd = new SetupCommand (cat, "pack", "p", new SetupCommandHandler (BuildPackage));
			cmd.Description = "Creates a package from an add-in configuration file.";
			cmd.Usage = "<file-path>";
			cmd.AppendDesc ("Creates an add-in package (.mpack file) which includes all files ");
			cmd.AppendDesc ("needed to deploy an add-in. The command parameter is the path to");
			cmd.AppendDesc ("the add-in's configuration file.");
			commands.Add (cmd);
	
			cmd = new SetupCommand (cat, "help", "h", new SetupCommandHandler (PrintHelp));
			cmd.Description = "Shows help about a command.";
			cmd.Usage = "<command>";
			commands.Add (cmd);

			cat = "Debug Commands";

			cmd = new SetupCommand (cat, "dump-file", null, new SetupCommandHandler (DumpRegistryFile));
			cmd.Description = "Prints the contents of a registry file.";
			cmd.Usage = "<file-path>";
			cmd.AppendDesc ("Prints the contents of a registry file for debugging.");
			commands.Add (cmd);
		}
	}
	
	class SetupCommand
	{
		string usage;
		
		public SetupCommand (string cat, string cmd, string shortCmd, SetupCommandHandler handler)
		{
			Category = cat;
			Command = cmd;
			ShortCommand = shortCmd;
			Handler = handler;
		}
		
		public void AppendDesc (string s)
		{
			LongDescription += s + "\n";
		}
		
		public string Category;
		public string Command;
		public string ShortCommand;
		public SetupCommandHandler Handler; 
		
		public string Usage {
			get { return usage != null ? Command + " " + usage : Command; }
			set { usage = value; }
		}
			
		public string CommandDesc {
			get {
				if (ShortCommand != null && ShortCommand.Length > 0)
					return Command + " (" + ShortCommand + ")";
				else
					return Command;
			}
		}
		
		public string Description = "";
		public string LongDescription = "";
	}
	
	public delegate void SetupCommandHandler (string[] args);
}

