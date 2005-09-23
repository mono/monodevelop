using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Drawing;

using Gtk;
using Gdk;

using MonoDevelop.Gui;
using MonoDevelop.Core.Services;

namespace MonoDevelop 
{
	public enum  DialogResult{
		Abort,
		Ignore,
		Continue
	}
	public delegate void ButtonHandler(ExceptionDialog eb, DialogResult dr);
	
	public class ExceptionDialog : Gtk.Window
	{
		private Gtk.Fixed fixedcontainer;
		private Gtk.Button continueButton;
		private Gtk.Button ignoreButton;
		private Gtk.Button abortButton;
		private Gtk.CheckButton copyErrorCheckButton;
		private Gtk.CheckButton includeSysInfoCheckButton;
		private Gtk.Label label;
		private Gtk.ScrolledWindow scrolledwindow;
		private Gtk.TextView exceptionTextView;
		private Gtk.Image image;
		private Exception exceptionThrown;
		private ButtonHandler buttonhandler;

		public ExceptionDialog (Exception e) : base ("Exception raised error")
		{
				this.exceptionThrown = e;
				InitializeComponent();
				this.exceptionTextView.Buffer.Text = e.ToString(); 
		}
	
		string getClipboardString()
		{
			Version v;
			string str = "";
			if (includeSysInfoCheckButton.Active) {
				str  = ".NET Version         : " + Environment.Version.ToString() + Environment.NewLine;
				str += "OS Version           : " + Environment.OSVersion.ToString() + Environment.NewLine;
				// str += "Boot Mode            : " + Information.BootMode + Environment.NewLine;
				str += "Working Set Memory   : " + (Environment.WorkingSet / 1024) + "kb" + Environment.NewLine + Environment.NewLine;
				v = Assembly.GetEntryAssembly().GetName().Version;
				str += "SharpDevelop Version : " + v.Major + "." + v.Minor + "." + v.Revision + "." + v.Build + Environment.NewLine;
			}
			
			str += "Exception thrown: " + Environment.NewLine;
			str += exceptionThrown.ToString();
			return str;
		}
			
		void CopyInfoToClipboard()
		{
			if (copyErrorCheckButton.Active) {
				Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", true)).SetText(getClipboardString()); // How does clipboard work?
			}
		}
	
		void AbortButton_Clicked(object obj, EventArgs ea)
		{
			CallButtonHandlers(DialogResult.Abort);
			Application.Quit();
		}
	
		void ContinueButton_Clicked(object sender, System.EventArgs e)
		{
			CopyInfoToClipboard();
			// open Mozilla via process.start to SharpDevelop bug reporting forum
			this.ResumeLayout();
			try {
				Process.Start("mozilla http://www.icsharpcode.net/OpenSource/SD/Forum/forum.asp?FORUM_ID=5");
			}
			catch (Exception ex) {}
			CallButtonHandlers(DialogResult.Continue);
		}
		
		void IgnoreButton_Clicked(object sender, System.EventArgs e)
		{
			CopyInfoToClipboard();
			this.ResumeLayout();
	
			CallButtonHandlers(DialogResult.Ignore);
		}
	
		void CallButtonHandlers(DialogResult dr) {
			if (this.buttonhandler != null)
				buttonhandler(this, dr);
		}
	
		public void InitializeComponent() {
			ResourceService resourceService = (ResourceService)ServiceManager.GetService(typeof(ResourceService));

			this.fixedcontainer = new Gtk.Fixed();
			this.continueButton = new Gtk.Button();
			this.ignoreButton = new Gtk.Button();
			this.abortButton = new Gtk.Button();
			this.copyErrorCheckButton = new Gtk.CheckButton();
			this.includeSysInfoCheckButton = new Gtk.CheckButton();
			this.label = new Gtk.Label();
			this.scrolledwindow = new Gtk.ScrolledWindow();
			this.exceptionTextView = new Gtk.TextView();

			try {
				this.image = new Gtk.Image(resourceService.GetBitmap("ErrorReport"));
			}
			catch (NullReferenceException ex) {
				this.image = new Gtk.Image();
				this.image.SetFromStock("gtk-dialog-error", Gtk.IconSize.Dialog);
			}
	
			this.SuspendLayout();
	
			//
			// continueButton
			//
			this.continueButton.Label = "Continue";
			this.continueButton.SetSizeRequest (112, 40);
			this.continueButton.Clicked += new System.EventHandler(this.ContinueButton_Clicked);
			//
			// abortButton
			//
			this.abortButton.Label = "Abort";
			this.abortButton.SetSizeRequest (112, 40);
			this.abortButton.Clicked += new System.EventHandler(this.AbortButton_Clicked);
			//
			// ignoreButton
			//
			this.ignoreButton.Label = "Ignore";
			this.ignoreButton.SetSizeRequest (112, 40);
			this.ignoreButton.Clicked += new System.EventHandler(this.IgnoreButton_Clicked);
			//
			// copyErrorCheckButton
			//
			this.copyErrorCheckButton.Label = "Copy error to clipboard";
			this.copyErrorCheckButton.SetSizeRequest (672, 24);
			//
			// includeSysInfoCheckButton
			//
			this.includeSysInfoCheckButton.Label = "Include system info (Mono version, O.S. version)";
			this.copyErrorCheckButton.SetSizeRequest (672, 24);
			//
			// label
			//
			this.label.LineWrap = true;
			this.label.Text = 	"An error has ocurred." + System.Environment.NewLine +
										"This may be due to a programming error." + System.Environment.NewLine +
										"Please, help us to make MonoDevelop a better program for everyone." + System.Environment.NewLine +
										"Thanks in advance for your help.";
			this.label.SetSizeRequest (480, 88);
			//
			// scrolledwindow
			//
			this.scrolledwindow.HscrollbarPolicy = Gtk.PolicyType.Automatic;
			this.scrolledwindow.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			this.scrolledwindow.AddWithViewport(this.exceptionTextView);
			this.scrolledwindow.SetSizeRequest (480, 256);
			//
			// exceptionTextView
			//
			this.exceptionTextView.Editable = false;
			//
			// image
			//
			this.image.SetSizeRequest (226, 466);
			//
			// fixedcontainer
			//
			this.fixedcontainer.SetSizeRequest (740, 483);
			this.fixedcontainer.Put(continueButton, 624, 432);
			this.fixedcontainer.Put(abortButton, 368, 432);
			this.fixedcontainer.Put(ignoreButton, 496, 432);
			this.fixedcontainer.Put(copyErrorCheckButton, 256, 360);
			this.fixedcontainer.Put(includeSysInfoCheckButton, 256, 392);
			this.fixedcontainer.Put(label, 256, 8);
			this.fixedcontainer.Put(scrolledwindow, 256, 96);
			this.fixedcontainer.Put(image, 8, 8);
			//
			// this
			//
			this.Resizable = false;
			this.WindowPosition = Gtk.WindowPosition.Center;
			this.Add(fixedcontainer);
		}
	
		public void AddButtonHandler(ButtonHandler bh) {
			this.buttonhandler += bh;
		}
	
		void SuspendLayout() {
			this.Modal = true;
			if (MonoDevelop.Gui.WorkbenchSingleton.Workbench != null) {
				this.TransientFor = (Gtk.Window) MonoDevelop.Gui.WorkbenchSingleton.Workbench;
			}
		}

		void ResumeLayout() {
			this.Modal = false;
		}
	
	}
}
