// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Ide.Gui.Content
{
	public interface IEditable: ITextBuffer
	{
		IClipboardHandler ClipboardHandler {
			get;
		}
		
		new string Text {
			get;
			set;
		}
		
		void Undo();
		void Redo();
		
		new string SelectedText { get; set; }
		
		void InsertText (object position, string text);
		void DeleteText (object position, int length);
		
		event EventHandler TextChanged;
	}
}
