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
using Mono.Addins;

using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core.Addins
{
	[Description ("An icon bound to a language or file extension.")]
	public class IconCodon : ExtensionNode
	{
		[Description ("Obsolete. Do not use.")]
		[NodeAttribute("location")]
		string location = null;
		
		[Description ("Name of the language represented by this icon. Optional.")]
		[NodeAttribute("language")]
		string language  = null;
		
		[Description ("Resource name.")]
		[NodeAttribute("resource")]
		string resource  = null;
		
		[Description ("File extensions represented by this icon. Optional.")]
		[NodeAttribute("extensions")]
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
	}
}
