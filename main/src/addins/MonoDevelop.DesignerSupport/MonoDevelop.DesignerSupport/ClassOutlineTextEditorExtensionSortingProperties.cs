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
		static ClassOutlineTextEditorExtensionSortingProperties defaultInstance;

		/*
		 * Provide properties for storage in MonoDevelopProperties.xml.
		 */

		public bool IsGroupingByType {
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

				defaultInstance.IsGroupingByType         = false;
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
	}
}

