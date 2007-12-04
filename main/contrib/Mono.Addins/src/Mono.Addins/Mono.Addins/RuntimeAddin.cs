//
// RuntimeAddin.cs
//
// Author:
//   Lluis Sanchez Gual,
//   Georg Wächter
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
using System.IO;
using System.Reflection;
using System.Xml;
using System.Resources;
using System.Globalization;

using Mono.Addins.Description;
using Mono.Addins.Localization;

namespace Mono.Addins
{
	public class RuntimeAddin
	{
		string id;
		string baseDirectory;
		string privatePath;
		Addin ainfo;
		
		Assembly[] assemblies;
		RuntimeAddin[] depAddins;
		ResourceManager[] resourceManagers;
		AddinLocalizer localizer;
		
		internal RuntimeAddin()
		{
		}
		
		internal Assembly[] Assemblies {
			get { return assemblies; }
		}
		
		public string Id {
			get { return Addin.GetIdName (id); }
		}
		
		public string Version {
			get { return Addin.GetIdVersion (id); }
		}
		
		internal Addin Addin {
			get { return ainfo; }
		}
		
		public override string ToString ()
		{
			return ainfo.ToString ();
		}

		void CreateResourceManagers ()
		{
			ArrayList managersList = new ArrayList ();

			// Search for embedded resource files
			foreach (Assembly asm in assemblies)
			{
				foreach (string res in asm.GetManifestResourceNames ()) {
					if (res.EndsWith (".resources"))
						managersList.Add (new ResourceManager (res.Substring (0, res.Length - ".resources".Length), asm));
				}
			}

			resourceManagers = (ResourceManager[]) managersList.ToArray (typeof(ResourceManager));
		}

		public string GetResourceString (string name)
		{
			return (string) GetResourceObject (name, true, null);
		}

		public string GetResourceString (string name, bool throwIfNotFound)
		{
			return (string) GetResourceObject (name, throwIfNotFound, null);
		}

		public string GetResourceString (string name, bool throwIfNotFound, CultureInfo culture)
		{
			return (string) GetResourceObject (name, throwIfNotFound, culture);
		}

		public object GetResourceObject (string name)
		{
			return GetResourceObject (name, true, null);
		}

		public object GetResourceObject (string name, bool throwIfNotFound)
		{
			return GetResourceObject (name, throwIfNotFound, null);
		}

		public object GetResourceObject (string name, bool throwIfNotFound, CultureInfo culture)
		{
			if (resourceManagers == null)
				CreateResourceManagers ();
			
			// Look in resources of this add-in
			foreach (ResourceManager manager in resourceManagers)
			{
				object t = manager.GetObject (name, culture);
				if (t != null)
					return t;
			}

			// Look in resources of dependent add-ins
			foreach (RuntimeAddin addin in depAddins)
			{
				object t = addin.GetResourceObject (name, false, culture);
				if (t != null)
					return t;
			}

			if (throwIfNotFound)
				throw new InvalidOperationException ("Resource object '" + name + "' not found in add-in '" + id + "'");

			return null;
		}


		public Type GetType (string typeName)
		{
			return GetType (typeName, true);
		}
		
		public Type GetType (string typeName, bool throwIfNotFound)
		{
			// Look in the addin assemblies
			
			Type at = Type.GetType (typeName, false);
			if (at != null)
				return at;
			
			foreach (Assembly asm in assemblies) {
				Type t = asm.GetType (typeName, false);
				if (t != null)
					return t;
			}
			
			// Look in the dependent add-ins
			foreach (RuntimeAddin addin in depAddins) {
				Type t = addin.GetType (typeName, false);
				if (t != null)
					return t;
			}
			
			if (throwIfNotFound)
				throw new InvalidOperationException ("Type '" + typeName + "' not found in add-in '" + id + "'");
			return null;
		}
		
		public object CreateInstance (string typeName)
		{
			return CreateInstance (typeName, true);
		}
		
		public object CreateInstance (string typeName, bool throwIfNotFound)
		{
			Type type = GetType (typeName, throwIfNotFound);
			if (type == null)
				return null;
			else
				return Activator.CreateInstance (type, true);
		}
		
		public string GetFilePath (string fileName)
		{
			return Path.Combine (baseDirectory, fileName);
		}
		
		public string PrivateDataPath {
			get {
				if (privatePath == null) {
					privatePath = ainfo.PrivateDataPath;
					if (!Directory.Exists (privatePath))
						Directory.CreateDirectory (privatePath);
				}
				return privatePath;
			}
		}
		
		public Stream GetResource (string resourceName)
		{
			return GetResource (resourceName, false);
		}
		
		public Stream GetResource (string resourceName, bool throwIfNotFound)
		{
			// Look in the addin assemblies
			
			foreach (Assembly asm in assemblies) {
				Stream res = asm.GetManifestResourceStream (resourceName);
				if (res != null)
					return res;
			}
			
			// Look in the dependent add-ins
			foreach (RuntimeAddin addin in depAddins) {
				Stream res = addin.GetResource (resourceName);
				if (res != null)
					return res;
			}
			
			if (throwIfNotFound)
				throw new InvalidOperationException ("Resource '" + resourceName + "' not found in add-in '" + id + "'");
				
			return null;
		}
		
		public AddinLocalizer Localizer {
			get {
				if (localizer != null)
					return localizer;
				else
					return AddinManager.DefaultLocalizer;
			}
		}
		
		internal AddinDescription Load (Addin iad)
		{
			ainfo = iad;
			
			ArrayList plugList = new ArrayList ();
			ArrayList asmList = new ArrayList ();
			
			AddinDescription description = iad.Description;
			id = description.AddinId;
			baseDirectory = description.BasePath;
			
			// Load the main modules
			LoadModule (description.MainModule, description.Namespace, plugList, asmList);
			
			// Load the optional modules, if the dependencies are present
			foreach (ModuleDescription module in description.OptionalModules) {
				if (CheckAddinDependencies (module))
					LoadModule (module, description.Namespace, plugList, asmList);
			}
			
			depAddins = (RuntimeAddin[]) plugList.ToArray (typeof(RuntimeAddin));
			assemblies = (Assembly[]) asmList.ToArray (typeof(Assembly));
			
			if (description.Localizer != null) {
				string cls = description.Localizer.GetAttribute ("type");
				
				// First try getting one of the stock localizers. If none of found try getting the type.
				object fob = CreateInstance ("Mono.Addins.Localization." + cls + "Localizer", false);
				if (fob == null)
					fob = CreateInstance (cls, true);
				
				IAddinLocalizerFactory factory = fob as IAddinLocalizerFactory;
				if (factory == null)
					throw new InvalidOperationException ("Localizer factory type '" + cls + "' must implement IAddinLocalizerFactory");
				localizer = new AddinLocalizer (factory.CreateLocalizer (this, description.Localizer));
			}
			
			return description;
		}
		
		void LoadModule (ModuleDescription module, string ns, ArrayList plugList, ArrayList asmList)
		{
			// Load the assemblies
			foreach (string s in module.Assemblies) {
				Assembly asm = null;

				// don't load the assembly if it's already loaded
				string asmPath = Path.Combine (baseDirectory, s);
				foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies ()) {
					// Sorry, you can't load addins from
					// dynamic assemblies as get_Location
					// throws a NotSupportedException
					if (a is System.Reflection.Emit.AssemblyBuilder) {
						continue;
					}

					if (a.Location == asmPath) {
						asm = a;
						break;
					}
				}

				if (asm == null) {
					asm = Assembly.LoadFrom (asmPath);
				}

				asmList.Add (asm);
			}
				
			// Collect dependent ids
			foreach (Dependency dep in module.Dependencies) {
				AddinDependency pdep = dep as AddinDependency;
				if (pdep != null) {
					RuntimeAddin adn = AddinManager.SessionService.GetAddin (Addin.GetFullId (ns, pdep.AddinId, pdep.Version));
					if (adn != null)
						plugList.Add (adn);
					else
						AddinManager.ReportError ("Add-in dependency not loaded: " + pdep.FullAddinId, module.ParentAddinDescription.AddinId, null, false);
				}
			}
		}
		
		internal void UnloadExtensions ()
		{
			// Create the extension points (but do not load them)
			AddinDescription emap = Addin.Description;
			if (emap == null) return;
				
			foreach (ExtensionNodeSet rel in emap.ExtensionNodeSets)
				AddinManager.SessionService.UnregisterNodeSet (rel);
		}
		
		bool CheckAddinDependencies (ModuleDescription module)
		{
			foreach (Dependency dep in module.Dependencies) {
				AddinDependency pdep = dep as AddinDependency;
				if (pdep != null && !AddinManager.SessionService.IsAddinLoaded (pdep.FullAddinId))
					return false;
			}
			return true;
		}
	}
}
