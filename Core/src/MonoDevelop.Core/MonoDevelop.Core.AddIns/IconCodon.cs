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
	[ExtensionNode ("Icon", "An icon bound to a language or file extension")]
	public class IconCodon : ExtensionNode
	{
		[NodeAttribute("language", "Name of the language represented by this icon. Optional.")]
		string language  = null;
		
		[NodeAttribute("resource", "Resource name.")]
		string resource  = null;
		
		[NodeAttribute("extensions", "File extensions represented by this icon. Optional.")]
		string[] extensions = null;
		
		public string Language {
			get {
				return language;
			}
			set {
				language = value;
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
