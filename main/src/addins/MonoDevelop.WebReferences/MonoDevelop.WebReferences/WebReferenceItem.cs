using System;
using System.Collections;
using System.IO;
using System.Web.Services.Protocols;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Net;
using System.Text.RegularExpressions;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;


namespace MonoDevelop.WebReferences
{
	/// <summary>Defines the properties and methods for the WebReferenceItem class.</summary>
	public class WebReferenceItem
	{
		#region Properties
		/// <summary>Gets or sets the name for the web reference item.</summary>
		/// <value>A string containing the name for the web reference.</value>
		public string Name
		{
			get { return name; }
			set { name = value; }
		}
		
		/// <summary>Gets or Sets the Map ProjectFile for the web reference.</summary>
		/// <value>A ProjectFile containing the map file for the web reference.</value>
		public ProjectFile MapFile
		{
			get { return mapFile; }
			set { mapFile = value; }
		}
		
		/// <summary>Gets or Sets the Proxy ProjectFile for the web reference.</summary>
		/// <value>A ProjectFile containing the proxy file for the web reference.</value>
		public ProjectFile ProxyFile
		{
			get { return proxyFile; }
			set { proxyFile = value; }
		}
		#endregion
		
		#region Member Variables
		private string name;
		private ProjectFile proxyFile;
		private ProjectFile mapFile;
		#endregion
		
		/// <summary>Initializes a new instance of the WebReferenceItem class.</summary>
		/// <param name="name">A string containing the name for the web reference.</param>
		public WebReferenceItem(string name)
		{
			this.name = name;
		}
		
		/// <summary>Initializes a new instance of the WebReferenceItem class.</summary>
		/// <param name="name">A string containing the name for the web reference.</param>
		/// <param name="proxyFile">A ProjectFile containing the proxy file.</param>
		/// <param name="mapFile">A ProjectFile containing the map file.</param>
		public WebReferenceItem(string name, ProjectFile proxyFile, ProjectFile mapFile)
		{
			this.name = name;
			this.proxyFile = proxyFile;
			this.mapFile = mapFile;
		}
		
		/// <summary>Update the web reference item by using the map file.</summary>
		public void Update()
		{
			// Read the map file into the discovery client protocol and setup the code generator
			DiscoveryProtocol protocol = new DiscoveryProtocol(),
			remoteProtocol = null;
			protocol.ReadAllUseBasePath(MapFile.FilePath);

			// Refresh the disco and wsdl from the server
			foreach (object doc in protocol.References.Values) { 
				if (doc is DiscoveryDocumentReference) {
					remoteProtocol = new DiscoveryProtocol();
					try {
						remoteProtocol.DiscoverAny(((DiscoveryDocumentReference)doc).Url);
						break;
					} catch (WebException) {
						remoteProtocol = null;
					}
				}
			}

			if(null != remoteProtocol){ protocol = remoteProtocol; }
			
			protocol.ResolveAll();
			
			// Re-generate the proxy and map files
			string basePath = new FileInfo(MapFile.FilePath).Directory.FullName;
			CodeGenerator codeGen = new CodeGenerator(ProxyFile.Project, (DiscoveryClientProtocol)protocol);
			codeGen.CreateProxyFile(basePath, MapFile.Project.Name + "." + Name, "Reference");
			protocol.WriteAll(basePath, "Reference.map");
			protocol.Dispose();
		}
		
		/// <summary>Delete the web reference from the project.</summary>
		public void Delete()
		{
			Project project = proxyFile.Project;
			project.Files.Remove(proxyFile);
			project.Files.Remove(mapFile);
			Directory.Delete(Path.Combine(Library.GetWebReferencePath(project), Name), true);
		}
	}	
}