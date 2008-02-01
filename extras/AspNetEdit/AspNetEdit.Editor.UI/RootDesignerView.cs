 /* 
 * RootDesignerView.cs - The Gecko# design surface returned by the WebForms Root Designer.
 * 			
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using AspNetEdit.JSCall;
using System.ComponentModel.Design;
using System.ComponentModel;
using System.Text;
using AspNetEdit.Editor.ComponentModel;
using System.Web.UI;
using System.Collections;
using Gtk;

namespace AspNetEdit.Editor.UI
{
	public class RootDesignerView : AspNetEdit.Integration.GeckoWebBrowser
	{
		private const string geckoChrome = "chrome://aspdesigner/content/"; 
		private CommandManager comm;
		private DesignerHost host;
		private IComponentChangeService changeService;
		private ISelectionService selectionService;
		private IMenuCommandService menuService;
		protected bool active = false;
		private string outDocument = null;
		
		
		//there's weird bug where a second Gecko instance *can't* be created
		//so until it's fixed we reuse share one instance
		//TODO: make it so we can have more than one shown at the same time
		public static RootDesignerView instance = null;
		
		public static RootDesignerView GetInstance (IDesignerHost host)
		{
			if (instance == null)
				instance = new RootDesignerView (host);
			instance.active = false;
			return instance;
		}

		private RootDesignerView (IDesignerHost host)
			: base()
		{
			//it's through this that we communicate with JavaScript
			comm = new CommandManager (this);

			//we use the host to get services and designers
			this.host =  host as DesignerHost;
			if (this.host == null)
				throw new ArgumentNullException ("host");

			//We use this to monitor component changes and update as necessary
			changeService = host.GetService (typeof (IComponentChangeService)) as IComponentChangeService;
			if (changeService == null)
				throw new Exception ("Could not obtain IComponentChangeService from host");

			//We use this to monitor and set selections
			selectionService = host.GetService (typeof (ISelectionService)) as ISelectionService;
			if (selectionService == null)
				throw new Exception ("Could not obtain ISelectionService from host");

			//This is used to add undo/redo, cut/paste etc commands to menu
			//Also to launch right-click menu
			menuService = host.GetService (typeof (IMenuCommandService)) as IMenuCommandService;
			//if (menuService == null)
			//	return;

			//Now we've got all services, register our events
			changeService.ComponentChanged += new ComponentChangedEventHandler (changeService_ComponentChanged);
			selectionService.SelectionChanged += new EventHandler (selectionService_SelectionChanged);
	
			//Register incoming calls from JavaScript
			comm.RegisterJSHandler ("Click", new ClrCall (JSClick));
			comm.RegisterJSHandler ("Activate", new ClrCall (JSActivate));
			comm.RegisterJSHandler ("ThrowException", new ClrCall (JSException));
			comm.RegisterJSHandler ("DebugStatement", new ClrCall (JSDebugStatement));
			comm.RegisterJSHandler ("ResizeControl", new ClrCall (JSResize));
			comm.RegisterJSHandler ("DocumentReturn", new ClrCall (JSDocumentReturn));
			comm.RegisterJSHandler ("RemoveControl", new ClrCall (JSRemoveControl));
			comm.RegisterJSHandler ("DeserializeAndAdd", new ClrCall (JSDeserializeAndAdd));
			comm.RegisterJSHandler ("Serialize", new ClrCall (JSSerialize));
			System.Diagnostics.Trace.WriteLine ("RootDesignerView created");
		}
		
		internal void BeginLoad ()
		{
			System.Diagnostics.Trace.WriteLine ("Loading XUL...");
			base.LoadUrl (geckoChrome);
		}
		
		public override void Destroy ()
		{
			System.Diagnostics.Trace.WriteLine ("RootDesignerView internally destroyed.");
			active = false;
			base.Destroy ();
		}

		#region Change service handlers

		void selectionService_SelectionChanged (object sender, EventArgs e)
		{
			if (!active) return;
			
			//deselect all
			comm.JSCall (GeckoFunctions.SelectControl, null, string.Empty);
			if (selectionService.SelectionCount == 0) return;
			
			ICollection selections = selectionService.GetSelectedComponents ();		
			
			foreach (IComponent comp in selections) {
				if (comp is WebFormPage) continue;
				Control control = comp as Control;
				if (control == null)
					throw new InvalidOperationException ("One of the selected components is not a System.Web.UI.Control.");
				//select the control
				comm.JSCall (GeckoFunctions.SelectControl, null, control.UniqueID);
			}
		}

		void changeService_ComponentChanged (object sender, ComponentChangedEventArgs e)
		{
			if (!active) return;
			UpdateRender (e.Component as Control);			
		}
		
		public void UpdateRender (Control control)
		{
			if (control == null)
				throw new InvalidOperationException ("The updated component is not a System.UI.WebControl");
			
			string ctext = Document.RenderDesignerControl (control);
			comm.JSCall (GeckoFunctions.UpdateControl, null, control.UniqueID, ctext);
		}
		
		#endregion
		
		#region document modification accessors for AspNetEdit.Editor.ComponentModel.Document
		
		internal void InsertFragment (string fragment)
		{
			System.Diagnostics.Trace.WriteLine ("Inserting document fragment: " + fragment);
			comm.JSCall (GeckoFunctions.InsertFragment, null, host.RootDocument.Serialize (fragment));
		}
		
		internal void AddControl (Control control)
		{
			if (!active) return;
			
			string ctext = Document.RenderDesignerControl (control);
			comm.JSCall (GeckoFunctions.AddControl, null, control.UniqueID, ctext);
		}

		internal void RemoveControl (Control control)
		{
			if (!active) return;
			
			comm.JSCall (GeckoFunctions.RemoveControl, null, control.UniqueID);
		}
		
		internal void RenameControl (string oldName, string newName)
		{
			throw new NotImplementedException ("Renaming controls not supported yet");
		}
		
		internal new string GetDocument ()
		{
			comm.JSCall (GeckoFunctions.GetPage, "DocumentReturn", null);
			
			int counter = 0;
			do {
				//only allow JS 20 seconds to return value
				if (counter > 200) throw new Exception ("Mozilla did not return value during 20 seconds");
				
				System.Threading.Thread.Sleep (100);
				counter++;
			}
			while (outDocument == null);
			System.Diagnostics.Trace.WriteLine ("Retrieved document from Gecko in ~" + (100*counter).ToString () + "ms.");		
			System.Diagnostics.Trace.WriteLine ("Document: " + outDocument);	
			
			string d = outDocument;
			outDocument = null;
			return d;
		}
		
		internal void DoCommand (string editorCommand)
		{
			System.Diagnostics.Trace.WriteLine ( "Executing command \"" + editorCommand +"\"");
			comm.JSCall (GeckoFunctions.DoCommand, null, editorCommand);
		}
		
		#endregion

		#region Inbound Gecko functions
		
				
		///<summary>
		/// Name:	DocumentReturn
		///			Callback function for when host initiates document save
		/// Arguments:
		///		string document:	the document text, with placeholder'd controls
		/// Returns:	none
		///</summary>
		private string JSDocumentReturn (string[] args)
		{
			if (args.Length != 1)
				throw new InvalidJSArgumentException ("DocumentReturn", -1);
			outDocument = args [0];
			return string.Empty;
		}
		
		//this is because of the Gecko# not wanting to give up its DomDocument until it's been shown.
		///<summary>
		/// Name:	Activate
		///			Called when the XUL document is all loaded and ready to recieve ASP.NET document
		/// Arguments:	none
		/// Returns:	none
		///</summary>
		private string JSActivate (string[] args)
		{
			if (active) {
				System.Diagnostics.Trace.WriteLine ("HELP! XUL reports having been initialised again! Suppressing, but need to be fixed.");
				return string.Empty;
			}
			
			System.Diagnostics.Trace.WriteLine ("XUL loaded.");
			//load document with filled-in design-time HTML
			string doc = host.RootDocument.GetLoadedDocument ();
			comm.JSCall (GeckoFunctions.LoadPage, null, doc);
			active = true;
			return string.Empty;
		}

		///<summary>
		/// Name:	Click
		///			Called when the document is clicked
		/// Arguments:
		///		enum ClickType: The button used to click (Single|Double|Right)
		///		string Component:	The unique ID if a Control, else empty
		/// Returns:	none
		///</summary>
		private string JSClick (string[] args)
		{
			if (args.Length != 2)
				throw new InvalidJSArgumentException ("Click", -1);
			
			//look up our component
			IComponent[] components = null;
			if (args[1].Length != 0)
				components = new IComponent[] {((DesignContainer) host.Container).GetComponent (args[1])};

			//decide which action to perfom and use services to perfom it
			switch (args[0]) {
				case "Single":
					selectionService.SetSelectedComponents (components);
					break;
				case "Double":
					//TODO: what happen when we double-click on the page?
					if (args[1].Length == 0) break;
					
					IDesigner designer = host.GetDesigner (components[0]);

					if (designer != null)
						designer.DoDefaultAction ();
					break;
				case "Right":
					//TODO: show context menu menuService.ShowContextMenu
					break;
				default:
					throw new InvalidJSArgumentException("Click", 0);
			}

			return string.Empty;
		}
		
		///<summary>
		/// Name:	ThrowException
		///			Throws managed exceptions on behalf of Javascript
		/// Arguments:
		///		string location:	some description of where the error occurred
		///		string message:		the exception's message
		/// Returns:	none
		///</summary>
		private string JSException (string[] args)
		{
			if (args.Length != 2)
				throw new InvalidJSArgumentException ("ThrowException", -1);
			
			throw new Exception (string.Format ("Error in javascript at {0}:\n{1}", args[0], args[1]));
		}

		///<summary>
		/// Name:	DebugStatement
		///			Writes to the console on behalf of Javascript
		/// Arguments:
		///		string message:	the debug message
		/// Returns:	none
		///</summary>
		private string JSDebugStatement (string[] args)
		{
			if (args.Length != 1)
				throw new InvalidJSArgumentException ("ThrowException", -1);
			
			System.Diagnostics.Trace.WriteLine ("Javascript: " + args[0]);
			return string.Empty;
		}
		
		///<summary>
		/// Name:	ResizeControl
		///			Writes to the console on behalf of Javascript
		/// Arguments:
		///		string id:	the control's ID
		///		string width:	the control's width
		///		string height:	the control's height
		/// Returns:	none
		///</summary>
		private string JSResize (string[] args)
		{
			if (args.Length != 3)
				throw new InvalidJSArgumentException ("ResizeControl", -1);
				
			//look up our component
			IComponent component = ((DesignContainer) host.Container).GetComponent (args[0]);
			System.Web.UI.WebControls.WebControl wc = component as System.Web.UI.WebControls.WebControl;
			if (wc == null)
				throw new InvalidJSArgumentException ("ResizeControl", 0);
			
			PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties (wc);
			PropertyDescriptor pdc_h = pdc.Find("Height", false);
			PropertyDescriptor pdc_w = pdc.Find("Width", false);
			
			//set the values
			pdc_w.SetValue (wc, pdc_w.Converter.ConvertFromInvariantString(args[1]));
			pdc_h.SetValue (wc, pdc_h.Converter.ConvertFromInvariantString(args[2]));
			
			System.Diagnostics.Trace.WriteLine (
				String.Format ("Javascript requesting size change to w:{0} h:{1} for control {2}.", args[1], args[2], args[0]));

			return string.Empty;
		}
		
		///<summary>
		/// Name:	RemoveControl
		///			Removes a control from the host when its Gecko representation is removed
		/// Arguments:
		///		string id:	the control's ID
		/// Returns:	none
		///</summary>
		private string JSRemoveControl (string[] args)
		{
			if (args.Length != 1)
				throw new InvalidJSArgumentException ("RemoveControl", -1);
				
			//look up our component
			DesignContainer container = (DesignContainer) host.Container;
			IComponent component = container.GetComponent (args[0]);
			if (component == null)
				throw new InvalidJSArgumentException ("RemoveControl", 0);
			
			//and remove it
			System.Diagnostics.Trace.WriteLine ("Removing control: " + args[0]);
			container.Remove (component);
			component.Dispose ();
			
			return string.Empty;
		}
		
		///<summary>
		/// Name:	Serialize
		///			Serialises a fragment of a Gecko document into ASP.NET code
		/// Arguments:
		///		string designerDocumentFragment:	the Gecko document fragment
		/// Returns:	the serialised document
		///</summary>
		private string JSSerialize (string[] args)
		{
			if (args.Length != 1)
				throw new InvalidJSArgumentException ("Serialize", -1);
						
			return host.RootDocument.Serialize (args [0]);
		}
		
		///<summary>
		/// Name:	DeserializeAndAdd
		///			Handles any ASP.NET code that gets pasted into the designer.
		/// Arguments:
		///		string designerDocumentFragment:	the ASP.NET document fragment
		/// Returns: none
		///</summary>
		private string JSDeserializeAndAdd (string[] args)
		{
			if (args.Length != 1)
				throw new InvalidJSArgumentException ("DeserializeAndAdd", -1);
						
			host.RootDocument.InsertFragment (args [0]);
			return string.Empty;
		}

		#endregion
		
		#region Outbound Gecko functions
		
		private class GeckoFunctions
		{
			///<summary>
			/// Add a control to the document
			/// Args:
			/// 	string id:		the unique ID of the control.
			/// 	string content:	The HTML content of the control
			/// Returns: none
			///</summary>
			public static readonly string AddControl = "JSCall_AddControl";
			
			///<summary>
			/// Updates the design-time HTML of a control to the document
			/// Args:
			/// 	string id:		the unique ID of the control.
			/// 	string content:	The HTML content of the control
			/// Returns: none
			///</summary>
			public static readonly string UpdateControl = "JSCall_UpdateControl";

			///<summary>
			/// Removes a control from the document
			/// Args:
			/// 	string id:		the unique ID of the control.
			/// Returns: none
			///</summary>
			public static readonly string RemoveControl = "JSCall_RemoveControl";
			
			///<summary>
			/// Selects a control
			/// Args:
			/// 	string id:		the unique ID of the control, or empty to clear selection.
			/// Returns: none
			///</summary>
			public static readonly string SelectControl = "JSCall_SelectControl";
			
			///<summary>
			/// Replaces the currently loaded document
			/// Args:
			/// 	string document:	the document text, with placeholder'd controls.
			/// Returns: none
			///</summary>
			public static readonly string LoadPage = "JSCall_LoadPage";
			
			///<summary>
			/// Replaces the currently loaded document
			/// Args: none
			/// Returns: none
			///</summary>
			public static readonly string GetPage = "JSCall_GetPage";
			
			///<summary>
			/// Passes a simple command to Gecko
			/// Args:
			///		string command:		Use the enum EditorCommand
			/// Returns: none
			///</summary>
			public static readonly string DoCommand = "JSCall_DoCommand";
			
			///<summary>
			/// Inserts a document fragment. Host should have deserialised it.
			/// Args:
			///		string fragment:		The document fragment
			/// Returns: none
			///</summary>
			public static readonly string InsertFragment = "JSCall_InsertFragment";
		}
		
		
		
		#endregion
	}
	
	//TODO: GetCommandState to check whether we can perform these commands
	
	//commands for DoCommand
	//simply triggers functionality in Mozilla editor
	public class EditorCommand
	{
		//clipboard
		public static readonly string Cut = "cmd_cut";
		public static readonly string Copy = "cmd_copy";
		public static readonly string Paste = "cmd_paste";
		public static readonly string Delete = "cmd_delete";
		
		//transactions
		public static readonly string Undo = "cmd_undo";
		public static readonly string Redo = "cmd_redo";
		
		//styles
		public static readonly string Bold = "cmd_bold";
		public static readonly string Italic = "cmd_italic";
		public static readonly string Underline = "cmd_underline";
		public static readonly string TeleType = "cmd_tt";
		public static readonly string Strikethrough = "cmd_strikethru";
		public static readonly string Superscript = "cmd_superscript";
		public static readonly string Subscript = "cmd_subscript";
		public static readonly string Indent = "cmd_indent";
		public static readonly string Outdent = "cmd_outdent";
		public static readonly string IncreaseFont = "cmd_increaseFont";
		public static readonly string DecreaseFont = "cmd_decreaseFont";
		
		//semantic
		public static readonly string Emphasis = "cmd_em";
		public static readonly string Strong = "cmd_strong";
		public static readonly string Citation = "cmd_cite";
		public static readonly string Abbreviation = "cmd_abbr";
		public static readonly string Acronym = "cmd_acronym";
		public static readonly string Code = "cmd_code";
		
		//lists
		public static readonly string OrderedList = "cmd_ol";
		public static readonly string UnorderedList = "cmd_ul";
		
		//public static readonly string NoBreak = "cmd_nobreak";
		//public static readonly string Underline = "cmd_dt";
		//public static readonly string Underline = "cmd_dd";
	}
}
