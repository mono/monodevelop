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

using System.Web.Services.Discovery;
using System.Net;
using System.Text;
using MonoDevelop.Projects;
using System.Web.Services.Description;
using System.IO;
using System.CodeDom.Compiler;
using System.CodeDom;

namespace MonoDevelop.WebReferences
{
	public class WebServiceEngineWS : WebServiceEngine
	{
		public override WebServiceDiscoveryResult Discover (string url)
		{
			DiscoveryClientProtocol protocol = DiscoResolve (url);
			if (protocol != null)
				return new WebServiceDiscoveryResultWS (protocol, null);
			else
				return null;
		}

		public override WebServiceDiscoveryResult Load (WebReferenceItem item)
		{
			// Read the map file into the discovery client protocol and setup the code generator
			DiscoveryProtocol protocol = new DiscoveryProtocol ();
			protocol.ReadAllUseBasePath (item.MapFile.FilePath);
			return new WebServiceDiscoveryResultWS (protocol, item);
		}
	}

	class WebServiceDiscoveryResultWS : WebServiceDiscoveryResult
	{
		DiscoveryClientProtocol protocol;

		public WebServiceDiscoveryResultWS (DiscoveryClientProtocol protocol, WebReferenceItem item): base (item)
		{
			this.protocol = protocol;
		}
		
		public DiscoveryClientProtocol Protocol {
			get { return this.protocol; }
		}

		public override string GetDescriptionMarkup ()
		{
			StringBuilder text = new StringBuilder ();
			foreach (object dd in protocol.Documents.Values) {
				if (dd is ServiceDescription) {
					Library.GenerateWsdlXml (text, protocol);
					break;
				} else if (dd is DiscoveryDocument) {
					Library.GenerateDiscoXml (text, (DiscoveryDocument)dd);
					break;
				}
			}
			return text.ToString ();
		}
		
		protected override string GenerateDescriptionFiles (string basePath)
		{
			protocol.ResolveAll ();
			protocol.WriteAll (basePath, "Reference.map");
			return Path.Combine (basePath, "Reference.map");
		}

		public override void Update ()
		{
			// Refresh the disco and wsdl from the server
			foreach (object doc in protocol.References.Values) {
				string url = null;
				if (doc is DiscoveryDocumentReference) {
					url = ((DiscoveryDocumentReference)doc).Url;
				} else if (doc is ContractReference) {
					url = ((ContractReference)doc).Url;
				}
				
				if (!string.IsNullOrEmpty (url)) {
					try {
						WebServiceEngine engine = WebReferencesService.WsEngine;
						WebServiceDiscoveryResultWS ws = (WebServiceDiscoveryResultWS) engine.Discover (url);
						protocol = ws.protocol;
						break;
					} catch (WebException) {
					}
				}
			}
			protocol.ResolveAll ();
			
			// Re-generate the proxy and map files
			string basePath = new FileInfo (Item.MapFile.FilePath).Directory.FullName;
			CreateProxyFile ((DotNetProject)Item.ProxyFile.Project, basePath, Item.MapFile.Project.Name + "." + Item.Name, "Reference");
			protocol.WriteAll (basePath, "Reference.map");
		}
		
		public override System.Collections.Generic.IEnumerable<string> GetAssemblyReferences ()
		{
			yield return "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
			yield return "System.Web.Services, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
			yield return "System.Xml, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
		}
		
		protected override string CreateProxyFile (DotNetProject dotNetProject, string basePath, string proxyNamespace, string referenceName)
		{
			// Setup the proxy namespace and compile unit
			ICodeGenerator codeGen = GetProvider (dotNetProject).CreateGenerator();
			CodeNamespace codeNamespace = new CodeNamespace(proxyNamespace);
			CodeConstructor urlConstructor = new CodeConstructor ();
			CodeCompileUnit codeUnit = new CodeCompileUnit();
			codeUnit.Namespaces.Add(codeNamespace);

			// Setup the importer and import the service description into the code unit
			ServiceDescriptionImporter importer = Library.ReadServiceDescriptionImporter(protocol);
			importer.Import(codeNamespace, codeUnit);

			// Add the new Constructor with Url as a paremeter
			// Search for the class which inherit SoapHttpClientProtocol (Which is the Service Class)
			foreach (CodeTypeDeclaration declarationType in codeUnit.Namespaces[0].Types) 
				if (declarationType.IsClass) 
					if (declarationType.BaseTypes.Count > 0)
						// Is a Service Class
						if (declarationType.BaseTypes [0].BaseType.IndexOf ("SoapHttpClientProtocol") > -1) {
							// Create new public constructor with the Url as parameter
							urlConstructor.Attributes = MemberAttributes.Public;
							urlConstructor.Parameters.Add (new CodeParameterDeclarationExpression ("System.String", "url"));
							urlConstructor.Statements.Add (new CodeAssignStatement (
						                                                        new CodePropertyReferenceExpression (new CodeThisReferenceExpression(), 
						                                                                                             "Url"),
						                                                        new CodeVariableReferenceExpression ("url")));
							declarationType.Members.Add (urlConstructor);
						}
			
			// Generate the code and save the file
			string fileSpec = Path.Combine (basePath, dotNetProject.LanguageBinding.GetFileName (referenceName));
			StreamWriter writer = new StreamWriter(fileSpec);
			codeGen.GenerateCodeFromCompileUnit(codeUnit, writer, new CodeGeneratorOptions());
			
			writer.Close();
			
			return fileSpec;
		}
	}
}

