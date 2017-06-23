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
	
	internal partial class WebReferenceDialog : Dialog
	{
		#region Widgets
		protected Widget browserWidget;
		protected IWebBrowser browser;
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
		readonly ClientOptions wcfOptions;
		DialogState state = DialogState.Uninitialized;
		Label docLabel;
		readonly DotNetProject project;
		readonly DotNetProject wcfProject;
		
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
					tbxReferenceName.Text = "";
				}
				isWebService = value;
				ChangeState (state);
			}
		}
		
		/// <summary>Gets or Sets the current url for web service</summary>
		/// <value>A string containing the url of the web service</value>
		public string ServiceUrl {
			get;
			set;
		}
		
		/// <summary>Gets or Sets the namespace prefix for the web service</summary>
		/// <value>A string containing namespace prefix value for the web service</value>
		public string NamespacePrefix {
			get;
			set;
		}
		
		/// <summary>Gets the default namespace for the web service based of the service url and namespace prefix</summary>
		/// <value>A string containing default namespace for the web service</value>
		public string DefaultNamespace
		{
			get { return NamespacePrefix; }
		}
		
		/// <summary>Gets the default reference name for the web service based of the service url</summary>
		/// <value>A string containing default reference name for the web service</value>
		public string DefaultReferenceName
		{
			get
			{
				var discoveryUri = new Uri (ServiceUrl);
				return discoveryUri != null ? MakeValidId (discoveryUri.Host) : String.Empty;
			}
		}
		
		static string MakeValidId (string name)
		{
			bool isWordStart = true;
			name = name.Replace ('-', '_'); 
			for (int n=0; n<name.Length; n++) {
				char c = name [n];
				if (char.IsNumber (c) && isWordStart) {
					if (n == 0)
						return "n" + name.Replace ('.','_');
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
			get { return tbxReferenceName.Text; }
		}
		
		/// <summary>Gets the namespace for the web reference.</summary>
		/// <value>A string containing the namespace for the web refrence.</value>
		public string Namespace
		{
			get { return tbxNamespace.Text; }
		}
		
		/// <summary>Gets the selected service discovery result.</summary>
		public WebServiceDiscoveryResult SelectedService
		{
			get { return selectedService; }
		}
		
		/// <summary>Gets or Sets the the base path of the where the web reference.</summary>
		/// <value>A string containing the base path where all the web references are stored in.</value>
		public string BasePath {
			get;
			set;
		}
		
		/// <summary>Gets the the base path for the current reference.</summary>
		/// <value>A string containing the base path for the current reference.</value>
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
		const string homeUrl = "http://www.w3schools.com/xml/tempconvert.asmx";
		WebServiceDiscoveryResult selectedService;
//		protected Gtk.Alignment frmBrowserAlign;
		#endregion
		
		/// <summary>Initializes a new instance of the AddWebReferenceDialog widget.</summary>
		public WebReferenceDialog (DotNetProject project)
		{
			Build ();
			this.BasePath = Library.GetWebReferencePath (project);
			this.isWebService = false;
			this.project = project;
			wcfProject = project;
			this.modified = true;
			this.NamespacePrefix = String.Empty;
			ServiceUrl = String.Empty;

			tbxReferenceURL.Text = homeUrl;

			wcfOptions = WebReferencesService.WcfEngine.DefaultClientOptions;
			if (project.IsPortableLibrary) {
				wcfOptions.GenerateAsynchronousMethods = false;
				wcfOptions.GenerateEventBasedAsynchronousMethods = true;
			}

			ChangeState (DialogState.Create);
			frmBrowser.Show ();
			this.Child.Show ();
		}

		public WebReferenceDialog (WebReferenceItem item, ClientOptions options)
		{
			Build ();
			this.isWebService = true;
			this.wcfOptions = options;
			wcfProject = item.Project;
			this.NamespacePrefix = item.Project.DefaultNamespace;
			ServiceUrl = String.Empty;

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

		void Browser_GoButtonClicked (object sender, EventArgs e)
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
		void Browser_URLKeyReleased (object sender, KeyReleaseEventArgs e)
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
		void Browser_LocationChanged (object sender, EventArgs e)
		{
			if (browser != null) {
				tbxReferenceURL.Text = browser.Location;
				btnNavBack.Sensitive = browser.CanGoBack;
				btnNavNext.Sensitive = browser.CanGoForward;
				// Query the current url for services
				ThreadPool.QueueUserWorkItem(new WaitCallback(QueryService), tbxReferenceURL.Text);
			}
		}
		
		void UpdateLocation ()
		{
			ThreadPool.QueueUserWorkItem(new WaitCallback(QueryService), tbxReferenceURL.Text);
		}

		
		/// <summary>Execute when the browser starts loading a document</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		void Browser_StartLoading (object sender, EventArgs e)
		{
			btnStop.Sensitive = true;
		}
		
		/// <summary>Execute the browser stop loading a document</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		void Browser_StopLoading (object sender, EventArgs e)
		{
			btnStop.Sensitive = false;
		}
		
		/// <summary>Execute when the Back button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		void Browser_BackButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				browser.GoBack();
		}
		
		/// <summary>Execute when the Next button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		void Browser_NextButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				browser.GoForward();
		}
		
		/// <summary>Execute when the Refresh button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		void Browser_RefreshButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				browser.Reload();
		}
		
		/// <summary>Execute when the Stop button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		void Browser_StopButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				browser.StopLoad();
		}
		
		/// <summary>Execute when the Home button has been clicked</summary>
		/// <param name="sender">An object that contains the sender data.</param>
		/// <param name="e">An EventArgs object that contains the event data.</param>
		void Browser_HomeButtonClicked (object sender, EventArgs e)
		{
			if (browser != null)
				browser.LoadUrl(homeUrl);
		}
		
		readonly object queryLock = new object ();
		/// <summary>Queries the web service to validate that the current url contains services</summary>
		/// <param name="param">An object that contains the parameter being passed from the ThreadPool.</param>
		void QueryService (object param)
		{
			string url = param as string;
			// Set the service url
			lock (queryLock) {
				if (ServiceUrl == url)
					return;
				ServiceUrl = url; 
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
				ServiceUrl = null;
				IsWebService = false;
				selectedService = null;
				LoggingService.LogError ("Error while discovering web services", ex);
				ShowError (ex.Message);
				return;
			}
			
			Application.Invoke ((o, args) => {
				UpdateService (service, url);
			});
		}
		
		void ShowError (string error)
		{
			Application.Invoke ((o, args) => {
				if (docLabel != null) {
					docLabel.Text = error;
					docLabel.LineWrapMode = Pango.WrapMode.Word;
					docLabel.Wrap = true;
				}
			});
		}
		
		void UpdateService (WebServiceDiscoveryResult service, string url)
		{
			var text = new StringBuilder ();
			
			if (service == null) {
				IsWebService = false;
				selectedService = null;
			} else {
				// Set the Default Namespace and Reference
				tbxNamespace.Text = DefaultNamespace;
				
				if (project != null) {
					string name = DefaultReferenceName;
					
					var items = WebReferencesService.GetWebReferenceItems (project);
					if (items.Any (it => it.Name == name)) {
						int num = 2;
						while (items.Any (it => it.Name == name + "_" + num))
							num++;
						name = name + "_" + num;
					}
					tbxReferenceName.Text = name;
				}
				
				IsWebService = true;
				selectedService = service;

				if (docLabel != null) {
					docLabel.Wrap = false;
					text.Append (service.GetDescriptionMarkup ());
				}
			}
			if (docLabel != null) {
				docLabel.Wrap = false;
				docLabel.Markup = text.Length >= 0 ? text.ToString () : GettextCatalog.GetString ("Web service not found.");
			}
			return;
		}
		
		protected virtual void OnBtnOKClicked (object sender, EventArgs e)
		{
			if (wcfConfig != null) {
				wcfConfig.Update ();
				modified |= wcfConfig.Modified;
			}
			
			if (project == null) {
				Respond (ResponseType.Ok);
				return;
			}

			if (WebReferencesService.GetWebReferenceItems (project).Any (r => r.Name == tbxReferenceName.Text)) {
				MessageService.ShowError (GettextCatalog.GetString ("Web reference already exists"), GettextCatalog.GetString ("A web service reference with the name '{0}' already exists in the project. Please use a different name.", tbxReferenceName.Text));
				return;
			}
			
			var webref = project.Items.GetAll <WebReferenceUrl> ().FirstOrDefault (r => r.Include == ServiceUrl);
			if (webref != null) {
				MessageService.ShowError (GettextCatalog.GetString ("Web reference already exists"), GettextCatalog.GetString ("The web service '{0}' already exists with the name '{1}'", ServiceUrl, webref.RelPath.FileName));
				return;
			}

			Respond (ResponseType.Ok);
		}
		
		protected virtual void OnComboModelChanged (object sender, EventArgs e)
		{
			ServiceUrl = null;
			ThreadPool.QueueUserWorkItem(new WaitCallback(QueryService), tbxReferenceURL.Text);
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
				comboModel.Sensitive = !project.IsPortableLibrary;
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
				frmBrowser.Forall (frmBrowser.Remove);

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

				wcfConfig = new WCFConfigWidget (wcfOptions, wcfProject);
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


		protected override void OnDestroyed ()
		{
			btnNavBack.Activated -= Browser_BackButtonClicked;
			btnNavNext.Activated -= Browser_NextButtonClicked;
			btnRefresh.Activated -= Browser_RefreshButtonClicked;
			btnStop.Activated -= Browser_StopButtonClicked;
			btnHome.Activated -= Browser_HomeButtonClicked;

			base.OnDestroyed ();
		}
	}
	
	class AskCredentials: GuiSyncObject, ICredentials
	{
		static readonly Dictionary<string,NetworkCredential> credentials = new Dictionary<string, NetworkCredential> ();
		
		readonly Dictionary<string,NetworkCredential> tempCredentials = new Dictionary<string, NetworkCredential> ();
		
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
			
			var dlg = new UserPasswordDialog (uri.Host);
			if (tempCredentials.TryGetValue (uri.Host, out nc) || credentials.TryGetValue (uri.Host, out nc)) {
				dlg.User = nc.UserName;
				dlg.Password = nc.Password;
			}
			try {
				if (MessageService.RunCustomDialog (dlg) == (int)ResponseType.Ok) {
					nc = new NetworkCredential (dlg.User, dlg.Password);
					tempCredentials [uri.Host + uri.AbsolutePath] = nc;
					tempCredentials [uri.Host] = nc;
					return nc;
				}
				Canceled = true;
				return null;
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}
	}
}
