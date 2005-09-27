// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Gui
{
	public interface IPadWindow
	{
		string Title { get; set; }
		string Icon { get; set; }
		bool Visible { get; set; }
		IPadContent Content { get; }
		
		void Activate ();
		
		event EventHandler PadShown;
		event EventHandler PadHidden;
	}
	
	internal class PadWindow: IPadWindow
	{
		string title;
		string icon;
		IPadContent content;
		IWorkbenchLayout layout;
		
		internal PadWindow (IWorkbenchLayout layout, IPadContent content)
		{
			this.layout = layout;
			this.content = content;
		}
		
		public IPadContent Content {
			get { return content; }
		}
		
		public string Title {
			get { return title; }
			set { 
				title = value;
				if (TitleChanged != null)
					TitleChanged (this, EventArgs.Empty);
			}
		}
		
		public string Icon  {
			get { return icon; }
			set { 
				icon = value;
				if (IconChanged != null)
					IconChanged (this, EventArgs.Empty);
			}
		}
		
		public bool Visible {
			get {
				return layout.IsVisible (content);
			}
			set {
				if (value) {
					layout.ShowPad (content);
					if (PadShown != null) PadShown (this, EventArgs.Empty);
				}
				else {
					layout.HidePad (content);
					if (PadHidden != null) PadHidden (this, EventArgs.Empty);
				}
			}
		}
		
		public void Activate ()
		{
			layout.ActivatePad (content);
		}
		
		public event EventHandler PadShown;
		public event EventHandler PadHidden;
		
		internal event EventHandler TitleChanged;
		internal event EventHandler IconChanged;
	}
}
