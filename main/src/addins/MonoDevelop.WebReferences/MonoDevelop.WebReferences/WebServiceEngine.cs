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

using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Web.Services.Discovery;
using MonoDevelop.WebReferences.Dialogs;
using System.Net;
using MonoDevelop.Core;

namespace MonoDevelop.WebReferences
{
	public abstract class WebServiceEngine
	{
		public abstract WebServiceDiscoveryResult Discover (string url);
		public abstract WebServiceDiscoveryResult Load (WebReferenceItem item);
		public abstract IEnumerable<WebReferenceItem> GetReferenceItems (DotNetProject project);
		
		public virtual void Delete (WebReferenceItem item)
		{
			var toRemove = new List<ProjectFile> (item.Project.Files.GetFilesInPath (item.BasePath));
			foreach (ProjectFile file in toRemove)
				item.Project.Files.Remove (file);
			FileService.DeleteDirectory (item.BasePath);
		}
		
		protected DiscoveryClientProtocol DiscoResolve (string url)
		{
			// Checks the availablity of any services
			var protocol = new DiscoveryClientProtocol ();
			var creds = new AskCredentials ();
			protocol.Credentials = creds;
			bool unauthorized;
			
			do {
				unauthorized = false;
				creds.Reset ();
				
				try {
					protocol.DiscoverAny (url);
				} catch (WebException wex) {
					var wr = wex.Response as HttpWebResponse;
					if (!creds.Canceled && wr != null && wr.StatusCode == HttpStatusCode.Unauthorized) {
						unauthorized = true;
						continue;
					}
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
	
	
}

