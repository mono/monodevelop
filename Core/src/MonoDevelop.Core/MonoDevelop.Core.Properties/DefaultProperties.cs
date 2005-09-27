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
	/// Default <code>IProperties</code> implementation, should
	/// be enough for most cases :)
	/// </summary>
	public class DefaultProperties : IProperties
	{
		Hashtable properties = new Hashtable();
		
		/// <summary>
		/// Gets a property out of the collection.
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
		public object GetProperty(string key, object defaultvalue)
		{
			if (!properties.ContainsKey(key)) {
				if (defaultvalue != null) {
					properties[key] = defaultvalue;
				}
				return defaultvalue;
			}
			
			object obj = properties[key];
			
			// stored an XmlElement in properties node ->
			// set a FromXmlElement of the defaultvalue type at this propertyposition.
			if (defaultvalue is IXmlConvertable && obj is XmlElement) {
				obj = properties[key] = ((IXmlConvertable)defaultvalue).FromXmlElement((XmlElement)((XmlElement)obj).FirstChild);
			}
			return obj;
		}
		
		/// <summary>
		/// Gets a property out of the collection.
		/// </summary>
		/// <returns>
		/// The property, or <code>null</code>, if the property wasn't found.
		/// </returns>
		/// <param name="key">
		/// The name of the property.
		/// </param>
		public object GetProperty(string key)
		{
			return GetProperty(key, (object)null);
		}
		
		/// <summary>
		/// Gets a <code>int</code> property out of the collection.
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
		public int GetProperty(string key, int defaultvalue)
		{
			return int.Parse(GetProperty(key, (object)defaultvalue).ToString());
		}
		
		/// <summary>
		/// Gets a <code>bool</code> property out of the collection.
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
		public bool GetProperty(string key, bool defaultvalue)
		{
			return bool.Parse(GetProperty(key, (object)defaultvalue).ToString());
		}

		/// <summary>
		/// Gets a <code>short</code> property out of the collection.
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
		public short GetProperty(string key, short defaultvalue)
		{
			return short.Parse(GetProperty(key, (object)defaultvalue).ToString());
		}

		/// <summary>
		/// Gets a <code>byte</code> property out of the collection.
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
		public byte GetProperty(string key, byte defaultvalue)
		{
			return byte.Parse(GetProperty(key, (object)defaultvalue).ToString());
		}
		
		/// <summary>
		/// Gets a <code>string</code> property out of the collection.
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
		public string GetProperty(string key, string defaultvalue)
		{
			return GetProperty(key, (object)defaultvalue).ToString();
		}
		
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
		public System.Enum GetProperty(string key, System.Enum defaultvalue)
		{
			return (System.Enum)Enum.Parse(defaultvalue.GetType(), GetProperty(key, (object)defaultvalue).ToString());
		}
		
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
		public void SetProperty(string key, object val)
		{
			object oldValue = properties[key];
			if (!val.Equals(oldValue)) {
				properties[key] = val;
				OnPropertyChanged(new PropertyEventArgs(this, key, oldValue, val));
			}
		}
		
		public DefaultProperties()
		{
		}
		
		protected void SetValueFromXmlElement(XmlElement element)
		{
			XmlNodeList nodes = element.ChildNodes;
			foreach (XmlElement el in nodes) {
				if (el.Name == "Property") {
					properties[el.Attributes["key"].InnerText] = el.Attributes["value"].InnerText;
				} else if (el.Name == "XmlConvertableProperty") {
					properties[el.Attributes["key"].InnerText] = el;
				} else {
					throw new UnknownPropertyNodeException(el.Name);
				}
			}
		}
		
		/// <summary>
		/// Converts a <code>XmlElement</code> to an <code>DefaultProperties</code>
		/// </summary>
		/// <returns>
		/// A new <code>DefaultProperties</code> object 
		/// </returns>
		public virtual object FromXmlElement(XmlElement element)
		{
			DefaultProperties defaultProperties = new DefaultProperties();
			defaultProperties.SetValueFromXmlElement(element);
			return defaultProperties;
		}
		
		/// <summary>
		/// Converts the <code>DefaultProperties</code> object to a <code>XmlElement</code>
		/// </summary>
		/// <returns>
		/// A new <code>XmlElement</code> object which represents the state
		/// of the <code>DefaultProperties</code> object.
		/// </returns>
		public virtual XmlElement ToXmlElement(XmlDocument doc)
		{
			XmlElement propertiesnode  = doc.CreateElement("Properties");
			
			foreach (DictionaryEntry entry in properties) {
				if (entry.Value != null) {
					if (entry.Value is XmlElement) { // write unchanged XmlElement back
						propertiesnode.AppendChild(doc.ImportNode((XmlElement)entry.Value, true));
					} else if (entry.Value is IXmlConvertable) { // An Xml convertable object
						XmlElement convertableNode = doc.CreateElement("XmlConvertableProperty");
						
						XmlAttribute key = doc.CreateAttribute("key");
						key.InnerText = entry.Key.ToString();
						convertableNode.Attributes.Append(key);
						
						convertableNode.AppendChild(((IXmlConvertable)entry.Value).ToXmlElement(doc));
						
						propertiesnode.AppendChild(convertableNode);
					} else {
						XmlElement el = doc.CreateElement("Property");
						
						XmlAttribute key   = doc.CreateAttribute("key");
						key.InnerText      = entry.Key.ToString();
						el.Attributes.Append(key);
	
						XmlAttribute val   = doc.CreateAttribute("value");
						val.InnerText      = entry.Value.ToString();
						el.Attributes.Append(val);
						
						propertiesnode.AppendChild(el);
					}
				}
			}
			return propertiesnode;
		}
		
		/// <summary>
		/// Returns a new instance of <code>IProperties</code> which has
		/// the same properties.
		/// </summary>
		public IProperties Clone()
		{
			DefaultProperties df = new DefaultProperties();
			df.properties = (Hashtable)properties.Clone();
			return df;
		}
		
		protected virtual void OnPropertyChanged(PropertyEventArgs e)
		{
			if (PropertyChanged != null) {
				PropertyChanged(this, e);
			}
		}
		
		public event PropertyEventHandler PropertyChanged;
	}
}
