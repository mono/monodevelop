// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;

using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Gui;

using ICSharpCode.AssemblyAnalyser.Rules;

namespace MonoDevelop.AssemblyAnalyser
{
	public class ShowAssemblyAnalyser : AbstractMenuCommand
	{
		public override void Run ()
		{
			if (AssemblyAnalyserView.AssemblyAnalyserViewInstance == null) {
				WorkbenchSingleton.Workbench.ShowView (new AssemblyAnalyserView ());
			} else {
				AssemblyAnalyserView.AssemblyAnalyserViewInstance.WorkbenchWindow.SelectWindow ();
			}
		}
	}
}
