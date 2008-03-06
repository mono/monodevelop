//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2006 Matthew Ward
//

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// The Xml Editor options.
	/// </summary>
	public class XmlEditorAddInOptions
	{
		public static readonly string OptionsProperty = "XmlEditor.AddIn.Options";
		//public static readonly string ShowAttributesWhenFoldedPropertyName = "ShowAttributesWhenFolded";
		public static readonly string ShowSchemaAnnotationPropertyName = "ShowSchemaAnnotation";
		public static readonly string AutoCompleteElementsPropertyName = "AutoCompleteElements";
		
		static IProperties properties;

		static XmlEditorAddInOptions()
 		{
 			properties = (IProperties)Runtime.Properties.GetProperty(OptionsProperty, new DefaultProperties());
		}

 		static IProperties Properties {
			get {
				Debug.Assert(properties != null);
				return properties;
 			}
		}
		
		#region Properties

		/// <summary>
		/// Raised when any xml editor property is changed.
		/// </summary>
		public static event PropertyEventHandler PropertyChanged {
			add {
				Properties.PropertyChanged += value;
			}
			
			remove {
				Properties.PropertyChanged -= value;
			}
		}
		
		/// <summary>
		/// Gets an association between a schema and a file extension.
		/// </summary>
		/// <remarks>
		/// <para>The property will be an xml element when the SharpDevelopProperties.xml
		/// is read on startup.  The property will be a schema association
		/// if the user changes the schema associated with the file
		/// extension in tools->options.</para>
		/// <para>The normal way of doing things is to
		/// pass the GetProperty method a default value which auto-magically
		/// turns the xml element into a schema association so we would not 
		/// have to check for both.  In this case, however, I do not want
		/// a default saved to the SharpDevelopProperties.xml file unless the user
		/// makes a change using Tools->Options.</para>
		/// <para>If we have a file extension that is currently missing a default 
		/// schema then if we  ship the schema at a later date the association will 
		/// be updated by the code if the user has not changed the settings themselves. 
		/// </para>
		/// <para>For example, the initial release of the xml editor add-in had
		/// no default schema for .xsl files, by default it was associated with
		/// no schema and this setting is saved if the user ever viewed the settings
		/// in the tools->options dialog.  Now, after the initial release the
		/// .xsl schema was created and shipped with SharpDevelop, there is
		/// no way to associate this schema to .xsl files by default since 
		/// the property exists in the SharpDevelopProperties.xml file.</para>
		/// <para>An alternative way of doing this might be to have the
		/// config info in the schema itself, which a special SharpDevelop 
		/// namespace.  I believe this is what Visual Studio does.  This
		/// way is not as flexible since it requires the user to locate
		/// the schema and change the association manually.</para>
		/// </remarks>
		public static XmlSchemaAssociation GetSchemaAssociation(string extension)
		{			
			object property = Properties.GetProperty(extension);
			
			XmlSchemaAssociation association = property as XmlSchemaAssociation;
			XmlElement element = property as XmlElement;
			
			if (element != null) {
				association = XmlSchemaAssociation.ConvertFromXmlElement(element) as XmlSchemaAssociation;
			}
			
			// Use default?
			if (association == null) {
				association = XmlSchemaAssociation.GetDefaultAssociation(extension);
			}
			
			return association;
		}
		
		public static void SetSchemaAssociation(XmlSchemaAssociation association)
		{
			Properties.SetProperty(association.Extension, association);
		}
		
//		public static bool ShowAttributesWhenFolded {
//			get {
//				return Properties.GetProperty(ShowAttributesWhenFoldedPropertyName, false);
//			}
//			
//			set {
//				Properties.SetProperty(ShowAttributesWhenFoldedPropertyName, value);
//			}
//		}
		
		public static bool ShowSchemaAnnotation {
			get {
				return Properties.GetProperty(ShowSchemaAnnotationPropertyName, true);
			}
			
			set {
				Properties.SetProperty(ShowSchemaAnnotationPropertyName, value);
			}
		}
		
		public static bool AutoCompleteElements {
			get {
				return Properties.GetProperty(AutoCompleteElementsPropertyName, true);
			}
			
			set {
				Properties.SetProperty(AutoCompleteElementsPropertyName, value);
			}
		}			
		
		#endregion
	}
}
