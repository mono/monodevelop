// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Gui
{
	/// <summary>
	/// The base interface for secondary view contents
	/// (designer, doc viewer etc.)
	/// </summary>
	public interface ISecondaryViewContent : IBaseViewContent
	{
		/// <summary>
		/// Is called when the view content is selected inside the window
		/// tab. NOT when the windows is selected.
		/// </summary>
		void Selected();
		
		/// <summary>
		/// Is called when the view content is deselected inside the window
		/// tab before the other window is selected. NOT when the windows is deselected.
		/// </summary>
		void Deselected();
		
		/// <summary>
		/// Is called before the save operation of the main IViewContent
		/// </summary>
		void NotifyBeforeSave();

		void BaseContentChanged ();

		string TabPageLabel {
			get;
		}
	}
}
