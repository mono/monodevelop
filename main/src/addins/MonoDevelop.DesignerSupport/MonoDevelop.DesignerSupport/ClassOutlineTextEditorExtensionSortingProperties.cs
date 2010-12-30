//
// ClassOutlineTextEditorExtensionSortingProperties.cs
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
using System.ComponentModel;

using MonoDevelop.Core;


namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Stores sorting status and is serialized to configuration properties.
	/// </summary>
	///
	/// <remarks>
	/// Stores the sorting configuration, e.g. if the class outline is currently sorted
	/// or what primary sort key values the individual node types have. This class is
	/// serialized to the configuration file MonoDevelopProperties.xml.
	/// </remarks>
	///
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtension"/>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtensionSorting"/>

	public class ClassOutlineTextEditorExtensionSortingProperties
	{
		bool isSorting;

		byte sortKeyFoldingRegion;
		byte sortKeyNamespace;
		byte sortKeyType;
		byte sortKeyField;
		byte sortKeyProperty;
		byte sortKeyEvent;
		byte sortKeyMethod;
		byte sortKeyLocalVariable;
		byte sortKeyParameter;

		/*
		 * Provide properties for display in PropertyGrid of preferences dialog
		 * and storage in MonoDevelopProperties.xml.
		 */

		[LocalizedCategory ("General")]
		[LocalizedDisplayName ("Sort Outline")]
		[LocalizedDescription ("If checked the entries in the document outline will be sorted. They are first sorted by"
			+ " the sort key. If two entries have the same sort key, they will be sorted alphabetically."
		    + " Entries with lower keys sort higher up in the hierarchy. Valid keys lie in [0, 255].")]

		public bool IsSorting
		{
			get { return isSorting; }
			set { isSorting = value; }
		}

		[LocalizedCategory ("Sort Keys")]
		[LocalizedDisplayName ("Regions")]
		[LocalizedDescription ("The sort key for regions. Valid keys lie in [0, 255].")]

		public byte SortKeyFoldingRegion
		{
			get { return sortKeyFoldingRegion; }
			set { sortKeyFoldingRegion = value; }
		}

		[LocalizedCategory ("Sort Keys")]
		[LocalizedDisplayName ("Namespaces")]
		[LocalizedDescription ("The sort key for namespaces. Valid keys lie in [0, 255].")]

		public byte SortKeyNamespace
		{
			get { return sortKeyNamespace; }
			set { sortKeyNamespace = value; }
		}

		[LocalizedCategory ("Sort Keys")]
		[LocalizedDisplayName ("Types")]
		[LocalizedDescription ("The sort key for types. Valid keys lie in [0, 255].")]

		public byte SortKeyType
		{
			get { return sortKeyType; }
			set { sortKeyType = value; }
		}

		[LocalizedCategory ("Sort Keys")]
		[LocalizedDisplayName ("Fields")]
		[LocalizedDescription ("The sort key for fields. Valid keys lie in [0, 255].")]

		public byte SortKeyField
		{
			get { return sortKeyField; }
			set { sortKeyField = value; }
		}

		[LocalizedCategory ("Sort Keys")]
		[LocalizedDisplayName ("Properties")]
		[LocalizedDescription ("The sort key for properties. Valid keys lie in [0, 255].")]

		public byte SortKeyProperty
		{
			get { return sortKeyProperty; }
			set { sortKeyProperty = value; }
		}

		[LocalizedCategory ("Sort Keys")]
		[LocalizedDisplayName ("Events")]
		[LocalizedDescription ("The sort key for events. Valid keys lie in [0, 255].")]

		public byte SortKeyEvent
		{
			get { return sortKeyEvent; }
			set { sortKeyEvent = value; }
		}

		[LocalizedCategory ("Sort Keys")]
		[LocalizedDisplayName ("Methods")]
		[LocalizedDescription ("The sort key for methods. Valid keys lie in [0, 255].")]

		public byte SortKeyMethod
		{
			get { return sortKeyMethod; }
			set { sortKeyMethod = value; }
		}

		[LocalizedCategory ("Sort Keys")]
		[LocalizedDisplayName ("Local Variables")]
		[LocalizedDescription ("The sort key for local variables. Valid keys lie in [0, 255].")]

		public byte SortKeyLocalVariable
		{
			get { return sortKeyLocalVariable; }
			set { sortKeyLocalVariable = value; }
		}

		[LocalizedCategory ("Sort Keys")]
		[LocalizedDisplayName ("Parameters")]
		[LocalizedDescription ("The sort key for parameters. Valid keys lie in [0, 255].")]

		public byte SortKeyParameter
		{
			get { return sortKeyParameter; }
			set { sortKeyParameter = value; }
		}

		/// <summary>
		/// Standard constructor required for serialization.
		/// </summary>

		public ClassOutlineTextEditorExtensionSortingProperties ()
		{
		}

		/// <summary>
		/// Returns a default initialized instance.
		/// </summary>
		///
		/// <returns>
		/// Instance with sorting disabled and default sort keys.
		/// </returns>

		public static ClassOutlineTextEditorExtensionSortingProperties GetDefaultInstance ()
		{
			ClassOutlineTextEditorExtensionSortingProperties properties = new ClassOutlineTextEditorExtensionSortingProperties ();

			properties.IsSorting             = false;

			properties.SortKeyFoldingRegion  = 0;
			properties.SortKeyNamespace      = 1;
			properties.SortKeyType           = 2;
			properties.SortKeyField          = 3;
			properties.SortKeyProperty       = 4;
			properties.SortKeyEvent          = 5;
			properties.SortKeyMethod         = 6;
			properties.SortKeyLocalVariable  = 7;
			properties.SortKeyParameter      = 8;

			return properties;
		}
	}
}

