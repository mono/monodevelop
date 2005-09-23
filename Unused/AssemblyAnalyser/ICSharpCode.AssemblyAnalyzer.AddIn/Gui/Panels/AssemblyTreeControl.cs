// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.Reflection;

using Gtk;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;

using AssemblyAnalyser = ICSharpCode.AssemblyAnalyser.AssemblyAnalyser;

namespace MonoDevelop.AssemblyAnalyser
{
	public class AssemblyTreeControl : TreeView
	{
		TreeStore assembliesStore;
		ResultListControl resultListControl;
		
		public ResultListControl ResultListControl {
			get {
				return resultListControl;
			}
			set {
				resultListControl = value;
			}
		}
		
		public AssemblyTreeControl ()
		{
			//ClassBrowserIconsService classBrowserIconService = (ClassBrowserIconsService) ServiceManager.GetService (typeof (ClassBrowserIconsService));
			assembliesStore = new TreeStore (typeof (string), typeof (ArrayList));
			//assemblyTreeView.ImageList = classBrowserIconService.ImageList;
			
			assembliesStore.AppendValues ("AssembliesNode");
			this.Model = assembliesStore;
			this.Selection.Changed += AssemblyTreeViewSelectionChanged;
			this.Show ();
		}
		
		void AssemblyTreeViewSelectionChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			TreeModel model;

			if (this.Selection.GetSelected (out model, out iter))
			{
				this.resultListControl.PrintReport ((ArrayList) model.GetValue (iter, 1));
			}
			else
			{
				PrintAllResolutions ();
			}
		}
		
		public void PrintAllResolutions ()
		{
			ArrayList allResolutions = new ArrayList ();
			TreeIter current;
			
			if (assembliesStore.GetIterFirst (out current))
			{
				// first one is always just the label I think
				// allResolutions.AddRange ((ArrayList) assembliesStore.GetValue (current, 1));
				
				while (assembliesStore.IterNext (ref current))
				{
					assembliesStore.IterNext (ref current);
			 		allResolutions.AddRange ((ArrayList) assembliesStore.GetValue (current, 1));
				}
			}
			
			this.resultListControl.PrintReport (allResolutions);
		}
		
		public void ClearContents()
		{
			assembliesStore.Clear ();
		}
		
		public void AnalyzeAssembly (AssemblyAnalyser current, string output)
		{
			Console.WriteLine ("analyze assembly called");
		}
		
		public void AddAssembly (string assemblyFileName, ArrayList resolutions)
		{
			assembliesStore.AppendValues (System.IO.Path.GetFileName (assemblyFileName), resolutions);
			//newNode.ImageIndex = newNode.SelectedImageIndex = 2;
			//assembliesNode.Expand();
		}
	}
}
