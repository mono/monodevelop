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
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Extensions
{
	[Description ("A file filter to be used in the Open File dialog.")]
	internal class FileFilterCodon : TypeExtensionNode
	{
		[Description ("Display name of the filter.")]
		[NodeAttribute ("_label", true)]
		string filtername = null;
		
		[Description ("Extensions to use as filter.")]
		[NodeAttribute("extensions", true)]
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
		
		public override object CreateInstance ()
		{
			return Runtime.StringParserService.Parse (filtername) + "|" + String.Join(";", extensions);
		}
	}
}
