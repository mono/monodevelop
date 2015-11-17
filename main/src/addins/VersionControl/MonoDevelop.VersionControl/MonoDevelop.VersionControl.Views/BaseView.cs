using System;
using System.IO;
using MonoDevelop.Ide.Gui;
using System.Text;
using Gtk;

namespace MonoDevelop.VersionControl
{
	public abstract class BaseView : ViewContent
	{
		string name;
		
		protected BaseView (string name)
		{
			this.name = name;
		}

		public override Widget Control {
			get
			{
				throw new NotImplementedException ();
			}
		}

		public override string TabPageLabel {
			get { return name; }
		}

		public override void Load (FileOpenInformation fileOpenInformation)
		{
			throw new NotImplementedException ();
		}
	}
}