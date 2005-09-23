// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;

using MonoDevelop.Core.AddIns.Conditions;

namespace MonoDevelop.Core.AddIns.Codons
{
	[CodonNameAttribute("FileTemplate")]
	public class FileTemplateCodon : AbstractCodon
	{
		[XmlMemberAttribute("resource", IsRequired = true)]
		string resource;
		
		public string Resource {
			get {
				return resource;
			}
			set {
				resource = value;
			}
		}
		
		/// <summary>
		/// Creates an item with the specified sub items. And the current
		/// Condition status for this item.
		/// </summary>
		public override object BuildItem(object owner, ArrayList subItems, ConditionCollection conditions)
		{
			return this;
		}
	}
}
