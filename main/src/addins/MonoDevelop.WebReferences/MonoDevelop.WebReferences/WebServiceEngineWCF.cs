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
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Web.Services.Discovery;
using System.Xml.Serialization;
using System.Xml;
using MonoDevelop.Projects;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.Xml.Schema;

using WSServiceDescription = System.Web.Services.Description.ServiceDescription;
using System.Text;
using Mono.ServiceContractTool;

namespace MonoDevelop.WebReferences
{
	public class WebServiceEngineWCF: WebServiceEngine
	{
		public override WebServiceDiscoveryResult Discover (string url)
		{
			DiscoveryClientProtocol prot;
			try {
				prot = DiscoResolve (url);
				if (prot != null)
					return new WebServiceDiscoveryResultWCF (prot, null, null, null);
			} catch {
				// Ignore?
			}
			
			MetadataSet metadata = ResolveWithWSMex (url);
			if (metadata != null)
				return new WebServiceDiscoveryResultWCF (null, metadata, null, null);
			else
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
			return new WebServiceDiscoveryResultWCF (protocol, null, item, resfile);
		}
		
	}
	
	class WebServiceDiscoveryResultWCF: WebServiceDiscoveryResult
	{
		MetadataSet metadata;
		DiscoveryClientProtocol protocol;
		ReferenceGroup refGroup;
		
		public WebServiceDiscoveryResultWCF (DiscoveryClientProtocol protocol, MetadataSet metadata, WebReferenceItem item, ReferenceGroup refGroup): base (item)
		{
			this.refGroup = refGroup;
			this.protocol = protocol;
			this.metadata = metadata;
		}

		public override string GetDescriptionMarkup ()
		{
			StringBuilder text = new StringBuilder ();
			
			if (protocol != null) {
				foreach (object dd in protocol.Documents.Values) {
					if (dd is ServiceDescription) {
						Library.GenerateWsdlXml (text, protocol);
						break;
					} else if (dd is DiscoveryDocument) {
						Library.GenerateDiscoXml (text, (DiscoveryDocument)dd);
						break;
					}
				}
			} else {
				// TODO
			}
			return text.ToString ();
		}
		
		protected override string GenerateDescriptionFiles (string basePath)
		{
			string file = Path.Combine (basePath, "Reference.svcmap");
			if (protocol != null) {
				protocol.ResolveAll ();
				protocol.WriteAll (basePath, "Reference.svcmap");
				refGroup = ConvertMapFile (file);
			}
			else {
				// TODO
				ReferenceGroup map = new ReferenceGroup ();
				map.Save (file);
				refGroup = map;
			}
			return file;
		}
		
		public override void Update ()
		{
			ReferenceGroup resfile = ReferenceGroup.Read (Item.MapFile.FilePath);
			if (resfile.MetadataSources.Count == 0)
				return;
			string url = resfile.MetadataSources [0].Address;
			WebServiceDiscoveryResultWCF wref = (WebServiceDiscoveryResultWCF) WebReferencesService.WcfEngine.Discover (url);
			if (wref == null)
				return;
			
			metadata = wref.metadata;
			protocol = wref.protocol;
			string basePath = new FileInfo (Item.MapFile.FilePath).Directory.FullName;
			string ns = Item.MapFile.Project.Name + "." + Item.Name;
			GenerateFiles ((DotNetProject)Item.ProxyFile.Project, basePath, ns, "Reference");
		}
		
		public override System.Collections.Generic.IEnumerable<string> GetAssemblyReferences ()
		{
			yield return "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
			yield return "System.ServiceModel, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
			yield return "System.Xml, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
		}
		
		protected override string CreateProxyFile (DotNetProject dotNetProject, string basePath, string proxyNamespace, string referenceName)
		{
			CodeCompileUnit ccu = new CodeCompileUnit ();
			CodeNamespace cns = new CodeNamespace (proxyNamespace);
			ccu.Namespaces.Add (cns);
			
			bool targetMoonlight = dotNetProject.TargetFramework.Id.StartsWith ("SL");
			bool targetMonoTouch = dotNetProject.TargetFramework.Id.StartsWith ("IPhone");
			
			ServiceContractGenerator generator = new ServiceContractGenerator (ccu);
			generator.Options = ServiceContractGenerationOptions.ChannelInterface | ServiceContractGenerationOptions.ClientClass;
			if (refGroup.ClientOptions.GenerateAsynchronousMethods)
				generator.Options |= ServiceContractGenerationOptions.AsynchronousMethods;
			if (refGroup.ClientOptions.GenerateInternalTypes)
				generator.Options |= ServiceContractGenerationOptions.InternalTypes;
			if (refGroup.ClientOptions.GenerateMessageContracts)
				generator.Options |= ServiceContractGenerationOptions.TypedMessages;
			if (targetMoonlight || targetMonoTouch)
				generator.Options |= ServiceContractGenerationOptions.EventBasedAsynchronousMethods;
			
			MetadataSet mset;
			if (protocol != null)
				mset = ToMetadataSet (protocol);
			else
				mset = metadata;

			CodeDomProvider code_provider = GetProvider (dotNetProject);
			
			List<IWsdlImportExtension> list = new List<IWsdlImportExtension> ();
			list.Add (new TransportBindingElementImporter ());
			list.Add (new XmlSerializerMessageContractImporter ());
			
			WsdlImporter importer = new WsdlImporter (mset);
			Collection<ContractDescription> contracts = importer.ImportAllContracts ();
			
			foreach (ContractDescription cd in contracts) {
				cd.Namespace = proxyNamespace;
				if (targetMoonlight || targetMonoTouch) {
					var moonctx = new MoonlightChannelBaseContext ();
					cd.Behaviors.Add (new MoonlightChannelBaseContractExtension (moonctx, targetMonoTouch));
					foreach (var od in cd.Operations)
						od.Behaviors.Add (new MoonlightChannelBaseOperationExtension (moonctx, targetMonoTouch));
					generator.GenerateServiceContractType (cd);
					moonctx.Fixup ();
				}
				else
					generator.GenerateServiceContractType (cd);
			}
			
			string fileSpec = Path.Combine (basePath, referenceName + "." + code_provider.FileExtension);
			using (TextWriter w = File.CreateText (fileSpec)) {
				code_provider.GenerateCodeFromCompileUnit (ccu, w, null);
			}
			return fileSpec;
		}
		
		ReferenceGroup ConvertMapFile (string mapFile)
		{
			DiscoveryClientProtocol prot = new DiscoveryClientProtocol ();
			DiscoveryClientResultCollection files = prot.ReadAll (mapFile);
			
			ReferenceGroup map = new ReferenceGroup ();
			
			if (refGroup != null)
				map.ClientOptions = refGroup.ClientOptions;
			
			Dictionary<string,int> sources = new Dictionary<string, int> ();
			foreach (DiscoveryClientResult res in files) {
				string url = res.Url;
				Uri uri = new Uri (url);
				if (!string.IsNullOrEmpty (uri.Query))
					url = url.Substring (0, url.Length - uri.Query.Length);
				int nSource;
				if (!sources.TryGetValue (url, out nSource)) {
					nSource = sources.Count + 1;
					sources [url] = nSource;
					MetadataSource ms = new MetadataSource ();
					ms.Address = url;
					ms.Protocol = uri.Scheme;
					ms.SourceId = nSource.ToString ();
					map.MetadataSources.Add (ms);
				}
				MetadataFile file = new MetadataFile ();
				file.FileName = res.Filename;
				file.ID = Guid.NewGuid ().ToString ();
				file.SourceId = nSource.ToString ();
				file.SourceUrl = res.Url;
				switch (Path.GetExtension (file.FileName).ToLower ()) {
					case ".disco": file.MetadataType = "Disco"; break;
					case ".wsdl": file.MetadataType = "Wsdl"; break;
					case ".xsd": file.MetadataType = "Schema"; break;
				}
				map.Metadata.Add (file);
			}
			map.Save (mapFile);
			return map;
		}
		
		MetadataSet ToMetadataSet (DiscoveryClientProtocol prot)
		{
			MetadataSet metadata = new MetadataSet ();
			foreach (object o in prot.Documents.Values) {
				if (o is System.Web.Services.Description.ServiceDescription) {
					metadata.MetadataSections.Add (
						new MetadataSection (MetadataSection.ServiceDescriptionDialect, "", (System.Web.Services.Description.ServiceDescription) o));
				}
				if (o is XmlSchema) {
					metadata.MetadataSections.Add (
						new MetadataSection (MetadataSection.XmlSchemaDialect, "", (XmlSchema) o));
				}
			}

			return metadata;
		}		
	}
	
	[XmlRoot (Namespace="urn:schemas-microsoft-com:xml-wcfservicemap")]
	public class ReferenceGroup
	{
		ClientOptions options = new ClientOptions ();
		List<MetadataSource> sources = new List<MetadataSource> ();
		List<MetadataFile> metadata = new List<MetadataFile> ();
		
		public ClientOptions ClientOptions {
			get { return options; }
			set { options = value; }
		}
		
		public List<MetadataSource> MetadataSources {
			get { return sources; }
		}
		
		public List<MetadataFile> Metadata {
			get { return metadata; }
		}
		
		public void Save (string file)
		{
			XmlSerializer ser = new XmlSerializer (typeof(ReferenceGroup));
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			using (XmlWriter w = XmlWriter.Create (file, settings)) {
				ser.Serialize (w, this);
			}
		}
		
		public static ReferenceGroup Read (string file)
		{
			using (StreamReader sr = new StreamReader (file)) {
				XmlSerializer ser = new XmlSerializer (typeof (ReferenceGroup));
				return (ReferenceGroup) ser.Deserialize (sr);
			}
		}
	}
	
	public class ClientOptions
	{
		public ClientOptions ()
		{
			EnableDataBinding = true;
			GenerateSerializableTypes = true;
			UseSerializerForFaults = true;
			ReferenceAllAssemblies = true;
			Serializer = "Auto";
			CollectionMappings = new List<CollectionMapping> ();
			ReferencedAssemblies = new List<ReferencedAssembly> ();
		}
		
		[XmlElement]
	    public bool GenerateAsynchronousMethods { get; set; }
		[XmlElement]
	    public bool EnableDataBinding { get; set; }
//	    string[] ExcludedTypes;
	    public bool ImportXmlTypes { get; set; }
	    public bool GenerateInternalTypes { get; set; }
	    public bool GenerateMessageContracts { get; set; }
//	    string[] NamespaceMappings;
	    List<CollectionMapping> CollectionMappings { get; set; }
	    public bool GenerateSerializableTypes { get; set; }
	    public string Serializer { get; set; }
	    public bool UseSerializerForFaults { get; set; }
	    public bool ReferenceAllAssemblies { get; set; }
	    List<ReferencedAssembly> ReferencedAssemblies { get; set; }
//	    string[] ReferencedDataContractTypes;
//	    string[] ServiceContractMappings;
	}
	
	public class CollectionMapping
	{
		[XmlAttribute]
		public string TypeName { get; set; }
		[XmlAttribute]
		public string Category { get; set; }
	}
	
	public class ReferencedAssembly
	{
		[XmlAttribute]
		public string AssemblyName { get; set; }
	}
	
	public class MetadataSource
	{
		[XmlAttribute]
		public string Address { get; set; }
		[XmlAttribute]
		public string Protocol { get; set; }
		[XmlAttribute]
		public string SourceId { get; set; }
	}
	
	public class MetadataFile
	{
		[XmlAttribute]
		public string FileName { get; set; }
		[XmlAttribute]
		public string MetadataType { get; set; }
		[XmlAttribute]
		public string ID { get; set; }
		[XmlAttribute]
		public string SourceId { get; set; }
		[XmlAttribute]
		public string SourceUrl { get; set; }
	}
	
	public class ExtensionFile
	{
		[XmlAttribute]
		public string FileName { get; set; }
		[XmlAttribute]
		public string Name { get; set; }
	}
}

