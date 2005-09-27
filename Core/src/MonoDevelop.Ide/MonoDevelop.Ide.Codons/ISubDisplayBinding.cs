// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	/// <summary>
	/// This class defines the SharpDevelop display binding interface, it is a factory
	/// structure, which creates IViewContents.
	/// </summary>
	public interface ISecondaryDisplayBinding
	{
		bool CanAttachTo(IViewContent content);
		
		ISecondaryViewContent CreateSecondaryViewContent(IViewContent viewContent);
	}
}
