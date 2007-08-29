// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Reflection;
using System.Globalization;

namespace MonoDevelop.Core.Properties
{
	/// <summary>
	/// Default <code>IProperties</code> implementation, should
	/// be enough for most cases :)
	/// </summary>
	public class DefaultProperties : IProperties
	{
		Dictionary<string, object> properties = new Dictionary<string, object> ();
		
		public object GetProperty (string key)
		{
			return GetProperty(key, (object)null);
		}
		
		public T GetProperty<T>(string property, T defaultValue)
		{
			if (!properties.ContainsKey(property)) {
				properties.Add(property, defaultValue);
				return defaultValue;
			}
			object o = properties[property];
			if (defaultValue is IXmlConvertable && o is XmlElement) {
				o = properties[property] = ((IXmlConvertable)defaultValue).FromXmlElement ((XmlElement)((XmlElement)o).FirstChild);
			} else if (o is string && typeof(T) != typeof(string)) {
				TypeConverter c = TypeDescriptor.GetConverter(typeof(T));
				try {
					o = c.ConvertFromInvariantString(o.ToString());
				} catch (Exception ex) {
					o = defaultValue;
				}
				properties[property] = o; // store for future look up
			} else if (o is ArrayList && typeof(T).IsArray) {
				ArrayList list = (ArrayList)o;
				Type elementType = typeof(T).GetElementType();
				Array arr = System.Array.CreateInstance(elementType, list.Count);
				TypeConverter c = TypeDescriptor.GetConverter(elementType);
				try {
					for (int i = 0; i < arr.Length; ++i) {
						if (list[i] != null) {
							arr.SetValue(c.ConvertFromInvariantString(list[i].ToString()), i);
						}
					}
					o = arr;
				} catch (Exception ex) {
					o = defaultValue;
				}
				properties[property] = o; // store for future look up
			} else if (!(o is string) && typeof(T) == typeof(string)) {
				TypeConverter c = TypeDescriptor.GetConverter(typeof(T));
				if (c.CanConvertTo(typeof(string))) {
					o = c.ConvertToInvariantString(o);
				} else {
					o = o.ToString();
				}
			}
			try {
				return (T)o;
			} catch (NullReferenceException) {
				// can happen when configuration is invalid -> o is null and a value type is expected
				return defaultValue;
			}
		}
		
		public void SetProperty(string key, object val)
		{
			object oldValue = GetProperty (key, val);
			if (!val.Equals (oldValue)) {
				if (val is DateTime)
					val = ((DateTime)val).ToString ("s", CultureInfo.InvariantCulture);
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
			foreach (XmlNode n in nodes) {
				XmlElement el = n as XmlElement;
				if (el == null)
					continue;
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
			
			foreach (KeyValuePair<string, object> entry in properties) {
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
			df.properties = new Dictionary<string, object> (properties);
			return df;
		}
		
		protected virtual void OnPropertyChanged(PropertyEventArgs e)
		{
			if (PropertyChanged != null) {
				PropertyChanged(this, e);
			}
		}
		
		public event EventHandler<PropertyEventArgs> PropertyChanged;
	}
}
