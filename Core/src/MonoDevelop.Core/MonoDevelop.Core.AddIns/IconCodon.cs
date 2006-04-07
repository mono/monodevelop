// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;
using System.ComponentModel;

using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core.AddIns
{
	[CodonNameAttribute("Icon")]
	[Description ("An icon bound to a language or file extension.")]
	public class IconCodon : AbstractCodon
	{
		[Description ("Obsolete. Do not use.")]
		[PathAttribute()]
		[XmlMemberAttribute("location")]
		string location = null;
		
		[Description ("Name of the language represented by this icon. Optional.")]
		[XmlMemberAttributeAttribute("language")]
		string language  = null;
		
		[Description ("Resource name.")]
		[XmlMemberAttributeAttribute("resource")]
		string resource  = null;
		
		[Description ("File extensions represented by this icon. Optional.")]
		[XmlMemberArrayAttribute("extensions")]
		string[] extensions = null;
		
		public string Language {
			get {
				return language;
			}
			set {
				language = value;
			}
		}
		
		public string Location {
			get {
				return location;
			}
			set {
				location = value;
			}
		}
		
		public string Resource {
			get {
				return resource;
			}
			set {
				resource = value;
			}
		}
		
		public string[] Extensions {
			get {
				return extensions;
			}
			set {
				extensions = value;
			}
		}
		
		/// <summary>
		/// Creates an item with the specified sub items. And the current
		/// Condition status for this item.
		/// </summary>
		public override object BuildItem(object owner, ArrayList subItems, ConditionCollection conditions)
		{
			if (subItems.Count > 0) {
				throw new ApplicationException("more than one level of icons don't make sense!");
			}
			
			return this;
		}
		
	}
}
