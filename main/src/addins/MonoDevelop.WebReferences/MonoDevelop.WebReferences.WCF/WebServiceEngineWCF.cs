// 
// WebServiceEngineWCF.cs
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
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Web.Services.Discovery;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.WebReferences.WCF
{
	public class WebServiceEngineWCF: WebServiceEngine
	{
		ClientOptions defaultOptions = new ClientOptions ();

		public ClientOptions DefaultClientOptions {
			get { return defaultOptions; }
		}
		
		public override WebServiceDiscoveryResult Discover (string url)
		{
			DiscoveryClientProtocol prot;
			try {
				prot = DiscoResolve (url);
				if (prot != null)
					return new WebServiceDiscoveryResultWCF (prot, null, null, null, DefaultClientOptions);
			} catch {
				// Ignore when MEX resolver is enabled
				throw;
			}
/*			
			MetadataSet metadata = ResolveWithWSMex (url);
			if (metadata != null)
				return new WebServiceDiscoveryResultWCF (null, metadata, null, null);
*/
			return null;
		}
		
		MetadataSet ResolveWithWSMex (string url)
		{
			MetadataSet metadata = null;
			try {
				MetadataExchangeClient client = new MetadataExchangeClient (new EndpointAddress (url));

				Console.WriteLine ("\nAttempting to download metadata from {0} using WS-MetadataExchange..", url);
				metadata = client.GetMetadata ();
			} catch (InvalidOperationException e) {
				//MetadataExchangeClient wraps exceptions, thrown while
				//fetching the metadata, in an InvalidOperationException
				string msg;
				if (e.InnerException == null)
					msg = e.Message;
				else
					msg = e.InnerException.ToString ();

				Console.WriteLine ("WS-MetadataExchange query failed for the url '{0}' with exception :\n {1}",
					url, msg);
			}

			return metadata;
		}
		
		public override IEnumerable<WebReferenceItem> GetReferenceItems (DotNetProject project)
		{
			foreach (WCFMetadataStorage mds in project.Items.GetAll<WCFMetadataStorage> ()) {
				ProjectFile mapFile = null;
				foreach (ProjectFile file in project.Files.GetFilesInPath (mds.Path)) {
					if (file.FilePath.Extension == ".svcmap") {
						mapFile = file;
						break;
					}
				}
				if (mapFile != null)
					yield return new WebReferenceItem (this, project, mds.Path.CanonicalPath.FileName, mapFile.FilePath.ParentDirectory, mapFile);
			}
		}
		
		
		public override WebServiceDiscoveryResult Load (WebReferenceItem item)
		{
			FilePath basePath = item.MapFile.FilePath.ParentDirectory;
			ReferenceGroup resfile = ReferenceGroup.Read (item.MapFile.FilePath);
			
			// TODO: Read as MetadataSet
			
			DiscoveryClientProtocol protocol = new DiscoveryClientProtocol ();
			
			foreach (MetadataFile dcr in resfile.Metadata)
			{
				DiscoveryReference dr;
				switch (dcr.MetadataType) {
					case "Wsdl":
						dr = new System.Web.Services.Discovery.ContractReference ();
						break;
					case "Disco":
						dr = new System.Web.Services.Discovery.DiscoveryDocumentReference ();
						break;
					case "Schema":
						dr = new System.Web.Services.Discovery.SchemaReference ();
						break;
					default:
						continue;
				}

				dr.Url = dcr.SourceUrl;
				FileStream fs = new FileStream (basePath.Combine (dcr.FileName), FileMode.Open, FileAccess.Read);
				protocol.Documents.Add (dr.Url, dr.ReadDocument (fs));
				fs.Close ();
				protocol.References.Add (dr.Url, dr);
			}
			return new WebServiceDiscoveryResultWCF (protocol, null, item, resfile, DefaultClientOptions);
		}
		
		public override void Delete (WebReferenceItem item)
		{
			base.Delete (item);
			DotNetProject project = item.Project;
			WCFMetadataStorage metStor = project.Items.GetAll<WCFMetadataStorage> ().FirstOrDefault (m => m.Path.CanonicalPath == item.BasePath);
			if (metStor != null) {
				project.Items.Remove (metStor);
				if (!project.Items.GetAll<WCFMetadataStorage> ().Any ()) {
					WCFMetadata dir = project.Items.GetAll<WCFMetadata> ().FirstOrDefault ();
					if (dir != null)
						project.Items.Remove (dir);
				}
			}
		}
	}
}

