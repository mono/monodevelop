// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;

using Mono.Addins;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode (Description="A project template.")]
	internal class ProjectTemplateCodon : ExtensionNode
	{
		[NodeAttribute("resource", true, "Name of the resource where the template is stored.")]
		string resource;
		
		public string Resource {
			get {
				return resource;
			}
			set {
				resource = value;
			}
		}
	}
}
