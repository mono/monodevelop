// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Text;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Actions;
using MonoDevelop.Gui;
using MonoDevelop.Gui.HtmlControl;
using MonoDevelop.Core.Services;

namespace MonoDevelop.DefaultEditor.Commands
{
	public class ToggleFolding : AbstractEditActionMenuCommand
	{
		public override IEditAction EditAction {
			get {
				//return new MonoDevelop.TextEditor.Actions.ToggleFolding();
				Console.WriteLine ("Not implemented in the new editor");
				return null;
			}
		}
	}
	
	public class ToggleAllFoldings : AbstractEditActionMenuCommand
	{
		public override IEditAction EditAction {
			get {
				Console.WriteLine ("Not implemented in the new Editor");
				return null;
				//return new MonoDevelop.TextEditor.Actions.ToggleAllFoldings();
			}
		}
	}
	
	public class ShowDefinitionsOnly : AbstractEditActionMenuCommand
	{
		public override IEditAction EditAction {
			get {
				Console.WriteLine ("Not implemented in the new Editor");
				return null;
				//return new MonoDevelop.TextEditor.Actions.ShowDefinitionsOnly();
			}
		}
	}

}
