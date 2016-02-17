using System;
using System.IO;
using MonoDevelop.Ide.Gui;
using System.Text;
using Gtk;
using System.Threading.Tasks;
using MonoDevelop.Components;

namespace MonoDevelop.VersionControl
{
	public abstract class BaseView : ViewContent
	{
		string name;
		
		protected BaseView (string name)
		{
			ContentName = this.name = name;
		}

		public override string TabPageLabel {
			get { return name; }
		}
	}
}