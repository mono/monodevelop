//
// AspNetEditViewContent.cs: The SecondaryViewContent that lets AspNetEdit 
//         be used as a designer in MD.
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
using System.IO;
using System.ComponentModel;
using Gtk;

using Mono.Addins;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.DesignerSupport;
using MonoDevelop.DesignerSupport.PropertyGrid;
using AspNetEdit.Editor;

namespace AspNetEdit.Integration
{
	
	public class AspNetEditViewContent : AbstractSecondaryViewContent, IToolboxConsumer //, IEditableTextBuffer
	{
		IViewContent viewContent;
		EditorProcess editorProcess;
		
		Gtk.Socket designerSocket;
		Gtk.Socket propGridSocket;
		
		Frame propertyFrame;
		DesignerFrame designerFrame;
		
		MonoDevelopProxy proxy;
		
		bool activated = false;
		bool suppressSerialisation = false;
		static string extensionError = null;
		
		internal AspNetEditViewContent (IViewContent viewContent)
		{
			this.viewContent = viewContent;
			
			designerFrame = new DesignerFrame (this);
			designerFrame.CanFocus = true;
			designerFrame.Shadow = ShadowType.None;
			designerFrame.BorderWidth = 0;
			
			propertyFrame = new Frame ();
			propertyFrame.CanFocus = true;
			propertyFrame.Shadow = ShadowType.None;
			propertyFrame.BorderWidth = 0;
			
			viewContent.WorkbenchWindow.Closing += workbenchWindowClosingHandler;
			viewContent.DirtyChanged += vcDirtyChanged;
			viewContent.BeforeSave += vcBeforeSave;
			
			designerFrame.Show ();
		}
		
		void workbenchWindowClosingHandler (object sender, WorkbenchWindowEventArgs args)
		{
			if (activated)
				suppressSerialisation = true;
		}
		
		void vcDirtyChanged (object sender, System.EventArgs e)
		{
			if (activated && !viewContent.IsDirty)
				viewContent.IsDirty = true;
		}
				
		void vcBeforeSave (object sender, System.EventArgs e)
		{
			if (activated)
				saveDocumentToTextView ();
		}
		
		public override Gtk.Widget Control {
			get { return designerFrame; }
		}
		
		public override string TabPageLabel {
			get { return "Designer"; }
		}
		
		bool disposed = false;
		
		public override void Dispose ()
		{
			if (disposed)
				return;
			
			disposed = true;
			
			base.WorkbenchWindow.Closing -= workbenchWindowClosingHandler;
			viewContent.DirtyChanged -= vcDirtyChanged;
			viewContent.BeforeSave -= vcBeforeSave;
			
			DestroyEditorAndSockets ();
			designerFrame.Destroy ();
			base.Dispose ();
		}
		
		public override void Selected ()
		{
			//check that the Mozilla extension is installed correctly, and if not, display an error
			if (extensionError != null) {
				return;
			} else if (!CheckExtension (ref extensionError)) {
				LoggingService.LogError (extensionError);
				Label errorlabel = new Label (extensionError);
				errorlabel.Wrap = true;
				
				HBox box = new HBox (false, 10);
				Image errorImage = new Image (Gtk.Stock.DialogError, Gtk.IconSize.Dialog);
				
				box.PackStart (new Label (), true, true, 0);
				box.PackStart (errorImage, false, false, 10);
				box.PackStart (errorlabel, true, false, 10);
				box.PackStart (new Label (), true, true, 0);
				
				designerFrame.Add (box);
				designerFrame.ShowAll ();
				return;
			} else {
				extensionError = null;
			}
			
			if (editorProcess != null)
				throw new Exception ("Editor should be null when document is selected");
			
			designerSocket = new Gtk.Socket ();
			designerSocket.Show ();
			designerFrame.Add (designerSocket);
			
			propGridSocket = new Gtk.Socket ();
			propGridSocket.Show ();
			propertyFrame.Add (propGridSocket);
			
			editorProcess = (EditorProcess) Runtime.ProcessService.CreateExternalProcessObject (typeof (EditorProcess), false);
			
			if (designerSocket.IsRealized)
				editorProcess.AttachDesigner (designerSocket.Id);
			if (propGridSocket.IsRealized)
				editorProcess.AttachPropertyGrid (propGridSocket.Id);
			
			designerSocket.Realized += delegate { editorProcess.AttachDesigner (designerSocket.Id); };
			propGridSocket.Realized += delegate { editorProcess.AttachPropertyGrid (propGridSocket.Id); };
			
			//designerSocket.FocusOutEvent += delegate {
			//	MonoDevelop.DesignerSupport.DesignerSupport.Service.PropertyPad.BlankPad (); };
			
			//hook up proxy for event binding
			string codeBehind = null;
			if (viewContent.Project != null) {
				string mimeType =
					MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeForUri (viewContent.ContentName);
				
				MonoDevelop.AspNet.Parser.AspNetParsedDocument cu = 
					MonoDevelop.Projects.Dom.Parser.ProjectDomService.Parse (
						null, viewContent.ContentName, mimeType)
					as MonoDevelop.AspNet.Parser.AspNetParsedDocument;
					
				if (cu != null && cu.PageInfo != null && !string.IsNullOrEmpty (cu.PageInfo.InheritedClass))
					codeBehind = cu.PageInfo.InheritedClass;
			}
			proxy = new MonoDevelopProxy (viewContent.Project, codeBehind);
			
			ITextBuffer textBuf = (ITextBuffer) viewContent.GetContent (typeof(ITextBuffer));			
			editorProcess.Initialise (proxy, textBuf.Text, viewContent.ContentName);
			
			activated = true;
			
			//FIXME: track 'dirtiness' properly
			viewContent.IsDirty = true;
		}
		
		public override void Deselected ()
		{
			activated = false;
			
			//don't need to save if window is closing
			if (!suppressSerialisation)
				saveDocumentToTextView ();
			
			DestroyEditorAndSockets ();
		}
			
		void saveDocumentToTextView ()
		{
			if (editorProcess != null && !editorProcess.ExceptionOccurred) {
				IEditableTextBuffer textBuf = (IEditableTextBuffer) viewContent.GetContent (typeof(IEditableTextBuffer));
				
				string doc = null;
				try {
					doc = editorProcess.Editor.GetDocument ();
				} catch (Exception e) {
					MonoDevelop.Core.Gui.MessageService.ShowException (e,
						AddinManager.CurrentLocalizer.GetString (
					        "The document could not be retrieved from the designer"));
				}
			
				if (doc != null)
					textBuf.Text = doc;
			}
		}
		
		void DestroyEditorAndSockets ()
		{
			if (proxy != null) {
				proxy.Dispose ();
				proxy = null;
			}
			
			if (editorProcess != null) {
				editorProcess.Dispose ();
				editorProcess = null;
			}
			
			if (propGridSocket != null) {
				propertyFrame.Remove (propGridSocket);
				propGridSocket.Dispose ();
				propGridSocket = null;
			}
			
			if (designerSocket != null) {
				designerFrame.Remove (designerSocket);
				designerSocket.Dispose ();
				designerSocket = null;
			}
		}
		
		#region IToolboxConsumer
		
		public void ConsumeItem (ItemToolboxNode node)
		{
			if (node is ToolboxItemToolboxNode)
				editorProcess.Editor.UseToolboxNode (node);
		}
		
		//used to filter toolbox items
		private static ToolboxItemFilterAttribute[] atts = new ToolboxItemFilterAttribute[] {
			new System.ComponentModel.ToolboxItemFilterAttribute ("System.Web.UI", ToolboxItemFilterType.Allow)
		};
			
		public ToolboxItemFilterAttribute[] ToolboxFilterAttributes {
			get { return atts; }
		}
		
		public System.Collections.Generic.IList<ItemToolboxNode> GetDynamicItems ()
		{
			return null;
		}
		
		//Used if ToolboxItemFilterAttribute demands ToolboxItemFilterType.Custom
		//If not expecting it, should just return false
		public bool CustomFilterSupports (ItemToolboxNode item)
		{
			return false;
		}
		
		public void DragItem (ItemToolboxNode item, Widget source, Gdk.DragContext ctx)
		{
		}
		
		public TargetEntry[] DragTargets {
			get { return null; }
		}
		
		string IToolboxConsumer.DefaultItemDomain {
			get { return null; }
		}

		#endregion IToolboxConsumer
		
		class DesignerFrame: Frame, ICustomPropertyPadProvider
		{
			AspNetEditViewContent view;
			
			public DesignerFrame (AspNetEditViewContent view)
			{
				this.view = view;
			}
			
			Gtk.Widget ICustomPropertyPadProvider.GetCustomPropertyWidget ()
			{
				return view.propertyFrame;
			}
			
			void ICustomPropertyPadProvider.DisposeCustomPropertyWidget ()
			{
			}
		}
		
		bool MozillaInstalled (ref string error)
		{
			string mozPath = System.Environment.GetEnvironmentVariable ("MOZILLA_FIVE_HOME");
			if (mozPath == null) {
				error = "MOZILLA_FIVE_HOME is not set.";
				return false;
			}
			
			string ffBrowserManifest = Path.Combine (Path.Combine (mozPath, "chrome"), "toolkit.manifest");
			if (!File.Exists (ffBrowserManifest)) {
				error = AddinManager.CurrentLocalizer.GetString (
				    "MOZILLA_FIVE_HOME does not appear to be pointing to a valid Mozilla runtime: \"{0}\".", mozPath);
				return false;
			}
			return true;
		}
		
		bool ExtensionInstalled (ref string error)
		{
			string mozPath = System.Environment.GetEnvironmentVariable ("MOZILLA_FIVE_HOME");
			string manifestLocation = Path.Combine (Path.Combine (mozPath, "chrome"), "aspdesigner.manifest");
			if (!System.IO.File.Exists (manifestLocation)) {
				error = AddinManager.CurrentLocalizer.GetString (
				    "The ASP.NET designer's Mozilla extension is not installed.");
				return false;
			} else {
				try {
					using (StreamReader reader = new StreamReader (manifestLocation)) {
						string line = reader.ReadLine ().Trim ();
						int startIndex = "content aspdesigner jar:".Length;
						int length = line.Length - "aspdesigner.jar!/content/aspdesigner/".Length - startIndex;
						string path = line.Substring (startIndex, length - 1);
						if (Path.GetFullPath (path) == Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location))
							return true;
					}
				} catch (System.UnauthorizedAccessException) {}
			}
			
			error = AddinManager.CurrentLocalizer.GetString (
			    "A Mozilla extension is installed for the ASP.NET designer, \n" +
			    "but it is either incorrectly installed or is not the correct version. \n" +
			    "It is only possible to have one version installed.");
			return false;
		}
		
		bool InstallExtension (string extensionStatus)
		{
			if (!MonoDevelop.Core.Gui.MessageService.Confirm (
			    AddinManager.CurrentLocalizer.GetString ("Mozilla extension installation"),
			    extensionStatus + "\n" + AddinManager.CurrentLocalizer.GetString ("Would you like to install it?"),
			    new MonoDevelop.Core.Gui.AlertButton (AddinManager.CurrentLocalizer.GetString ("Install extension"))))
				return false;
			
			string sourcePath = Path.GetTempFileName ();
			using (TextWriter writer = new StreamWriter (sourcePath)) {
				string jarfile = Path.Combine (Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location), "aspdesigner.jar");
				writer.WriteLine ("content aspdesigner jar:{0}!/content/aspdesigner/", jarfile);
				writer.WriteLine ("locale aspdesigner en-US jar:{0}!/locale/en-US/aspdesigner/", jarfile);
			}
			
			string mozPath = System.Environment.GetEnvironmentVariable ("MOZILLA_FIVE_HOME");
			string manifestLocation = Path.Combine (Path.Combine (mozPath, "chrome"), "aspdesigner.manifest");
			
			//string installCommand = String.Format ("\"sh -c \\\"cp '{0}' '{1}'; chmod a+r '{1}'\\\"\"", sourcePath, manifestLocation);
			string installCommand = String.Format ("\"install '{0}' '{1}'\"", sourcePath, manifestLocation);
			LoggingService.LogInfo ("Attempting to run root command: '{0}'", installCommand);
			ProcessWrapper process = null;
			try {
				try {
					process = Runtime.ProcessService.StartProcess ("xdg-su", "-c " + installCommand, null, null);
				} catch (System.ComponentModel.Win32Exception) {
					process = Runtime.ProcessService.StartProcess ("gnomesu", "-c " + installCommand, null, null);
				}
				//FIXME: this will hang the GTK thread until we the command completes
				process.WaitForOutput ();
				File.Delete (sourcePath);
				return (process != null && process.ExitCode == 0);
			} catch (Exception ex) {
				LoggingService.LogError ("Error installing ASP.NET designer Mozilla extension.", ex);
			}
			MonoDevelop.Core.Gui.MessageService.ShowError (
			    AddinManager.CurrentLocalizer.GetString ("Could not execute command as root. \n"+
			        "Please manually run the command \n{0}\nbefore continuing.", installCommand));
			File.Delete (sourcePath);
			return true;
		}
		
		bool CheckExtension (ref string error)
		{
			try {
				if (!MozillaInstalled (ref error))
					return false;
				if (!ExtensionInstalled (ref error))
					if (!InstallExtension (error))
						return false;
				if (ExtensionInstalled (ref error))
					return true;
			} catch (Exception ex) {
				error = AddinManager.CurrentLocalizer.GetString ("Unhandled error:\n{0}", ex.ToString ());
			}
			return false;
		}
	}
}
