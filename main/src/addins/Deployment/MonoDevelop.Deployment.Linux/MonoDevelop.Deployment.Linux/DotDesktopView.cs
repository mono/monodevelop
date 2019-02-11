
using System;
using MonoDevelop.Components;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment.Linux
{
	[PlatformCondition ("linux")]
	[ExportFileDocumentController (FileExtension = ".desktop", MimeType = "application/x-desktop", Name = "Desktop Entry", CanUseAsDefault = true, InsertBefore = "DefaultDisplayBinding")]
	public class DotDesktopView: FileDocumentController
	{
		DotDesktopViewWidget widget;
		DesktopEntry entry;

		protected override Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			TabPageLabel = GettextCatalog.GetString ("Desktop Entry");
			return base.OnInitialize (modelDescriptor, status);
		}

		protected override Control OnGetViewControl (DocumentViewContent view)
		{
			widget = new DotDesktopViewWidget ();
			widget.Changed += delegate {
				HasUnsavedChanges = true;
			};
			entry = new DesktopEntry ();
			widget.DesktopEntry = entry;

			entry.Load (FilePath);
			widget.DesktopEntry = entry;
			return (Control)widget;
		}

		protected override Task OnSave ()
		{
			entry.Save (FilePath);
			return Task.CompletedTask;
		}
	}
}
