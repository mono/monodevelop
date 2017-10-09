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

using System;
using System.Collections.Generic;
using System.Diagnostics;

using MonoDevelop.Core;

namespace MonoDevelop.Xml.Editor
{
	/// <summary>
	/// The Xml Editor options.
	/// </summary>
	public static class XmlEditorOptions
	{
		static readonly string OptionsProperty = "XmlEditor.AddIn.Options";
		static readonly string ShowSchemaAnnotationPropertyName = "ShowSchemaAnnotation";
		static readonly string AutoCompleteElementsPropertyName = "AutoCompleteElements";
		static readonly string AutoInsertFragmentsPropertyName = "AutoInsertFragment";
		static readonly string AssociationPrefix = "Association";
		static readonly string AutoShowCodeCompletionPropertyName = "AutoShowCodeCompletion";
		
		static readonly Properties properties;
		static bool showSchemaAnnotation, autoCompleteElements, autoInsertFragments, autoShowCodeCompletion;

		static XmlEditorOptions ()
 		{
 			properties = PropertyService.Get (OptionsProperty, new Properties());
			Properties.PropertyChanged += HandlePropertiesPropertyChanged;
			
			showSchemaAnnotation = properties.Get<bool> (ShowSchemaAnnotationPropertyName, false);
			autoCompleteElements = properties.Get<bool> (AutoCompleteElementsPropertyName, false);
			autoInsertFragments = properties.Get<bool> (AutoInsertFragmentsPropertyName, true);
			autoShowCodeCompletion = properties.Get<bool> (AutoShowCodeCompletionPropertyName, true);
		}

 		static void HandlePropertiesPropertyChanged (object sender, PropertyChangedEventArgs e)
 		{
			if (e.Key == ShowSchemaAnnotationPropertyName) {
				showSchemaAnnotation = (bool)e.NewValue;
			} else if (e.Key == AutoCompleteElementsPropertyName) {
				autoCompleteElements = (bool)e.NewValue;
			} else if (e.Key == AutoInsertFragmentsPropertyName) {
				autoInsertFragments = (bool)e.NewValue;
			} else if (XmlFileAssociationChanged != null && e.Key.StartsWith (AssociationPrefix, StringComparison.Ordinal)) {
				var ext = e.Key.Substring (AssociationPrefix.Length);
				var assoc = e.NewValue as XmlFileAssociation;
				XmlFileAssociationChanged (null, new XmlFileAssociationChangedEventArgs (ext, assoc));
			} else if (e.Key == AutoShowCodeCompletionPropertyName) {
				autoShowCodeCompletion = (bool)e.NewValue;
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
				if (key.StartsWith (AssociationPrefix, StringComparison.Ordinal))
					yield return key.Substring (AssociationPrefix.Length);
		}
		
		public static IEnumerable<XmlFileAssociation> GetFileAssociations ()
		{
			var keys = new List<string> (Properties.Keys);
			foreach (string key in keys) {
				if (key.StartsWith (AssociationPrefix, StringComparison.Ordinal)) {
					var assoc = Properties.Get<XmlFileAssociation> (key);
					//ignore bad data in properties
					if (assoc != null)
						yield return assoc;
				}
			}
		}
		
		public static bool ShowSchemaAnnotation {
			get {
				return showSchemaAnnotation;
			}
			set {
				if (showSchemaAnnotation == value)
					return;
				showSchemaAnnotation = value;
				properties.Set (ShowSchemaAnnotationPropertyName, value);
			}
		}

		public static bool AutoCompleteElements {
			get {
				return autoCompleteElements;
			}
			set {
				if (autoCompleteElements == value)
					return;
				autoCompleteElements = value;
				properties.Set (AutoCompleteElementsPropertyName, value);
			}
		}
		
		/// <summary>
		/// Automatically insert fragments such as ="" when committing an attribute and > when pressing / in a tag.
		/// </summary>
		public static bool AutoInsertFragments {
			get {
				return autoInsertFragments;
			}
			set {
				if (autoInsertFragments == value)
					return;
				autoInsertFragments = value;
				properties.Set (AutoInsertFragmentsPropertyName, value);
			}
		}

		public static bool AutoShowCodeCompletion {
			get {
				return autoShowCodeCompletion;
			}
			set {
				if (autoShowCodeCompletion == value)
					return;
				autoShowCodeCompletion = value;
				properties.Set (AutoShowCodeCompletionPropertyName, value);
			}
		}

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
