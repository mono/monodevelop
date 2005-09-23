// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using Gtk;

using AssemblyAnalyser = ICSharpCode.AssemblyAnalyser.AssemblyAnalyser;

namespace MonoDevelop.AssemblyAnalyser
{
	public class AssemblyAnalyserControl : Frame
	{
		HPaned horiz = new HPaned ();
		VPaned vert = new VPaned ();

		ResultListControl resultListControl;
		AssemblyTreeControl assemblyTreeControl;
		ResultDetailsView resultDetailsView;
		
		public AssemblyAnalyserControl ()
		{
			this.resultDetailsView = new ResultDetailsView ();
			this.assemblyTreeControl = new AssemblyTreeControl ();
			this.resultListControl = new ResultListControl ();
			
			horiz.Add1 (assemblyTreeControl);
			horiz.Add2 (vert);
			
			vert.Add1 (resultListControl);
			vert.Add2 (resultDetailsView);
			
			horiz.Position = 200;
			vert.Position = 200;

			resultListControl.ResultDetailsView = resultDetailsView;
			assemblyTreeControl.ResultListControl = resultListControl;
			this.Add (horiz);
			this.ShowAll ();
		}
		
		public void ClearContents ()
		{
			resultListControl.ClearContents ();
			assemblyTreeControl.ClearContents ();
		}
		
		public void AnalyzeAssembly (AssemblyAnalyser analyser, string fileName)
		{
			if (File.Exists (fileName)) {
				analyser.Analyse (fileName);
				assemblyTreeControl.AddAssembly (fileName, analyser.Resolutions);
			}
		}
		
		public void PrintAllResolutions ()
		{
			assemblyTreeControl.PrintAllResolutions ();
		}
	}
}
