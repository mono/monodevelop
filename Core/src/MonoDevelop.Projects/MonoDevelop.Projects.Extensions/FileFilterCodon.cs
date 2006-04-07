// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.ComponentModel;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Extensions
{
	[CodonNameAttribute("FileFilter")]
	[Description ("A file filter to be used in the Open File dialog.")]
	internal class FileFilterCodon : AbstractCodon
	{
		[Description ("Display name of the filter.")]
		[XmlMemberAttribute ("_label", IsRequired=true)]
		string filtername       = null;
		
		[Description ("Extensions to use as filter.")]
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
