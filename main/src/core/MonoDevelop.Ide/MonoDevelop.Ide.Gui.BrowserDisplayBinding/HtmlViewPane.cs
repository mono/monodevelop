//  HtmlViewPane.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using Gtk;

using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.WebBrowser;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.BrowserDisplayBinding
{
	public class BrowserPane : AbstractViewContent, ISecondaryViewContent
	{	
		protected HtmlViewPane htmlViewPane;
		protected IViewContent parent;

		public void Selected ()
		{	
		}

		public void Deselected ()
		{
		}

		public void NotifyBeforeSave ()
		{
		}

		public override string TabPageLabel
		{
			get {
				if (parent != null)
					return GettextCatalog.GetString ("Preview");
				else
					return GettextCatalog.GetString ("Web Browser");
			}
		}
		
		public void BaseContentChanged ()
		{
			try {
				ITextBuffer buffer = (ITextBuffer) parent.GetContent (typeof(ITextBuffer));
				htmlViewPane.IWebBrowser.LoadHtml (buffer.Text);
			} catch (Exception e) {
				LoggingService.LogError ("Exception in BrowserPane.BaseContentChanged", e);
			}
		}

		public override Widget Control {
			get {
				return htmlViewPane;
			}
		}
		
		public override bool IsDirty {
			get {
				return false;
			}
			set {
			}
		}
		
		public override bool IsViewOnly {
			get {
				return true;
			}
		}
		
		public BrowserPane (bool showNavigation, IViewContent parent) : this (showNavigation)
		{
			this.parent = parent;
			
			//suppress in-window hyperlinking, but only when this used as is an 
			//ISecondaryViewContent, i.e, it has a parent
			htmlViewPane.IWebBrowser.LocationChanging += CatchUri;
		}

		public BrowserPane (bool showNavigation)
		{
			htmlViewPane = new HtmlViewPane(showNavigation);
			htmlViewPane.IWebBrowser.TitleChanged += OnTitleChanged;
		}
		
		void CatchUri (object sender, LocationChangingEventArgs e)
		{
			e.SuppressChange = true;
			IdeApp.Services.PlatformService.ShowUrl (e.NextLocation);
		}
		
		public BrowserPane () : this (true)
		{
		}
		
		public override void Dispose()
		{
			htmlViewPane.Destroy();
		}
		
		public override void Load(string url)
		{
			htmlViewPane.Navigate(url);
		}
		
		public override void Save(string url)
		{
			Load(url);
		}
		
		private void OnTitleChanged (object o, TitleChangedEventArgs args)
		{
			ContentName = args.Title;; 
		}
	}
	
	public class HtmlViewPane : Gtk.Frame
	{
		IWebBrowser htmlControl = null;
		
		VBox topPanel = new VBox (false, 2);
		Navbar nav = new Navbar ();
		
		bool loading = false;
		
		public IWebBrowser IWebBrowser {
			get {
				return htmlControl;
			}
		}
		
		public HtmlViewPane (bool showNavigation)
		{
			Shadow = Gtk.ShadowType.In;
			VBox mainbox = new VBox (false, 2);
			
			if (showNavigation) {
				
				nav.Back += new EventHandler (OnBackClicked);
				nav.Forward += new EventHandler (OnForwardClicked);
				nav.Stop += new EventHandler (OnStopClicked);
				nav.Reload += new EventHandler (OnRefreshClicked);
				nav.Go += new EventHandler (OnEntryActivated);
				
				topPanel.PackStart (nav);
				mainbox.PackStart (topPanel, false, false, 2);
			} 
			
			htmlControl = WebBrowserService.GetWebBrowser ();
			Widget htmlControlWidget = (Widget) htmlControl;
			htmlControl.NetStart += new EventHandler (OnNetStart);
			htmlControl.NetStop += new EventHandler (OnNetStop);
			htmlControl.LocationChanged += new LocationChangedHandler (OnLocationChanged);
			htmlControlWidget.ShowAll ();
			
			mainbox.PackStart (htmlControlWidget);
			this.Add (mainbox);
			this.ShowAll ();
		}
		
		void OnEntryActivated (object o, EventArgs args)
		{
			htmlControl.LoadUrl (nav.Url);
		}
		
		public void CreatedWebBrowserHandle(object sender, EventArgs evArgs) 
		{
		}
		
		public void Navigate(string name)
		{
			nav.Url = name;
			htmlControl.LoadUrl (name);
		}

		private void OnNetStart (object o, EventArgs args)
		{
			IdeApp.Workbench.StatusBar.ShowMessage (GettextCatalog.GetString ("Loading..."));
			loading = true;
			GLib.Idle.Add (new GLib.IdleHandler (Pulse));
		}

		bool Pulse ()
		{
			if (loading) {
				IdeApp.Workbench.StatusBar.Pulse ();
				System.Threading.Thread.Sleep (100);
				return true;
			}
			IdeApp.Workbench.StatusBar.EndProgress ();
			IdeApp.Workbench.StatusBar.ShowMessage (GettextCatalog.GetString ("Done."));
			return false;
		}

		private void OnNetStop (object o, EventArgs args)
		{
			loading = false;
		}

		void OnLocationChanged (object o, EventArgs args)
		{
			nav.Url = htmlControl.Location;
		}

		private void OnBackClicked (object o, EventArgs args)
		{
			htmlControl.GoBack ();
		}
		
		private void OnForwardClicked (object o, EventArgs args)
		{
			htmlControl.GoForward ();
		}
		
		private void OnStopClicked (object o, EventArgs args)
		{
			htmlControl.StopLoad ();
		}
		
		private void OnRefreshClicked (object o, EventArgs args)
		{
			htmlControl.Reload ();
		}
	}
}
