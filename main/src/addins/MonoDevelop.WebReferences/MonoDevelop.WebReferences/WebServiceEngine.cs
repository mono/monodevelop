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
using System.Web.Services.Discovery;
using MonoDevelop.WebReferences.Dialogs;
using System.Net;

namespace MonoDevelop.WebReferences
{
	public static class WebReferencesService
	{
		public static WebServiceEngine WsEngine = new WebServiceEngineWS ();
		public static WebServiceEngine WcfEngine = new WebServiceEngineWCF ();
		
		public static WebServiceEngine GetEngineForFile (FilePath file)
		{
			if (file.Extension == ".svcmap")
				return WcfEngine;
			else if (file.Extension == ".map")
				return WsEngine;
			else
				throw new InvalidOperationException ("Not a web service reference map file");
		}
		
		public static bool IsWebReferenceMapFile (FilePath file)
		{
			return file.Extension == ".map" || file.Extension == ".svcmap";
		}
	}
	
	public abstract class WebServiceEngine
	{
		public abstract WebServiceDiscoveryResult Discover (string url);
		public abstract WebServiceDiscoveryResult Load (WebReferenceItem item);
		
		protected DiscoveryClientProtocol DiscoResolve (string url)
		{
			// Checks the availablity of any services
			DiscoveryClientProtocol protocol = new DiscoveryClientProtocol ();
			AskCredentials creds = new AskCredentials ();
			protocol.Credentials = creds;
			bool unauthorized;
			
			do {
				unauthorized = false;
				creds.Reset ();
				
				try {
					protocol.DiscoverAny (url);
				} catch (WebException wex) {
					HttpWebResponse wr = wex.Response as HttpWebResponse;
					if (!creds.Canceled && wr != null && wr.StatusCode == HttpStatusCode.Unauthorized) {
						unauthorized = true;
						continue;
					} else
						throw;
				}
			} while (unauthorized);
			
			if (protocol != null) {
				creds.Store ();
				if (protocol.References.Count == 0)
					return null;
			}
			return protocol;
		}
	}
	
	public abstract class WebServiceDiscoveryResult
	{
		WebReferenceItem item;
		
		public WebServiceDiscoveryResult (WebReferenceItem item)
		{
			this.item = item;
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
		
		public virtual void GenerateFiles (DotNetProject project, string referencePath, string namspace, string referenceName)
		{
			// Create the base directory if it does not exists
			string basePath = referencePath;
			if (!Directory.Exists (basePath))
				Directory.CreateDirectory (basePath);
			
			// Generate the wsdl, disco and map files
			string mapSpec = GenerateDescriptionFiles (basePath);
			
			// Generate the proxy class
			string proxySpec = CreateProxyFile (project, basePath, namspace + "." + referenceName, "Reference");
			
			// Remove old files from the service directory
			List<ProjectFile> toRemove = new List<ProjectFile>(project.Files.GetFilesInPath (basePath));
			foreach (ProjectFile f in toRemove)
				project.Files.Remove (f);
			
			ProjectFile mapFile = new ProjectFile (mapSpec);
			mapFile.BuildAction = BuildAction.None;
			mapFile.Subtype = Subtype.Code;
			project.Files.Add (mapFile);
			
			ProjectFile proxyFile = new ProjectFile (proxySpec);
			proxyFile.BuildAction = BuildAction.Compile;
			proxyFile.Subtype = Subtype.Code;
			project.Files.Add (proxyFile);
			
			item = new WebReferenceItem (referenceName, proxyFile, mapFile);
			
			// Add references to the project if they do not exist
			ProjectReference gacRef;
			
			foreach (string refName in GetAssemblyReferences ()) {
				string targetName = project.TargetRuntime.AssemblyContext.GetAssemblyNameForVersion (refName, null, project.TargetFramework);
				//FIXME: warn when we could not find a matching target assembly
				if (targetName != null) {
					gacRef = new ProjectReference (ReferenceType.Gac, targetName);
					if (!project.References.Contains (gacRef))
						project.References.Add (gacRef);
				}
			}
		}
		
		protected abstract string GenerateDescriptionFiles (string basePath);
		
		protected abstract string CreateProxyFile (DotNetProject dotNetProject, string basePath, string proxyNamespace, string referenceName);
		
		public abstract void Update ();
	}
}

