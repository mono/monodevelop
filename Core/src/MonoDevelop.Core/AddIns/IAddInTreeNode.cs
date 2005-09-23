// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Xml;

using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Core.AddIns.Codons;

namespace MonoDevelop.Core.AddIns
{
	/// <summary>
	/// This interface represents a tree node in the <see cref="IAddInTree"/>
	/// </summary>
	public interface IAddInTreeNode
	{
		/// <value>
		/// A hash table containing the child nodes. Where the key is the
		/// node name and the value is a <see cref="IAddInTreeNode"/> object.
		/// </value>
		Hashtable ChildNodes {
			get;
		}
		
		/// <value>
		/// A codon defined in this node, or <code>null</code> if no codon
		/// was defined.
		/// </value>
		ICodon Codon {
			get;
		}
		
		/// <value>
		/// All conditions for this TreeNode.
		/// </value>
		ConditionCollection ConditionCollection {
			get;
		}
		
		/// <remarks>
		/// Gets the current ConditionFailedAction 
		/// </remarks>
		ConditionFailedAction GetCurrentConditionFailedAction(object caller);
			
		/// <remarks>
		/// Builds all child items of this node using the <code>BuildItem</code>
		/// method of each codon in the child tree.
		/// </remarks>
		/// <returns>
		/// An <code>ArrayList</code> that contains the build sub items that
		/// this node contains
		/// </returns>
		ArrayList BuildChildItems(object caller);
		
		/// <remarks>
		/// Builds one child item of this node using the <code>BuildItem</code>
		/// method of the codon in the child tree. The sub item with the <code>ID</code>
		/// <code>childItemID</code> will be build.
		/// </remarks>
		object    BuildChildItem(string childItemID, object caller);
	}
}
