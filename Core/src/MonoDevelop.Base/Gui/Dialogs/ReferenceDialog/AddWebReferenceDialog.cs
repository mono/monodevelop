// created on 10/11/2002 at 2:06 PM

using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Xml;
using System.Xml.Xsl;
using System.CodeDom.Compiler;
using System.Web.Services.Description;

using MonoDevelop.Gui;
using MonoDevelop.Core.Services;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui.BrowserDisplayBinding;

namespace MonoDevelop.Gui.Dialogs
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	internal class AddWebReferenceDialog //: System.Windows.Forms.Form
	{/*
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;		
		private AxWebBrowser axBrowser;
		private System.Windows.Forms.Button btnBack;
		private System.Windows.Forms.Button btnForward;
		private System.Windows.Forms.Button btnRefresh;
		private System.Windows.Forms.Button btnAbort;
		private System.Windows.Forms.Button btnGo;
		private System.Windows.Forms.ComboBox cboUrl;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnCancel;
		private Crownwood.Magic.Controls.TabControl tabControl1;
		private Crownwood.Magic.Controls.TabPage tabPage1;
		private Crownwood.Magic.Controls.TabPage tabPage2;
		private System.Windows.Forms.Label label1;
		
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.PictureBox pictureBox2;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.TreeView treeView1;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.ColumnHeader header1;
		private System.Windows.Forms.ColumnHeader header2;
		private System.Windows.Forms.ImageList imgList;
		private System.Windows.Forms.CheckBox checkBox1;
		
		
		private System.Windows.Forms.ToolTip tips;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private Project project = null;
		private ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));
		private FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
	
		private TempFileCollection tempFiles = new TempFileCollection();
	
		ArrayList referenceInformations = new ArrayList();
		public ArrayList ReferenceInformations 
		{
			get {																		
				return referenceInformations;
			}
		}

		public AddWebReferenceDialog(Project p)
		{			
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
			this.project = p;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
				
		private void InitializeComponent()
		{
			this.tips = new System.Windows.Forms.ToolTip();
			this.panel1 = new System.Windows.Forms.Panel();
			this.cboUrl = new System.Windows.Forms.ComboBox();
			this.btnGo = new System.Windows.Forms.Button();
			this.btnRefresh = new System.Windows.Forms.Button();
			this.btnBack = new System.Windows.Forms.Button();
			this.btnForward = new System.Windows.Forms.Button();
			this.btnAbort = new System.Windows.Forms.Button();
			this.panel2 = new System.Windows.Forms.Panel();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.axBrowser = new MonoDevelop.BrowserDisplayBinding.AxWebBrowser();			
			this.tabControl1 = new Crownwood.Magic.Controls.TabControl();
			this.tabPage1 = new Crownwood.Magic.Controls.TabPage();
			this.tabPage2 = new Crownwood.Magic.Controls.TabPage();
			this.label1 = new System.Windows.Forms.Label();
			this.panel3 = new System.Windows.Forms.Panel();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.listView1 = new System.Windows.Forms.ListView();
			this.imgList = new System.Windows.Forms.ImageList();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
						
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.panel3.SuspendLayout();			
			((System.ComponentModel.ISupportInitialize)(this.axBrowser)).BeginInit();
			this.SuspendLayout();
									
			// 
			// panel1
			// 
			this.panel1.Controls.AddRange(new System.Windows.Forms.Control[] {
																				 this.cboUrl,
																				 this.label1,																				 
																				 this.btnGo,
																				 this.btnRefresh,
																				 this.btnAbort,
																				 this.btnForward,
																				 this.btnBack});
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(486, 23);
			this.panel1.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.Dock = System.Windows.Forms.DockStyle.Left;
			this.label1.Location = new System.Drawing.Point(112, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(84, 23);
			this.label1.TabIndex = 5;
			this.label1.Text = "Address:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			
			//
			// cboUrl
			// 
			this.cboUrl.Dock = System.Windows.Forms.DockStyle.Fill;
			//this.cboUrl.Location = new System.Drawing.Point(103, 0);
			this.cboUrl.Name = "cboUrl";
			//this.cboUrl.Size = new System.Drawing.Size(346, 23);
			this.cboUrl.TabIndex = 4;
			// for testing only
			this.cboUrl.Text = "http://www.learnxmlws.com/services/weatherRetriever.asmx?WSDL";
			// 
			// btnGo
			// 
			this.btnGo.Dock = System.Windows.Forms.DockStyle.Right;
			this.btnGo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnGo.Location = new System.Drawing.Point(449, 0);
			this.btnGo.Image = this.resourceService.GetBitmap("Icons.16x16.SelectionArrow");
			this.btnGo.Name = "btnGo";
			this.btnGo.Size = new System.Drawing.Size(28, 23);
			this.btnGo.TabIndex = 3;
			this.btnGo.Text = String.Empty;
			this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
			this.tips.SetToolTip(btnGo, "GO");
			// 
			// btnRefresh
			// 
			this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Left;
			this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnRefresh.Image = this.resourceService.GetBitmap("Icons.16x16.BrowserRefresh");
			this.btnRefresh.Location = new System.Drawing.Point(75, 0);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new System.Drawing.Size(28, 23);
			this.btnRefresh.TabIndex = 1;
			this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
			this.tips.SetToolTip(btnRefresh, "Refresh");
			// 
			// btnBack
			// 
			this.btnBack.Dock = System.Windows.Forms.DockStyle.Left;
			this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnBack.Image = this.resourceService.GetBitmap("Icons.16x16.BrowserBefore");
			this.btnBack.Name = "btnBack";
			this.btnBack.Size = new System.Drawing.Size(28,23);
			this.btnBack.TabIndex = 0;
			this.btnBack.Text = String.Empty;
			this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
			this.tips.SetToolTip(btnBack, "Back");
			// 
			// btnForward
			// 
			this.btnForward.Dock = System.Windows.Forms.DockStyle.Left;
			this.btnForward.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnForward.Image = this.resourceService.GetBitmap("Icons.16x16.BrowserAfter");
			this.btnForward.Name = "btnForward";
			this.btnForward.Size = new System.Drawing.Size(28,23);
			this.btnForward.TabIndex = 0;
			this.btnForward.Text = String.Empty;
			this.btnForward.Click += new System.EventHandler(this.btnForward_Click);
			this.tips.SetToolTip(btnForward, "Forward");
			// 
			// btnAbort
			// 
			this.btnAbort.Dock = System.Windows.Forms.DockStyle.Left;
			this.btnAbort.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnAbort.Image = this.resourceService.GetBitmap("Icons.16x16.BrowserCancel");
			this.btnAbort.Name = "btnAbort";
			this.btnAbort.Size = new System.Drawing.Size(28,23);
			this.btnAbort.TabIndex = 0;
			this.btnAbort.Text = String.Empty;
			this.btnAbort.Click += new System.EventHandler(this.btnAbort_Click);
			this.tips.SetToolTip(btnAbort, "Stop");
			// 
			// panel2
			// 
			this.panel2.Controls.AddRange(new System.Windows.Forms.Control[] {
																				 this.checkBox1,
																				 this.btnCancel,
																				 this.btnAdd});
			this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel2.Location = new System.Drawing.Point(0, 303);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(486, 39);
			this.panel2.TabIndex = 2;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnCancel.Location = new System.Drawing.Point(404, 10);
			this.btnCancel.FlatStyle = FlatStyle.Flat;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnAdd
			// 
			this.btnAdd.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnAdd.Enabled = false;
			this.btnAdd.FlatStyle = FlatStyle.Flat;
			this.btnAdd.Location = new System.Drawing.Point(280, 10);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(120, 23);
			this.btnAdd.TabIndex = 0;
			this.btnAdd.Text = "Add Web Reference";
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			this.btnAdd.DialogResult = DialogResult.OK;
			// 
			// axBrowser
			// 
			this.axBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.axBrowser.Enabled = true;
			this.axBrowser.Location = new System.Drawing.Point(0, 23);
			//this.axBrowser.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axBrowser.OcxState")));
			this.axBrowser.Size = new System.Drawing.Size(336, 280);
			this.axBrowser.TabIndex = 3;
			this.axBrowser.NavigateComplete2 += new DWebBrowserEvents2_NavigateComplete2EventHandler(this.axBrowser_NavigateComplete2);
			this.axBrowser.NavigateError += new DWebBrowserEvents2_NavigateErrorEventHandler(this.axBrowser_NavigateError);			
			this.axBrowser.BeforeNavigate2 += new DWebBrowserEvents2_BeforeNavigate2EventHandler(this.axBrowser_BeforeNavigate2);
			
			// 
			// tabControl1
			// 
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.HideTabsMode = Crownwood.Magic.Controls.TabControl.HideTabsModes.ShowAlways;
			this.tabControl1.Location = new System.Drawing.Point(0, 23);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.SelectedTab = this.tabPage1;
			this.tabControl1.Size = new System.Drawing.Size(486, 280);
			this.tabControl1.TabIndex = 4;
			this.tabControl1.TabPages.AddRange(new Crownwood.Magic.Controls.TabPage[] {
																						  this.tabPage1,
																						  this.tabPage2});
			
			// 
			// panel3
			// 
			this.panel3.Controls.AddRange(new System.Windows.Forms.Control[] {
																				 this.pictureBox2,
																				 this.pictureBox1});
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(759, 88);
			this.panel3.TabIndex = 2;
			// 
			// pictureBox2
			// 
			this.pictureBox2.BackgroundImage = resourceService.GetBitmap("GradientTop");
			this.pictureBox2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBox2.Location = new System.Drawing.Point(226, 0);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(533, 88);
			this.pictureBox2.TabIndex = 3;
			this.pictureBox2.TabStop = false;
			// 
			// pictureBox1
			// 
			this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Left;
			this.pictureBox1.Image = resourceService.GetBitmap("WebReferencesDialog");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(226, 88);
			this.pictureBox1.TabIndex = 2;
			this.pictureBox1.TabStop = false;
			//
			// imgList
			//
			this.imgList.Images.Add(resourceService.GetBitmap("Icons.16x16.Assembly"));
			this.imgList.Images.Add(resourceService.GetBitmap("Icons.16x16.Assembly"));
			this.imgList.Images.Add(resourceService.GetBitmap("Icons.16x16.Class"));
			this.imgList.Images.Add(resourceService.GetBitmap("Icons.16x16.Class"));
			this.imgList.Images.Add(resourceService.GetBitmap("Icons.16x16.Interface"));
			this.imgList.Images.Add(resourceService.GetBitmap("Icons.16x16.Interface"));
			this.imgList.Images.Add(resourceService.GetBitmap("Icons.16x16.Library"));
			
			//
			// treeView1
			// 						
			this.treeView1.LabelEdit = false;			
			this.treeView1.ImageList = this.imgList;
			this.treeView1.ShowRootLines = false;
			this.treeView1.HotTracking = true;
			this.treeView1.Cursor = Cursors.Hand;
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Left;
			this.treeView1.ImageIndex = -1;
			this.treeView1.Location = new System.Drawing.Point(0, 88);
			this.treeView1.Name = "treeView1";
			this.treeView1.SelectedImageIndex = -1;
			this.treeView1.Size = new System.Drawing.Size(280, 185);
			this.treeView1.TabIndex = 3;
			this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
			
			// 
			// splitter1
			// 
			this.splitter1.Dock = System.Windows.Forms.DockStyle.Left;
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(3, 180);
			this.splitter1.TabIndex = 4;
			this.splitter1.TabStop = false;
			// 
			// listView1
			// 
			this.header1 = new ColumnHeader();
			header1.Text = "Property";
			this.header2 = new ColumnHeader();
			header2.Text = "Value";
			
			this.listView1.Columns.Add(header1);
			this.listView1.Columns.Add(header2);
			this.listView1.View = View.Details;
			
			this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listView1.Location = new System.Drawing.Point(124, 88);
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(635, 180);
			this.listView1.TabIndex = 5;
			this.listView1.Resize += new System.EventHandler(this.listView1_Resize);
			//
			// tabPage1
			// 						
			this.tabPage1.BackColor = System.Drawing.Color.White;
			this.tabPage1.Controls.AddRange(new System.Windows.Forms.Control[] {
																				   this.listView1,
																				   this.splitter1,
																				   this.treeView1});
			this.tabPage1.Icon = resourceService.GetIcon("Icons.16x16.Class");
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Size = new System.Drawing.Size(486, 255);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Title = "Available Web Services";
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.AddRange(new System.Windows.Forms.Control[] {
																				   this.axBrowser});
			this.tabPage2.Icon = resourceService.GetIcon("Icons.16x16.HTMLIcon");
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Selected = false;
			this.tabPage2.Size = new System.Drawing.Size(486, 255);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Title = "WSDL";
			
			// 
			// checkBox1 for debugging only
			// 
			this.checkBox1.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
			this.checkBox1.Location = new System.Drawing.Point(6, 9);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.TabIndex = 2;
			this.checkBox1.Text = "Generate Assembly";
			this.checkBox1.Enabled = false;
			this.checkBox1.Visible = false;
			
			// 
			// AddWebReferenceDialog
			// 
			this.Icon = resourceService.GetIcon("Icons.16x16.ClosedWebReferenceFolder");			
			this.AcceptButton = this.btnGo;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(722, 467);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {																		  
																		  this.tabControl1,
																		  this.panel2,
																		  this.panel1,
																		  this.panel3});
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.MaximizeBox = false;
			this.MinimizeBox = false;			
			this.Name = "AddWebReferenceDialog";
			this.ShowInTaskbar = false;
			this.Text = "Add Web Reference";
			this.panel1.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.axBrowser)).EndInit();
			this.ResumeLayout(false);

		}
		
		private void btnBack_Click(object sender, System.EventArgs e)
		{
			axBrowser.GoBack();
		}
		
		private void btnForward_Click(object sender, System.EventArgs e)
		{
			axBrowser.GoForward();
		}
		
		private void btnAbort_Click(object sender, System.EventArgs e)
		{
			axBrowser.Stop();
			this.btnAdd.Enabled = false;
		}

		private void btnRefresh_Click(object sender, System.EventArgs e)
		{
			axBrowser.Refresh();
			cboUrl.Text = axBrowser.LocationURL;
		}

		private void btnGo_Click(object sender, System.EventArgs e)
		{							
			object url = (object)cboUrl.Text;
			object flags = null;
			object targetframename = (object)"_top";
			object headers = null;
			object postdata = null;
			
			axBrowser.Navigate2(ref url, ref flags, ref targetframename, ref postdata, ref headers);						
		}

		ServiceDescription serviceDescription = null;
		ServiceDescription ServiceDescription {
			get {
				return serviceDescription;				
			}
			set {				
				this.btnAdd.Enabled = (value != null);
				serviceDescription = value;								
			}
		}
		private void btnAdd_Click(object sender, System.EventArgs e)
		{							
			if(!this.checkBox1.Checked) {
				ArrayList fileList = WebReference.GenerateWebProxyCode(project, cboUrl.Text);
				if(fileList != null) {
					this.referenceInformations.AddRange(fileList);					
				}
			} else {
				ProjectReference webRef = WebReference.GenerateWebProxyDLL(project, cboUrl.Text);
				if(webRef != null) {
					this.referenceInformations.Add(webRef);
				}
			}			
			this.Close();
		}

		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void axBrowser_NavigateComplete2(object sender, DWebBrowserEvents2_NavigateComplete2Event e)
		{
			this.treeView1.BeginUpdate();
			
			//TODO: maybe do some validation on the WSDL url here
			ServiceDescription desc = WebReference.ReadServiceDescription(axBrowser.LocationURL);
			this.treeView1.Nodes.Clear();
			if(desc == null) 
				return;
			
			TreeNode rootNode = new TreeNode(WebReference.GetNamespaceFromUri(axBrowser.LocationURL));			
			rootNode.Tag = desc;
			rootNode.ImageIndex = 6;
			rootNode.SelectedImageIndex = 6;
			this.treeView1.Nodes.Add(rootNode);
			
			foreach(Service svc in desc.Services) 
			{
				// add a Service node
				TreeNode serviceNode = new TreeNode(svc.Name);				
				serviceNode.Tag = svc;
				serviceNode.ImageIndex = 0;
				serviceNode.SelectedImageIndex = 1;
				rootNode.Nodes.Add(serviceNode);
				
				foreach(Port port in svc.Ports) 
				{					
					TreeNode portNode = new TreeNode(port.Name);
					portNode.Tag = port;
					portNode.ImageIndex = 2;
					portNode.SelectedImageIndex = 3;
					serviceNode.Nodes.Add(portNode);
					
										
					// get the operations
					foreach(Operation operation in desc.PortTypes[port.Name].Operations) 
					{
						TreeNode operationNode = new TreeNode(operation.Name);
						operationNode.Tag = operation;
						operationNode.ImageIndex = 4;
						operationNode.SelectedImageIndex = 5;
						portNode.Nodes.Add(operationNode);
					}				
				}															
			}
			this.treeView1.ExpandAll();	
			this.treeView1.EndUpdate();
			
			this.ServiceDescription = desc;			
			this.Cursor = Cursors.Default;			
		}
		private void treeView1_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{						
			this.listView1.BeginUpdate();
						
			ListViewItem item;
			
			this.listView1.Items.Clear();
						
			if(e.Node.Tag is ServiceDescription) 
			{
				ServiceDescription desc = (ServiceDescription)e.Node.Tag;
				item = new ListViewItem();				
				item.Text = "Retrieval URI";
				item.SubItems.Add(desc.RetrievalUrl);
				this.listView1.Items.Add(item);
			}
			else if(e.Node.Tag is Service)
			{
				Service service = (Service)e.Node.Tag;
				item = new ListViewItem();				
				item.Text = "Documentation";
				item.SubItems.Add(service.Documentation);
				this.listView1.Items.Add(item);

			}
			else if(e.Node.Tag is Port) 
			{
				Port port = (Port)e.Node.Tag;

				item = new ListViewItem();				
				item.Text = "Documentation";
				item.SubItems.Add(port.Documentation);
				this.listView1.Items.Add(item);
				
				item = new ListViewItem();				
				item.Text = "Binding";
				item.SubItems.Add(port.Binding.Name);
				this.listView1.Items.Add(item);
				
				item = new ListViewItem();				
				item.Text = "Service";
				item.SubItems.Add(port.Service.Name);												
				this.listView1.Items.Add(item);

			}
			else if(e.Node.Tag is Operation) 
			{
				Operation operation = (Operation)e.Node.Tag;
				
				item = new ListViewItem();
				item.Text = "Documentation";
				item.SubItems.Add(operation.Documentation);
				this.listView1.Items.Add(item);

				item = new ListViewItem();
				item.Text = "Parameters";
				item.SubItems.Add(operation.ParameterOrderString);
				this.listView1.Items.Add(item);
			}
			
			this.listView1.EndUpdate();		
		}
		private void listView1_Resize(object sender, System.EventArgs e)
		{
			// resize the column headers
			this.header1.Width = this.listView1.Width / 2;
			this.header2.Width = this.listView1.Width / 2;
		}
		
		private void axBrowser_BeforeNavigate2(object sender, DWebBrowserEvents2_BeforeNavigate2Event e)
		{
			this.Cursor = Cursors.WaitCursor;
			this.ServiceDescription = null;
		}

		private void axBrowser_NavigateError(object sender, DWebBrowserEvents2_NavigateErrorEvent e)
		{
			this.Cursor = Cursors.Default;
			this.treeView1.Nodes.Clear();
			this.ServiceDescription = null;
		}
		*/
	}
}
