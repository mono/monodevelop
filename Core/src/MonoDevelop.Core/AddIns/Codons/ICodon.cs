// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;
using System.Xml; 
using MonoDevelop.Core.AddIns.Conditions;

namespace MonoDevelop.Core.AddIns.Codons
{
	/// <summary>
	/// The <code>ICodon</code> interface describes the basic funcionality
	/// a codon must have.
	/// </summary>
	public interface ICodon
	{
		/// <summary>
		/// returns the add-in in which this codon object was declared
		/// </summary>
		AddIn AddIn {
			get;
			set;
		}
		
		/// <summary>
		/// returns the name of the xml node of this codon. (it is the same
		/// for each codon type)
		/// </summary>
		string Name {
			get;
		}
		
		/// <summary>
		/// returns true, if the codon handles the condition status on it's own, if 
		/// set to false (default) disabled codons are filtered out during build
		/// </summary>
		bool HandleConditions {
			get;
		}
		
		/// <summary>
		/// returns the ID of this codon object.
		/// </summary>
		string ID {
			get;
		}
		
		/// <summary>
		/// returns the Class which is used in the action corresponding to
		/// this codon (may return null, if no action for this codon is
		/// given)
		/// </summary>
		string Class {
			get;
		}
		
		/// <summary>
		/// Insert this codon after the codons defined in this string array
		/// </summary>
		string[] InsertAfter {
			get;
			set;
		}
		
		/// <summary>
		/// Insert this codon before the codons defined in this string array
		/// </summary>
		string[] InsertBefore {
			get;
		}
		
		/// <summary>
		/// Creates an item with the specified sub items and the current
		/// Conditions for this item.
		/// </summary>
		object BuildItem(object owner, ArrayList subItems, ConditionCollection conditions);
	}
}
