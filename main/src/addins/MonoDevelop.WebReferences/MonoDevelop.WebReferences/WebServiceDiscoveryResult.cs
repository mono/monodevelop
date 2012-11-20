// 
// WebServiceEngine.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.IO;
using System.CodeDom.Compiler;

namespace MonoDevelop.WebReferences
{
	public abstract class WebServiceDiscoveryResult
	{
		WebReferenceItem item;
		WebServiceEngine engine;
		
		public WebServiceDiscoveryResult (WebServiceEngine engine, WebReferenceItem item)
		{
			this.item = item;
			this.engine = engine;
		}

		public WebReferenceItem Item {
			get { return this.item; }
		}
		
		CodeDomProvider provider;
		
		protected CodeDomProvider GetProvider (DotNetProject dotNetProject)
		{
			if (provider == null)
				provider = dotNetProject.LanguageBinding.GetCodeDomProvider();
				
			// Throw an exception if no provider has been set
			if (provider == null)
				throw new Exception("Language not supported");

			return provider;
		}
		
		public abstract string GetDescriptionMarkup ();
		
		public abstract IEnumerable<string> GetAssemblyReferences ();
		
		public abstract FilePath GetReferencePath (DotNetProject project, string refName);

		public abstract string GetServiceURL ();
		
		public abstract string ProxyGenerator { get; }
		
		public virtual void GenerateFiles (DotNetProject project, string namspace, string referenceName)
		{
			//make sure we have a valid value for the namespace
			if (string.IsNullOrEmpty (namspace)) {
				namspace = project.GetDefaultNamespace (null);
			}
			
			// Create the base directory if it does not exists
			FilePath basePath = GetReferencePath (project, referenceName).CanonicalPath;
			if (!Directory.Exists (basePath))
				Directory.CreateDirectory (basePath);
			
			// Remove old files from the service directory
			List<ProjectFile> toRemove = new List<ProjectFile>(project.Files.GetFilesInPath (basePath));
			foreach (ProjectFile f in toRemove)
				project.Files.Remove (f);
			
			// Generate the wsdl, disco and map files
			string mapSpec = GenerateDescriptionFiles (project, basePath);
			
			// Generate the proxy class
			string proxySpec = CreateProxyFile (project, basePath, namspace + "." + referenceName, "Reference");
			
			ProjectFile mapFile = new ProjectFile (mapSpec);
			mapFile.BuildAction = BuildAction.None;
			mapFile.Subtype = Subtype.Code;
			mapFile.Generator = ProxyGenerator;
			project.Files.Add (mapFile);
			
			ProjectFile proxyFile = new ProjectFile (proxySpec);
			proxyFile.BuildAction = BuildAction.Compile;
			proxyFile.Subtype = Subtype.Code;
			proxyFile.DependsOn = mapFile.FilePath;
			project.Files.Add (proxyFile);
			
			mapFile.LastGenOutput = proxyFile.FilePath.FileName;
			
			item = new WebReferenceItem (engine, project, referenceName, basePath, mapFile);
			
			// Add references to the project if they do not exist
			ProjectReference packageRef;
			
			foreach (string refName in GetAssemblyReferences ()) {
				string targetName = project.TargetRuntime.AssemblyContext.GetAssemblyNameForVersion (refName, null, project.TargetFramework);
				//FIXME: warn when we could not find a matching target assembly
				if (targetName != null) {
					packageRef = new ProjectReference (ReferenceType.Package, targetName);
					if (!project.References.Contains (packageRef))
						project.References.Add (packageRef);
				}
			}
			WebReferencesService.NotifyWebReferencesChanged (project);
		}
		
		protected abstract string GenerateDescriptionFiles (DotNetProject dotNetProject, FilePath basePath);
		
		protected abstract string CreateProxyFile (DotNetProject dotNetProject, FilePath basePath, string proxyNamespace, string referenceName);
		
		public abstract void Update ();
	}
}
