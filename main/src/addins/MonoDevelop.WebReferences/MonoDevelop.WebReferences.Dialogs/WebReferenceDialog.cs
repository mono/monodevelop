using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.WebBrowser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.WebReferences;
using Gtk;

namespace MonoDevelop.WebReferences.Dialogs
{
	
	internal partial class WebReferenceDialog : Gtk.Dialog
	{
		#region Widgets
		protected Widget browserWidget = null;
		protected IWebBrowser browser = null;
		#endregion
		
		Label docLabel;
		
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
					return discoveryUri.Host;
				else
					return String.Empty;
			}
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
		
		/// <summary>Gets the selected service discovery client protocol.</summary>
		/// <value>A DiscoveryClientProtocol containing the web reference information.</value>
		public DiscoveryClientProtocol SelectedService
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
		private DiscoveryClientProtocol selectedService;
		private string basePath = "";
		protected Gtk.Alignment frmBrowserAlign;
		#endregion
		
		/// <summary>Initializes a new instance of the AddWebReferenceDialog widget.</summary>
		public WebReferenceDialog(string basePath)
		{
			Build();
			this.basePath = basePath;
			this.IsWebService = false;
			
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
				btnNext.Sensitive = false;
				btnBack.Sensitive = false;
				btnRefresh.Sensitive = false;
				btnStop.Sensitive = false;
				btnHome.Sensitive = false;
				
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
		private void Browser_URLKeyReleased (object sender, EventArgs e)
		{
			if (sender.Equals(btnOK) && Directory.Exists(this.ReferencePath))
			{
				string message = GettextCatalog.GetString ("The reference name '{0}' already exists.", this.ReferenceName);
				MessageService.ShowWarning(message);
				return;	
			}
			else
			{
				Respond((sender.Equals(btnOK)) ? Gtk.ResponseType.Ok : Gtk.ResponseType.Cancel);
				Destroy();
			}
		}
		
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
			
			// Checks the availablity of any services
			DiscoveryClientProtocol protocol = new DiscoveryClientProtocol ();
			try
			{
				protocol.DiscoverAny (url);
			}
			catch (Exception)
			{
				protocol = null;
			}
			
			Application.Invoke (delegate {
				UpdateService (protocol, url);
			});
		}
		
		void UpdateService (DiscoveryClientProtocol protocol, string url)
		{
			StringBuilder text = new StringBuilder ();
			
			if (protocol == null) {
				this.IsWebService = false;
				this.selectedService = null;
			}
			else {
				// Set the Default Namespace and Reference
				this.tbxNamespace.Text = this.DefaultNamespace;
				this.tbxReferenceName.Text = this.DefaultReferenceName;
				this.IsWebService = true;
				this.selectedService = protocol;
				
				if (docLabel != null) {
					foreach (object dd in protocol.Documents.Values) {
						if (dd is ServiceDescription) {
							Library.GenerateWsdlXml (text, protocol);
							break;
						}
						else if (dd is DiscoveryDocument) {
							Library.GenerateDiscoXml(text, (DiscoveryDocument)dd);
							break;
						}
					}
				}
			}
			if (docLabel != null) {
				if (text.Length >= 0)
					docLabel.Markup = text.ToString ();
				else
					docLabel.Markup = GettextCatalog.GetString ("Web service not found.");
			}
			return;
		}
	}
	
}
