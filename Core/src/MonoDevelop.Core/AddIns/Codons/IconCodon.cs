// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Conditions;

namespace MonoDevelop.Core.AddIns.Codons
{
	[CodonNameAttribute("Icon")]
	public class IconCodon : AbstractCodon
	{
		[PathAttribute()]
		[XmlMemberAttribute("location")]
		string location = null;
		
		[XmlMemberAttributeAttribute("language")]
		string language  = null;
		
		[XmlMemberAttributeAttribute("resource")]
		string resource  = null;
		
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
