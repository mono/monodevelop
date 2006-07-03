// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System.Collections;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Projects
{
	/// <summary>
	/// This is the base interfaces for all configurations (projects and combines)
	/// </summary>
	[DataItem (FallbackType=typeof(UnknownConfiguration))]
	public interface IConfiguration : System.ICloneable
	{
		/// <summary>
		/// The name of the configuration
		/// </summary>
		string Name {
			get;
		}
		
		void CopyFrom (IConfiguration configuration);
	}
	
	public class UnknownConfiguration: IConfiguration, IExtendedDataItem
	{
		Hashtable properties;
		
		public object Clone()
		{
			return (IConfiguration) MemberwiseClone ();
		}
		
		public void CopyFrom (IConfiguration configuration)
		{
			UnknownConfiguration other = (UnknownConfiguration) configuration;
			if (other.properties != null)
				properties = other.properties;
		}
		
		public string Name {
			get { return "Unknown Configuration"; }
		}
		
		public override string ToString()
		{
			return "Unknown Configuration";
		}
		
		public IDictionary ExtendedProperties {
			get {
				if (properties == null) properties = new Hashtable ();
				return properties;
			}
		}
	}
}
