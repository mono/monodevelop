// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;

using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

using ICSharpCode.AssemblyAnalyser.Rules;

namespace MonoDevelop.AssemblyAnalyser
{
	public class ShowAssemblyAnalyser : AbstractMenuCommand
	{
		public override void Run ()
		{
			if (AssemblyAnalyserView.AssemblyAnalyserViewInstance == null) {
				IdeApp.Workbench.OpenDocument (new AssemblyAnalyserView ());
			} else {
				AssemblyAnalyserView.AssemblyAnalyserViewInstance.WorkbenchWindow.SelectWindow ();
			}
		}
	}
}
