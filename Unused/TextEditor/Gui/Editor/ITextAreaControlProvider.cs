// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Text;

using MonoDevelop.TextEditor.Document;
using MonoDevelop.Services;
using MonoDevelop.Gui;
using MonoDevelop.TextEditor;

namespace MonoDevelop.DefaultEditor.Gui.Editor
{
	public interface ITextEditorControlProvider
	{
		TextEditorControl TextEditorControl {
			get;
		}
	}
}
