// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.Gui
{
	public class WorkbenchContext
	{
		string id;
		static Hashtable contexts = new Hashtable ();
		
		WorkbenchContext (string id)
		{
			this.id = id;
		}
		
		public static WorkbenchContext GetContext (string id)
		{
			WorkbenchContext ctx = (WorkbenchContext) contexts [id];
			if (ctx == null) {
				ctx = new WorkbenchContext (id);
				contexts [id] = ctx;
			}
			return ctx;
		}
		
		public static WorkbenchContext Edit {
			get { return GetContext ("Edit"); }
		}
		
		public static WorkbenchContext Debug {
			get { return GetContext ("Debug"); }
		}
		
		public string Id {
			get { return id; }
		}
	}
	
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public interface IWorkbench : IMementoCapable
	{
		/// <summary>
		/// The title shown in the title bar.
		/// </summary>
		string Title {
			get;
			set;
		}
		
		/// <summary>
		/// A collection in which all active workspace windows are saved.
		/// </summary>
		ViewContentCollection ViewContentCollection {
			get;
		}
		
		/// <summary>
		/// A collection in which all active workspace windows are saved.
		/// </summary>
		PadContentCollection PadContentCollection {
			get;
		}
		
		/// <summary>
		/// The active workbench window.
		/// </summary>
		IWorkbenchWindow ActiveWorkbenchWindow {
			get;
		}
		
		IWorkbenchLayout WorkbenchLayout {
			get;
			set;
		}
				
		/// <summary>
		/// Inserts a new <see cref="IViewContent"/> object in the workspace.
		/// </summary>
		void ShowView (IViewContent content, bool bringToFront);
		
		/// <summary>
		/// Inserts a new <see cref="IPadContent"/> object in the workspace.
		/// </summary>
		void ShowPad(IPadContent content);
		
		/// <summary>
		/// Returns a pad from a specific type.
		/// </summary>
		IPadContent GetPad(Type type);
		
		/// <summary>
		/// Tries to make the pad visible to the user.
		/// </summary>
		void BringToFront (IPadContent content);
		
		/// <summary>
		/// Closes the IViewContent content when content is open.
		/// </summary>
		void CloseContent(IViewContent content);
		
		/// <summary>
		/// Closes all views inside the workbench.
		/// </summary>
		void CloseAllViews();
		
		/// <summary>
		/// Re-initializes all components of the workbench, should be called
		/// when a special property is changed that affects layout stuff.
		/// (like language change) 
		/// </summary>
		void RedrawAllComponents();

		/// <summary>
		/// Is called, when the workbench window which the user has into
		/// the foreground (e.g. editable) changed to a new one.
		/// </summary>
		event EventHandler ActiveWorkbenchWindowChanged;

		/// <summary>
		/// The context the workbench is currently in
		/// </summary>
		WorkbenchContext Context {
			get;
			set;
		}
		
		/// <summary>
		/// Called when the Context property changes
		/// </summary>
		event EventHandler ContextChanged;
	}
}
