// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System.Xml;

namespace MonoDevelop.Core.Properties
{
	/// <summary>
	/// If you want define own, complex options you can implement this interface
	/// and save it in the main Option class, your class will be saved as xml in
	/// the global properties.
	/// Use your class like any other property. (the conversion will be transparent)
	/// </summary>
	public interface IXmlConvertable
	{
		/// <summary>
		/// Converts a <code>XmlElement</code> to an <code>IXmlConvertable</code>
		/// </summary>
		/// <returns>
		/// A new <code>IXmlConvertable</code> object 
		/// </returns>
		object FromXmlElement(XmlElement element);
		
		/// <summary>
		/// Converts the <code>IXmlConvertable</code> object to a <code>XmlElement</code>
		/// </summary>
		/// <returns>
		/// A new <code>XmlElement</code> object which represents the state
		/// of the <code>IXmlConvertable</code> object.
		/// </returns>
		XmlElement ToXmlElement(XmlDocument doc);
	}
}
