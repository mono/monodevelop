using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Web.Services.Discovery;
using System.Web.Services.Description;
using MonoDevelop.Projects;

namespace MonoDevelop.WebReferences
{
	/// <summary>Defines the properties and methods for the CodeGenerator class.</summary>
	public class CodeGenerator
	{
		#region Properties
		/// <summary>Gets or sets the project assocated with the Code Generator.</summary>
		/// <value>A Project containing all the project information.</summary>
		public Project Project
		{
			get { return project; }
			set { project = value; }
		}
		
		/// <summary>Gets the lanague binding assocated with the Code Generator.</summary>
		/// <value>An IDotNetLanguageBinding containing the default language information for the current project.</summary>
		public IDotNetLanguageBinding LanguageBinding
		{
			get
			{
				if (languageBinding == null)
				{
					DotNetProject dotNetProject = (DotNetProject) project;
					languageBinding = dotNetProject.LanguageBinding;
				}
				return languageBinding;
			}
		}
		
		/// <summary>Gets or sets the discovery client protocol assocated with the code generator.</summary>
		/// <value>A DiscoveryClientProtocol containing all the web service information.</value>
		public DiscoveryClientProtocol Protocol
		{
			get { return protocol; }
			set { protocol = value; }
		}
		
		// <summary>Gets the default file language for the project.</summary>
		/// <value>A string containing the default language.</value>
		public string DefaultLanguage
		{
			get { return this.LanguageBinding.Language; }
		}
		
		/// <summary>Gets the default file extension based on the default language for the project.</summary>
		/// <value>A string containing the default file extension.</value>
		public string DefaultExtension
		{
			get
			{
				string ext = "";
				switch (this.DefaultLanguage)
				{
					case "C#":
						ext = ".cs";
						break;
					case "VBNet":
						ext = ".vb";
						break;
				}
				return ext;
			}
		}
		
		/// <summary>Gets the CodeDomProvider for the default lanuage.</summary>
		/// <value>A CodeDomProvider containing the code generation provider for the default language.</value>
		public CodeDomProvider Provider
		{
			get
			{
				if (provider == null)
					provider = LanguageBinding.GetCodeDomProvider();
					
				// Throw an exception if no provider has been set
				if (provider == null)
					throw new Exception("Language not supported");
					
				return provider;
			}
		}
		#endregion
		
		#region Member Variables
		private Project project;
		private DiscoveryClientProtocol protocol;
		private CodeDomProvider provider;
		private IDotNetLanguageBinding languageBinding;
		#endregion
		
		/// <summary>Initializes a new instance of the CodeGenerator class by specifying the parent project and discovery client protocol.</summary>
		/// <param name="project">A Project containing the parent project for the Code Generator.</param>
		/// <param name="protocol">A DiscoveryClientProtocol containing the protocol for the Code Generator.</param>
		public CodeGenerator(Project project, DiscoveryClientProtocol protocol)
		{
			this.project = project;
			this.protocol = protocol;
		}
		
		/// <summary>Generate the proxy file for the web service.</summary>
		/// <param name="basePath">A string containing the base path for the proxy file.</param>
		/// <param name="proxyNamespace">A string containing the namespace for the proxy class.</param>
		/// <param name="referenceName">A string containing the file name for the proxy file.</param>
		public string CreateProxyFile(string basePath, string proxyNamespace, string referenceName)
		{
			// Setup the proxy namespacec and compile unit
			ICodeGenerator codeGen = Provider.CreateGenerator();
			CodeNamespace codeNamespace = new CodeNamespace(proxyNamespace);
			CodeConstructor urlConstructor = new CodeConstructor ();
			CodeCompileUnit codeUnit = new CodeCompileUnit();
			codeUnit.Namespaces.Add(codeNamespace);

			// Setup the importer and import the service description into the code unit
			ServiceDescriptionImporter importer = Library.ReadServiceDescriptionImporter(protocol);
			ServiceDescriptionImportWarnings warnings = importer.Import(codeNamespace, codeUnit);

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
			string fileSpec = Path.Combine(basePath, referenceName + DefaultExtension);
			StreamWriter writer = new StreamWriter(fileSpec);
			codeGen.GenerateCodeFromCompileUnit(codeUnit, writer, new CodeGeneratorOptions());
			
			writer.Close();
			
			return fileSpec;
		}
		
		/// <summary>Generate the map file for the web service.</summary>
		/// <param name="basePath">A string containing the base path for the map file.</param>
		/// <param name="referenceName">A string containing the file name for the map file.</param>
		public string CreateMapFile (string basePath, string filename)
		{
			protocol.ResolveAll ();
			protocol.WriteAll (basePath, filename);
			return Path.Combine(basePath, filename);
		}
	}	
}
