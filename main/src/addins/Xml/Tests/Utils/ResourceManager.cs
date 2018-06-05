using System.IO;

namespace MonoDevelop.Xml.Tests.Utils
{
	/// <summary>
	/// Returns strings from the embedded test resources.
	/// </summary>
	public class ResourceManager
	{
		/// <summary>
		/// Returns the xhtml strict schema xml.
		/// </summary>
		public static Stream GetXhtmlStrictSchema()
		{
			return GetResource("xhtml1-strict.xsd");
		}
		
		/// <summary>
		/// Returns the xsd schema.
		/// </summary>
		public static Stream GetXsdSchema()
		{
			return GetResource("XMLSchema.xsd");
		}

		static Stream GetResource (string name)
			=> typeof (ResourceManager).Assembly.GetManifestResourceStream (name);
	}
}
