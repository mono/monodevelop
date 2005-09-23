// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System.Drawing;
using MonoDevelop.Gui;
using Gtk;
using MonoDevelop.Gui.Components;

namespace MonoDevelop.Services
{
	public interface IStatusBarService
	{
		Widget Control {
			get; 
		}

		bool CancelEnabled {
			get;
			set;
		}
		
		void BeginProgress (string name);
		void SetProgressFraction (double work);
		void EndProgress ();
		void Pulse ();
		
		IStatusIcon ShowStatusIcon (Gtk.Image image);
		void HideStatusIcon (IStatusIcon icon);
		
		void ShowErrorMessage(string message);
		
		void SetMessage (string message);					
		void SetMessage (Gtk.Image image, string message);
		
		void SetCaretPosition (int ln, int col, int ch);
		void SetInsertMode (bool insertMode);
	}
}
