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
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.CSharp.ClassOutline
{
	/// <remarks>
	/// This implementation uses a primary sort key (int based on node's group) and
	/// a secondary sort key (string based on node's name) for comparison.
	/// </remarks>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineSettings"/>
	class OutlineNodeComparer : IComparer<TreeIter>
	{
		const string DEFAULT_REGION_NAME = "region";
		
		AstAmbience ambience;
		TreeModel model;
		OutlineSettings settings;
		int[] groupTable;

		/// <param name="ambience">
		/// The ambience used on retrieval of node names.
		/// </param>
		/// <param name="settings">
		/// The properties used on retrieval of node sort keys and sorting settings.
		/// </param>
		/// <param name="model">
		/// The model containing the nodes to compare.
		/// </param>
		public OutlineNodeComparer (AstAmbience ambience, OutlineSettings settings, TreeModel model)
		{
			this.ambience = ambience;
			this.settings = settings;
			this.model = model;
			BuildGroupTable ();
		}

		public int Compare (TreeIter a, TreeIter b)
		{
			return CompareNodes (model, a, b);
		}

		/// <summary>
		/// Compares nodes by primary (group) and secondary (name) sort keys depending on
		/// sort properties.
		/// </summary>
		/// <remarks>
		/// For methods, constructors and destructors are sorted at the top.
		/// </remarks>
		/// <param name="model">
		/// The TreeModel that the iterators refer to.
		/// </param>
		/// <param name="node1">
		/// The first tree node that will be compared.
		/// </param>
		/// <param name="node2">
		/// The second tree node that will be compared.
		/// </param>
		/// <returns>
		/// Less than zero if nodeA &lt; nodeB
		/// Zero if nodeA == nodeB.
		/// Greater than zero if nodeA &gt; nodeB.
		/// </returns>
		public int CompareNodes (TreeModel model, TreeIter node1, TreeIter node2)
		{
			object o1 = model.GetValue (node1, 0);
			object o2 = model.GetValue (node2, 0);
			if (o1 == null) {
				return o2 == null? 0 : 1;
			} else if (o2 == null) {
				return -1;
			}
			
			if (settings.IsGrouped) {
				int groupOrder = GetGroupPriority (o1) - GetGroupPriority (o2);
				if (groupOrder != 0)
					return groupOrder;
				
				var m1 = o1 as BaseMethodDeclarationSyntax;
				if (m1 != null)
					return CompareMethods (m1, (BaseMethodDeclarationSyntax)o2, settings.IsSorted);
			}
			
			if (settings.IsSorted)
				return CompareName (o1, o2);
			
			return CompareRegion (o1, o2);
		}

		int CompareName (object o1, object o2)
		{
			var sort = string.Compare (
				GetSortName (o1),
				GetSortName (o2),
				System.Globalization.CultureInfo.CurrentCulture,
				System.Globalization.CompareOptions.IgnoreSymbols);
			if (sort == 0)
				return CompareRegion (o1, o2);
			return sort;
		}

		int CompareMethods (BaseMethodDeclarationSyntax m1, BaseMethodDeclarationSyntax m2, bool isSortingAlphabetically)
		{
			// Here we sort constructors before finalizers before other methods.
			// Remember that two constructors have the same name.

			// Sort constructors at top.
			
			bool isCtor1 = m1 is ConstructorDeclarationSyntax;
			bool isCtor2 = m2 is ConstructorDeclarationSyntax;

			if (isCtor1) {
				if (isCtor2)
					return CompareRegion (m1, m2);
				else
					return -1;
			} else if (isCtor2) {
				return 1;
			}
			
			// Sort finalizers after constructors.
			//
			// Sorting two finalizers even though this is not valid C#. This gives a correct
			// outline during editing.
			
			bool isFinalizer1 = IsFinalizer (m1);
			bool isFinalizer2 = IsFinalizer (m2);
			
			if (isFinalizer1) {
				if (isFinalizer2)
					return CompareRegion (m1, m2);
				else
					return -1;
			} else if (isFinalizer2) {
				return 1;
			}
			
			if (isSortingAlphabetically)
				return CompareName (m1, m2);
			else
				return CompareRegion (m1, m2);
		}

		bool IsConstructor (object node)
		{
			return node is ConstructorDeclarationSyntax;
		}

		bool IsFinalizer (object node)
		{
			return node is DestructorDeclarationSyntax;
		}
		
		const int GROUP_INDEX_REGIONS = 0;
		const int GROUP_INDEX_NAMESPACES = 1;
		const int GROUP_INDEX_TYPES = 2;
		const int GROUP_INDEX_FIELDS = 3;
		const int GROUP_INDEX_PROPERTIES = 4;
		const int GROUP_INDEX_EVENTS = 5;
		const int GROUP_INDEX_METHODS = 6;
		
		void BuildGroupTable ()
		{
			groupTable = new int[7];
			int i = -10;
			foreach (var g in settings.GroupOrder) {
				switch (g) {
				case OutlineSettings.GroupRegions:
					groupTable[GROUP_INDEX_REGIONS] = i++;
					break;
				case OutlineSettings.GroupNamespaces:
					groupTable[GROUP_INDEX_NAMESPACES] = i++;
					break;
				case OutlineSettings.GroupTypes:
					groupTable[GROUP_INDEX_TYPES] = i++;
					break;
				case OutlineSettings.GroupFields:
					groupTable[GROUP_INDEX_FIELDS] = i++;
					break;
				case OutlineSettings.GroupProperties:
					groupTable[GROUP_INDEX_PROPERTIES] = i++;
					break;
				case OutlineSettings.GroupEvents:
					groupTable[GROUP_INDEX_EVENTS] = i++;
					break;
				case OutlineSettings.GroupMethods:
					groupTable[GROUP_INDEX_METHODS] = i++;
					break;
				}
			}
		}

		int GetGroupPriority (object node)
		{
			if (node is SyntaxTrivia)
				return groupTable[GROUP_INDEX_REGIONS];
			if (node is string)
				return groupTable[GROUP_INDEX_NAMESPACES];
			if (node is BaseTypeDeclarationSyntax)
				return groupTable[GROUP_INDEX_TYPES];
			if (node is FieldDeclarationSyntax)
				return groupTable[GROUP_INDEX_FIELDS];
			if (node is PropertyDeclarationSyntax)
				return groupTable[GROUP_INDEX_PROPERTIES];
			if (node is EventDeclarationSyntax || node is EventFieldDeclarationSyntax)
				return groupTable[GROUP_INDEX_EVENTS];
			if (node is BaseMethodDeclarationSyntax)
				return groupTable[GROUP_INDEX_METHODS];
			return 0;
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
			if (node is SyntaxNode) {
				// Return the name without type or parameters
				return ambience.GetEntityMarkup ((SyntaxNode)node);
			}
		
			if (node is SyntaxTrivia) {
				// Return trimmed region name or fallback
				string name = ((SyntaxTrivia)node).ToString ().Trim ();
				
				// ClassOutlineTextEditorExtension uses a fallback name for regions
				// so we do the same here with a slighty different name.
				if (name.Length == 0)
					name = DEFAULT_REGION_NAME;
				
				return name;
			}
			
			return string.Empty;
		}
		
		internal static int GetOffset (object o)
		{
			var m = o as SyntaxNode;
			if (m != null)
				return m.SpanStart;
			//			if (o is FoldingRegion)
			//	return ((FoldingRegion)o).Region;
			return 0;
		}
		
		internal static int CompareRegion (object o1, object o2)
		{
			return GetOffset (o1).CompareTo (GetOffset (o2));
		}
	}
}
