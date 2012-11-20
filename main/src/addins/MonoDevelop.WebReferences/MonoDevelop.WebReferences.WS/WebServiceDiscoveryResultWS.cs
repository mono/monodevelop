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
using System.Linq;
using System.Text;
using MonoDevelop.Projects;
using System.Web.Services.Description;
using System.IO;
using System.CodeDom.Compiler;
using System.CodeDom;
using MonoDevelop.Core;
using WebReferencesDir = MonoDevelop.WebReferences.WS.WebReferences;
using System.Collections.Generic;

namespace MonoDevelop.WebReferences.WS
{
	class WebServiceDiscoveryResultWS : WebServiceDiscoveryResult
	{
		DiscoveryClientProtocol protocol;

		public WebServiceDiscoveryResultWS (DiscoveryClientProtocol protocol, WebReferenceItem item): base (WebReferencesService.WsEngine, item)
		{
			this.protocol = protocol;
		}
		
		public DiscoveryClientProtocol Protocol {
			get { return this.protocol; }
		}

		public override FilePath GetReferencePath (DotNetProject project, string refName)
		{
			return project.BaseDirectory.Combine ("Web References").Combine (refName);
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
		
		public override string ProxyGenerator {
			get {
				return "MSDiscoCodeGenerator";
			}
		}
		
		protected override string GenerateDescriptionFiles (DotNetProject project, FilePath basePath)
		{
			if (!project.Items.GetAll<WebReferencesDir> ().Any ()) {
				WebReferencesDir met = new WebReferencesDir ();
				met.Path = basePath.ParentDirectory;
				project.Items.Add (met);
			}
			
			WebReferenceUrl wru = project.Items.GetAll<WebReferenceUrl> ().FirstOrDefault (m => m.RelPath.CanonicalPath == basePath);
			if (wru == null) {
				wru = new WebReferenceUrl (protocol.Url);
				wru.RelPath = basePath;
				project.Items.Add (wru);
			}
			
			protocol.ResolveAll ();
			DiscoveryClientResultCollection files = protocol.WriteAll (basePath, "Reference.map");
			
			foreach (DiscoveryClientResult dr in files)
				project.AddFile (new FilePath (dr.Filename).ToAbsolute (basePath), BuildAction.None);
			
			return Path.Combine (basePath, "Reference.map");
		}

		public override void Update ()
		{
			WebReferenceUrl wru = Item.Project.Items.GetAll<WebReferenceUrl> ().FirstOrDefault (m => m.RelPath.CanonicalPath == Item.BasePath);
			if (wru == null)
				return;
			
			WebServiceDiscoveryResultWS wref = (WebServiceDiscoveryResultWS) WebReferencesService.WsEngine.Discover (wru.UpdateFromURL);
			if (wref == null)
				return;
			
			protocol = wref.protocol;
			
			// Re-generate the proxy and map files
			GenerateFiles (Item.Project, Item.Project.DefaultNamespace, Item.Name);
		}
		
		public override System.Collections.Generic.IEnumerable<string> GetAssemblyReferences ()
		{
			yield return "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
			yield return "System.Web.Services, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
			yield return "System.Xml, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
		}
		
		protected override string CreateProxyFile (DotNetProject dotNetProject, FilePath basePath, string proxyNamespace, string referenceName)
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

		public override string GetServiceURL ()
		{
			WebReferenceUrl wru = Item.Project.Items.GetAll<WebReferenceUrl> ().FirstOrDefault (m => m.RelPath.CanonicalPath == Item.BasePath);
			if (wru == null)
				return null;

			return wru.ServiceLocationURL;
		}
	}
}
