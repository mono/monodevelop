// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Reflection;

namespace MonoDevelop.Core.Properties
{
	/// <summary>
	/// The <code>IProperties</code> interface defines a set of properties
	/// </summary>
	public interface IProperties : IXmlConvertable
	{
		/// <summary>
		/// Gets a property out of the collection. The defaultvalue must either 
		/// have a cast to a string (and back) or implement the 
		/// <code>IXmlConcertable</code> interface.
		/// </summary>
		/// <returns>
		/// The property, or <code>defaultvalue</code>, if the property wasn't
		/// found.
		/// </returns>
		/// <param name="key">
		/// The name of the property.
		/// </param>
		/// <param name="defaultvalue">
		/// The default value of the property.
		/// </param>
		object GetProperty(string key, object defaultvalue);
		
		/// <summary>
		/// Gets a property out of the collection.
		/// </summary>
		/// <returns>
		/// The property, or <code>null</code>, if the property wasn't 
		/// found.
		/// </returns>
		/// <param name="key">
		/// The name of the property.
		/// </param>
		object GetProperty(string key);
		
		/// <summary>
		/// Gets a <code>int</code> property out of the collection.
		/// </summary>
		/// <returns>
		/// The property, or <code>defaultvalue</code>, if the property wasn't
		/// found.
		/// </returns>
		/// <param name="key">
		/// The name of the property.
		/// </param>
		/// <param name="defaultvalue">
		/// The default value of the property.
		/// </param>
		int GetProperty(string key, int defaultvalue);
		
		/// <summary>
		/// Gets a <code>bool</code> property out of the collection.
		/// </summary>
		/// <returns>
		/// The property, or <code>defaultvalue</code>, if the property wasn't
		/// found.
		/// </returns>
		/// <param name="key">
		/// The name of the property.
		/// </param>
		/// <param name="defaultvalue">
		/// The default value of the property.
		/// </param>
		bool GetProperty(string key, bool defaultvalue);

		/// <summary>
		/// Gets a <code>short</code> property out of the collection.
		/// </summary>
		/// <returns>
		/// The property, or <code>defaultvalue</code>, if the property wasn't
		/// found.
		/// </returns>
		/// <param name="key">
		/// The name of the property.
		/// </param>
		/// <param name="defaultvalue">
		/// The default value of the property.
		/// </param>
		short GetProperty(string key, short defaultvalue);

		/// <summary>
		/// Gets a <code>byte</code> property out of the collection.
		/// </summary>
		/// <returns>
		/// The property, or <code>defaultvalue</code>, if the property wasn't
		/// found.
		/// </returns>
		/// <param name="key">
		/// The name of the property.
		/// </param>
		/// <param name="defaultvalue">
		/// The default value of the property.
		/// </param>
		byte GetProperty(string key, byte defaultvalue);

		/// <summary>
		/// Gets a <code>string</code> property out of the collection.
		/// </summary>
		/// <returns>
		/// The property, or <code>defaultvalue</code>, if the property wasn't
		/// found.
		/// </returns>
		/// <param name="key">
		/// The name of the property.
		/// </param>
		/// <param name="defaultvalue">
		/// The default value of the property.
		/// </param>
		string GetProperty(string key, string defaultvalue);
		
		/// <summary>
		/// Gets a <code>enum</code> property out of the collection.
		/// </summary>
		/// <returns>
		/// The property, or <code>defaultvalue</code>, if the property wasn't found.
		/// </returns>
		/// <param name="key">
		/// The name of the property.
		/// </param>
		/// <param name="defaultvalue">
		/// The default value of the property.
		/// </param>
		System.Enum GetProperty(string key, System.Enum defaultvalue);
		
		/// <summary>
		/// Sets the property <code>key</code> to the value <code>val</code>.
		/// If <code>val</code> is null, the property will be taken out from the
		/// properties.
		/// </summary>
		/// <param name="key">
		/// The name of the property.
		/// </param>
		/// <param name="val">
		/// The value of the property.
		/// </param>
		void SetProperty(string key, object val);
		
		/// <summary>
		/// Returns a new instance of <code>IProperties</code> which has 
		/// the same properties.
		/// </summary>
		IProperties Clone();
		
		/// <summary>
		/// The property changed event handler, it is called
		/// when a property has changed.
		/// </summary>
		event PropertyEventHandler PropertyChanged;
	}
}
