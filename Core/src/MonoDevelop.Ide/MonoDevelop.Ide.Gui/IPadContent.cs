// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;

namespace MonoDevelop.Ide.Gui 
{
	/// <summary>
	/// The IPadContent interface is the basic interface to all "tool" windows
	/// in SharpDevelop.
	/// </summary>
	public interface IPadContent : IDisposable
	{
		void Initialize (IPadWindow container);

		/// <summary>
		/// Returns the Gtk Widget for this pad.
		/// </summary>
		Gtk.Widget Control {
			get;
		}

		
		/// <summary>
		/// Re-initializes all components of the pad. Don't call unless
		/// you know what you do.
		/// </summary>
		void RedrawContent();
	}
}
