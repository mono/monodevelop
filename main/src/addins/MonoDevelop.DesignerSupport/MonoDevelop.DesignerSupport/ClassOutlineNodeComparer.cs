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
	/// This implementation uses a primary sort key (int based on node's group) and
	/// a secondary sort key (string based on node's name) for comparison.
	/// </remarks>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineSortingProperties"/>
	public class ClassOutlineNodeComparer : IComparer<TreeIter>
	{
		const string DEFAULT_REGION_NAME = "region";

		Ambience      ambience;
		TreeModel     model;
		GroupComparer groupComparer = new GroupComparer ();

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
		/// sort properties.
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
		/// Less than zero if nodeA < nodeB
		/// Zero if nodeA == nodeB.
		/// Greater than zero if nodeA > nodeB.
		/// </returns>
		public int CompareNodes (TreeModel model, TreeIter nodeA, TreeIter nodeB)
		{
			object objectA = model.GetValue (nodeA, 0);
			object objectB = model.GetValue (nodeB, 0);

			string nameA = GetSortName (objectA);
			string nameB = GetSortName (objectB);

			Group groupA = GetGroup (objectA);
			Group groupB = GetGroup (objectB);

			bool isGrouping              = Properties.IsGrouping;
			bool isSortingAlphabetically = Properties.IsSortingAlphabetically;

			if (isGrouping) {

				int groupOrder = groupComparer.Compare (groupA, groupB);

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
		/// Returns the sort group based on the group of the node.
		/// </summary>
		/// <remarks>
		/// The sort group is retrieved from the properties.
		/// </remarks>
		/// <param name="node">
		/// A node in the tree. Expected to be either an IMember or a FoldingRegion.
		/// </param>
		/// <returns>
		/// The sorting group for this node.
		/// If the node is not an IMember or a FoldingRegion then the group for parameters
		/// is returned.
		/// </returns>
		Group GetGroup (object node)
		{
			if (node is FoldingRegion) {
				return Properties.FoldingRegionsGroup;
			}
			else if (node is Namespace)	{
				return Properties.NamespacesGroup;
			}
			else if (node is IType) {
				return Properties.TypesGroup;
			}
			else if (node is IField) {
				return Properties.FieldsGroup;
			}
			else if (node is IProperty) {
				return Properties.PropertiesGroup;
			}
			else if (node is IEvent) {
				return Properties.EventsGroup;
			}
			else if (node is IMethod) {
				return Properties.MethodsGroup;
			}
			else if (node is LocalVariable) {
				return Properties.LocalVariablesGroup;
			}
			else {
				return Properties.ParametersGroup;
			}
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

				//
				// ClassOutlineTextEditorExtension uses a fallback name for regions
				// so we do the same here with a slighty different name.
				//

				if (string.IsNullOrEmpty (name)) {
					name = DEFAULT_REGION_NAME;
				}

				return name;
			}

			return string.Empty;
		}
	}
}

