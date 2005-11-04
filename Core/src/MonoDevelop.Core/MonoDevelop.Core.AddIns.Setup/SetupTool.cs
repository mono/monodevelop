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
			Console.WriteLine ("MonoDevelop Setup Utility");
			
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
					
				case "rinstall":
				case "ri":
					RemoteInstall (parms);
					break;
					
				case "uninstall":
				case "u":
					Uninstall (parms);
					break;
					
				case "list":
				case "l":
					ListInstalled (parms);
					break;
	
				case "list-av":
				case "la":
					ListAvailable (parms);
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
				case "pb":
					BuildPackage (parms);
					break;
					
				default:
					Console.WriteLine ("Unknown command: " + cmd);
					return 1;
			}
			
			return 0;
		}
		
		void Install (string[] args)
		{
			PackageCollection packs = new PackageCollection ();
			for (int n=0; n<args.Length; n++)
				packs.Add (AddinPackage.FromFile (args [n]));
		
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
		
		void RemoteInstall (string[] args)
		{
			if (args.Length < 1)
				throw new InstallException ("The addin id is required.");
			
			AddinRepositoryEntry[] ads = Runtime.SetupService.GetAvailableAddin (args[0], null);
			if (ads.Length == 0)
				throw new InstallException ("The addin '" + args[0] + "' is not available.");
			if (ads.Length > 1) {
				if (args.Length < 2) {
					Console.WriteLine ("The addin version is required because there are several versions of the same addin available.");
					return;
				}
				ads = Runtime.SetupService.GetAvailableAddin (args[0], args[1]);
				if (ads.Length == 0)
					throw new InstallException ("The addin " + args[0] + " v" + args[1] + " is not available.");
			}
		
			PackageCollection packs = new PackageCollection ();
			packs.Add (AddinPackage.FromRepository (ads[0]));
			Install (packs);
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
			Console.WriteLine ("Installed addins:");
			AddinSetupInfo[] addins = Runtime.SetupService.GetInstalledAddins ();
			foreach (AddinSetupInfo addin in addins) {
				Console.WriteLine (" - " + addin.Addin.Id + " " + addin.Addin.Version);
			}
		}
		
		void ListAvailable (string[] args)
		{
			Console.WriteLine ("Available addins:");
			AddinRepositoryEntry[] addins = Runtime.SetupService.GetAvailableAddins ();
			foreach (AddinRepositoryEntry addin in addins) {
				Console.WriteLine (" - " + addin.Addin.Id + " " + addin.Addin.Version + " (" + addin.Repository.Name + ")");
			}
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
		
		void PrintHelp ()
		{
			Console.WriteLine ("install (i)      Installs addins");
			Console.WriteLine ("uninstall (u)    Unistalls addins");
			Console.WriteLine ("list (l)         Lists installed addins");
			Console.WriteLine ("rinstall (ri)    Installs addins from a remote repository");
			Console.WriteLine ("list-av (la)     Lists addins available in registered repositories");
			Console.WriteLine ("rep-add (ra)     Registers repositories");
			Console.WriteLine ("rep-remove (rr)  Unregisters repositories");
			Console.WriteLine ("rep-list (rl)    Lists registered repositories");
			Console.WriteLine ("rep-update (ru)  Updates the lists of addins available in repositories");
			Console.WriteLine ("rep-build (rb)   Creates a repository index file for a directory structure");
			Console.WriteLine ("pack (pb)        Creates a package from an add-in configuration file");
		} 
	}
}

