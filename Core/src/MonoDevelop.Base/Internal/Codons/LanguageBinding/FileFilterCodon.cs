// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Services;

namespace MonoDevelop.Core.AddIns.Codons
{
	[CodonNameAttribute("FileFilter")]
	public class FileFilterCodon : AbstractCodon
	{
		[XmlMemberAttribute("name", IsRequired=true)]
		string filtername       = null;
		
		[XmlMemberArrayAttribute("extensions", IsRequired=true)]
		string[] extensions = null;
		
		public string FilterName {
			get {
				return filtername;
			}
			set {
				filtername = value;
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
				throw new ApplicationException("more than one level of file filters don't make sense!");
			}
			return Runtime.StringParserService.Parse(filtername) + "|" + String.Join(";", extensions);
		}
	}
}
