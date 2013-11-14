using System;
using System.IO;
using System.Xml.Serialization;
using System.Web.Services.Discovery;

namespace MonoDevelop.WebReferences
{
	/// <summary>Provides support for programmatically invoking XML Web services discovery.</summary>
	[System.ComponentModel.DesignerCategory ("Code")]
	public class DiscoveryProtocol : DiscoveryClientProtocol
	{
		/// <summary>
		/// Reads in a file containing a map of saved discovery documents populating the Documents and References properties, 
		/// with discovery documents, XML Schema Definition (XSD) schemas, and service descriptions referenced in the file.
		/// </summary>
		/// <param name="topLevelFilename">Name of file to read in, containing the map of saved discovery documents.</param>
		/// <returns>
		/// A DiscoveryClientResultCollection containing the results found in the file with the map of saved discovery documents. 
		/// The file format is a DiscoveryClientProtocol.DiscoveryClientResultsFile class serialized into XML; however, one would 
		/// typically create the file using only the WriteAll method or Disco.exe.
		/// </returns>
		public DiscoveryClientResultCollection ReadAllUseBasePath(string topLevelFilename)
		{
			string basePath = (new FileInfo(topLevelFilename)).Directory.FullName;
			var sr = new StreamReader (topLevelFilename);
			var ser = new XmlSerializer (typeof (DiscoveryClientResultsFile));
			var resfile = (DiscoveryClientResultsFile) ser.Deserialize (sr);
			sr.Close ();
			
			foreach (DiscoveryClientResult dcr in resfile.Results)
			{
				// Done this cause Type.GetType(dcr.ReferenceTypeName) returned null
				Type type;
				switch (dcr.ReferenceTypeName)
				{
					case "System.Web.Services.Discovery.ContractReference":
						type = typeof(ContractReference);
						break;
					case "System.Web.Services.Discovery.DiscoveryDocumentReference":
						type = typeof(DiscoveryDocumentReference);
						break;
					default:
						continue;
				}
				
				var dr = (DiscoveryReference) Activator.CreateInstance(type);
				dr.Url = dcr.Url;
				var fs = new FileStream (Path.Combine(basePath, dcr.Filename), FileMode.Open, FileAccess.Read);
				Documents.Add (dr.Url, dr.ReadDocument (fs));
				fs.Close ();
				References.Add (dr.Url, dr);
			}
			return resfile.Results;	
		}
	}	
}