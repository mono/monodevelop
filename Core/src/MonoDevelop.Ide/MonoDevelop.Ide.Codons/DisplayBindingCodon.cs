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
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode (Description="A display binding. The specified class must implement MonoDevelop.Ide.Codons.IDisplayBinding.")]
	internal class DisplayBindingCodon : TypeExtensionNode
	{
		public IDisplayBinding DisplayBinding {
			get {
				return GetInstance () as IDisplayBinding;
			}
		}
		public ISecondaryDisplayBinding SecondaryDisplayBinding {
			get {
				return GetInstance () as ISecondaryDisplayBinding;
			}
		}
	}
}
