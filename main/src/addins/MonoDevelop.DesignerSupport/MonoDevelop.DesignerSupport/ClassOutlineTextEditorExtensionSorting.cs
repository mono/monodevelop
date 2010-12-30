//
// ClassOutlineTextEditorExtensionSorting.cs
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
using System.Xml.Serialization;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;


namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Provides the function used to compare tree nodes for sorting in the tree view
	/// of ClassOutlineTextEditorExtension.
	/// </summary>
	///
	/// <remarks>
	/// This implementation uses a primary sort key (byte based on node's type) and
	/// a secondary sort key (string based on node's name) for comparison.
	/// </remarks>
	///
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtensionSortingProperties"/>

	public class ClassOutlineTextEditorExtensionSorting
	{
		const string DEFAULT_REGION_NAME        = "region";

		Ambience ambience;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		///
		/// <param name="ambience">
		/// The ambience used on retrieval of node names.
		/// </param>

		public ClassOutlineTextEditorExtensionSorting (Ambience ambience)
		{
			this.ambience   = ambience;
		}

		/// <summary>
		/// Compares nodes by primary and secondary sort keys.
		/// </summary>
		///
		/// <remarks>
		/// The primary sort key is retrieved with GetPrimarySortKey.
		/// The secondary sort key is returned by GetSortName.
		/// When comparing names symbols are ignored (e.g. sort 'Foo()' next to '~Foo()').
		/// </remarks>
		///
		/// <param name="model">
		/// The TreeModel that the iterators refer to.
		/// </param>
		/// <param name="nodeA">
		/// The first tree node that will be compared.
		/// </param>
		/// <param name="nodeB">
		/// The second tree node that will be compared.
		/// </param>
		///
		/// <returns>
		/// Less than zero if iterA < iterB.
		/// Zero if iterA == iterB.
		/// Greater than zero if iterA > iterB.
		/// </returns>

		public virtual int Compare (TreeModel model, TreeIter nodeA, TreeIter nodeB)
		{
			object objectA = model.GetValue (nodeA, 0);
			object objectB = model.GetValue (nodeB, 0);

			string nameA = GetSortName (objectA);
			string nameB = GetSortName (objectB);

			byte primarySortKeyA = GetPrimarySortKey (objectA);
			byte primarySortKeyB = GetPrimarySortKey (objectB);

			if (primarySortKeyA == primarySortKeyB) {

				// Compare by secondary sort key name ignoring symbols

				return String.Compare (nameA, nameB,
					System.Globalization.CultureInfo.CurrentCulture,
					System.Globalization.CompareOptions.IgnoreSymbols);
			}
			else {

				// Compare by primary sort key

				if (primarySortKeyA < primarySortKeyB) {
					return -1;
				}
				else if (primarySortKeyA == primarySortKeyB) {
					return 0;
				}
				else {
					return 1;
				}
			}
		}

		/// <summary>
		/// Returns the primary sort key based on the type of the node.
		/// </summary>
		///
		/// <remarks>
		/// The sort key is retrieved from the properties.
		/// </remarks>
		///
		/// <param name="node">
		/// A node in the tree. Expected to be either an IMember or a FoldingRegion.
		/// </param>
		///
		/// <returns>
		/// A byte representing the primary sort key.
		/// byte.MaxValue if node is neither an IMember nor a FoldingRegion.
		/// </returns>

		protected virtual byte GetPrimarySortKey (object node)
		{
			ClassOutlineTextEditorExtensionSortingProperties properties = PropertyService.Get<ClassOutlineTextEditorExtensionSortingProperties> (
				ClassOutlineTextEditorExtension.SORTING_PROPERTY);

			if (node is FoldingRegion) {
				return properties.SortKeyFoldingRegion;
			}
			else if (node is Namespace)	{
				return properties.SortKeyNamespace;
			}
			else if (node is IType) {
				return properties.SortKeyType;
			}
			else if (node is IField) {
				return properties.SortKeyField;
			}
			else if (node is IProperty) {
				return properties.SortKeyProperty;
			}
			else if (node is IEvent) {
				return properties.SortKeyEvent;
			}
			else if (node is IMethod) {
				return properties.SortKeyMethod;
			}
			else if (node is LocalVariable) {
				return properties.SortKeyLocalVariable;
			}
			else if (node is IParameter) {
				return properties.SortKeyParameter;
			}

			return byte.MaxValue;
		}

		/// <summary>
		/// Returns the name of the node that should be used as a secondary sort key.
		/// </summary>
		///
		/// <param name="node">
		/// A node in the tree. Expected to be either an IMember or a FoldingRegion.
		/// </param>
		///
		/// <returns>
		/// A string representing the secondary sort key.
		/// The empty string if node is neither an IMember nor a FoldingRegion.
		/// </returns>

		protected virtual string GetSortName (object node)
		{
			if (node is IMember) {

				// Return the name without type or parameters

				return ambience.GetString ((IMember)node, 0);

			} else if (node is FoldingRegion) {

				// Return trimmed region name or fallback

				string name = ((FoldingRegion)node).Name.Trim ();

				/*
				 * ClassOutlineTextEditorExtension uses a fallback name for regions
				 * so we do the same here with a slighty different name.
				 */

				if (string.IsNullOrEmpty (name)) {
					name = DEFAULT_REGION_NAME;
				}

				return name;
			}

			return string.Empty;
		}
	}
}

