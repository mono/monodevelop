//
// ClassOutlineSortingProperties.cs
//
// Authors:
//  Helmut Duregger <helmutduregger@gmx.at>
//
// Copyright (c) 2010 Helmut Duregger
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
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

using MonoDevelop.Core;


namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Stores sorting status and is serialized to configuration properties.
	/// </summary>
	/// <remarks>
	/// Stores the sorting configuration, e.g. if the class outline is currently sorted
	/// or what primary sort key values the individual node groups have. This class is
	/// serialized to the configuration file MonoDevelopProperties.xml.
	/// </remarks>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtension"/>
	public class ClassOutlineSortingProperties : ISortingProperties
	{
		private static ClassOutlineSortingProperties defaultInstance;

		// Define default sort keys and localized names

		private Group foldingRegionsGroup  = new Group ( 0, GettextCatalog.GetString ("Regions")         );
		private Group namespacesGroup      = new Group ( 1, GettextCatalog.GetString ("Namespaces")      );
		private Group typesGroup           = new Group ( 2, GettextCatalog.GetString ("Types")           );
		private Group fieldsGroup          = new Group ( 3, GettextCatalog.GetString ("Fields")          );
		private Group propertiesGroup      = new Group ( 4, GettextCatalog.GetString ("Properties")      );
		private Group eventsGroup          = new Group ( 5, GettextCatalog.GetString ("Events")          );
		private Group methodsGroup         = new Group ( 6, GettextCatalog.GetString ("Methods")         );
		private Group localVariablesGroup  = new Group ( 7, GettextCatalog.GetString ("Local Variables") );
		private Group parametersGroup      = new Group ( 8, GettextCatalog.GetString ("Parameters")      );

		public Group FoldingRegionsGroup {
			get {
				return foldingRegionsGroup;
			}
		}

		public Group NamespacesGroup {
			get {
				return namespacesGroup;
			}
		}

		public Group TypesGroup {
			get {
				return typesGroup;
			}
		}

		public Group FieldsGroup {
			get {
				return fieldsGroup;
			}
		}

		public Group PropertiesGroup {
			get {
				return propertiesGroup;
			}
		}

		public Group EventsGroup {
			get {
				return eventsGroup;
			}
		}

		public Group MethodsGroup {
			get {
				return methodsGroup;
			}
		}

		public Group LocalVariablesGroup {
			get {
				return localVariablesGroup;
			}
		}

		public Group ParametersGroup {
			get {
				return parametersGroup;
			}
		}

		/// <summary>
		/// Name of the sorting property entry in MonoDevelopProperties.xml.
		/// </summary>
		public const string SORTING_PROPERTY_NAME = "MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtension.Sorting";

		public event EventHandler EventSortingPropertiesChanged;

		public void SortingPropertiesChanged (object o, EventArgs e)
		{
			if (EventSortingPropertiesChanged != null) {
				EventSortingPropertiesChanged (o, e);
			}
		}

		public List<Group> GetGroups ()
		{
			// Create a random ordered list of our groups

			var groups = new List<Group> {

				foldingRegionsGroup,
				namespacesGroup,
				typesGroup,
				fieldsGroup,
				propertiesGroup,
				eventsGroup,
				methodsGroup,
				localVariablesGroup,
				parametersGroup
			};

			return groups;
		}

		//
		// Provide properties for serialization in MonoDevelopProperties.xml.
		//

		[XmlAttribute]
		public bool IsGrouping {
			get;
			set;
		}

		[XmlAttribute]
		public bool IsSortingAlphabetically {
			get;
			set;
		}

		[XmlAttribute]
		public int SortKeyFoldingRegions {
			get {
				return foldingRegionsGroup.SortKey;
			}
			set {
				foldingRegionsGroup.SortKey = value;
			}
		}

		[XmlAttribute]
		public int SortKeyNamespaces {
			get {
				return namespacesGroup.SortKey;
			}
			set {
				namespacesGroup.SortKey = value;
			}
		}

		[XmlAttribute]
		public int SortKeyTypes	{
			get {
				return typesGroup.SortKey;
			}
			set {
				typesGroup.SortKey = value;
			}
		}

		[XmlAttribute]
		public int SortKeyFields {
			get {
				return fieldsGroup.SortKey;
			}
			set {
				fieldsGroup.SortKey = value;
			}
		}

		[XmlAttribute]
		public int SortKeyProperties	{
			get {
				return propertiesGroup.SortKey;
			}
			set {
				propertiesGroup.SortKey = value;
			}
		}

		[XmlAttribute]
		public int SortKeyEvents {
			get {
				return eventsGroup.SortKey;
			}
			set {
				eventsGroup.SortKey = value;
			}
		}

		[XmlAttribute]
		public int SortKeyMethods {
			get {
				return methodsGroup.SortKey;
			}
			set {
				methodsGroup.SortKey = value;
			}
		}

		[XmlAttribute]
		public int SortKeyLocalVariables {
			get {
				return localVariablesGroup.SortKey;
			}
			set {
				localVariablesGroup.SortKey = value;
			}
		}

		[XmlAttribute]
		public int SortKeyParameters {
			get {
				return parametersGroup.SortKey;
			}
			set {
				parametersGroup.SortKey = value;
			}
		}


		/// <summary>
		/// Returns a default initialized instance.
		/// </summary>
		/// <returns>
		/// Instance with sorting disabled and default sort keys.
		/// </returns>
		public static ClassOutlineSortingProperties GetDefaultInstance ()
		{
			if (defaultInstance == null) {

				defaultInstance = new ClassOutlineSortingProperties {

					IsGrouping              = false,
					IsSortingAlphabetically = false
				};
			}

			return defaultInstance;
		}
	}
}

