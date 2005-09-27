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

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.SourceEditor.Document;
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.Actions;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.HtmlControl;
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor.Commands
{
	public class ToggleFolding : AbstractEditActionMenuCommand
	{
		public override IEditAction EditAction {
			get {
				//return new MonoDevelop.SourceEditor.Actions.ToggleFolding();
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
				//return new MonoDevelop.SourceEditor.Actions.ToggleAllFoldings();
			}
		}
	}
	
	public class ShowDefinitionsOnly : AbstractEditActionMenuCommand
	{
		public override IEditAction EditAction {
			get {
				Console.WriteLine ("Not implemented in the new Editor");
				return null;
				//return new MonoDevelop.SourceEditor.Actions.ShowDefinitionsOnly();
			}
		}
	}

}
