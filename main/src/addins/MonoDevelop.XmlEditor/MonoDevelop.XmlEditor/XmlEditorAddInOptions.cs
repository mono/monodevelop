//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2007 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using MonoDevelop.Core;
using System;
using System.Collections.Generic;
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
		public static readonly string ShowSchemaAnnotationPropertyName = "ShowSchemaAnnotation";
		public static readonly string AutoCompleteElementsPropertyName = "AutoCompleteElements";
		public static readonly string AssociationPrefix = "Association";
		
		static Properties properties;

		static XmlEditorAddInOptions()
 		{
 			properties = PropertyService.Get(OptionsProperty, new Properties());
		}

 		static Properties Properties {
			get {
				Debug.Assert(properties != null);
				return properties;
 			}
		}
		
		#region Properties

		/// <summary>
		/// Raised when any xml editor property is changed.
		/// </summary>
		public static event EventHandler<PropertyChangedEventArgs> PropertyChanged {
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
		public static XmlSchemaAssociation GetSchemaAssociation (string extension)
		{
			XmlSchemaAssociation association = Properties.Get<XmlSchemaAssociation> (AssociationPrefix + extension.ToLowerInvariant ());
			
			if (association == null)
				association = XmlSchemaAssociation.GetDefaultAssociation (extension);
			
			return association;
		}
		
		public static void RemoveSchemaAssociation (string extension)
		{
			Properties.Set (AssociationPrefix + extension.ToLowerInvariant (), null); 
		}
		
		public static void SetSchemaAssociation (XmlSchemaAssociation association)
		{
			Properties.Set (AssociationPrefix + association.Extension.ToLowerInvariant (), association);
		}
		
		public static IEnumerable<string> RegisteredFileExtensions
		{
			get {
				//for some reason we get an out of sync error unless we copy the list
				List<string> tempList = new List<string> (Properties.Keys);
				foreach (string key in tempList)
					if (key.StartsWith (AssociationPrefix))
						yield return key.Substring (AssociationPrefix.Length);
			}
		}
		
		public static bool ShowSchemaAnnotation {
			get {
				return Properties.Get(ShowSchemaAnnotationPropertyName, true);
			}
			
			set {
				Properties.Set(ShowSchemaAnnotationPropertyName, value);
			}
		}
		
		public static bool AutoCompleteElements {
			get {
				return Properties.Get(AutoCompleteElementsPropertyName, true);
			}
			
			set {
				Properties.Set(AutoCompleteElementsPropertyName, value);
			}
		}			
		
		#endregion
	}
}
