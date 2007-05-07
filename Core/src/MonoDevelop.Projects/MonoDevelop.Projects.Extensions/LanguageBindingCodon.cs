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
using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Extensions
{
	[ExtensionNode (Description="A language binding. The specified class must implement MonoDevelop.Projects.ILanguageBinding")]
	internal class LanguageBindingCodon : TypeExtensionNode
	{
		[NodeAttribute("supportedextensions", "File extensions supported by this binding (to be shown in the Open File dialog)")]
		string[] supportedExtensions;
		
		public string[] Supportedextensions {
			get {
				return supportedExtensions;
			}
			set {
				supportedExtensions = value;
			}
		}
		
		public ILanguageBinding LanguageBinding {
			get {
				return (ILanguageBinding) GetInstance ();
			}
		}
	}
}
