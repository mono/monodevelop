//
// ClassOutlineNodeComparer.cs
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
using System.Xml.Serialization;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;


namespace MonoDevelop.DesignerSupport
{
	/// <remarks>
	/// This implementation uses a primary sort key (byte based on node's group) and
	/// a secondary sort key (string based on node's name) for comparison.
	/// </remarks>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineSortingProperties"/>
	public class ClassOutlineNodeComparer : IComparer<TreeIter>
	{
		const string DEFAULT_REGION_NAME = "region";

		Ambience  ambience;
		TreeModel model;

		public ClassOutlineSortingProperties Properties {
			get;
			set;
		}

		/// <param name="ambience">
		/// The ambience used on retrieval of node names.
		/// </param>
		/// <param name="properties">
		/// The properties used on retrieval of node sort keys and sorting settings.
		/// </param>
		/// <param name="model">
		/// The model containing the nodes to compare.
		/// </param>
		public ClassOutlineNodeComparer (Ambience ambience, ClassOutlineSortingProperties properties, TreeModel model)
		{
			this.ambience = ambience;
			Properties    = properties;
			this.model    = model;
		}

		/// <seealso cref="System.Collections.Generic.IComparer"/>
		public int Compare (TreeIter a, TreeIter b)
		{
			return CompareNodes (model, a, b);
		}

		/// <summary>
		/// Compares nodes by primary (group) and secondary (name) sort keys depending on
		/// sort settings.
		/// </summary>
		/// <remarks>
		/// When comparing names symbols are ignored (e.g. sort 'Foo()' next to '~Foo()').
		/// </remarks>
		/// <param name="model">
		/// The TreeModel that the iterators refer to.
		/// </param>
		/// <param name="nodeA">
		/// The first tree node that will be compared.
		/// </param>
		/// <param name="nodeB">
		/// The second tree node that will be compared.
		/// </param>
		/// <returns>
		/// Less than zero if iterA < iterB.
		/// Zero if iterA == iterB.
		/// Greater than zero if iterA > iterB.
		/// </returns>
		public int CompareNodes (TreeModel model, TreeIter nodeA, TreeIter nodeB)
		{
			object objectA = model.GetValue (nodeA, 0);
			object objectB = model.GetValue (nodeB, 0);

			string nameA = GetSortName (objectA);
			string nameB = GetSortName (objectB);

			byte groupKeyA = GetGroupKey (objectA);
			byte groupKeyB = GetGroupKey (objectB);

			bool isGrouping              = Properties.IsGrouping;
			bool isSortingAlphabetically = Properties.IsSortingAlphabetically;

			if (isGrouping) {

				int groupOrder = CompareKeys (groupKeyA, groupKeyB);

				// If items have the same group

				if (groupOrder == 0) {

					if (isSortingAlphabetically) {

						return String.Compare (nameA, nameB,
							System.Globalization.CultureInfo.CurrentCulture,
							System.Globalization.CompareOptions.IgnoreSymbols);
					}
				}

				return groupOrder;

			} else {

				if (isSortingAlphabetically) {

					return String.Compare (nameA, nameB,
						System.Globalization.CultureInfo.CurrentCulture,
						System.Globalization.CompareOptions.IgnoreSymbols);
				}
			}

			return 0;
		}

		/// <summary>
		/// Compares the two keys.
		/// </summary>
		/// <param name="groupKeyA">
		/// A <see cref="System.Byte"/> which is the sort key of a group.
		/// </param>
		/// <param name="groupKeyB">
		/// A <see cref="System.Byte"/> which is the sort key of a group.
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/> giving the order of the two keys.
		/// </returns>
		public static int CompareKeys (byte groupKeyA, byte groupKeyB)
		{
			if (groupKeyA < groupKeyB) {
				return -1;
			} else if (groupKeyA == groupKeyB) {
				return 0;
			} else {
				return 1;
			}
		}

		/// <summary>
		/// Returns the primary sort key based on the group of the node.
		/// </summary>
		/// <remarks>
		/// The sort key is retrieved from the properties.
		/// </remarks>
		/// <param name="node">
		/// A node in the tree. Expected to be either an IMember or a FoldingRegion.
		/// </param>
		/// <returns>
		/// A byte representing the primary sort key.
		/// byte.MaxValue if node is neither an IMember nor a FoldingRegion.
		/// </returns>
		byte GetGroupKey (object node)
		{
			if (node is FoldingRegion) {
				return Properties.SortKeyFoldingRegion;
			}
			else if (node is Namespace)	{
				return Properties.SortKeyNamespace;
			}
			else if (node is IType) {
				return Properties.SortKeyType;
			}
			else if (node is IField) {
				return Properties.SortKeyField;
			}
			else if (node is IProperty) {
				return Properties.SortKeyProperty;
			}
			else if (node is IEvent) {
				return Properties.SortKeyEvent;
			}
			else if (node is IMethod) {
				return Properties.SortKeyMethod;
			}
			else if (node is LocalVariable) {
				return Properties.SortKeyLocalVariable;
			}
			else if (node is IParameter) {
				return Properties.SortKeyParameter;
			}

			return byte.MaxValue;
		}

		/// <summary>
		/// Returns the name of the node that should be used as a secondary sort key.
		/// </summary>
		/// <param name="node">
		/// A node in the tree. Expected to be either an IMember or a FoldingRegion.
		/// </param>
		/// <returns>
		/// A string representing the secondary sort key.
		/// The empty string if node is neither an IMember nor a FoldingRegion.
		/// </returns>
		string GetSortName (object node)
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

