// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;

namespace MonoDevelop.Gui 
{
	/// <summary>
	/// The IPadContent interface is the basic interface to all "tool" windows
	/// in SharpDevelop.
	/// </summary>
	public interface IPadContent : IDisposable
	{
		/// <summary>
		/// Id of the pad
		/// </summary>
		string Id {
			get;
		}
		
		/// <summary>
		/// Returns the default placement of the pad: left, right, top, bottom.
		/// Relative positions can be used, for example: "ProjectPad/left"
		/// would show the pad at the left of the project pad. When using
		/// relative placements several positions can be provided. If the
		/// pad can be placed in the first position, the next one will be
		/// tried. For example "ProjectPad/left; bottom".
		/// </summary>
		string DefaultPlacement {
			get;
		}
		
		/// <summary>
		/// Returns the title of the pad.
		/// </summary>
		string Title {
			get;
		}
		
		/// <summary>
		/// Returns the icon bitmap resource name of the pad. May be null, if the pad has no
		/// icon defined.
		/// </summary>
		string Icon {
			get;
		}

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
		
		/// <summary>
		/// Is called when the title of this pad has changed.
		/// </summary>
		event EventHandler TitleChanged;
		
		/// <summary>
		/// Is called when the icon of this pad has changed.
		/// </summary>
		event EventHandler IconChanged;
	}
}
