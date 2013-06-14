using System.IO;
using System.Reflection;
using System.Xml;

namespace MonoDevelop.XmlEditor.Tests.Utils
{
	/// <summary>
	/// Returns strings from the embedded test resources.
	/// </summary>
	public class ResourceManager
	{
		static ResourceManager manager;
		
		static ResourceManager()
		{
			manager = new ResourceManager();
		}
		
		/// <summary>
		/// Returns the xhtml strict schema xml.
		/// </summary>
		public static XmlTextReader GetXhtmlStrictSchema()
		{
			return manager.GetXml("xhtml1-strict-modified.xsd");
		}
		
		/// <summary>
		/// Returns the xsd schema.
		/// </summary>
		public static XmlTextReader GetXsdSchema()
		{
			return manager.GetXml("XMLSchema.xsd");
		}
		
		/// <summary>
		/// Returns the xml read from the specified file which is embedded
		/// in this assembly as a resource.
		/// </summary>
		public XmlTextReader GetXml(string fileName)
		{
			XmlTextReader reader = null;
			
			Assembly assembly = Assembly.GetAssembly(this.GetType());
			
			Stream resourceStream = assembly.GetManifestResourceStream(fileName);
			if (resourceStream != null) {
				reader = new XmlTextReader(resourceStream);
			}
			
			return reader;
		}
		
	}
}
