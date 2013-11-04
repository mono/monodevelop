// 
// WebServiceEngineWS.cs
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

using System.Linq;
using System.Web.Services.Discovery;
using MonoDevelop.Projects;
using System.IO;
using MonoDevelop.Core;
using System.Collections.Generic;
using WebReferencesDir = MonoDevelop.WebReferences.WS.WebReferences;

namespace MonoDevelop.WebReferences.WS
{
	public class WebServiceEngineWS : WebServiceEngine
	{
		public override WebServiceDiscoveryResult Discover (string url)
		{
			DiscoveryClientProtocol protocol = DiscoResolve (url);
			if (protocol != null) {
				protocol.Url = url;
				return new WebServiceDiscoveryResultWS (protocol, null);
			}
			return null;
		}

		public override WebServiceDiscoveryResult Load (WebReferenceItem item)
		{
			// Read the map file into the discovery client protocol and setup the code generator
			var protocol = new DiscoveryProtocol ();
			protocol.ReadAllUseBasePath (item.MapFile.FilePath);
			return new WebServiceDiscoveryResultWS (protocol, item);
		}
		
		public override IEnumerable<WebReferenceItem> GetReferenceItems (DotNetProject project)
		{
			IEnumerable<WebReferenceUrl> refs = project.Items.GetAll<WebReferenceUrl> ();
			if (!refs.Any ()) {
				// Maybe it is an old project which doesn't have reference info
				FilePath refsDir = project.BaseDirectory.Combine ("Web References");
				if (Directory.Exists (refsDir) && project.Files.GetFilesInPath (refsDir).Any ())
					ImportReferenceUrlItems (project);
				refs = project.Items.GetAll<WebReferenceUrl> ();
			}
			
			foreach (WebReferenceUrl wru in refs) {
				ProjectFile mapFile = null;
				foreach (ProjectFile file in project.Files.GetFilesInPath (wru.RelPath)) {
					if (file.FilePath.Extension == ".map") {
						mapFile = file;
						break;
					}
				}
				if (mapFile != null)
					yield return new WebReferenceItem (this, project, wru.RelPath.CanonicalPath.FileName, mapFile.FilePath.ParentDirectory, mapFile);
			}
		}
		
		public override void Delete (WebReferenceItem item)
		{
			base.Delete (item);
			DotNetProject project = item.Project;
			WebReferenceUrl wru = project.Items.GetAll<WebReferenceUrl> ().FirstOrDefault (m => m.RelPath.CanonicalPath == item.BasePath);
			if (wru != null) {
				project.Items.Remove (wru);
				if (!project.Items.GetAll<WebReferenceUrl> ().Any ()) {
					WebReferencesDir dir = project.Items.GetAll<WebReferencesDir> ().FirstOrDefault ();
					if (dir != null)
						project.Items.Remove (dir);
				}
			}
		}
		
		
		static void ImportReferenceUrlItems (DotNetProject project)
		{
			FilePath refsDir = project.BaseDirectory.Combine ("Web References");
			
			foreach (ProjectFile file in project.Files.GetFilesInPath (refsDir)) {
				if (file.Subtype == Subtype.Directory)
					continue;
				if (file.FilePath.ParentDirectory.ParentDirectory != refsDir)
					continue;
				
				if (file.FilePath.Extension == ".map") {
					string url = GetUrl (file.FilePath);
					if (url == null)
						continue;
					var wru = new WebReferenceUrl (url);
					wru.RelPath = file.FilePath.ParentDirectory;
					project.Items.Add (wru);
				}
			}
		}
		
		static string GetUrl (FilePath mapPath)
		{
			var protocol = new DiscoveryProtocol ();
			protocol.ReadAllUseBasePath (mapPath);
			
			// Refresh the disco and wsdl from the server
			foreach (object doc in protocol.References.Values) {
				string url = null;
				var discoveryDocumentReference = doc as DiscoveryDocumentReference;
				if (discoveryDocumentReference != null) {
					url = discoveryDocumentReference.Url;
				} else {
					var contractReference = doc as ContractReference;
					if (contractReference != null)
						url = contractReference.Url;
				}
				
				if (!string.IsNullOrEmpty (url))
					return url;
			}
			return null;
		}
	}

	
}

