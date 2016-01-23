
using System;
using MonoDevelop.Components;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment.Linux
{
	public class DotDesktopView: ViewContent
	{
		DotDesktopViewWidget widget;
		DesktopEntry entry;
		
		public DotDesktopView ()
		{
			widget = new DotDesktopViewWidget ();
			widget.Changed += delegate {
				IsDirty = true;
			};
			entry = new DesktopEntry ();
			widget.DesktopEntry = entry;
		}
		
		public override string TabPageLabel {
			get {
				return MonoDevelop.Core.GettextCatalog.GetString ("Desktop Entry");
			}
		}
		
		public override Task Load (FileOpenInformation fileOpenInformation)
		{
			ContentName = fileOpenInformation.FileName;
			entry.Load (fileOpenInformation.FileName);
			widget.DesktopEntry = entry;
			return Task.FromResult (true);
		}
		
		public override Task Save (FileSaveInformation fileSaveInformation)
		{
			entry.Save (fileSaveInformation.FileName);
			IsDirty = false;
			return Task.FromResult (true);
		}
		
		public override Control Control {
			get { return widget; }
		}
	}
}
