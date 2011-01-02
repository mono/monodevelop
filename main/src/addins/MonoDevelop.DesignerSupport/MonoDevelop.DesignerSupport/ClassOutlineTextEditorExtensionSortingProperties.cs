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
	/// or what primary sort key values the individual node groups have. This class is
	/// serialized to the configuration file MonoDevelopProperties.xml.
	/// </remarks>
	///
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtension"/>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtensionSorting"/>

	public class ClassOutlineTextEditorExtensionSortingProperties
	{
		static ClassOutlineTextEditorExtensionSortingProperties defaultInstance;

		public enum Group
		{
			FoldingRegion,
			Namespace,
			Type,
			Field,
			Property,
			Event,
			Method,
			LocalVariable,
			Parameter
		}

		/*
		 * Provide properties for storage in MonoDevelopProperties.xml.
		 */

		public bool IsGrouping {
			get;
			set;
		}

		public bool IsSortingAlphabetically {
			get;
			set;
		}

		public byte SortKeyFoldingRegion {
			get;
			set;
		}

		public byte SortKeyNamespace {
			get;
			set;
		}

		public byte SortKeyType	{
			get;
			set;
		}

		public byte SortKeyField {
			get;
			set;
		}

		public byte SortKeyProperty	{
			get;
			set;
		}

		public byte SortKeyEvent {
			get;
			set;
		}

		public byte SortKeyMethod {
			get;
			set;
		}

		public byte SortKeyLocalVariable {
			get;
			set;
		}

		public byte SortKeyParameter {
			get;
			set;
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
			if (defaultInstance == null) {

				defaultInstance = new ClassOutlineTextEditorExtensionSortingProperties ();

				defaultInstance.IsGrouping               = false;
				defaultInstance.IsSortingAlphabetically  = false;

				defaultInstance.SortKeyFoldingRegion     = 0;
				defaultInstance.SortKeyNamespace         = 1;
				defaultInstance.SortKeyType              = 2;
				defaultInstance.SortKeyField             = 3;
				defaultInstance.SortKeyProperty          = 4;
				defaultInstance.SortKeyEvent             = 5;
				defaultInstance.SortKeyMethod            = 6;
				defaultInstance.SortKeyLocalVariable     = 7;
				defaultInstance.SortKeyParameter         = 8;
			}

			return defaultInstance;
		}

		/// <summary>
		/// Sets the sort key in properties for the given group to key.
		/// </summary>
		///
		/// <param name="properties">
		/// A <see cref="ClassOutlineTextEditorExtensionSortingProperties"/> that stores the sort keys.
		/// </param>
		///
		/// <param name="grp">
		/// A <see cref="ClassOutlineTextEditorExtensionSortingProperties.Group"/> to set.
		/// </param>
		///
		/// <param name="key">
		/// A <see cref="System.Byte"/> that is the new sort key of the group.
		/// </param>

		public static void SetKeyForGroup (ClassOutlineTextEditorExtensionSortingProperties properties,
		                                   Group grp,
		                                   byte key)
		{
			switch (grp) {
				case Group.Event: {
					properties.SortKeyEvent = key;
					break;
				}

				case Group.Field: {
					properties.SortKeyField = key;
					break;
				}

				case Group.FoldingRegion: {
					properties.SortKeyFoldingRegion = key;
					break;
				}

				case Group.LocalVariable: {
					properties.SortKeyLocalVariable = key;
					break;
				}

				case Group.Method: {
					properties.SortKeyMethod = key;
					break;
				}

				case Group.Namespace: {
					properties.SortKeyNamespace = key;
					break;
				}

				case Group.Parameter: {
					properties.SortKeyParameter = key;
					break;
				}

				case Group.Property: {
					properties.SortKeyProperty = key;
					break;
				}

				case Group.Type: {
					properties.SortKeyType = key;
					break;
				}

				default: {
					throw new ArgumentOutOfRangeException ();
				}
			}
		}

		/// <summary>
		/// Returns the sort key stored in properties for the given group.
		/// </summary>
		///
		/// <param name="properties">
		/// A <see cref="ClassOutlineTextEditorExtensionSortingProperties"/> that stores the sort keys.
		/// </param>
		///
		/// <param name="grp">
		/// A <see cref="ClassOutlineTextEditorExtensionSortingProperties.Group"/> to query.
		/// </param>
		///
		/// <returns>
		/// A <see cref="System.Byte"/> that is the sort key of the Group.
		/// </returns>

		public static byte GetKeyForGroup (ClassOutlineTextEditorExtensionSortingProperties properties,
		                                   Group grp)
		{
			switch (grp) {
				case Group.Event: {
					return properties.SortKeyEvent;
				}

				case Group.Field: {
					return properties.SortKeyField;
				}

				case Group.FoldingRegion: {
					return properties.SortKeyFoldingRegion;
				}

				case Group.LocalVariable: {
					return properties.SortKeyLocalVariable;
				}

				case Group.Method: {
					return properties.SortKeyMethod;
				}

				case Group.Namespace: {
					return properties.SortKeyNamespace;
				}

				case Group.Parameter: {
					return properties.SortKeyParameter;
				}

				case Group.Property: {
					return properties.SortKeyProperty;
				}

				case Group.Type: {
					return properties.SortKeyType;
				}

				default: {
					throw new ArgumentOutOfRangeException ();
				}
			}
		}
	}
}

