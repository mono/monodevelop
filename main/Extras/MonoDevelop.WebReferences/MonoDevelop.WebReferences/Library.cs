using System;
using System.CodeDom;
using System.IO;
using System.Net;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Xml;
using System.Xml.Schema;
using MonoDevelop.Projects;


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
		
		/// <summary>Generate a XmlDocument for the a DiscoverDocument.</summary>
		/// <param name="discovery">A DiscoveryDocument containing the details for the disco services.</param>
		/// <returns>An XmlDocument containing the generated xml for the specified discovery document.</returns>
		public static XmlDocument GenerateDiscoXml (DiscoveryDocument discovery)
		{
			XmlDocument xdoc = new XmlDocument ();
			XmlElement docelem = xdoc.CreateElement ("services");
			xdoc.AppendChild (docelem);
			foreach (DiscoveryReference dref in discovery.References)
			{
				if (dref is ContractReference)
				{
					XmlElement service = xdoc.CreateElement ("service");
					docelem.AppendChild (service);
					service.SetAttribute ("name", Path.GetFileNameWithoutExtension (dref.DefaultFilename));
					service.SetAttribute ("url", dref.Url);
				}
				if (dref is DiscoveryDocumentReference)
				{
					XmlElement service = xdoc.CreateElement ("disco");
					docelem.AppendChild (service);
					service.SetAttribute ("url", dref.Url);
				}
			}
			return xdoc;
		}
		
		/// <summary>Generate an XmlDocument for the specified DiscoveryClientProtocol.</summary>
		/// <param name="protocol">A DiscoveryClientProtocol containing the information for the service.</param>
		/// <returns>An XmlDocument containing the generated xml for the specified discovery protocol.</returns>
		public static XmlDocument GenerateWsdlXml (DiscoveryClientProtocol protocol)
		{
			// Code Namespace & Compile Unit
			CodeNamespace codeNamespace = new CodeNamespace();
			CodeCompileUnit codeUnit = new CodeCompileUnit();
			codeUnit.Namespaces.Add(codeNamespace);
			
			// Import and set the warning
			ServiceDescriptionImporter importer = ReadServiceDescriptionImporter(protocol);
			importer.Import(codeNamespace, codeUnit);
			
			// Create Xml Document
			XmlDocument xdoc = new XmlDocument ();
			XmlElement docelem = xdoc.CreateElement ("services");
			xdoc.AppendChild (docelem);
			
			foreach (CodeTypeDeclaration type in codeNamespace.Types)
			{
				XmlElement service = xdoc.CreateElement ("service");
				service.SetAttribute ("name", type.Name);
				foreach (CodeTypeMember mem in type.Members)
				{
					CodeMemberMethod met = mem as CodeMemberMethod;
					if (met != null && !(mem is CodeConstructor))
					{
						// Method
						XmlElement xmet = xdoc.CreateElement ("method");
						xmet.SetAttribute ("name", met.Name);
						// Asynch Begin & End Results
						string returnType = met.ReturnType.BaseType;
						if (met.Name.StartsWith ("Begin") && returnType == "System.IAsyncResult") 
							continue;	// BeginXXX method
						if (met.Parameters.Count > 0)
						{
							CodeParameterDeclarationExpression par = met.Parameters [met.Parameters.Count-1];
							if (met.Name.StartsWith ("End") && par.Type.BaseType == "System.IAsyncResult")
								continue;	// EndXXX method
						}
						xmet.SetAttribute ("return", returnType);
						// Parameters
						foreach (CodeParameterDeclarationExpression par in met.Parameters)
						{
							XmlElement xpar = xdoc.CreateElement ("parameter");
							xmet.AppendChild (xpar);
							xpar.SetAttribute ("name", par.Name);
							xpar.SetAttribute ("type", par.Type.BaseType);
						}
						// Comments
						AddCommentElements (xdoc, xmet, met);
						service.AppendChild (xmet);
					}
				}
				if (service.ChildNodes.Count > 0) 
				{
					AddCommentElements (xdoc, service, type);
					docelem.AppendChild (service);
				}
			}
			return xdoc;
		}
		
		/// <summary>Add CodeTypeMember comment elements to the specified XmlDocument.</summary>
		/// <param name="xdoc">An XmlDocument that will be used to add the comment to.</param>
		/// <param name="xmet">An XmlElement that the comment elements will be appended to.</param>
		/// <param name="member">A CodeTypeMember containg all the comments that will be added to the xmet element.</param>
		public static void AddCommentElements (XmlDocument xdoc, XmlElement xmet, CodeTypeMember member)
		{
			foreach (CodeCommentStatement comment in member.Comments)
			{
				XmlElement xcom = xdoc.CreateElement ("comment");
				xmet.AppendChild (xcom);
				string com = comment.Comment.Text;
				com = com.Replace ("<remarks>","");
				com = com.Replace ("</remarks>","");
				com = com.Replace ("<remarks/>","");
				xcom.SetAttribute ("text", com);
			}
		}
		
		/// <summary>Gets the path where all web references will be stored for the specified project.</summary>
		/// <param name="project">A Project containing the root project information.</project>
		/// <returns>A string containing the base path for web references.</returns>
		public static string GetWebReferencePath (Project project)
		{
			return Path.Combine(project.BaseDirectory, "WebReferences");
		}
		
		/// <summary>Checks whether or not the current project does contain any web references.</summary>
		/// <returns>True if the project does contain any web references, otherwise false.</returns>
		public static bool ProjectContainsWebReference(Project project)
		{
			string webRefPath = Library.GetWebReferencePath(project);
			foreach (ProjectFile file in project.ProjectFiles)
			{
				if (file.FilePath.StartsWith(webRefPath))
					return true;
			}
			return false;
		}
	}
	
}
