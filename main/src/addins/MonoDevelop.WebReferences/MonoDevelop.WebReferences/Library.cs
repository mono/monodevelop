using System;
using System.CodeDom;
using System.Text;
using System.IO;
using System.Net;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Xml;
using System.Xml.Schema;
using MonoDevelop.Projects;
using MonoDevelop.Core;


namespace MonoDevelop.WebReferences
{
	/// <summary>A Library class containig generic static methods for Web Services.</summary>
	public class Library
	{
		/// <summary>Read the service description for a specified uri.</summary>
		/// <param name="uri">A string containing the unique reference identifier for the service.</param>
		/// <returns>A ServiceDescription for the specified uri.</returns>
		public static ServiceDescription ReadServiceDescription(string uri) 
		{
			ServiceDescription desc = new ServiceDescription();
			try 
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
				WebResponse response  = request.GetResponse();
			
				desc = ServiceDescription.Read(response.GetResponseStream());
				response.Close();
				desc.RetrievalUrl = uri;
			} 
			catch (Exception) {} 
			
			return desc;
		}
		
		/// <summary>Read the specified protocol into an ServiceDescriptionImporter.</summary>
		/// <param name="protocol">A DiscoveryClientProtocol containing the service protocol detail.</param>
		/// <returns>A ServiceDescriptionImporter for the specified protocol.</returns>
		public static ServiceDescriptionImporter ReadServiceDescriptionImporter(DiscoveryClientProtocol protocol)
		{
   			// Service Description Importer
			ServiceDescriptionImporter importer = new ServiceDescriptionImporter();
			importer.ProtocolName = "Soap";
			// Add all the schemas and service descriptions to the importer
			protocol.ResolveAll ();
			foreach (object doc in protocol.Documents.Values)
			{
				if (doc is ServiceDescription)
					importer.AddServiceDescription((ServiceDescription)doc, null, null);
				else if (doc is XmlSchema)
					importer.Schemas.Add((XmlSchema)doc);
			}			
			return importer;
		}
		
		/// <summary>Generate a text description for the a DiscoverDocument.</summary>
		/// <param name="discovery">A DiscoveryDocument containing the details for the disco services.</param>
		/// <returns>An XmlDocument containing the generated xml for the specified discovery document.</returns>
		public static void GenerateDiscoXml (StringBuilder text, DiscoveryDocument doc)
		{
			text.Append ("<big><b>" + GettextCatalog.GetString ("Web Service References") + "</b></big>\n\n");
			foreach (DiscoveryReference dref in doc.References)
			{
				if (dref is ContractReference) {
					text.AppendFormat ("<b>Service: {0}</b>\n<span size='small'>{1}</span>", System.IO.Path.GetFileNameWithoutExtension (dref.DefaultFilename), dref.Url);
				}
				else if (dref is DiscoveryDocumentReference) {
					text.AppendFormat ("<b>Discovery document</b>\n<small>{0}</small>", dref.Url);
				}
				text.Append ("\n\n");
			}
		}
		
		/// <summary>Generate a text description for the specified DiscoveryClientProtocol.</summary>
		/// <param name="protocol">A DiscoveryClientProtocol containing the information for the service.</param>
		/// <returns>An XmlDocument containing the generated xml for the specified discovery protocol.</returns>
		public static void GenerateWsdlXml (StringBuilder text, DiscoveryClientProtocol protocol)
		{
			// Code Namespace & Compile Unit
			CodeNamespace codeNamespace = new CodeNamespace();
			CodeCompileUnit codeUnit = new CodeCompileUnit();
			codeUnit.Namespaces.Add(codeNamespace);
			
			// Import and set the warning
			ServiceDescriptionImporter importer = ReadServiceDescriptionImporter(protocol);
			importer.Import(codeNamespace, codeUnit);
			
			foreach (CodeTypeDeclaration type in codeNamespace.Types)
			{
				if (type.BaseTypes.Count == 0 || type.BaseTypes[0].BaseType != "System.Web.Services.Protocols.SoapHttpClientProtocol")
					continue;
					
				text.AppendFormat ("<big><b><u>{0}</u></b></big>\n\n", type.Name);
				string coms = GetCommentElements (type);
				if (coms != null)
					text.Append (coms).Append ("\n\n");
				
				foreach (CodeTypeMember mem in type.Members)
				{
					CodeMemberMethod met = mem as CodeMemberMethod;
					if (met != null && !(mem is CodeConstructor))
					{
						// Method
						// Asynch Begin & End Results
						string returnType = met.ReturnType.BaseType;
						if (met.Name.StartsWith ("Begin") && returnType == "System.IAsyncResult") 
							continue;	// BeginXXX method
						if (met.Name.EndsWith ("Async"))
							continue;
						if (met.Name.StartsWith ("On") && met.Name.EndsWith ("Completed"))
							continue;
						if (met.Parameters.Count > 0)
						{
							CodeParameterDeclarationExpression par = met.Parameters [met.Parameters.Count-1];
							if (met.Name.StartsWith ("End") && par.Type.BaseType == "System.IAsyncResult")
								continue;	// EndXXX method
						}
						text.AppendFormat ("<b>{0}</b> (", met.Name);
						// Parameters
						for (int n=0; n < met.Parameters.Count; n++) {
							CodeParameterDeclarationExpression par = met.Parameters [n];
							if (n > 0)
								text.Append (", ");
							text.AppendFormat ("{0}: <i>{1}</i>", par.Name, par.Type.BaseType);
						}
						text.Append (")");
						if (returnType != "System.Void")
							text.AppendFormat (": <i>{0}</i>", returnType);
						
						// Comments
						coms = GetCommentElements (met);
						if (coms != null)
							text.Append ("\n").Append (coms);
						text.Append ("\n\n");
					}
				}
			}
		}
		
		public static string GetCommentElements (CodeTypeMember member)
		{
			StringBuilder coms = new StringBuilder ();
			foreach (CodeCommentStatement comment in member.Comments)
			{
				string com = comment.Comment.Text;
				com = com.Replace ("<remarks>","");
				com = com.Replace ("</remarks>","");
				com = com.Replace ("<remarks/>","");
				com = com.Trim (' ', '\n', '\t', '\r');
				if (com.Length > 0)
					coms.Append (com);
			}
			if (coms.Length > 0)
				return coms.ToString ();
			else
				return null;
		}
		
		/// <summary>Gets the path where all web references will be stored for the specified project.</summary>
		/// <param name="project">A Project containing the root project information.</project>
		/// <returns>A string containing the base path for web references.</returns>
		public static FilePath GetWebReferencePath (Project project)
		{
			return project.BaseDirectory.Combine ("WebReferences");
		}
		
		/// <summary>Checks whether or not the current project does contain any web references.</summary>
		/// <returns>True if the project does contain any web references, otherwise false.</returns>
		public static bool ProjectContainsWebReference(Project project)
		{
			string webRefPath = Library.GetWebReferencePath(project);
			foreach (ProjectFile file in project.Files)
			{
				if (file.FilePath.IsChildPathOf (webRefPath))
					return true;
			}
			return false;
		}
	}
	
}
