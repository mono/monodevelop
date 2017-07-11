//
// RemoteDesignerProcess.cs: Base class for designer in remote process.
//    Handles Gtk sockets and threads.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
			System.Diagnostics.Trace.WriteLine ("Designer GUI thread starting.");
			GLib.ExceptionManager.UnhandledException += OnUnhandledException;
			
			//want to restart application loop when it's killed by an exception,
			//but let it die if Application.Quit() is called
			bool keepRunning = true;
			while (keepRunning) {
				try {
					keepRunning = false;
					Application.Run ();
				} catch (Exception e) {
					keepRunning = true;
					exceptionOccurred = true;
					HandleError (e);
				}
				
				//put in a 0.5s wait to limit the restarting rate in case we get 
				//repeatedly reoccurring exceptions
				System.Threading.Thread.Sleep (500);
			}
			
			GLib.ExceptionManager.UnhandledException -= OnUnhandledException;
			System.Diagnostics.Trace.WriteLine ("Designer GUI thread ending.");
		}
		
		public bool ExecuteSuccessfully (EventHandler handler)
		{
			bool success = true;
			
			try {
				handler (null, EventArgs.Empty);
			} catch (Exception e) {
				exceptionOccurred = true;
				success = false;
				HandleError (e);
			}
			
			return success;
		}
		
		public bool ExceptionOccurred
		{
			get { return exceptionOccurred; }
		}
		
		public virtual void RecoverFromException ()
		{
			exceptionOccurred = false;
		}
		
		protected virtual void HandleError (Exception e)
		{
			MonoDevelop.Core.LoggingService.LogError ("An exception occurred in the designer GUI thread", e);
			
			string err = "<b><big>The designer has encountered a fatal error:</big></b>\n\n"
				+ System.Web.HttpUtility.HtmlEncode (e.ToString ());
			
			if (gtkThread != null 
			    && gtkThread.ManagedThreadId != System.Threading.Thread.CurrentThread.ManagedThreadId) {
				Gtk.Application.Invoke ((o, args) => {
					ShowText (err);
				});
			} else {
				ShowText (err);
			}
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
			Application.Invoke ( (o, args) => {
				designerPlug = AttachChildViaPlug (socket, designerFrame);
			});
		}
		
		public void AttachPropertyGrid (uint socket)
		{
			Application.Invoke ( (o, args) => {
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
		
		bool disposed = false;
		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			
			System.Diagnostics.Trace.WriteLine ("Trying to close designer GUI thread");
			Application.Invoke ( (o, args) => {
				Application.Quit ();
				DisposePhase2 ();
			});
		}
		
		void DisposePhase2 ()
		{
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
			System.Diagnostics.Trace.WriteLine ("Designer GUI thread cleaned up.");
		}
		
		void OnUnhandledException (UnhandledExceptionEventArgs args)
		{
			HandleError ((Exception)args.ExceptionObject);
		}
	}
}
	