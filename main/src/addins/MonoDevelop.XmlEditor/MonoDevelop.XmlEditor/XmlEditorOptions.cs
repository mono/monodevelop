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
	public static class XmlEditorOptions
	{
		internal static readonly string OptionsProperty = "XmlEditor.AddIn.Options";
		internal static readonly string ShowSchemaAnnotationPropertyName = "ShowSchemaAnnotation";
		internal static readonly string AutoCompleteElementsPropertyName = "AutoCompleteElements";
		internal static readonly string AutoInsertFragmentsPropertyName = "AutoInsertFragment";
		internal static readonly string AssociationPrefix = "Association";
		
		static Properties properties;

		static XmlEditorOptions ()
 		{
 			properties = PropertyService.Get (OptionsProperty, new Properties());
			Properties.PropertyChanged += HandlePropertiesPropertyChanged;
			
			ShowSchemaAnnotation = properties.Get<bool> (ShowSchemaAnnotationPropertyName, false);
			AutoCompleteElements = properties.Get<bool> (AutoCompleteElementsPropertyName, false);
			AutoInsertFragments = properties.Get<bool> (AutoInsertFragmentsPropertyName, false);
		}

 		static void HandlePropertiesPropertyChanged (object sender, PropertyChangedEventArgs e)
 		{
			if (e.Key == ShowSchemaAnnotationPropertyName) {
				ShowSchemaAnnotation = (bool)e.NewValue;
			} else if (e.Key == AutoCompleteElementsPropertyName) {
				AutoCompleteElements = (bool)e.NewValue;
			} else if (e.Key == AutoInsertFragmentsPropertyName) {
				AutoInsertFragments = (bool)e.NewValue;
			} else if (XmlFileAssociationChanged != null && e.Key.StartsWith (AssociationPrefix)) {
				var ext = e.Key.Substring (AssociationPrefix.Length);
				var assoc = e.NewValue as XmlFileAssociation;
				XmlFileAssociationChanged (null, new XmlFileAssociationChangedEventArgs (ext, assoc));
			}
 		}

 		internal static Properties Properties {
			get {
				Debug.Assert(properties != null);
				return properties;
 			}
		}
		
		#region Properties

		/// <summary>Raised when any use scheme association changes </summary>
		public static event EventHandler<XmlFileAssociationChangedEventArgs> XmlFileAssociationChanged;
		
		public static XmlFileAssociation GetFileAssociation (string extension)
		{
			return Properties.Get<XmlFileAssociation> (AssociationPrefix + extension.ToLowerInvariant ());
		}
		
		public static void RemoveFileAssociation (string extension)
		{
			Properties.Set (AssociationPrefix + extension.ToLowerInvariant (), null); 
		}
		
		public static void SetFileAssociation (XmlFileAssociation association)
		{
			Properties.Set (AssociationPrefix + association.Extension, association);
		}
		
		public static IEnumerable<string> GetFileExtensions ()
		{
			//for some reason we get an out of sync error unless we copy the list
			var keys = new List<string> (Properties.Keys);
			foreach (string key in keys)
				if (key.StartsWith (AssociationPrefix))
					yield return key.Substring (AssociationPrefix.Length);
		}
		
		public static IEnumerable<XmlFileAssociation> GetFileAssociations ()
		{
			var keys = new List<string> (Properties.Keys);
			foreach (string key in keys) {
				if (key.StartsWith (AssociationPrefix)) {
					var assoc = Properties.Get<XmlFileAssociation> (key);
					//ignore bad data in properties
					if (assoc != null)
						yield return assoc;
				}
			}
		}
		
		public static bool ShowSchemaAnnotation { get; private set; }
		public static bool AutoCompleteElements { get; private set; }
		
		/// <summary>
		/// Automatically insert fragments such as ="" when committing an attribute and > when pressing / in a tag.
		/// Off by default since it forces the user to alter typing behaviour.
		/// </summary>
		public static bool AutoInsertFragments { get; private set; }
		
		#endregion
	}
	
	public class XmlFileAssociationChangedEventArgs : EventArgs
	{
		public string Extension { get; private set; }
		public XmlFileAssociation Association { get; private set; }
		
		public XmlFileAssociationChangedEventArgs (string extension, XmlFileAssociation association)
		{
			this.Extension = extension;
			this.Association = association;
		}		
	}
}
