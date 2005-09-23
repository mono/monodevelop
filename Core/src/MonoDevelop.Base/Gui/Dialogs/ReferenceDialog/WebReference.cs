// created on 10/11/2002 at 2:08 PM

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Xml;
using System.Net;
using System.Web.Services.Description;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;

using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Gui.Dialogs
{
	/// <summary>
	/// Summary description for WebReference.
	/// </summary>
	internal class WebReference
	{	
		///
		/// <summary>Creates a ServiceDescription object from a valid URI</summary>
		/// 
		public static ServiceDescription ReadServiceDescription(string uri) {
			ServiceDescription desc = null;
			
			try {
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
				WebResponse response  = request.GetResponse();
			
				desc = ServiceDescription.Read(response.GetResponseStream());
				response.Close();
				desc.RetrievalUrl = uri;
			} catch (Exception) {				
				// possibly error reading WSDL?
				return null;
			} 		
			if(desc.Services.Count == 0)
				return null;
			
			return desc;
		}
		
		///
		/// <summary>Generates a valid directory from a URI</summary>
		/// 
		public static string GetDirectoryFromUri(string uri) {
			// TODO: construct the namespace using th URL in the WSDL
			string tmp = uri;
			if(uri.IndexOf("://") > -1) {
				tmp = uri.Substring(uri.IndexOf("://") + 3);
			}
			tmp = tmp.Substring(0, tmp.LastIndexOf("/"));			
			string[] dirs = tmp.Split(new Char[] {'/'});
						
			StringBuilder savedir = new StringBuilder();
			savedir.Append(dirs[0]);
		
			return savedir.ToString();
		}
		
		///
		/// <summary>Generates a valid Namespace from a URI</summary>
		/// 
		public static string GetNamespaceFromUri(string uri) {
			// TODO: construct the namespace using th URL in the WSDL
			string tmp = uri;
			if(uri.IndexOf("://") > -1) {
				tmp = uri.Substring(uri.IndexOf("://") + 3);
			}
			tmp = tmp.Substring(0, tmp.LastIndexOf("/"));			
			string[] dirs = tmp.Split(new Char[] {'/'});
											
			return(dirs[0]);			
		}
		
		
		public static ProjectReference GenerateWebProxyDLL(Project project, ServiceDescription desc) {
			ProjectReference refInfo = null;
			
			string nmspace = GetNamespaceFromUri(desc.RetrievalUrl);
			
			StringBuilder savedir = new StringBuilder();
			savedir.Append(project.BaseDirectory);
			savedir.Append(Path.DirectorySeparatorChar);
			savedir.Append("WebReferences");			
			// second, create the path if it doesn't exist
			if(!Directory.Exists(savedir.ToString()))
				Directory.CreateDirectory(savedir.ToString());
			
			// generate the assembly
			ServiceDescriptionImporter importer = new ServiceDescriptionImporter();
			importer.AddServiceDescription(desc, null, null);
			
			CodeNamespace codeNamespace = new CodeNamespace(nmspace);
			CodeCompileUnit codeUnit = new CodeCompileUnit();
			codeUnit.Namespaces.Add(codeNamespace);
			importer.Import(codeNamespace, codeUnit);
			
			CodeDomProvider provider = new Microsoft.CSharp.CSharpCodeProvider();			
			System.CodeDom.Compiler.ICodeCompiler compiler;
			
			
			if(provider != null) {
				compiler = provider.CreateCompiler();
				CompilerParameters parms = new CompilerParameters();
				parms.ReferencedAssemblies.Add("System.Dll");
				parms.ReferencedAssemblies.Add("System.Xml.Dll");
				parms.ReferencedAssemblies.Add("System.Web.Services.Dll");
				parms.OutputAssembly = project.BaseDirectory + Path.DirectorySeparatorChar + "WebReferences" + Path.DirectorySeparatorChar + nmspace + ".Reference.Dll";
				CompilerResults results = compiler.CompileAssemblyFromDom(parms, codeUnit);
				Assembly assembly = results.CompiledAssembly;
				
				if(assembly != null) {
					refInfo = new ProjectReference();
					refInfo.ReferenceType = ReferenceType.Assembly;
					refInfo.Reference = parms.OutputAssembly;
				}
			}
			
			return refInfo;
		}
		
		///
		/// <summary>Generates a Web Service proxy DLL from a URI</summary>
		/// 
		public static ProjectReference GenerateWebProxyDLL(Project project, string url) {
									
			ServiceDescription desc = ReadServiceDescription(url);						
			return GenerateWebProxyDLL(project, desc);
									
		}
		
		public static ArrayList GenerateWebProxyCode(Project project, ServiceDescription desc) {		
			ArrayList fileList = null;
			
			string serviceName = String.Empty;
			if(desc.Services.Count > 0) {
				serviceName = desc.Services[0].Name;
			} else {
				serviceName = "UnknownService";
			}									
			
			string webRefFolder = "Web References";
			string nmspace = GetNamespaceFromUri(desc.RetrievalUrl);
												
			StringBuilder savedir = new StringBuilder();
			savedir.Append(project.BaseDirectory);
			savedir.Append(Path.DirectorySeparatorChar);
			savedir.Append(webRefFolder);
			savedir.Append(Path.DirectorySeparatorChar);			
			savedir.Append(GetDirectoryFromUri(desc.RetrievalUrl) + Path.DirectorySeparatorChar + serviceName);
			
			// second, create the path if it doesn't exist
			if(!Directory.Exists(savedir.ToString()))
				Directory.CreateDirectory(savedir.ToString());
			
			// generate the assembly
			ServiceDescriptionImporter importer = new ServiceDescriptionImporter();
			importer.AddServiceDescription(desc, null, null);
			
			CodeNamespace codeNamespace = new CodeNamespace(nmspace);
			CodeCompileUnit codeUnit = new CodeCompileUnit();
			codeUnit.Namespaces.Add(codeNamespace);
			importer.Import(codeNamespace, codeUnit);
			
			CodeDomProvider provider;
			System.CodeDom.Compiler.ICodeGenerator generator;
			
			String ext = String.Empty;
			switch(project.ProjectType) {
				case "C#":
					provider = new Microsoft.CSharp.CSharpCodeProvider();
					ext = "cs";
					break;
				case "VBNET":
					provider = new Microsoft.VisualBasic.VBCodeProvider();					
					ext = "vb";
					break;
							
				default:
					// project type not supported error
					provider = null;			
					break;
			}

			string filename = savedir.ToString() + Path.DirectorySeparatorChar + serviceName + "WebProxy." + ext;
			string wsdlfilename = savedir.ToString() + Path.DirectorySeparatorChar + serviceName + ".wsdl";
						
			if(provider != null) {				
				StreamWriter sw = new StreamWriter(filename);

				generator = provider.CreateGenerator();
				CodeGeneratorOptions options = new CodeGeneratorOptions();
				options.BracingStyle = "C";
				generator.GenerateCodeFromCompileUnit(codeUnit, sw, options);
				sw.Close();

				if(File.Exists(filename)) 
				{
					fileList = new ArrayList();
					
					// add project files to the list
					ProjectFile pfile = new ProjectFile();
					
					pfile.Name = project.BaseDirectory + Path.DirectorySeparatorChar + webRefFolder;
					pfile.BuildAction = BuildAction.Nothing;					
					pfile.Subtype = Subtype.WebReferences;
					pfile.DependsOn = String.Empty;
					pfile.Data = String.Empty;										
					fileList.Add(pfile);
					
					/*
					pfile = new ProjectFile();
					pfile.Name = project.BaseDirectory + @"\Web References\" + nmspace;
					pfile.BuildAction = BuildAction.Nothing;
					pfile.Subtype = Subtype.Directory;
					pfile.DependsOn = project.BaseDirectory + @"\Web References\";
					pfile.WebReferenceUrl = String.Empty;															
					fileList.Add(pfile);
					*/					
					/*
					pfile = new ProjectFile();
					pfile.Name = project.BaseDirectory + @"\Web References\" + nmspace + @"\" + serviceName;
					pfile.BuildAction = BuildAction.Nothing;
					pfile.Subtype = Subtype.Directory;
					pfile.DependsOn = project.BaseDirectory + @"\Web References\" + nmspace + @"\";
					pfile.WebReferenceUrl = desc.RetrievalUrl;
					fileList.Add(pfile);										
					*/
					// the Web Reference Proxy
					pfile = new ProjectFile();
					pfile.Name = filename;
					pfile.BuildAction = BuildAction.Compile;
					pfile.Subtype = Subtype.Code;
					pfile.DependsOn = project.BaseDirectory + Path.DirectorySeparatorChar + webRefFolder;
					pfile.Data = desc.RetrievalUrl;					
					fileList.Add(pfile);										
					
					// the WSDL File used to generate the Proxy
					desc.Write(wsdlfilename);
					pfile = new ProjectFile();
					pfile.Name = wsdlfilename;
					pfile.BuildAction = BuildAction.Nothing;
					pfile.Subtype = Subtype.Code;
					pfile.DependsOn = project.BaseDirectory + Path.DirectorySeparatorChar + webRefFolder;
					pfile.Data = desc.RetrievalUrl;
					fileList.Add(pfile);
				}
			}
			
			return fileList;
		}
		
		///
		/// <summary>Generates a Web Service proxy class from a URI</summary>
		/// 
		public static ArrayList GenerateWebProxyCode(Project project, string url) {
			
			
			ServiceDescription desc = ReadServiceDescription(url);
			
			return GenerateWebProxyCode(project, desc);
			
			
		}
	}
}
