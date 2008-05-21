// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="David Alpert" email="david@spinthemoose.com"/>
//     <version>$Revision: 1963 $</version>
// </file>

using System;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Gui {
	/// <summary>
	/// Provides the infrastructure to handle generalized code navigation.
	/// </summary>
	/// <remarks>
	/// <para>This service is not limited to navigating code; rather, it
	/// integrates with SharpDevelop's extendable <see cref="IViewContent"/>
	/// interface so that each window has the opportunity to implement a
	/// contextually appropriate navigation scheme.</para>
	/// <para>The default scheme, <see cref="DefaultNavigationPoint"/>, is
	/// created automatically in the <see cref="AbstractViewContent"/>
	/// implementation.  This scheme supports the basic function of logging a
	/// filename and returning to that file's default view.</para>
	/// <para>The default text editor provides a slightly more sophisticated
	/// scheme, <see cref="ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor.TextEditorNavigationPoint">
	/// TextEditorNavigationPoint</see>, that logs filename and line number.</para>
	/// <para>To implement your own navigation scheme, implement
	/// <see cref="IViewContent"/> or derive from
	/// <see cref="AbstractViewContent"/> and override the
	/// <see cref="IViewContent.BuildNavigationPoint">BuildNavigationPoint</see>
	/// method.</para>
	/// <para>
	/// <i>History logic based in part on Orlando Curioso's <i>Code Project</i> article:
	/// <see href="http://www.codeproject.com/cs/miscctrl/WinFormsHistory.asp">
	/// Navigational history (go back/forward) for WinForms controls</see></i>
	/// </para>
	/// </remarks>
	public class NavigationService {
#region Private members
		static LinkedList<INavigationPoint> history = new LinkedList<INavigationPoint> ();
		static LinkedListNode<INavigationPoint> currentNode; // autoinitialized to null (FxCop)
		static INavigationPoint navPoint;
		static bool loggingSuspended; // autoinitialized to false (FxCop)
		static uint timeout;
#endregion
		
		// TODO: FxCop says "find another way to do this" (ReviewVisibleEventHandlers)
		static NavigationService ()
		{
			//WorkbenchSingleton.WorkbenchCreated += WorkbenchCreatedHandler;
			
			IdeApp.Workspace.FileRenamedInProject += FileRenamed;
			IdeApp.Workspace.LastWorkspaceItemClosed += SolutionClosed;
		}
		
#region Public Properties
		public static bool CanNavigateBack {
			get { return currentNode != history.First && currentNode != null; }
		}
		
		public static bool CanNavigateForward {
			get { return currentNode != history.Last && currentNode != null; }
		}
		
		public static int Count {
			get { return history.Count; }
		}
		
		public static INavigationPoint CurrentPosition {
			get {
				return currentNode == null ? (INavigationPoint) null : currentNode.Value;
			}
			set {
				Log (value);
			}
		}
		
		public static bool IsLogging {
			get { return !loggingSuspended; }
		}
#endregion
		
		public static INavigationPoint BuildNavPoint (IEditableTextBuffer textBuffer)
		{
			int line, column;
			textBuffer.GetLineColumnFromPosition (textBuffer.CursorPosition, out line, out column);
			MonoDevelop.Ide.Gui.Search.IDocumentInformation docInfo = textBuffer as MonoDevelop.Ide.Gui.Search.IDocumentInformation;
			return new TextNavigationPoint (textBuffer.Name, line, column, docInfo != null ? docInfo.GetLineTextAtOffset (textBuffer.CursorPosition) : "");
		}
		

#region Public Methods
		// TODO: FxCop says "find another way to do this" (ReviewVisibleEventHandlers)
		public static void ContentChanging (object sender, EventArgs e)
		{
			foreach (INavigationPoint p in history) {
				p.ContentChanging (sender, e);
			}
		}
		
		public static void Log (INavigationPoint pointToLog)
		{
			if (loggingSuspended)
				return;
			
			LogInternal (pointToLog);
		}
		
		// refactoring this out of Log() allows the NavigationService
		// to call this and ensure it will work regardless of the
		// requested state of loggingSuspended
		private static void LogInternal (INavigationPoint p)
		{
			if (p == null || p.FileName == null || p.FileName == String.Empty)
				return;
			
			if (timeout != 0)
					GLib.Source.Remove (timeout);
			
			if (navPoint != null && navPoint.FileName != p.FileName) {
				// user switched files: always log most recent navpoint before file switch
				LogNavPoint ();
			}
			
			// defer logging
			timeout = GLib.Timeout.Add (1000, LogNavPoint);
			navPoint = p;
		}
		
		static bool LogNavPoint ()
		{
			if (currentNode == null) {
				currentNode = history.AddFirst (navPoint);
			} else if (navPoint.Equals (currentNode.Value)) {
				// replace it
				currentNode.Value = navPoint;
			} else {
				currentNode = history.AddAfter (currentNode, navPoint);
				if (history.Count > 100)
					history.RemoveFirst ();
			}
			
			navPoint = null;
			timeout = 0;
			
			OnHistoryChanged ();
			
			return false;
		}
		
		public static ICollection<INavigationPoint> Points
		{
			get { return new List<INavigationPoint> (history); }
		}
		
		public static void ClearHistory ()
		{
			ClearHistory (false);
		}
		
		public static void ClearHistory (bool clearCurrentPosition)
		{
			INavigationPoint currentPosition = CurrentPosition;
			
			history.Clear ();
			currentNode = null;
			
			if (!clearCurrentPosition)
				LogInternal (currentPosition);
			
			OnHistoryChanged ();
		}
		
		public static void Go (int delta)
		{
			if (delta == 0) {
				return;
			} else if (delta < 0) {
				// move backwards
				while (delta < 0 && currentNode != history.First) {
					currentNode = currentNode.Previous;
					delta++;
				}
			} else {
				// move forwards
				while (delta > 0 && currentNode != history.Last) {
					currentNode = currentNode.Next;
					delta--;
				}
			}
			
			SyncViewWithModel ();
			
			OnHistoryChanged ();
		}
		
		public static void Go (INavigationPoint target)
		{
			if (target == null)
				return;
			
			LinkedListNode<INavigationPoint> targetNode;
			targetNode = history.Find (target);
			
			if (targetNode != null) {
				currentNode = targetNode;
			} else {
				LoggingService.LogError ("Logging additional point: {0}", target);
				LogInternal (target);
				//currentNode = history.AddAfter(currentNode, target);
			}
			
			SyncViewWithModel ();
			
			OnHistoryChanged ();
		}
		
		private static void SyncViewWithModel ()
		{
			//LoggingService.Info ("suspend logging");
			SuspendLogging ();
			
			if (CurrentPosition != null) {
				CurrentPosition.JumpTo ();
			}
			
			//LoggingService.Info ("resume logging");
			ResumeLogging ();
		}
		
		public static void SuspendLogging ()
		{
			loggingSuspended = true;
		}
		
		public static void ResumeLogging ()
		{
			// ENH: possible enhancement: use int instead of bool so resume statements are incremental rather than definitive.
			loggingSuspended = false;
		}
#endregion
		
		// the following code is not covered by Unit tests as i wasn't sure
		// how to test code triggered by the user interacting with the workbench
#region event trapping
		
//#region ViewContent events
//		static void WorkbenchCreatedHandler (object sender, EventArgs e)
//		{
//			IdeApp.Workbench.DocumentOpened += 
//				new ViewContentEventHandler (ViewContentOpened);
//			IdeApp.Workbench.DocumentClosed +=
//				new ViewContentEventHandler (ViewContentClosed);
//			//WorkbenchSingleton.Workbench.ViewOpened +=
//			//	new ViewContentEventHandler (ViewContentOpened);
//			//WorkbenchSingleton.Workbench.ViewClosed +=
//			//	new ViewContentEventHandler (ViewContentClosed);
//		}
//		
//		static void ViewContentOpened (object sender, ViewContentEventArgs e)
//		{
//			//Log (e.Content);
//			e.Content.WorkbenchWindow.WindowSelected += WorkBenchWindowSelected;
//		}
//		
//		static void ViewContentClosed (object sender, ViewContentEventArgs e)
//		{
//			e.Content.WorkbenchWindow.WindowSelected -= WorkBenchWindowSelected;
//		}
//		
//		static IWorkbenchWindow lastSelectedWindow;
//		
//		static void WorkBenchWindowSelected (object sender, EventArgs e)
//		{
//			try {
//				IWorkbenchWindow window = sender as IWorkbenchWindow;
//				if (window == lastSelectedWindow) {
//					return;
//				}
//				
//				//int n = NavigationService.Count;
//				Log (window);
//				
//				//LoggingService.DebugFormatted ("WorkbenchSelected: logging {0}", window.Title);
//				
//				// HACK: Navigation - for some reason, JumpToFilePosition returns _before_ this
//				//       gets fired, (not the behaviour i expected) so we need to remember the
//				//       previous selected window to ensure that we only log once per visit to
//				//       a given window.
//				lastSelectedWindow = window;
//			} catch (Exception ex) {
//				LoggingService.ErrorFormatted ("{0}:\n{1}", ex.Message, ex.StackTrace);
//				throw;
//			}
//		}
//#endregion
		
		static void FileRenamed (object sender, ProjectFileRenamedEventArgs e)
		{
			foreach (INavigationPoint p in history) {
				if (p.FileName.Equals (e.OldName)) {
					p.FileNameChanged (e.NewName);
				}
			}
		}
		
		static void SolutionClosed (object sender, EventArgs e)
		{
			NavigationService.ClearHistory (true);
		}
#endregion
		
#region Public Events
		public static event EventHandler HistoryChanged;
		
		static void OnHistoryChanged ()
		{
			if (HistoryChanged != null) {
				HistoryChanged (NavigationService.CurrentPosition, EventArgs.Empty);
			}
		}
#endregion
	}
}
