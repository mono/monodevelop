//
// ConsoleAddinInstaller.cs
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

namespace Mono.Addins.Setup
{
	public class ConsoleAddinInstaller: IAddinInstaller
	{
		bool prompt;
		bool repoUpdated;
		int logLevel = 1;
		
		public ConsoleAddinInstaller ()
		{
		}
		
		public bool UserPrompt {
			get { return prompt; }
			set {
				prompt = value;
				if (prompt && logLevel == 0)
					logLevel = 1;
			}
		}
		
		public int LogLevel {
			get { return logLevel; }
			set { logLevel = value; }
		}
		
		void IAddinInstaller.InstallAddins (AddinRegistry reg, string message, string[] addinIds)
		{
			if (logLevel > 0) {
				if (message != null && message.Length > 0) {
					Console.WriteLine (message);
				} else {
					Console.WriteLine ("Additional extensions are required to perform this operation.");
				}
			}
			ArrayList entries = new ArrayList ();
			SetupService setup = new SetupService (reg);
			string idNotFound;
			do {
				idNotFound = null;
				foreach (string id in addinIds) {
					string name = Addin.GetIdName (id);
					string version = Addin.GetIdVersion (id);
					AddinRepositoryEntry[] ares = setup.Repositories.GetAvailableAddin (name, version);
					if (ares.Length == 0) {
						idNotFound = id;
						entries.Clear ();
						break;
					} else
						entries.Add (ares[0]);
				}
				if (idNotFound != null) {
					if (repoUpdated)
						throw new InstallException ("Add-in '" + idNotFound + "' not found in the registered add-in repositories");
					if (prompt) {
						Console.WriteLine ("The add-in '" + idNotFound + "' could not be found in the registered repositories.");
						Console.WriteLine ("The repository indices may be outdated.");
						if (!Confirm ("Do you wan't to update them now?"))
							throw new InstallException ("Add-in '" + idNotFound + "' not found in the registered add-in repositories");
					}
					setup.Repositories.UpdateAllRepositories (new ConsoleProgressStatus (logLevel));
					repoUpdated = true;
				}
			}
			while (idNotFound != null);
			
			if (logLevel > 0) {
				Console.WriteLine ("The following add-ins will be installed:");
				foreach (AddinRepositoryEntry addin in entries)
					Console.WriteLine (" - " + addin.Addin.Name + " v" + addin.Addin.Version);
				
				if (prompt) {
					if (!Confirm ("Do you want to continue with the installation?"))
						throw new InstallException ("Installation cancelled");
				}
			}
			setup.Install (new ConsoleProgressStatus (logLevel), (AddinRepositoryEntry[]) entries.ToArray (typeof(AddinRepositoryEntry)));
		}
		
		bool Confirm (string msg)
		{
			string res;
			do {
				Console.Write (msg + " (Y/n): ");
				res = Console.ReadLine ();
				if (res.Length > 0 && res.ToLower()[0] == 'n')
					return false;
			} while (res.Length > 0 && res.ToLower()[0] != 'y');
			return true;
		}
	}
}
