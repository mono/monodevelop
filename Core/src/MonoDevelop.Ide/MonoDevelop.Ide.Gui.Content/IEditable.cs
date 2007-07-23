// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Ide.Gui.Content
{
	public interface IEditableTextBuffer: ITextBuffer, IEditableTextFile
	{
		IClipboardHandler ClipboardHandler {
			get;
		}
		
		void Undo();
		void Redo();
		
		void BeginAtomicUndo ();
		void EndAtomicUndo ();
			
		new string SelectedText { get; set; }
		
		event EventHandler TextChanged;
	}
}
