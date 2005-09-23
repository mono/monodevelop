// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;

using MonoDevelop.Core.AddIns.Conditions;

namespace MonoDevelop.Core.AddIns.Codons
{
	/// <summary>
	/// An abstract implementation of the <code>ICodon</code> interface.
	/// </summary>
	public abstract class AbstractCodon : ICodon
	{
		[XmlMemberAttribute("id")]
		string id = null;
		
		[XmlMemberAttributeAttribute("class")]
		string myClass = null;
		
		[XmlMemberArrayAttribute("insertafter")]
		string[] insertafter = null;
		
		[XmlMemberArrayAttribute("insertbefore")]
		string[] insertbefore = null;
		
		static int internalIdCount;
		
		AddIn  addIn = null;
		
		/// <summary>
		/// Returns the AddIn in which the codon is defined.
		/// </summary>
		public AddIn AddIn {
			get {
				return addIn;
			}
			set {
				addIn = value;
			}
		}
		
		/// <summary>
		/// Returns the Name of the codon. (XmlNode name)
		/// </summary>
		public string Name {
			get {
				string name = null;
				CodonNameAttribute codonName = (CodonNameAttribute)Attribute.GetCustomAttribute(GetType(), typeof(CodonNameAttribute));
				if (codonName != null) {
					name = codonName.Name;
				}
				return name;
			}
		}
		
		/// <summary>
		/// Returns the uniqe ID of the codon.
		/// </summary>
		public string ID {
			get {
				if (id == null)
					id = "___" + (internalIdCount++);
				return id;
			}
			set {
				id = value;
			}
		}
		
		/// <summary>
		/// Returns the class attribute of the codon
		/// (this is optional, but for most codons useful, therefore
		/// it is in the base class).
		/// </summary>
		public string Class {
			get {
				return myClass;
			}
			set {
				myClass = value;
			}
		}
		
		/// <summary>
		/// returns true, if the codon handles the condition status on it's own, if 
		/// set to false (default) disabled codons are filtered out during build
		/// </summary>
		public virtual bool HandleConditions {
			get {
				return false;
			}
		}
		
		/// <summary>
		/// Insert this codon after the InsertAfter codon ID.
		/// </summary>
		public string[] InsertAfter {
			get {
				return insertafter;
			}
			set {
				insertafter = value;
			}
		}
		
		/// <summary>
		/// Insert this codon before the InsertAfter codon ID.
		/// </summary>
		public string[] InsertBefore {
			get {
				return insertbefore;
			}
			set {
				insertbefore = value;
			}
		}
		
		/// <summary>
		/// Creates an item with the specified sub items and the current
		/// Conditions for this item.
		/// </summary>
		public abstract object BuildItem(object owner, ArrayList subItems, ConditionCollection conditions);
	}
}
