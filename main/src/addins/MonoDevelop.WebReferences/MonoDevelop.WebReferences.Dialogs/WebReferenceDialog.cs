using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.WebBrowser;
using MonoDevelop.WebReferences;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.WebReferences.Dialogs
{
	
	internal partial class WebReferenceDialog : Gtk.Dialog
	{
		#region Widgets
		protected Widget browserWidget = null;
		protected IWebBrowser browser = null;
		#endregion
		
		Label docLabel;
		DotNetProject project;
		
		#region Properties
		/// <summary>Gets or Sets whether the current location of the browser is a valid web service or not.</summary>
		/// <value>True if the current location of the browser is a Web Service, otherwise false.</value>
		public bool IsWebService
		{
			get { return btnOK.Sensitive; }
			set 
			{
				// Clear out the Reference and Namespace Entry
				if (btnOK.Sensitive && !value)
				{
					this.tbxReferenceName.Text = "";
				}
				btnOK.Sensitive = value;
				tbxReferenceName.Sensitive = value;
			}
		}
		
		/// <summary>Gets or Sets the current url for web service</summary>
		/// <value>A string containing the url of the web service</value>
		public string ServiceUrl
		{
			get { return serviceUrl; }
			set { serviceUrl = value; }
		}
		
		/// <summary>Gets or Sets the namespace prefix for the web service</summary>
		/// <value>A string containing namespace prefix value for the web service</value>
		public string NamespacePrefix
		{
			get { return namespacePrefix; }
			set { namespacePrefix = value; }
		}
		
		/// <summary>Gets the default namespace for the web service based of the service url and namespace prefix</summary>
		/// <value>A string containing default namespace for the web service</value>
		public string DefaultNamespace
		{
			get { return namespacePrefix; }
		}
		
		/// <summary>Gets the default reference name for the web service based of the service url</summary>
		/// <value>A string containing default reference name for the web service</value>
		public string DefaultReferenceName
		{
			get
			{
				Uri discoveryUri = new Uri(this.ServiceUrl);
				if (discoveryUri != null)
					return MakeValidId (discoveryUri.Host);
				else
					return String.Empty;
			}
		}
		
		string MakeValidId (string name)
		{
			bool isWordStart = true;
			for (int n=0; n<name.Length; n++) {
				char c = name [n];
				if (char.IsNumber (c) && isWordStart) {
					if (n == 0)
						return "n" + name.Replace ('.','_');
					else
						return name.Replace ('.','_');
				}
				isWordStart = c == '.';
			}
			return name;
		}
		
		/// <summary>Gets the name for the web reference.</summary>
		/// <value>A string containing the name for the web reference.</value>
		public string ReferenceName
		{
			get { return this.tbxReferenceName.Text; }
		}
		
		/// <summary>Gets the namespace for the web reference.</summary>
		/// <value>A string containing the namespace for the web refrence.</value>
		public string Namespace
		{
			get { return this.tbxNamespace.Text; }
		}
		
		/// <summary>Gets the selected service discovery result.</summary>
		public WebServiceDiscoveryResult SelectedService
		{
			get { return selectedService; }
		}
		
		/// <summary>Gets or Sets the the base path of the where the web reference.</summary>
		/// <value>A string containing the base path where all the web references are stored in.</summary>
		public string BasePath
		{
			get { return basePath; }
			set { basePath = value; }
		}
		
		/// <summary>Gets the the base path for the current reference.</summary>
		/// <value>A string containing the base path for the current reference.</summary>
		public string ReferencePath
		{
			get { return System.IO.Path.Combine(BasePath, ReferenceName); }
		}
		#endregion
		
		#region Member Variables
		private string homeUrl = "http://www.w3schools.com/WebServices/TempConvert.asmx";
		private string serviceUrl = "";
		private string namespacePrefix = "";
		private WebServiceDiscoveryResult selectedService;
		private string basePath = "";
//		protected Gtk.Alignment frmBrowserAlign;
		#endregion
		
		/// <summary>Initializes a new instance of the AddWebReferenceDialog widget.</summary>
		public WebReferenceDialog (DotNetProject project)
		{
			Build();
			this.basePath = Library.GetWebReferencePath (project);
			this.IsWebService = false;
			this.project = project;
			
			// Add the mozilla control to the frame
			if (WebBrowserService.CanGetWebBrowser) {
				browser = WebBrowserService.GetWebBrowser ();
				browserWidget = (Widget) browser;
				browser.LocationChanged += Browser_LocationChanged;
				browser.NetStart += Browser_StartLoading;
				browser.NetStop += Browser_StopLoading;
				frmBrowser.Add(browserWidget);
				browser.LoadUrl(this.homeUrl);
				browserWidget.Show();
			} else {
				tlbNavigate.Visible = false;
				
				ScrolledWindow sw = new ScrolledWindow ();
				sw.ShadowType = ShadowType.In;
				docLabel = new Label ();
				docLabel.Xpad = 6;
				docLabel.Ypad = 6;
				docLabel.Xalign = 0;
				docLabel.Yalign = 0;
				sw.AddWithViewport (docLabel);
				sw.ShowAll ();
				
				frmBrowser.Add (sw);
				tbxReferenceURL.Text = homeUrl;
				UpdateLocation ();
			}

			frmBrowser.Show();
			this.ShowAll();
		}
		
		/// <summary>Execute the event when any of the buttons on the action panel has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
//		private void Browser_URLKeyReleased (object sender, EventArgs e)
//		{
//			if (sender.Equals(btnOK) && Directory.Exists(this.ReferencePath))
//			{
//				string message = GettextCatalog.GetString ("The reference name '{0}' already exists.", this.ReferenceName);
//				MessageService.ShowWarning(message);
//				return;	
//			}
//			else
//			{
//				Respond((sender.Equals(btnOK)) ? Gtk.ResponseType.Ok : Gtk.ResponseType.Cancel);
//				Destroy();
//			}
//		}
		
		/// <summary>Execute the event when the Browser Go button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		private void Browser_GoButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				browser.LoadUrl(tbxReferenceURL.Text);
			else
				UpdateLocation ();
		}
		
		/// <summary>Execute the event when the Enter key has been pressed on the Url Entry</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		private void Browser_URLKeyReleased (object sender, Gtk.KeyReleaseEventArgs e)
		{
			if (e.Event.Key == Gdk.Key.Return)
			{
				if (browser != null)
					browser.LoadUrl(tbxReferenceURL.Text);
			}
		}
		
		/// <summary>Execute the event when the Location of the Browser has changed</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		private void Browser_LocationChanged (object sender, EventArgs e)
		{
			if (browser != null) {
				this.tbxReferenceURL.Text = this.browser.Location;
				this.btnBack.Sensitive = browser.CanGoBack;
				this.btnNext.Sensitive = browser.CanGoForward;
				// Query the current url for services
				ThreadPool.QueueUserWorkItem(new WaitCallback(QueryService), this.tbxReferenceURL.Text);
			}
		}
		
		void UpdateLocation ()
		{
			ThreadPool.QueueUserWorkItem(new WaitCallback(QueryService), this.tbxReferenceURL.Text);
		}

		
		/// <summary>Execute when the browser starts loading a document</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		private void Browser_StartLoading (object sender, EventArgs e)
		{
			this.btnStop.Sensitive = true;
		}
		
		/// <summary>Execute the browser stop loading a document</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		private void Browser_StopLoading (object sender, EventArgs e)
		{
			this.btnStop.Sensitive = false;
		}
		
		/// <summary>Execute when the Back button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		private void Browser_BackButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				this.browser.GoBack();
		}
		
		/// <summary>Execute when the Next button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		private void Browser_NextButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				this.browser.GoForward();
		}
		
		/// <summary>Execute when the Refresh button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		private void Browser_RefreshButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				this.browser.Reload();
		}
		
		/// <summary>Execute when the Stop button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		private void Browser_StopButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				this.browser.StopLoad();
		}
		
		/// <summary>Execute when the Home button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		private void Browser_HomeButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				this.browser.LoadUrl(this.homeUrl);
		}
		
		/// <summary>Queries the web service to validate that the current url contains services</summary>
		/// <param name="param">An object that contains the parameter being passed from the ThreadPool.</param>
		private void QueryService (object param)
		{
			string url = param as string;
			// Set the service url
			lock (this)
			{
				if (serviceUrl == url) 
					return;
				serviceUrl = url; 
			}
			
			WebServiceEngine serviceEngine;
			if (comboModel.Active == 0)
				serviceEngine = WebReferencesService.WcfEngine;
			else
				serviceEngine = WebReferencesService.WsEngine;
			
			WebServiceDiscoveryResult service = null;
			
			// Checks the availablity of any services
			
			try {
				service = serviceEngine.Discover (url);
			} catch (Exception ex) {
				serviceUrl = null;
				this.IsWebService = false;
				this.selectedService = null;
				LoggingService.LogError ("Error while discovering web services", ex);
				ShowError (ex.Message);
				return;
			}
			
			Application.Invoke (delegate {
				UpdateService (service, url);
			});
		}
		
		void ShowError (string error)
		{
			Application.Invoke (delegate {
				if (docLabel != null) {
					docLabel.Text = error;
					docLabel.LineWrapMode = Pango.WrapMode.Word;
					docLabel.Wrap = true;
				}
			});
		}
		
		void UpdateService (WebServiceDiscoveryResult service, string url)
		{
			StringBuilder text = new StringBuilder ();
			
			if (service == null) {
				this.IsWebService = false;
				this.selectedService = null;
			}
			else {
				// Set the Default Namespace and Reference
				this.tbxNamespace.Text = this.DefaultNamespace;
				
				string name = this.DefaultReferenceName;
				
				var items = WebReferencesService.GetWebReferenceItems (project);
				if (items.Any (it => it.Name == name)) {
					int num = 2;
					while (items.Any (it => it.Name == name + "_" + num))
						num++;
					name = name + "_" + num;
				}
				this.tbxReferenceName.Text = name;
				
				this.IsWebService = true;
				this.selectedService = service;
				
				if (docLabel != null) {
					docLabel.Wrap = false;
					text.Append (service.GetDescriptionMarkup ());
				}
			}
			if (docLabel != null) {
				docLabel.Wrap = false;
				if (text.Length >= 0)
					docLabel.Markup = text.ToString ();
				else
					docLabel.Markup = GettextCatalog.GetString ("Web service not found.");
			}
			return;
		}
		
		protected virtual void OnBtnOKClicked (object sender, System.EventArgs e)
		{
			if (WebReferencesService.GetWebReferenceItems (project).Any (r => r.Name == this.tbxReferenceName.Text)) {
				MessageService.ShowError (GettextCatalog.GetString ("Web reference already exists"), GettextCatalog.GetString ("A web service reference with the name '{0}' already exists in the project. Please use a different name.", this.tbxReferenceName.Text));
				return;
			}
			Respond (Gtk.ResponseType.Ok);
		}
		
		protected virtual void OnComboModelChanged (object sender, System.EventArgs e)
		{
			serviceUrl = null;
			ThreadPool.QueueUserWorkItem(new WaitCallback(QueryService), this.tbxReferenceURL.Text);
		}
	}
	
	class AskCredentials: GuiSyncObject, ICredentials
	{
		static Dictionary<string,NetworkCredential> credentials = new Dictionary<string, NetworkCredential> ();
		
		Dictionary<string,NetworkCredential> tempCredentials = new Dictionary<string, NetworkCredential> ();
		
		public bool Canceled;
		
		public void Reset ()
		{
			tempCredentials.Clear ();
		}
		
		public void Store ()
		{
			foreach (var creds in tempCredentials)
				credentials [creds.Key] = creds.Value;
		}
		
		public NetworkCredential GetCredential (Uri uri, string authType)
		{
			NetworkCredential nc;
			if (tempCredentials.TryGetValue (uri.Host + uri.AbsolutePath, out nc))
				return nc; // Exact match
			
			UserPasswordDialog dlg = new UserPasswordDialog (uri.Host);
			if (tempCredentials.TryGetValue (uri.Host, out nc) || credentials.TryGetValue (uri.Host, out nc)) {
				dlg.User = nc.UserName;
				dlg.Password = nc.Password;
			}
			try {
				if (MessageService.ShowCustomDialog (dlg) == (int) ResponseType.Ok) {
					nc = new NetworkCredential (dlg.User, dlg.Password);
					tempCredentials [uri.Host + uri.AbsolutePath] = nc;
					tempCredentials [uri.Host] = nc;
					return nc;
				}
				else {
					Canceled = true;
					return null;
				}
			} finally {
				dlg.Destroy ();
			}
		}
	}
}
