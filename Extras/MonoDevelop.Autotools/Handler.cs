
using System;
using System.Text;
using System.Collections;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects.Deployment;
using MonoDevelop.Projects.Gui.Deployment;

namespace MonoDevelop.Autotools
{
	public class Handler: IDeployHandler
	{
		public string Id { 
			get { return "MonoDevelop.Autotools.Deployer"; }
		}
		
		public string Description {
			get { return "Tarball"; }
		}
		
		public string Icon {
			get { return null; }
		}
		
		public bool CanDeploy (CombineEntry entry)
		{
			Combine combine = entry as Combine;
			if ( combine == null ) return false;
			SolutionDeployer deployer = new SolutionDeployer ();
			return deployer.CanDeploy ( combine );
		}
		
		public DeployTarget CreateTarget (CombineEntry entry)
		{
			return new TarballDeployTarget (Id);
		}
	}
	
	public class TarballDeployTarget: DeployTarget
	{
		[ItemProperty ("TargetDirectory")]
		string targetDir;
		
		public TarballDeployTarget ()
		{
		}
		
		public TarballDeployTarget (string handlerId): base (handlerId)
		{
		}
		
		public override void Deploy (IProgressMonitor monitor, CombineEntry entry)
		{
			Combine combine = entry as Combine;
			SolutionDeployer deployer = new SolutionDeployer ();
			deployer.Deploy ( combine, targetDir, monitor );
		}
		
		public override void CopyFrom (DeployTarget other)
		{
			base.CopyFrom (other);
			targetDir = ((TarballDeployTarget)other).targetDir;
		}
		
		public string TargetDir {
			get { return targetDir; }
			set { targetDir = value; }
		}
	}
	
	public class TarballTargetEditor: IDeployTargetEditor
	{
		public bool CanEdit (DeployTarget target)
		{
			return target is TarballDeployTarget;
		}
		
		public Gtk.Widget CreateEditor (DeployTarget target)
		{
			return new TarballTargetEditorWidget ((TarballDeployTarget) target);
		}
	}
	
	public class TarballTargetEditorWidget: Gtk.HBox
	{
		public TarballTargetEditorWidget (TarballDeployTarget target)
		{
			Gtk.Label lab = new Gtk.Label ("Deploy directory:");
			PackStart (lab, false, false, 0);
			
			Gnome.FileEntry fe = new Gnome.FileEntry ("tarball-folders","Target Directory");
			fe.GtkEntry.Text = target.TargetDir;
			fe.Directory = true;
			fe.Modal = true;
			fe.UseFilechooser = true;
			fe.FilechooserAction = Gtk.FileChooserAction.SelectFolder;
			fe.GtkEntry.Changed += delegate (object s, EventArgs args) {
				target.TargetDir = fe.GtkEntry.Text;
			};
			
			PackStart (fe, true, true, 6);
			ShowAll ();
		}
	}
}
