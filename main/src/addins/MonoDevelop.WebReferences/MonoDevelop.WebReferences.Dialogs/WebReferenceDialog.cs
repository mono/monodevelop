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
using MonoDevelop.WebReferences.WS;
using MonoDevelop.WebReferences.WCF;

namespace MonoDevelop.WebReferences.Dialogs
{
	
	internal partial class WebReferenceDialog : Gtk.Dialog
	{
		#region Widgets
		protected Widget browserWidget = null;
		protected IWebBrowser browser = null;
		#endregion

		enum DialogState {
			Uninitialized,
			Create,
			Modify,
			CreateConfig,
			ModifyConfig
		}

		bool modified;
		bool isWebService;
		WCFConfigWidget wcfConfig;
		ClientOptions wcfOptions;
		DialogState state = DialogState.Uninitialized;
		Label docLabel;
		DotNetProject project;
		
		#region Properties
		/// <summary>Gets or Sets whether the current location of the browser is a valid web service or not.</summary>
		/// <value>True if the current location of the browser is a Web Service, otherwise false.</value>
		public bool IsWebService
		{
			get { return isWebService; }
			private set 
			{
				// Clear out the Reference and Namespace Entry
				if (isWebService && !value)
				{
					this.tbxReferenceName.Text = "";
				}
				isWebService = value;
				ChangeState (state);
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

		/// Whether or not the dialog has been modified
		public bool Modified {
			get { return modified; }
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
			this.isWebService = false;
			this.project = project;
			this.modified = true;

			tbxReferenceURL.Text = homeUrl;

			wcfOptions = WebReferencesService.WcfEngine.DefaultClientOptions;

			ChangeState (DialogState.Create);
			frmBrowser.Show ();
			this.Child.Show ();
		}

		public WebReferenceDialog (WebReferenceItem item, ClientOptions options)
		{
			Build ();
			this.isWebService = true;
			this.wcfOptions = options;
			this.namespacePrefix = item.Project.DefaultNamespace;

			ChangeState (DialogState.ModifyConfig);
			
			var service = item.Load ();
			var url = service.GetServiceURL ();

			if (service is WebServiceDiscoveryResultWCF)
				comboModel.Active = 0;
			else
				comboModel.Active = 1;

			UpdateService (service, url);

			tbxReferenceURL.Text = url;
			tbxReferenceName.Text = item.Name;
			tbxNamespace.Text = item.Project.DefaultNamespace;

			frmBrowser.Show ();
			this.Child.Show ();
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
			modified = true;
			switch (state) {
			case DialogState.Create:
			case DialogState.CreateConfig:
				ChangeState (DialogState.Create);
				break;

			case DialogState.Modify:
			case DialogState.ModifyConfig:
				ChangeState (DialogState.Modify);
				break;

			default:
				throw new InvalidOperationException ();
			}

			if (browser != null)
				browser.LoadUrl (tbxReferenceURL.Text);
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
				this.btnNavBack.Sensitive = browser.CanGoBack;
				this.btnNavNext.Sensitive = browser.CanGoForward;
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
			lock (this) {
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
			} else {
				// Set the Default Namespace and Reference
				this.tbxNamespace.Text = this.DefaultNamespace;
				
				if (project != null) {
					string name = this.DefaultReferenceName;
					
					var items = WebReferencesService.GetWebReferenceItems (project);
					if (items.Any (it => it.Name == name)) {
						int num = 2;
						while (items.Any (it => it.Name == name + "_" + num))
							num++;
						name = name + "_" + num;
					}
					this.tbxReferenceName.Text = name;
				}
				
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
			if (wcfConfig != null) {
				wcfConfig.Update ();
				modified |= wcfConfig.Modified;
			}
			
			if (project == null) {
				Respond (Gtk.ResponseType.Ok);
				return;
			}

			if (WebReferencesService.GetWebReferenceItems (project).Any (r => r.Name == this.tbxReferenceName.Text)) {
				MessageService.ShowError (GettextCatalog.GetString ("Web reference already exists"), GettextCatalog.GetString ("A web service reference with the name '{0}' already exists in the project. Please use a different name.", this.tbxReferenceName.Text));
				return;
			}
			
			var webref = project.Items.GetAll <WebReferenceUrl> ().FirstOrDefault (r => r.Include == ServiceUrl);
			if (webref != null) {
				MessageService.ShowError (GettextCatalog.GetString ("Web reference already exists"), GettextCatalog.GetString ("The web service '{0}' already exists with the name '{1}'", ServiceUrl, webref.RelPath.FileName));
				return;
			}

			Respond (Gtk.ResponseType.Ok);
		}
		
		protected virtual void OnComboModelChanged (object sender, System.EventArgs e)
		{
			serviceUrl = null;
			ThreadPool.QueueUserWorkItem(new WaitCallback(QueryService), this.tbxReferenceURL.Text);
		}

		protected void OnBtnConfigClicked (object sender, EventArgs e)
		{
			switch (state) {
			case DialogState.Create:
				ChangeState (DialogState.CreateConfig);
				break;
			case DialogState.Modify:
				ChangeState (DialogState.ModifyConfig);
				break;
			default:
				throw new InvalidOperationException ();
			}
		}

		void ChangeState (DialogState newState)
		{
			bool hasConfig = comboModel.Active == 0;

			switch (newState) {
			case DialogState.Create:
				btnBack.Visible = false;
				btnConfig.Visible = true;
				btnConfig.Sensitive = isWebService && hasConfig;
				btnOK.Visible = true;
				btnOK.Sensitive = isWebService;
				tlbNavigate.Visible = WebBrowserService.CanGetWebBrowser;
				tbxReferenceName.Sensitive = isWebService;
				comboModel.Sensitive = true;
				break;

			case DialogState.CreateConfig:
				btnBack.Visible = true;
				btnBack.Sensitive = true;
				btnConfig.Visible = false;
				btnOK.Visible = true;
				btnOK.Sensitive = true;
				tlbNavigate.Visible = false;
				tbxReferenceName.Sensitive = false;
				comboModel.Sensitive = false;
				break;

			case DialogState.Modify:
				btnBack.Visible = false;
				btnConfig.Visible = true;
				btnConfig.Sensitive = isWebService && hasConfig;
				btnOK.Visible = true;
				btnOK.Sensitive = isWebService;
				tlbNavigate.Visible = WebBrowserService.CanGetWebBrowser;
				tbxReferenceName.Sensitive = false;
				comboModel.Sensitive = false;
				break;

			case DialogState.ModifyConfig:
				btnBack.Visible = false;
				btnConfig.Visible = false;
				btnOK.Visible = true;
				btnOK.Sensitive = true;
				tlbNavigate.Visible = false;
				tbxReferenceName.Sensitive = false;
				comboModel.Sensitive = false;
				break;
				
			default:
				throw new InvalidOperationException ();
			}

			if (wcfConfig != null)
				wcfConfig.Update ();

			if (state == newState)
				return;

			if (state != DialogState.Uninitialized)
				frmBrowser.Forall (c => frmBrowser.Remove (c));

			browser = null;
			browserWidget = null;
			docLabel = null;
			wcfConfig = null;

			state = newState;
			
			ScrolledWindow sw;

			switch (state) {
			case DialogState.Create:
			case DialogState.Modify:
				if (WebBrowserService.CanGetWebBrowser) {
					browser = WebBrowserService.GetWebBrowser ();
					browserWidget = (Widget) browser;
					browser.LocationChanged += Browser_LocationChanged;
					browser.NetStart += Browser_StartLoading;
					browser.NetStop += Browser_StopLoading;
					frmBrowser.Add (browserWidget);
					browser.LoadUrl (tbxReferenceURL.Text);
					browserWidget.Show ();
				} else {
					docLabel = new Label ();
					docLabel.Xpad = 6;
					docLabel.Ypad = 6;
					docLabel.Xalign = 0;
					docLabel.Yalign = 0;

					sw = new ScrolledWindow ();
					sw.ShadowType = ShadowType.In;
					sw.AddWithViewport (docLabel);
					sw.ShowAll ();
					frmBrowser.Add (sw);
					UpdateLocation ();
				}
				break;

			case DialogState.ModifyConfig:
			case DialogState.CreateConfig:
				if (!hasConfig)
					return;

				sw = new ScrolledWindow ();
				sw.ShadowType = ShadowType.In;

				wcfConfig = new WCFConfigWidget (wcfOptions);
				sw.AddWithViewport (wcfConfig);
				sw.ShowAll ();
				frmBrowser.Add (sw);
				break;

			default:
				throw new InvalidOperationException ();
			}
		}

		protected void OnBtnBackClicked (object sender, EventArgs e)
		{
			switch (state) {
			case DialogState.CreateConfig:
				ChangeState (DialogState.Create);
				break;
			case DialogState.ModifyConfig:
				ChangeState (DialogState.Modify);
				break;
			default:
				throw new InvalidOperationException ();
			}
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
				if (MessageService.RunCustomDialog (dlg) == (int) ResponseType.Ok) {
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
