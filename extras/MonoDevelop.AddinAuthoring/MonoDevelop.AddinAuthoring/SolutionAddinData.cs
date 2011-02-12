// 
// SolutionAddinData.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Mono.Addins;
using Mono.Addins.Setup;
using MonoDevelop.Ide;

namespace MonoDevelop.AddinAuthoring
{
	public class SolutionAddinData: IDisposable
	{
		AddinRegistry registry;
		Solution solution;
		RegistryInfo regInfo;
		AddinFileSystem customFileSystem;
		bool registryNeedsUpdate;
		bool updatePlanned;
		
		const int RegistryChangeNotifDelay = 1 * 1000;
		
		public SolutionAddinData (Solution sol)
		{
			solution = sol;
			customFileSystem = new AddinFileSystem (sol);
			customFileSystem.Changed += OnSolutionChanged;
		}
		
		public void Dispose ()
		{
			customFileSystem.Dispose ();
		}
		
		FilePath TempRegistryPath {
			get {
				
				return solution.BaseDirectory.Combine (".temp-addin-registry").Combine (IdeApp.Workspace.ActiveConfigurationId ?? "Default"); 
			}
		}
		
		public string ApplicationName {
			get { return regInfo != null ? regInfo.ApplicationName : null; }
			set {
				if (regInfo != null && regInfo.ApplicationName == value)
					return;
				Application app = SetupService.GetExtensibleApplication (value);
				if (app != null)
					ExternalRegistryInfo = new RegistryInfo (app);
				else {
					RegistryInfo ri = new RegistryInfo ();
					ri.ApplicationName = value;
					ExternalRegistryInfo = ri;
				}
			}
		}
		
		public RegistryInfo ExternalRegistryInfo {
			get {
				return regInfo;
			}
			set {
				regInfo = value;
				registry = null;
				if (value != null)
					solution.UserProperties.SetValue ("MonoDevelop.AddinAuthoring.RegistryInfo", value);
				else
					solution.UserProperties.RemoveValue ("MonoDevelop.AddinAuthoring.RegistryInfo");
			}
		}
		
		public FilePath TestRegistryPath {
			get { return TempRegistryPath; }
		}
		
		public void SetupTestRegistry ()
		{
			UpdateRegistry ();
		}
		
		public AddinRegistry Registry {
			get {
				lock (this) {
					if (registry == null) {
						ResetRegistry ();
						UpdateRegistry ();
					}
					else if (registryNeedsUpdate)
						UpdateRegistry ();
				}
				return registry;
			}
		}
		
		public void ResetRegistry ()
		{
			registry = null;
			RegistryInfo ri = ExternalRegistryInfo;
			if (ri != null) {
				if (string.IsNullOrEmpty (ri.ApplicationPath) || string.IsNullOrEmpty (ri.RegistryPath))
					throw new InvalidOperationException ();
				else
					registry = new AddinRegistry (ri.RegistryPath, ri.ApplicationPath);
			}
			if (registry == null) {
				FilePath path = TempRegistryPath;
				registry = new AddinRegistry (path, path);
			}
			registry.RegisterExtension (customFileSystem);
		}
		
		public void UpdateRegistry ()
		{
			registryNeedsUpdate = false;
			
			FilePath addinsPath = TempRegistryPath.Combine ("addins");
			if (!Directory.Exists (addinsPath))
				Directory.CreateDirectory (addinsPath);
			using (StreamWriter sw = new StreamWriter (TempRegistryPath.Combine ("dummy.addin.xml"))) {
				sw.WriteLine ("<Addin id=\"__\" version=\"1.0\" namespace=\"__\" isroot=\"true\" name=\"__\"/>");
			}
			FilePath includeFile = addinsPath.Combine ("all.addins");
			using (StreamWriter sw = new StreamWriter (includeFile)) {
				XmlTextWriter tw = new XmlTextWriter (sw);
				tw.Formatting = Formatting.Indented;
				tw.WriteStartElement ("Addins");
				foreach (DotNetProject p in solution.GetAllSolutionItems<DotNetProject> ()) {
					tw.WriteElementString ("Directory", p.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration).ParentDirectory);
				}
				tw.WriteEndElement ();
			}
			Registry.Update (new ConsoleProgressStatus (false));
			AddinAuthoringService.NotifyRegistryChanged (Registry);
		}

		void OnSolutionChanged (object sender, EventArgs e)
		{
			lock (this) {
				if (!updatePlanned) {
					updatePlanned = true;
					GLib.Timeout.Add (RegistryChangeNotifDelay, OnDelayedUpdate);
				}
			}
		}
		
		bool OnDelayedUpdate ()
		{
			lock (this) {
				updatePlanned = false;
				UpdateRegistry ();
			}
			return false;
		}
		
		public void NotifyChanged ()
		{
			foreach (DotNetProject p in solution.GetAllSolutionItems<DotNetProject> ()) {
				AddinData data = p.GetAddinData ();
				if (data != null)
					data.NotifyChanged (false);
			}
		}
	}
}
