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
using System.Linq;
using System.IO;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Web.Services.Discovery;
using System.Runtime.Serialization;
using MonoDevelop.Projects;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.Xml.Schema;
using System.Text;
using Mono.ServiceContractTool;
using MonoDevelop.Core;

namespace MonoDevelop.WebReferences.WCF
{
	class WebServiceDiscoveryResultWCF: WebServiceDiscoveryResult
	{
		MetadataSet metadata;
		DiscoveryClientProtocol protocol;
		ReferenceGroup refGroup;
		ClientOptions defaultOptions;

		public WebServiceDiscoveryResultWCF (DiscoveryClientProtocol protocol, MetadataSet metadata, WebReferenceItem item, ReferenceGroup refGroup, ClientOptions defaultOptions): base (WebReferencesService.WcfEngine, item)
		{
			this.refGroup = refGroup;
			this.protocol = protocol;
			this.metadata = metadata;
			this.defaultOptions = defaultOptions;
		}

		public override FilePath GetReferencePath (DotNetProject project, string refName)
		{
			return project.BaseDirectory.Combine ("Service References").Combine (refName);
		}

		public ClientOptions ClientOptions {
			get { return refGroup != null ? refGroup.ClientOptions : null; }
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
		
		public override string ProxyGenerator {
			get {
				return "WCF Proxy Generator";
			}
		}
		
		protected override string GenerateDescriptionFiles (DotNetProject project, FilePath basePath)
		{
			if (!project.Items.GetAll<WCFMetadata> ().Any ()) {
				WCFMetadata met = new WCFMetadata ();
				met.Path = basePath.ParentDirectory;
				project.Items.Add (met);
			}
			
			WCFMetadataStorage metStor = project.Items.GetAll<WCFMetadataStorage> ().FirstOrDefault (m => m.Path.CanonicalPath == basePath);
			if (metStor == null)
				project.Items.Add (new WCFMetadataStorage () { Path = basePath });
			
			string file = Path.Combine (basePath, "Reference.svcmap");
			if (protocol != null) {
				protocol.ResolveAll ();
				protocol.WriteAll (basePath, "Reference.svcmap");
				refGroup = ConvertMapFile (file);
			} else {
				// TODO
				ReferenceGroup map = new ReferenceGroup ();
				map.ClientOptions = defaultOptions;
				map.Save (file);
				map.ID = Guid.NewGuid ().ToString ();
				refGroup = map;
			}
			foreach (MetadataFile mfile in refGroup.Metadata)
				project.AddFile (new FilePath (mfile.FileName).ToAbsolute (basePath), BuildAction.None);
			
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
			GenerateFiles (Item.Project, Item.Project.DefaultNamespace, Item.Name);
		}
		
		public override System.Collections.Generic.IEnumerable<string> GetAssemblyReferences ()
		{
			yield return "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
			yield return "System.ServiceModel, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
			yield return "System.Xml, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
		}
		
		protected override string CreateProxyFile (DotNetProject dotNetProject, FilePath basePath, string proxyNamespace, string referenceName)
		{
			CodeCompileUnit ccu = new CodeCompileUnit ();
			CodeNamespace cns = new CodeNamespace (proxyNamespace);
			ccu.Namespaces.Add (cns);
			
			bool targetMoonlight = dotNetProject.TargetFramework.Id.Identifier == ("Silverlight");
			bool targetMonoTouch = dotNetProject.TargetFramework.Id.Identifier == ("MonoTouch");
			bool targetMonoDroid = dotNetProject.TargetFramework.Id.Identifier == ("MonoDroid");
			
			bool targetCoreClr = targetMoonlight || targetMonoDroid || targetMonoTouch;
			bool generateSyncMethods = targetMonoDroid | targetMonoTouch;
			
			ServiceContractGenerator generator = new ServiceContractGenerator (ccu);
			generator.Options = ServiceContractGenerationOptions.ChannelInterface | ServiceContractGenerationOptions.ClientClass;
			if (refGroup.ClientOptions.GenerateAsynchronousMethods || targetCoreClr)
				generator.Options |= ServiceContractGenerationOptions.AsynchronousMethods;
			if (refGroup.ClientOptions.GenerateInternalTypes)
				generator.Options |= ServiceContractGenerationOptions.InternalTypes;
			if (refGroup.ClientOptions.GenerateMessageContracts)
				generator.Options |= ServiceContractGenerationOptions.TypedMessages;
//			if (targetMoonlight || targetMonoTouch)
//				generator.Options |= ServiceContractGenerationOptions.EventBasedAsynchronousMethods;
			
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
			try {
				ConfigureImporter (importer);
			} catch {
				;
			}

			Collection<ContractDescription> contracts = importer.ImportAllContracts ();
			
			foreach (ContractDescription cd in contracts) {
				cd.Namespace = proxyNamespace;
				if (targetCoreClr) {
					var moonctx = new MoonlightChannelBaseContext ();
					cd.Behaviors.Add (new MoonlightChannelBaseContractExtension (moonctx, generateSyncMethods));
					foreach (var od in cd.Operations)
						od.Behaviors.Add (new MoonlightChannelBaseOperationExtension (moonctx, generateSyncMethods));
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

		void ConfigureImporter (WsdlImporter importer)
		{
			var xsdImporter = new XsdDataContractImporter ();
			var options = new ImportOptions ();

			var listMapping = refGroup.ClientOptions.CollectionMappings.FirstOrDefault (
				m => m.Category == "List");
			if (listMapping != null) {
				var listType = Dialogs.WCFConfigWidget.GetType (listMapping.TypeName);
				if (listType != null)
					options.ReferencedCollectionTypes.Add (listType);
			}

			var dictMapping = refGroup.ClientOptions.CollectionMappings.FirstOrDefault (
				m => m.Category == "Dictionary");
			if (dictMapping != null) {
				var dictType = Dialogs.WCFConfigWidget.GetType (dictMapping.TypeName);
				if (dictType != null)
					options.ReferencedCollectionTypes.Add (dictType);
			}

			xsdImporter.Options = options;
			importer.State.Add (typeof (XsdDataContractImporter), xsdImporter);
		}
		
		ReferenceGroup ConvertMapFile (string mapFile)
		{
			DiscoveryClientProtocol prot = new DiscoveryClientProtocol ();
			DiscoveryClientResultCollection files = prot.ReadAll (mapFile);
			
			ReferenceGroup map = new ReferenceGroup ();
			
			if (refGroup != null) {
				map.ClientOptions = refGroup.ClientOptions;
				map.ID = refGroup.ID;
			} else {
				map.ClientOptions = defaultOptions;
				map.ID = Guid.NewGuid ().ToString ();
			}
			
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

		public override string GetServiceURL ()
		{
			ReferenceGroup resfile = ReferenceGroup.Read (Item.MapFile.FilePath);
			if (resfile.MetadataSources.Count == 0)
				return null;
			return resfile.MetadataSources [0].Address;
		}
	}
}
