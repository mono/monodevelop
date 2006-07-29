
using System;
using Gtk;

using MonoDevelop.Core.Execution;
using MonoDevelop.Core;

namespace MonoDevelop.DesignerSupport
{
	
	[AddinDependency ("MonoDevelop.DesignerSupport")]
	public abstract class RemoteDesignerProcess : RemoteProcessObject
	{
		Frame designerFrame;
		Frame propGridFrame;
		Plug designerPlug;
		Plug propGridPlug;
		System.Threading.Thread gtkThread = null;
		bool exceptionOccurred = false;
		
		public RemoteDesignerProcess ()
		{
			Application.Init ();
			
			designerFrame = new Gtk.Frame ();
			propGridFrame = new Gtk.Frame ();
			designerFrame.Shadow = ShadowType.None;
			propGridFrame.Shadow = ShadowType.None;
			designerFrame.BorderWidth = 0;
			
			designerFrame.Show ();
			propGridFrame.Show ();
		}
		
		#region threading, error handling
		
		//we need another thread for GTK, or this one will lock up the remoting channel and freeze MD
		protected void StartGuiThread ()
		{
			if (gtkThread == null) {
				gtkThread = new System.Threading.Thread (GuiThread);
				gtkThread.Start ();
			}
		}
		
		void GuiThread ()
		{
			while (true) {
				try {
					Application.Run ();
				} catch (Exception e) {
					exceptionOccurred = true;
					HandleError (e);
				}
			}
		}
		
		public bool ExceptionOcurred
		{
			get { return exceptionOccurred; }
		}
		
		public virtual void RecoverFromException ()
		{
			exceptionOccurred = false;
		}
		
		protected virtual void HandleError (Exception e)
		{
			ShowText ("<b><big>The designer has encountered a fatal error:</big></b>\n\n" + System.Web.HttpUtility.HtmlEncode (e.ToString ()));
		}
		
		protected void ShowText (string markup)
		{
			Label label= new Label ();
			label.Markup = markup;
			
			Frame padFrame = new Gtk.Frame ();
			padFrame.Add (label);
			padFrame.BorderWidth = 10;
			padFrame.Shadow = ShadowType.None;
			
			ScrolledWindow scrollW = new ScrolledWindow ();
			scrollW.AddWithViewport (padFrame);
			scrollW.BorderWidth = 0;
			scrollW.ShadowType = ShadowType.None;
			scrollW.ShowAll ();
			DesignerWidget = scrollW;
		}
		
		#endregion
		
		protected Widget PropertyGridWidget {
			get {
				return propGridFrame.Child;
			}
			set {
				if (propGridFrame.Child != null)
					propGridFrame.Remove (propGridFrame.Child);
				if (value != null) {
					propGridFrame.Add (value);
					propGridFrame.Child.Show ();
				}
				
			}
		}
		
		protected Widget DesignerWidget {
			get {
				return designerFrame.Child;
			}
			set {
				if (designerFrame.Child != null)
					designerFrame.Remove (designerFrame.Child);
				if (value != null) {
					designerFrame.Add (value);
					designerFrame.ShowAll ();
				}
			}
		}
		
		#region plugs/sockets
		
		public void AttachDesigner (uint socket)
		{
			Application.Invoke ( delegate {
				designerPlug = AttachChildViaPlug (socket, designerFrame);
			});
		}
		
		public void AttachPropertyGrid (uint socket)
		{
			Application.Invoke ( delegate {
				propGridPlug = AttachChildViaPlug (socket, propGridFrame);
			});
		}
		
		private Plug AttachChildViaPlug (uint socket, Gtk.Widget child)
		{
			System.Diagnostics.Trace.WriteLine ("Attaching plug to socket " + socket);
			Plug plug = new Plug (socket);
			
			//if our plugs are unrealised and reattached too quickly, parent may not have removed the child yet
			if(child.Parent != null)
				((Container) child.Parent).Remove (child);
			plug.Add (child);
			
			plug.Unrealized += RemovePlugChildWhenUnrealised;
			plug.Show ();
			return plug;
		}
		
		private void RemovePlugChildWhenUnrealised (object sender, EventArgs e)
		{
			Plug plug = (Gtk.Plug) sender;
			System.Diagnostics.Trace.WriteLine ("Plug unrealised; removing child before it gets destroyed");
			if (plug.Child != null)
				plug.Remove (plug.Child);
		}
		
		#endregion plugs
		
		public override void Dispose ()
		{
			//kill the GUI thread
			Application.Invoke ( delegate {
				Application.Quit ();
			});
			
			//clean up widgets
			if (designerPlug != null)
				designerPlug.Dispose ();
			if (propGridPlug != null)
				propGridPlug.Dispose ();
			if (designerFrame != null)
				designerFrame.Dispose ();
			if (propGridFrame != null)
				propGridFrame.Dispose ();
			
			base.Dispose ();
		}
	}
}
