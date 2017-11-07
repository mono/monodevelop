//
// CommandManager.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

using MonoDevelop.Components.Commands.ExtensionNodes;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Components.Commands
{
	public class CommandManager: IDisposable
	{
		// Carbon.framework/Versions/A/Frameworks/HIToolbox.framework/Versions/A/Headers/Events.h
		enum JIS_VKS {
			Yen         = 0x5d,
			Underscore  = 0x5e,
			KeypadComma = 0x5f,
			Eisu        = 0x66,
			Kana        = 0x68
		}

		Gtk.Window rootWidget;
		KeyBindingManager bindings;
		Gtk.AccelGroup accelGroup;
		uint statusUpdateWait = 500;
		DateTime lastUserInteraction;
		KeyboardShortcut[] chords;
		string chord;
		internal const int SlowCommandWarningTime = 25;
		
		Dictionary<object,Command> cmds = new Dictionary<object,Command> ();
		Hashtable handlerInfo = new Hashtable ();
		List<ICommandBar> toolbars = new List<ICommandBar> ();
		CommandTargetChain globalHandlerChain;
		ArrayList commandUpdateErrors = new ArrayList ();
		List<ICommandTargetVisitor> visitors = new List<ICommandTargetVisitor> ();
		LinkedList<Gtk.Window> topLevelWindows = new LinkedList<Gtk.Window> ();
		Stack delegatorStack = new Stack ();

		HashSet<object> visitedTargets = new HashSet<object> ();
		
		bool disposed;
		bool toolbarUpdaterRunning;
		bool enableToolbarUpdate;
		int guiLock;
		int lastX, lastY;
		
		// Fields used to keep track of the application focus
		bool appHasFocus;
		Gtk.Window lastFocused;
		DateTime focusCheckDelayTimeout = DateTime.MinValue;
		
		internal static readonly object CommandRouteTerminator = new object ();
		
		internal bool handlerFoundInMulticast;
		Gtk.Widget lastActiveWidget;

#if MAC
		Foundation.NSObject keyMonitor;
		uint throttleLastEventTime = 0;
#endif

		Dictionary<Command, HashSet<Command>> conflicts;
		internal Dictionary<Command, HashSet<Command>> Conflicts {
			get {
				if (conflicts == null)
					LoadConflicts ();
				return conflicts;
			}
		}

		public CommandManager (): this (null)
		{
		}
		
		public CommandManager (Window root)
		{
			if (root != null)
				rootWidget = root;
			bindings = new KeyBindingManager ();
			ActionCommand c = new ActionCommand (CommandSystemCommands.ToolbarList, "Toolbar List", null, null, ActionType.Check);
			c.CommandArray = true;
			RegisterCommand (c);
		}

		/// <summary>
		/// Loads command definitions from the provided extension path
		/// </summary>
		public void LoadCommands (string addinPath)
		{
			AddinManager.AddExtensionNodeHandler (addinPath, OnExtensionChange);
		}

		/// <summary>
		/// Loads key binding schemes from the provided extension path
		/// </summary>
		public void LoadKeyBindingSchemes (string addinPath)
		{
			KeyBindingService.LoadBindingsFromExtensionPath (addinPath);
		}

		void OnExtensionChange (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				if (args.ExtensionNode is CommandCodon)
					RegisterCommand ((Command) args.ExtensionObject);
				else
					// It's a category node. Track changes in the category.
					args.ExtensionNode.ExtensionNodeChanged += OnExtensionChange;
			}
			else {
				if (args.ExtensionNode is CommandCodon)
					UnregisterCommand ((Command)args.ExtensionObject);
				else
					args.ExtensionNode.ExtensionNodeChanged -= OnExtensionChange;
			}
		}
		
		/// <summary>
		/// Creates a menu bar from the menu definition at the provided extension path
		/// </summary>
		internal Gtk.MenuBar CreateMenuBar (string addinPath)
		{
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			return CreateMenuBar (addinPath, cset);
		}
		
		/// <summary>
		/// Creates a menu from the provided extension path
		/// </summary>
		public Gtk.Menu CreateMenu (string addinPath)
		{
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			return CreateMenu (cset);
		}

		/// <summary>
		/// Shows a context menu.
		/// </summary>
		/// <param name='parent'>
		/// Widget for which the context menu is being shown
		/// </param>
		/// <param name='evt'>
		/// Current event object
		/// </param>
		/// <param name='addinPath'>
		/// Extension path to the definition of the menu
		/// </param>
		public void ShowContextMenu (Control parent, Gdk.EventButton evt, string addinPath)
		{
			ShowContextMenu (parent, evt, CreateCommandEntrySet (addinPath));
		}
		
		/// <summary>
		/// Shows a context menu.
		/// </summary>
		/// <param name='parent'>
		/// Widget for which the context menu is being shown
		/// </param>
		/// <param name='evt'>
		/// Current event object
		/// </param>
		/// <param name='ctx'>
		/// Extension context to use to query the extension path
		/// </param>
		/// <param name='addinPath'>
		/// Extension path to the definition of the menu
		/// </param>
		public void ShowContextMenu (Control parent, Gdk.EventButton evt,
			ExtensionContext ctx, string addinPath)
		{
			ShowContextMenu (parent, evt, CreateCommandEntrySet (ctx, addinPath));
		}
		
		/// <summary>
		/// Creates a command entry set.
		/// </summary>
		/// <returns>
		/// The command entry set.
		/// </returns>
		/// <param name='ctx'>
		/// Extension context to use to query the extension path
		/// </param>
		/// <param name='addinPath'>
		/// Extension path with the command definitions
		/// </param>
		public CommandEntrySet CreateCommandEntrySet (ExtensionContext ctx, string addinPath)
		{
			CommandEntrySet cset = new CommandEntrySet ();
			object[] items = ctx.GetExtensionObjects (addinPath, false);
			foreach (CommandEntry e in items)
				cset.Add (e);
			return cset;
		}
		
		/// <summary>
		/// Creates a command entry set.
		/// </summary>
		/// <returns>
		/// The command entry set.
		/// </returns>
		/// <param name='addinPath'>
		/// Extension path with the command definitions
		/// </param>
		public CommandEntrySet CreateCommandEntrySet (string addinPath)
		{
			CommandEntrySet cset = new CommandEntrySet ();
			object[] items = AddinManager.GetExtensionObjects (addinPath, false);
			foreach (CommandEntry e in items)
				cset.Add (e);
			return cset;
		}
		
		bool isEnabled = true;
		
		/// <summary>
		/// Gets or sets a value indicating whether the command manager is enabled. When disabled, all commands are disabled.
		/// </summary>
		public bool IsEnabled {
			get {
				return isEnabled;
			}
			set {
				isEnabled = value;
			}
		}


		/// <summary>
		/// The command currently being executed or for which the status is being checked
		/// </summary>
		public Command CurrentCommand { get; private set; }

		bool CanUseBinding (KeyboardShortcut[] chords, KeyboardShortcut[] accels, out KeyBinding binding, out bool isChord)
		{
			if (chords != null) {
				foreach (var chord in chords) {
					foreach (var accel in accels) {
						binding = new KeyBinding (chord, accel);
						if (bindings.BindingExists (binding)) {
							isChord = false;
							return true;
						}
					}
				}
			} else {
				foreach (var accel in accels) {
					if (bindings.ChordExists (accel)) {
						// Chords take precedence over bindings with the same shortcut.
						binding = null;
						isChord = true;
						return false;
					}
					
					binding = new KeyBinding (accel);
					if (bindings.BindingExists (binding)) {
						isChord = false;
						return true;
					}
				}
			}

			isChord = false;
			binding = null;
			
			return false;
		}
		
		public event EventHandler<KeyBindingFailedEventArgs> KeyBindingFailed;

#if MAC
		AppKit.NSEvent OnNSEventKeyPress (AppKit.NSEvent ev)
		{
			// If we have a native window that can handle this command, let it process
			// the keys itself and do not go through the command manager.
			// Events in Gtk windows do not pass through here except when they're done
			// in native NSViews. PerformKeyEquivalent for them will not return true,
			// so we're always going to fallback to the command manager for them.
			// If no window is focused, it's probably because a gtk window had focus
			// and the focus didn't go to any other window on close. (debug popup on hover
			// that gets closed on unhover). So if no keywindow is focused, events will
			// pass through here and let us use the command manager.
			var window = AppKit.NSApplication.SharedApplication.KeyWindow;
			if (window != null) {
				// Try the handler in the native window.
				if (window.PerformKeyEquivalent (ev))
					return null;

				// Try the default NSApplication handlers, like copy/paste commands inside native entries
				if (PerformDefaultNSAppAction (window, ev))
					return null;

				// If this is Eisu or Kana on a Japanese keyboard make sure not to exit yet or
				// the input source will not switch as expected.
				if (ev.KeyCode != (ushort)JIS_VKS.Eisu && ev.KeyCode != (ushort)JIS_VKS.Kana)
				{
					// If the window is a gtk window and is registered in the command manager
					// process the events through the handler.
					var gtkWindow = MonoDevelop.Components.Mac.GtkMacInterop.GetGtkWindow(window);
					if (gtkWindow != null && !TopLevelWindowStack.Contains(gtkWindow))
						return null;
				}
			}

			// If a modal dialog is running then the menus are disabled, even if the commands are not
			// See MDMenuItem::IsGloballyDisabled
			if (DesktopService.IsModalDialogRunning ()) {
				return ev;
			}

			var gdkev = MonoDevelop.Components.Mac.GtkMacInterop.ConvertKeyEvent (ev);
			if (gdkev != null) {
				if (ProcessKeyEvent (gdkev))
					return null;
			}
			return ev;
		}

		bool PerformDefaultNSAppAction (AppKit.NSWindow window, AppKit.NSEvent ev)
		{
			// Try the user defined bindings first
			var gdkev = Mac.GtkMacInterop.ConvertKeyEvent (ev);
			if (gdkev != null) {
				bool complete;
				KeyboardShortcut [] accels = KeyBindingManager.AccelsFromKey (gdkev, out complete);
				if (complete) {
					foreach (var accel in accels) {
						var binding = KeyBindingManager.AccelLabelFromKey (accel.Key, accel.Modifier);

						if (IsCommandBinding (Ide.Commands.EditCommands.Copy, binding))
							return AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("copy:"), null, window);

						if (IsCommandBinding (Ide.Commands.EditCommands.Paste, binding))
							return AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("paste:"), null, window);

						if (IsCommandBinding (Ide.Commands.EditCommands.Cut, binding))
							return AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("cut:"), null, window);

						if (IsCommandBinding (Ide.Commands.EditCommands.SelectAll, binding))
							return AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("selectAll:"), null, window);

						if (IsCommandBinding (Ide.Commands.EditCommands.Undo, binding))
							return AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("undo:"), null, window);

						if (IsCommandBinding (Ide.Commands.EditCommands.Redo, binding))
							return AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("redo:"), null, window);
					}
				}
			}

			// Try default OSX selectors
			bool actionResult = false;
			if (ev.Type == AppKit.NSEventType.KeyDown) {
				if ((ev.ModifierFlags & AppKit.NSEventModifierMask.CommandKeyMask) != 0) {
					switch (ev.CharactersIgnoringModifiers) {
					case "c":
						actionResult = AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("copy:"), null, window);
						break;
					case "v":
						actionResult = AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("paste:"), null, window);
						break;
					case "x":
						actionResult = AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("cut:"), null, window);
						break;
					case "a":
						actionResult = AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("selectAll:"), null, window);
						break;
					case "z":
						actionResult = AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("undo:"), null, window);
						break;
					case "Z":
						actionResult = AppKit.NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("redo:"), null, window);
						break;
					}
				}
			}
			return actionResult;
		}

		bool IsCommandBinding (object commandId, string binding)
		{
			var cmd = GetCommand (ToCommandId (commandId));
			if (cmd != null) {
				var bds = KeyBindingService.CurrentKeyBindingSet.GetBindings (cmd);
				return bds.Contains (binding);
			}
			return false;
		}
#endif

		[GLib.ConnectBefore]
		void OnKeyPressed (object o, Gtk.KeyPressEventArgs e)
		{
			e.RetVal = ProcessKeyEvent (e.Event);
		}

		[GLib.ConnectBefore]
		void OnKeyReleased (object o, Gtk.KeyReleaseEventArgs e)
		{
			bool complete;
			// KeyboardShortcut[] accels = 
			KeyBindingManager.AccelsFromKey (e.Event, out complete);

			if (!complete) {
				// incomplete accel
				NotifyIncompleteKeyReleased (e.Event);
			}
		}

		internal bool ProcessKeyEvent (Gdk.EventKey ev)
		{
			if (!IsEnabled)
				return true;

			RegisterUserInteraction ();
			
			bool complete;
			KeyboardShortcut[] accels = KeyBindingManager.AccelsFromKey (ev, out complete);

			if (!complete) {
				// incomplete accel
				NotifyIncompleteKeyPressed (ev);
				return true;
			}
			
			List<Command> commands = null;
			KeyBinding binding;
			bool isChord;
			bool result;

			if (CanUseBinding (chords, accels, out binding, out isChord)) {
				commands = bindings.Commands (binding);
				result = true;
				chords = null;
				chord = null;
			} else if (isChord) {
				chord = KeyBindingManager.AccelLabelFromKey (ev);
				chords = accels;
				return true;
			} else if (chords != null) {
				// Note: The user has entered a valid chord but the accel was invalid.
				if (KeyBindingFailed != null) {
					string accel = KeyBindingManager.AccelLabelFromKey (ev);
					
					KeyBindingFailed (this, new KeyBindingFailedEventArgs (GettextCatalog.GetString ("The key combination ({0}, {1}) is not a command.", chord, accel)));
				}
				
				chords = null;
				chord = null;
				return true;
			} else {
				chords = null;
				chord = null;
				
				NotifyKeyPressed (ev);
				return false;
			}

			var toplevelFocus = IdeApp.Workbench.HasToplevelFocus;

			var conflict = new List<Command> ();

			bool bypass = false;
			var dispatched = false;

			for (int i = 0; i < commands.Count; i++) {
				CommandInfo cinfo = GetCommandInfo (commands[i].Id, new CommandTargetRoute ());
				if (cinfo.IsUpdatingAsynchronously)
					cinfo.UpdateTask.Wait (); // Not nice, but we need a synchronous result here
				if (cinfo.Bypass) {
					bypass = true;
					continue;
				}

				if (cinfo.Enabled && cinfo.Visible) {
					if (!dispatched)
						dispatched = DispatchCommand (commands [i].Id, null, null, CommandSource.Keybinding, ev.Time);
					conflict.Add (commands [i]);
				} else
					bypass = true; // allow Gtk to handle the event if the command is disabled
			}

			if (conflict.Count > 1) {
				bool newConflict = false;
				foreach (var item in conflict) {
					HashSet<Command> itemConflicts;
					if (!Conflicts.TryGetValue (item, out itemConflicts))
						Conflicts [item] = itemConflicts = new HashSet<Command> ();
					var tmp = conflict.Where (c => c != item);
					if (!itemConflicts.IsSupersetOf (tmp)) {
						itemConflicts.UnionWith (tmp);
						newConflict = true;
					}
				}
				if (newConflict)
					SaveConflicts ();
				if (KeyBindingFailed != null)
					KeyBindingFailed (this, new KeyBindingFailedEventArgs (GettextCatalog.GetString ("The key combination ({0}) has conflicts.", KeyBindingManager.BindingToDisplayLabel (binding.ToString (), false))));
			}

			if (dispatched)
				return result;

			// The command has not been handled.
			// If there is at least a handler that sets the bypass flag, allow gtk to execute the default action
			
			if (commands.Count > 0 && !bypass) {
				result = true;
			} else {
				result = false;
				NotifyKeyPressed (ev);
			}
			
			chords = null;
			return result;
		}

		void LoadConflicts ()
		{
			if (conflicts == null)
				conflicts = new Dictionary<Command, HashSet<Command>> ();

			var file = UserProfile.Current.CacheDir.Combine ("CommandConflicts.xml");

			if (!File.Exists (file))
				return;

			try {
				using (var reader = new XmlTextReader (file)) {
					bool foundConflicts = false;
					conflicts.Clear ();

					while (reader.Read ()) {
						if (reader.IsStartElement ("conflicts")) {
							foundConflicts = true;
							break;
						}
					}

					if (!foundConflicts || reader.GetAttribute ("version") != "1.0")
						return;

					while (reader.Read ()) {
						if (reader.IsStartElement ("conflict")) {
							var conflictId = reader.GetAttribute ("id");
							var command = GetCommand (conflictId);
							if (command == null)
								continue;

							var conflict = new HashSet<Command> ();
							conflicts.Add (command, conflict);
							while (reader.Read ()) {
								if (reader.IsStartElement ("command")) {
									var cmdId = reader.ReadElementContentAsString ();
									var cmd = GetCommand (cmdId);
									if (cmd == null)
										continue;
									conflict.Add (cmd);
								} else
									break;
							}
						}
					}
				}
			} catch (Exception e) {
				conflicts.Clear ();
				LoggingService.LogError ("Loading command conflicts from " + file + " failed.", e);
			}
		}

		void SaveConflicts ()
		{
			if (!Directory.Exists (UserProfile.Current.CacheDir))
				Directory.CreateDirectory (UserProfile.Current.CacheDir);

			string file = UserProfile.Current.CacheDir.Combine ("CommandConflicts.xml");

			try {
				using (var stream = new FileStream (file + '~', FileMode.Create))
				using (var writer = new XmlTextWriter (stream, Encoding.UTF8)) {
					writer.Formatting = Formatting.Indented;
					writer.IndentChar = ' ';
					writer.Indentation = 2;

					writer.WriteStartElement ("conflicts");
					writer.WriteAttributeString ("version", "1.0");

					foreach (var conflict in conflicts) {
						writer.WriteStartElement ("conflict");
						writer.WriteAttributeString ("id", conflict.Key.Id.ToString ());
						foreach (var cmd in conflict.Value) {
							writer.WriteStartElement ("command");
							writer.WriteString (cmd.Id.ToString ());
							writer.WriteEndElement ();
						}
						writer.WriteEndElement ();
					}

					writer.WriteEndElement ();
				}
				FileService.SystemRename (file + '~', file);
			} catch (Exception e) {
				LoggingService.LogError ("Saving command conflicts to " + file + " failed.", e);
			}
		}
		
		void NotifyKeyPressed (Gdk.EventKey ev)
		{
			if (KeyPressed != null)
				KeyPressed (this, new KeyPressArgs () { Key = ev.Key, KeyValue = ev.KeyValue, Modifiers = ev.State });
		}

		void NotifyIncompleteKeyPressed (Gdk.EventKey ev)
		{
			if (IncompleteKeyPressed != null)
				IncompleteKeyPressed (this, new KeyPressArgs () { Key = ev.Key, KeyValue = ev.KeyValue, Modifiers = ev.State });
		}

		void NotifyIncompleteKeyReleased (Gdk.EventKey ev)
		{
			if (IncompleteKeyReleased != null)
				IncompleteKeyReleased (this, new KeyPressArgs () { Key = ev.Key, KeyValue = ev.KeyValue, Modifiers = ev.State });
		}
		
		/// <summary>
		/// Sets the root window. The manager will start the command route at this window, if no other is active.
		/// </summary>
		public void SetRootWindow (Window root)
		{
			if (rootWidget != null)
				rootWidget.KeyPressEvent -= OnKeyPressed;
			
			rootWidget = root;
			rootWidget.AddAccelGroup (AccelGroup);
			RegisterTopWindow (rootWidget);
		}

		internal IEnumerable<Gtk.Window> TopLevelWindowStack {
			get { return topLevelWindows; }
		}
		
		internal void RegisterTopWindow (Gtk.Window win)
		{
			if (topLevelWindows.First != null && topLevelWindows.First.Value == win)
				return;

#if MAC
			if (topLevelWindows.Count == 0) {
				keyMonitor = AppKit.NSEvent.AddLocalMonitorForEventsMatchingMask (AppKit.NSEventMask.KeyDown, OnNSEventKeyPress);
			}
#endif

			// Ensure all events that were subscribed in StartWaitingForUserInteraction are unsubscribed
			// before doing any change to the topLevelWindows list
			EndWaitingForUserInteraction ();

			var node = topLevelWindows.Find (win);
			if (node != null) {
				if (win.HasToplevelFocus) {
					topLevelWindows.Remove (node);
					topLevelWindows.AddFirst (node);
				}
			} else {
				topLevelWindows.AddFirst (win);
				win.KeyPressEvent += OnKeyPressed;
				win.KeyReleaseEvent += OnKeyReleased;
				win.ButtonPressEvent += HandleButtonPressEvent;
				win.Destroyed += TopLevelDestroyed;
			}
		}

		[GLib.ConnectBefore]
		void HandleButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			RegisterUserInteraction ();
		}

		void TopLevelDestroyed (object o, EventArgs args)
		{
			RegisterUserInteraction ();

			Gtk.Window w = (Gtk.Window)o;
			w.Destroyed -= TopLevelDestroyed;
			w.KeyPressEvent -= OnKeyPressed;
			w.KeyReleaseEvent -= OnKeyReleased;
			w.ButtonPressEvent -= HandleButtonPressEvent;
			topLevelWindows.Remove (w);
#if MAC
			if (topLevelWindows.Count == 0) {
				if (keyMonitor != null) {
					AppKit.NSEvent.RemoveMonitor (keyMonitor);
					keyMonitor = null;
				}
			}
#endif

			if (w == lastFocused)
				lastFocused = null;
		}
		
		public void Dispose ()
		{
			disposed = true;
			if (bindings != null) {
				bindings.Dispose ();
				bindings = null;
			}

#if MAC
			if (keyMonitor != null) {
				AppKit.NSEvent.RemoveMonitor (keyMonitor);
				keyMonitor = null;
			}
#endif
			lastFocused = null;
		}
		
		/// <summary>
		/// Disables all commands
		/// </summary>
		public bool LockAll ()
		{
			guiLock++;
			if (guiLock == 1) {
				foreach (ICommandBar toolbar in toolbars)
					toolbar.SetEnabled (false);
				return true;
			} else
				return false;
		}
		
		/// <summary>
		/// Unlocks the command manager
		/// </summary>
		public bool UnlockAll ()
		{
			if (guiLock == 1) {
				foreach (ICommandBar toolbar in toolbars)
					toolbar.SetEnabled (true);
			}
			
			if (guiLock > 0)
				guiLock--;
			return guiLock == 0;
		}
		
		/// <summary>
		/// When set to true, the toolbar status will be updated periodically while the gui is idle.
		/// idle update.
		/// </summary>
		public bool EnableIdleUpdate {
			get { return enableToolbarUpdate; }
			set {
				if (enableToolbarUpdate != value) {
					enableToolbarUpdate = value;
					if (value) {
						if (toolbars.Count > 0 || visitors.Count > 0)
							StartStatusUpdater ();
					} else {
						StopStatusUpdater ();
					}
				}
			}
		}

		/// <summary>
		/// Registers a new command.
		/// </summary>
		/// <param name='cmd'>
		/// The command.
		/// </param>
		public void RegisterCommand (Command cmd)
		{
			KeyBindingService.StoreDefaultBinding (cmd);
			KeyBindingService.LoadBinding (cmd);
			
			cmds[cmd.Id] = cmd;
			bindings.RegisterCommand (cmd);
		}
		
		/// <summary>
		/// Unregisters a command.
		/// </summary>
		/// <param name='cmd'>
		/// The command.
		/// </param>
		public void UnregisterCommand (Command cmd)
		{
			bindings.UnregisterCommand (cmd);
			cmds.Remove (cmd.Id);
		}
		
		/// <summary>
		/// Loads user defined key bindings.
		/// </summary>
		public void LoadUserBindings ()
		{
			foreach (Command cmd in cmds.Values)
				KeyBindingService.LoadBinding (cmd);
		}
		
		/// <summary>
		/// Registers a global command handler.
		/// </summary>
		/// <param name='handler'>
		/// The handler
		/// </param>
		/// <remarks>
		/// Global command handler are added to the end of the command route.
		/// </remarks>
		public void RegisterGlobalHandler (object handler)
		{
			globalHandlerChain = CommandTargetChain.AddTarget (globalHandlerChain, handler);
		}

		/// <summary>
		/// Unregisters a global handler.
		/// </summary>
		/// <param name='handler'>
		/// The handler.
		/// </param>
		public void UnregisterGlobalHandler (object handler)
		{
			globalHandlerChain = CommandTargetChain.RemoveTarget (globalHandlerChain, handler);
		}
		
		/// <summary>
		/// Registers a command target visitor.
		/// </summary>
		/// <param name='visitor'>
		/// The visitor.
		/// </param>
		/// <remarks>
		/// Command target visitors can be used to visit the whole active command route
		/// to perform custom actions on the objects of the route. The command manager
		/// periodically visits the command route. The visit frequency varies, but it
		/// is usually at least once a second.
		/// </remarks>
		public void RegisterCommandTargetVisitor (ICommandTargetVisitor visitor)
		{
			visitors.Add (visitor);
			StartStatusUpdater ();
		}
		
		/// <summary>
		/// Unregisters a command target visitor.
		/// </summary>
		/// <param name='visitor'>
		/// The visitor.
		/// </param>
		public void UnregisterCommandTargetVisitor (ICommandTargetVisitor visitor)
		{
			visitors.Remove (visitor);

			StopStatusUpdaterIfNeeded ();
		}
		
		/// <summary>
		/// Gets a registered command.
		/// </summary>
		/// <returns>
		/// The command.
		/// </returns>
		/// <param name='cmdId'>
		/// The identifier of the command
		/// </param>
		public Command GetCommand (object cmdId)
		{
			// Include the type name when converting enum members to ids.
			cmdId = ToCommandId (cmdId);
			
			Command cmd;
			if (cmds.TryGetValue (cmdId, out cmd))
				return cmd;
			else
				return null;
		}

		/// <summary>
		/// Gets all registered commands
		/// </summary>
		public IEnumerable<Command> GetCommands ()
		{
			return cmds.Values;
		}

		/// <summary>
		/// Gets an action command.
		/// </summary>
		/// <returns>
		/// The action command.
		/// </returns>
		/// <param name='cmdId'>
		/// The command identifier.
		/// </param>
		public ActionCommand GetActionCommand (object cmdId)
		{
			return GetCommand (cmdId) as ActionCommand;
		}

		/// <summary>
		/// Gets all registered commands with the specified binding
		/// </summary>
		internal IEnumerable<Command> GetCommands (KeyBinding binding)
		{
			var commands = bindings.Commands (binding);
			if (commands == null)
				yield break;
			foreach (var cmd in commands)
				yield return cmd;
		}
		
		/// <summary>
		/// Creates a menu bar.
		/// </summary>
		/// <returns>
		/// The menu bar.
		/// </returns>
		/// <param name='name'>
		/// Unused
		/// </param>
		/// <param name='entrySet'>
		/// Entry set with the definition of the commands to be included in the menu bar
		/// </param>
		internal Gtk.MenuBar CreateMenuBar (string name, CommandEntrySet entrySet)
		{
			Gtk.MenuBar topMenu = new CommandMenuBar (this);
			foreach (CommandEntry entry in entrySet) {
				Gtk.MenuItem mi = entry.CreateMenuItem (this);
				CustomItem ci = mi.Child as CustomItem;
				if (ci != null)
					ci.SetMenuStyle (topMenu);
				topMenu.Append (mi);
			}
			return topMenu;
		}
		
/*		public Gtk.Toolbar CreateToolbar (CommandEntrySet entrySet)
		{
			return CreateToolbar ("", entrySet);
		}
		
*/	
		/// <summary>
		/// Appends commands to a menu
		/// </summary>
		/// <returns>
		/// The menu.
		/// </returns>
		/// <param name='entrySet'>
		/// Entry set with the command definitions
		/// </param>
		/// <param name='menu'>
		/// The menu where to add the commands
		/// </param>
		internal Gtk.Menu CreateMenu (CommandEntrySet entrySet, CommandMenu menu)
		{
			foreach (CommandEntry entry in entrySet) {
				Gtk.MenuItem mi = entry.CreateMenuItem (this);
				CustomItem ci = mi.Child as CustomItem;
				if (ci != null)
					ci.SetMenuStyle (menu);
				menu.Append (mi);
			}
			return menu;
		}

#if MAC
		/// <summary>
		/// Creates a menu.
		/// </summary>
		/// <returns>
		/// The menu.
		/// </returns>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		public AppKit.NSMenu CreateNSMenu (CommandEntrySet entrySet)
		{
			return CreateNSMenu (entrySet, new CommandMenu (this));
		}

		/// <summary>
		/// Creates the menu.
		/// </summary>
		/// <returns>
		/// The menu.
		/// </returns>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		/// <param name='initialTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		public AppKit.NSMenu CreateNSMenu (CommandEntrySet entrySet, object initialTarget)
		{
			return CreateNSMenu (entrySet, initialTarget, null);
		}

		/// <summary>
		/// Creates the menu.
		/// </summary>
		/// <returns>
		/// The menu.
		/// </returns>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		/// <param name='initialTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		/// <param name='closeHandler'>
		/// EventHandler to be run when the menu closes
		/// </param>
		public AppKit.NSMenu CreateNSMenu (CommandEntrySet entrySet, object initialTarget, EventHandler closeHandler)
		{
			return new MonoDevelop.Components.Mac.MDMenu (this, entrySet, CommandSource.ContextMenu, initialTarget, closeHandler);
		}
#endif

		/// <summary>
		/// Creates a menu.
		/// </summary>
		/// <returns>
		/// The menu.
		/// </returns>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		public Gtk.Menu CreateMenu (CommandEntrySet entrySet)
		{
			return CreateMenu (entrySet, new CommandMenu (this));
		}

		/// <summary>
		/// Creates a menu.
		/// </summary>
		/// <returns>
		/// The menu.
		/// </returns>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		/// <param name='closeHandler'>
		/// EventHandler to be run when the menu closes
		/// </param> 
		public Gtk.Menu CreateMenu (CommandEntrySet entrySet, EventHandler closeHandler)
		{
			return CreateMenu (entrySet, new CommandMenu (this), closeHandler);
		}

		/// <summary>
		/// Creates the menu.
		/// </summary>
		/// <returns>
		/// The menu.
		/// </returns>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		/// <param name='initialTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		public Gtk.Menu CreateMenu (CommandEntrySet entrySet, object initialTarget)
		{
			return CreateMenu (entrySet, initialTarget, null);
		}
		
		/// <summary>
		/// Creates the menu.
		/// </summary>
		/// <returns>
		/// The menu.
		/// </returns>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		/// <param name='initialTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		/// <param name='closeHandler'>
		/// EventHandler to be run when the menu closes
		/// </param> 
		public Gtk.Menu CreateMenu (CommandEntrySet entrySet, object initialTarget, EventHandler closeHandler)
		{
			var menu = (CommandMenu) CreateMenu (entrySet, new CommandMenu (this));
			menu.InitialCommandTarget = initialTarget;
			if (closeHandler != null) {
				menu.Hidden += closeHandler;
			}
			return menu;
		}

		/// <summary>
		/// Shows a context menu.
		/// </summary>
		/// <param name='parent'>
		/// Widget for which the context menu is being shown
		/// </param>
		/// <param name='evt'>
		/// Current event
		/// </param>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		/// <param name='initialCommandTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		public bool ShowContextMenu (Control parent, Gdk.EventButton evt, CommandEntrySet entrySet,
			object initialCommandTarget = null)
		{
			return ShowContextMenu (parent, evt, entrySet, initialCommandTarget, null);
		}

		/// <summary>
		/// Shows a context menu.
		/// </summary>
		/// <param name='parent'>
		/// Widget for which the context menu is being shown
		/// </param>
		/// <param name='evt'>
		/// Current event
		/// </param>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		/// <param name='initialCommandTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		/// <param name='closeHandler'>
		/// An event handler which will be called when the menu closes
		/// </param>
		public bool ShowContextMenu (Control parent, Gdk.EventButton evt, CommandEntrySet entrySet,
			object initialCommandTarget, EventHandler closeHandler)
		{
#if MAC
			var menu = CreateNSMenu (entrySet, initialCommandTarget ?? parent, closeHandler);
			ContextMenuExtensionsMac.ShowContextMenu (parent, evt, menu);
#else
			var menu = CreateMenu (entrySet, closeHandler);
			if (menu != null)
				ShowContextMenu (parent, evt, menu, initialCommandTarget);
#endif
			return true;
		}

		/// <summary>
		/// Shows the context menu.
		/// </summary>
		/// <returns><c>true</c>, if context menu was shown, <c>false</c> otherwise.</returns>
		/// <param name="parent">Widget for which the context menu is shown</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="entrySet">Entry set with the command definitions</param>
		/// <param name="initialCommandTarget">Initial command target.</param>
		public bool ShowContextMenu (Control parent, int x, int y, CommandEntrySet entrySet,
			object initialCommandTarget = null)
		{
#if MAC
			var menu = CreateNSMenu (entrySet, initialCommandTarget ?? parent);
			ContextMenuExtensionsMac.ShowContextMenu (parent, x, y, menu);
#else
			var menu = CreateMenu (entrySet);
			if (menu != null)
				ShowContextMenu (parent, x, y, menu, initialCommandTarget);
#endif

			return true;
		}

		/// <summary>
		/// Shows a context menu.
		/// </summary>
		/// <param name='parent'>
		/// Widget for which the context menu is being shown
		/// </param>
		/// <param name='evt'>
		/// Current event
		/// </param>
		/// <param name='menu'>
		/// Menu to be shown
		/// </param>
		/// <param name='initialCommandTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		public void ShowContextMenu (Control parent, Gdk.EventButton evt, Gtk.Menu menu,
			object initialCommandTarget = null)
		{
			if (menu is CommandMenu) {
				((CommandMenu)menu).InitialCommandTarget = initialCommandTarget ?? parent;
			}
			
			MonoDevelop.Components.GtkWorkarounds.ShowContextMenu (menu, parent, evt);
		}

		public void ShowContextMenu (Control parent, int x, int y, Gtk.Menu menu,
			object initialCommandTarget = null)
		{
			if (menu is CommandMenu) {
				((CommandMenu)menu).InitialCommandTarget = initialCommandTarget ?? parent;
			}

			MonoDevelop.Components.GtkWorkarounds.ShowContextMenu (menu, parent, x, y);
		}

		/// <summary>
		/// Dispatches a command.
		/// </summary>
		/// <returns>
		/// True if a handler for the command was found
		/// </returns>
		/// <param name='commandId'>
		/// Identifier of the command
		/// </param>
		/// <remarks>
		/// This methods tries to execute a command by looking for a handler in the active command route.
		/// </remarks>
		public bool DispatchCommand (object commandId)
		{
			return DispatchCommand (commandId, null, null, CommandSource.Unknown);
		}
		
		/// <summary>
		/// Dispatches a command.
		/// </summary>
		/// <returns>
		/// True if a handler for the command was found
		/// </returns>
		/// <param name='commandId'>
		/// Identifier of the command
		/// </param>
		/// <param name='source'>
		/// What is causing the command to be dispatched
		/// </param>
		public bool DispatchCommand (object commandId, CommandSource source)
		{
			return DispatchCommand (commandId, null, null, source);
		}
		
		/// <summary>
		/// Dispatches a command.
		/// </summary>
		/// <returns>
		/// True if a handler for the command was found
		/// </returns>
		/// <param name='commandId'>
		/// Identifier of the command
		/// </param>
		/// <param name='dataItem'>
		/// Data item for the command. It must be one of the data items obtained by calling GetCommandInfo.
		/// </param>
		public bool DispatchCommand (object commandId, object dataItem)
		{
			return DispatchCommand (commandId, dataItem, null, CommandSource.Unknown);
		}
		
		/// <summary>
		/// Dispatches a command.
		/// </summary>
		/// <returns>
		/// True if a handler for the command was found
		/// </returns>
		/// <param name='commandId'>
		/// Identifier of the command
		/// </param>
		/// <param name='dataItem'>
		/// Data item for the command. It must be one of the data items obtained by calling GetCommandInfo.
		/// </param>
		/// <param name='source'>
		/// What is causing the command to be dispatched
		/// </param>
		public bool DispatchCommand (object commandId, object dataItem, CommandSource source)
		{
			return DispatchCommand (commandId, dataItem, null, source);
		}

		/// <summary>
		/// Dispatches a command.
		/// </summary>
		/// <returns>
		/// True if a handler for the command was found
		/// </returns>
		/// <param name='commandId'>
		/// Identifier of the command
		/// </param>
		/// <param name='dataItem'>
		/// Data item for the command. It must be one of the data items obtained by calling GetCommandInfo.
		/// </param>
		/// <param name='initialTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		public bool DispatchCommand (object commandId, object dataItem, object initialTarget)
		{
			return DispatchCommand (commandId, dataItem, initialTarget, CommandSource.Unknown);
		}
		
		/// <summary>
		/// Dispatches a command.
		/// </summary>
		/// <returns>
		/// True if a handler for the command was found
		/// </returns>
		/// <param name='commandId'>
		/// Identifier of the command
		/// </param>
		/// <param name='dataItem'>
		/// Data item for the command. It must be one of the data items obtained by calling GetCommandInfo.
		/// </param>
		/// <param name='initialTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		/// <param name='source'>
		/// What is causing the command to be dispatched
		/// </param>
		public bool DispatchCommand (object commandId, object dataItem, object initialTarget, CommandSource source)
		{
			return DispatchCommand (commandId, dataItem, initialTarget, source, null, null);
		}

		/// <summary>
		/// Dispatches a command.
		/// </summary>
		/// <returns>
		/// True if a handler for the command was found
		/// </returns>
		/// <param name='commandId'>
		/// Identifier of the command
		/// </param>
		/// <param name='dataItem'>
		/// Data item for the command. It must be one of the data items obtained by calling GetCommandInfo.
		/// </param>
		/// <param name='initialTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		/// <param name='source'>
		/// What is causing the command to be dispatched
		/// </param>
		/// <param name='time'>
		/// The time of the event, if any, that triggered this command
		/// </param>
		public bool DispatchCommand (object commandId, object dataItem, object initialTarget, CommandSource source, uint? time)
		{
			return DispatchCommand (commandId, dataItem, initialTarget, source, time, null);
		}

		internal bool DispatchCommand (object commandId, object dataItem, object initialTarget, CommandSource source, CommandInfo sourceUpdateInfo)
		{
			return DispatchCommand (commandId, dataItem, initialTarget, source, null, sourceUpdateInfo);
		}

		internal bool DispatchCommand (object commandId, object dataItem, object initialTarget, CommandSource source, uint? time, CommandInfo sourceUpdateInfo)
		{
			// (*) Before executing the command, DispatchCommand executes the command update handler to make sure the command is enabled in the given
			// context. This is necessary because the status of the command may have changed since it was last checked (for example, since the menu
			// was shown). In general this is not a problem because command update handlers are fast and cheap. However, it may be a problem
			// for async command update handlers. The sourceUpdateInfo argument can be used in this case to provide the update info that was obtained
			// when checking the status of the command before showing it to the user, so it doesn't need to be queried again.

			// (**) The above special case works when the command is being executed from a menu, because the command update info has already been
			// obtained to build the menu. However in other cases, such as execution through keyboard shortcuts or direct executions of
			// the DispatchCommand method from code, sourceUpdateInfo may not be available. In those cases, if the command update handler is asynchronous,
			// DispatchCommand will *not* wait for the update handler to end, it will use whatever value the handler sets before starting the
			// async operation.

			RegisterUserInteraction ();
			
			if (guiLock > 0)
				return false;

#if MAC
			if (time != null) {
				nint timeVal = 0;

				timeVal = Foundation.NSUserDefaults.StandardUserDefaults.IntForKey ("KeyRepeat") * 25;

				if (time - throttleLastEventTime < timeVal)
					return false;

				throttleLastEventTime = (uint)time;
			}
#endif

			commandId = CommandManager.ToCommandId (commandId);

			List<HandlerCallback> handlers = new List<HandlerCallback> ();
			ActionCommand cmd = null;

			try {
				cmd = GetActionCommand (commandId);
				if (cmd == null)
					return false;

				CurrentCommand = cmd;
				CommandTargetRoute targetRoute = new CommandTargetRoute (initialTarget);
				object cmdTarget = GetFirstCommandTarget (targetRoute);
				CommandInfo info = new CommandInfo (cmd);

				while (cmdTarget != null)
				{
					HandlerTypeInfo typeInfo = GetTypeHandlerInfo (cmdTarget);
					
					bool bypass = false;
					
					CommandUpdaterInfo cui = typeInfo.GetCommandUpdater (commandId);
					if (cui != null) {
						if (sourceUpdateInfo != null && cmdTarget == sourceUpdateInfo.SourceTarget && sourceUpdateInfo.IsUpdatingAsynchronously) {
							// If the source update info was provided and it was part of an asynchronous command update, reuse it to avoid
							// running the asynchronous update again. In other cases, the command update should be fast, so the check will be run again.
							// See (*) above.
							info = sourceUpdateInfo;
						} else if (cmd.CommandArray) {
							// Make sure that the option is still active
							info.ArrayInfo = new CommandArrayInfo (info);
							cui.Run (cmdTarget, info.ArrayInfo);
							info.ArrayInfo.CancelAsyncUpdate (); // See (**) above
							if (!info.ArrayInfo.Bypass) {
								if (info.ArrayInfo.FindCommandInfo (dataItem) == null)
									return false;
							} else
								bypass = true;
						} else {
							info.Bypass = false;
							cui.Run (cmdTarget, info);
							info.CancelAsyncUpdate (); // See (**) above
							bypass = info.Bypass;
							
							if (!bypass && (!info.Enabled || !info.Visible))
								return false;
						}
					}
					
					if (!bypass) {
						CommandHandlerInfo chi = typeInfo.GetCommandHandler (commandId);
						if (chi != null) {
							object localTarget = cmdTarget;
							if (cmd.CommandArray) {
								handlers.Add (delegate {
									OnCommandActivating (commandId, info, dataItem, localTarget, source);
									var t = DateTime.Now;
									try {
										chi.Run (localTarget, cmd, dataItem);
									} finally {
										OnCommandActivated (commandId, info, dataItem, localTarget, source, DateTime.Now - t);
									}
								});
							}
							else {
								handlers.Add (delegate {
									OnCommandActivating (commandId, info, dataItem, localTarget, source);
									var t = DateTime.Now;
									try {
										chi.Run (localTarget, cmd);
									} finally {
										OnCommandActivated (commandId, info, dataItem, localTarget, source, DateTime.Now - t);
									}
								});
							}
							handlerFoundInMulticast = true;
							cmdTarget = NextMulticastTarget (targetRoute);
							if (cmdTarget == null)
								break;
							else
								continue;
						}
					}
					cmdTarget = GetNextCommandTarget (targetRoute, cmdTarget);
				}

				if (handlers.Count > 0) {
					foreach (HandlerCallback c in handlers)
						c ();
					UpdateToolbars ();
					return true;
				}
	
				if (DefaultDispatchCommand (cmd, info, dataItem, cmdTarget, source)) {
					UpdateToolbars ();
					return true;
				}
			}
			catch (Exception ex) {
				string name = (cmd != null && cmd.Text != null && cmd.Text.Length > 0) ? cmd.Text : commandId.ToString ();
				name = name.Replace ("_","");
				ReportError (commandId, "Error while executing command: " + name, ex);
			}
			finally {
				CurrentCommand = null;
			}
			return false;
		}
		
		bool DefaultDispatchCommand (ActionCommand cmd, CommandInfo info, object dataItem, object target, CommandSource source)
		{
			DefaultUpdateCommandInfo (cmd, info);
			info.CancelAsyncUpdate ();
			
			if (cmd.CommandArray) {
				//if (info.ArrayInfo.FindCommandInfo (dataItem) == null)
				//	return false;
			}
			else if (!info.Enabled || !info.Visible)
				return false;
			
			if (cmd.DefaultHandler == null) {
				if (cmd.DefaultHandlerType == null)
					return false;
				cmd.DefaultHandler = (CommandHandler) Activator.CreateInstance (cmd.DefaultHandlerType);
			}
			OnCommandActivating (cmd.Id, info, dataItem, target, source);

			var t = DateTime.Now;
			try {
				cmd.DefaultHandler.InternalRun (dataItem);
			} finally {
				OnCommandActivated (cmd.Id, info, dataItem, target, source, DateTime.Now - t);
			}
			return true;
		}
		
		void OnCommandActivating (object commandId, CommandInfo commandInfo, object dataItem, object target, CommandSource source)
		{
			if (CommandActivating != null)
				CommandActivating (this, new CommandActivationEventArgs (commandId, commandInfo, dataItem, target, source));
		}
		
		void OnCommandActivated (object commandId, CommandInfo commandInfo, object dataItem, object target, CommandSource source, TimeSpan time)
		{
			if (CommandActivated != null)
				CommandActivated (this, new CommandActivationEventArgs (commandId, commandInfo, dataItem, target, source, time));
		}
		
		/// <summary>
		/// Raised just before a command is executed
		/// </summary>
		public event EventHandler<CommandActivationEventArgs> CommandActivating;
		
		/// <summary>
		/// Raised just after a command has been executed
		/// </summary>
		public event EventHandler<CommandActivationEventArgs> CommandActivated;
		
		/// <summary>
		/// Retrieves status information about a command by looking for a handler in the active command route.
		/// </summary>
		/// <returns>
		/// The command information.
		/// </returns>
		/// <param name='commandId'>
		/// Identifier of the command.
		/// </param>
		public CommandInfo GetCommandInfo (object commandId)
		{
			return GetCommandInfo (commandId, new CommandTargetRoute ());
		}

		/// <summary>
		/// Retrieves status information about a command by looking for a handler in the active command route.
		/// </summary>
		/// <returns>
		/// The command information.
		/// </returns>
		/// <param name='commandId'>
		/// Identifier of the command.
		/// </param>
		/// <param name='targetRoute'>
		/// Command route origin
		/// </param>
		public CommandInfo GetCommandInfo (object commandId, CommandTargetRoute targetRoute)
		{
			return GetCommandInfo (commandId, targetRoute, default (CancellationToken));
		}
		
		/// <summary>
		/// Retrieves status information about a command by looking for a handler in the active command route.
		/// </summary>
		/// <returns>
		/// The command information.
		/// </returns>
		/// <param name='commandId'>
		/// Identifier of the command.
		/// </param>
		/// <param name='targetRoute'>
		/// Command route origin
		/// </param>
		public CommandInfo GetCommandInfo (object commandId, CommandTargetRoute targetRoute, CancellationToken cancelToken)
		{
			commandId = CommandManager.ToCommandId (commandId);
			ActionCommand cmd = GetActionCommand (commandId);
			if (cmd == null)
				throw new InvalidOperationException ("Invalid action command id: " + commandId);

			NotifyCommandTargetScanStarted ();
			CommandInfo info = new CommandInfo (cmd);

			try {
				bool multiCastEnabled = true;
				bool multiCastVisible = false;

				CurrentCommand = cmd;

				object cmdTarget = GetFirstCommandTarget (targetRoute);

				while (cmdTarget != null)
				{
					HandlerTypeInfo typeInfo = GetTypeHandlerInfo (cmdTarget);
					CommandUpdaterInfo cui = typeInfo.GetCommandUpdater (commandId);
					
					bool bypass = false;
					bool handlerFound = false;
					
					if (cui != null) {
						if (cmd.CommandArray) {
							info.ArrayInfo = new CommandArrayInfo (info);
							cui.Run (cmdTarget, info.ArrayInfo);
							if (!info.ArrayInfo.Bypass) {
								if (guiLock > 0)
									info.Enabled = false;
								handlerFound = true;
							}
						}
						else {
							info.Bypass = false;
							cui.Run (cmdTarget, info);
							if (!info.Bypass) {
								if (guiLock > 0)
									info.Enabled = false;
								handlerFound = true;
							}
						}
						if (!handlerFound)
							bypass = true;
					}

					if (handlerFound) {
						handlerFoundInMulticast = true;
						if (!info.Enabled || !info.Visible)
							multiCastEnabled = false;
						if (info.Visible)
							multiCastVisible = true;
						cmdTarget = NextMulticastTarget (targetRoute);
						if (cmdTarget == null) {
							if (!multiCastEnabled)
								info.Enabled = false;
							if (multiCastVisible)
								info.Visible = true;
							return info;
						}
						if (info.Enabled && !info.Bypass)
							return info;
						continue;
					}
					else if (!bypass && typeInfo.GetCommandHandler (commandId) != null) {
						info.Enabled = guiLock == 0;
						info.Visible = true;
						
						return info;
					}
					
					cmdTarget = GetNextCommandTarget (targetRoute, cmdTarget);
				}
				
				info.Bypass = false;
				DefaultUpdateCommandInfo (cmd, info);
			}
			catch (Exception ex) {
				if (!commandUpdateErrors.Contains (commandId)) {
					commandUpdateErrors.Add (commandId);
					ReportError (commandId, "Error while updating status of command: " + commandId, ex);
				}
				info.Enabled = false;
				info.Visible = true;
			} finally {
				NotifyCommandTargetScanFinished ();
				CurrentCommand = null;
			}

			if (guiLock > 0)
				info.Enabled = false;
			return info;
		}
		
		void DefaultUpdateCommandInfo (ActionCommand cmd, CommandInfo info)
		{
			if (cmd.DefaultHandler == null) {
				if (cmd.DefaultHandlerType == null) {
					info.Enabled = false;
					if (!cmd.DisabledVisible)
						info.Visible = false;
					return;
				}
				cmd.DefaultHandler = (CommandHandler) Activator.CreateInstance (cmd.DefaultHandlerType);
			}
			if (cmd.CommandArray) {
				info.ArrayInfo = new CommandArrayInfo (info);
				cmd.DefaultHandler.InternalUpdate (info.ArrayInfo);
			}
			else
				cmd.DefaultHandler.InternalUpdate (info);
		}
		
		/// <summary>
		/// Visits the active command route
		/// </summary>
		/// <returns>
		/// Visitor result
		/// </returns>
		/// <param name='visitor'>
		/// Visitor.
		/// </param>
		/// <param name='initialTarget'>
		/// Initial target (provide null to use the default initial target)
		/// </param>
		public object VisitCommandTargets (ICommandTargetVisitor visitor, object initialTarget)
		{
			CommandTargetRoute targetRoute = new CommandTargetRoute (initialTarget);
			object cmdTarget = GetFirstCommandTarget (targetRoute);

			visitor.Start ();

			try {
				while (cmdTarget != null)
				{
					if (visitor.Visit (cmdTarget))
						return cmdTarget;

					cmdTarget = GetNextCommandTarget (targetRoute, cmdTarget);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error while visiting command targets", ex);
			} finally {
				visitor.End ();
			}
			return null;
		}
		
		internal bool DispatchCommandFromAccel (object commandId, object dataItem, object initialTarget)
		{
			// Dispatches a command that has been fired by an accelerator.
			// The difference from a normal dispatch is that there may
			// be several commands bound to the same accelerator, and in
			// this case it will execute the one that is enabled.
			
			// If the original key has been modified
			// by a CommandUpdate handler, it won't work. That's a limitation,
			// but checking all possible commands would be too slow.
			
			Command cmd = GetCommand (commandId);
			if (cmd == null)
				return false;
			
			string accel = cmd.AccelKey;
			KeyBinding binding;
			
			if (accel == null || !KeyBinding.TryParse (accel, out binding))
				return DispatchCommand (commandId, dataItem, initialTarget, CommandSource.Keybinding);
			
			List<Command> list = bindings.Commands (binding);
			if (list == null || list.Count == 1) {
				// The command is not overloaded, so it can be handled normally.
				return DispatchCommand (commandId, dataItem, initialTarget, CommandSource.Keybinding);
			}
			
			CommandTargetRoute targetChain = new CommandTargetRoute (initialTarget);
			
			// Get the accelerator used to fire the command and make sure it has not changed.
			CommandInfo accelInfo = GetCommandInfo (commandId, targetChain);
			bool res = DispatchCommand (commandId, accelInfo.DataItem, initialTarget, CommandSource.Keybinding);

			// If the accelerator has changed, we can't handle overloading.
			if (res || accel != accelInfo.AccelKey)
				return res;
			
			// Execution failed. Now try to execute alternate commands
			// bound to the same key.
			
			for (int i = 0; i < list.Count; i++) {
				if (list[i].Id == commandId) // already handled above.
					continue;
				
				CommandInfo cinfo = GetCommandInfo (list[i].Id, targetChain);
				if (cinfo.AccelKey != accel) // Key changed by a handler, just ignore the command.
					continue;
				
				if (DispatchCommand (list[i].Id, cinfo.DataItem, initialTarget, CommandSource.Keybinding))
					return true;
			}
			
			return false;
		}
		
		internal Gtk.AccelGroup AccelGroup {
			get {
				if (accelGroup == null) {
					accelGroup = new Gtk.AccelGroup ();
				} 
				return accelGroup;
			}
		}
		
		internal void NotifySelected (CommandInfo cmdInfo)
		{
			if (CommandSelected != null) {
				CommandSelectedEventArgs args = new CommandSelectedEventArgs (cmdInfo);
				CommandSelected (this, args);
			}
		}
		
		internal void NotifyDeselected ()
		{
			if (CommandDeselected != null)
				CommandDeselected (this, EventArgs.Empty);
		}
		
		HandlerTypeInfo GetTypeHandlerInfo (object cmdTarget)
		{
			HandlerTypeInfo typeInfo = (HandlerTypeInfo) handlerInfo [cmdTarget.GetType ()];
			if (typeInfo != null) return typeInfo;
			
			Type type = cmdTarget.GetType ();
			typeInfo = new HandlerTypeInfo ();
			
			List<CommandHandlerInfo> handlers = new List<CommandHandlerInfo> ();
			List<CommandUpdaterInfo> updaters = new List<CommandUpdaterInfo> ();
			
			Type curType = type;
			while (curType != null && curType.Assembly != typeof(Gtk.Widget).Assembly && curType.Assembly != typeof(object).Assembly) {
				MethodInfo[] methods = curType.GetMethods (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				foreach (MethodInfo method in methods) {

					ICommandUpdateHandler customHandlerChain = null;
					ICommandArrayUpdateHandler customArrayHandlerChain = null;
					ICommandTargetHandler customTargetHandlerChain = null;
					ICommandArrayTargetHandler customArrayTargetHandlerChain = null;
					int handlersStart = handlers.Count;
					
					foreach (object attr in method.GetCustomAttributes (true)) {
						if (attr is CommandHandlerAttribute)
							handlers.Add(new CommandHandlerInfo (method, (CommandHandlerAttribute)attr));
						else if (attr is CommandUpdateHandlerAttribute)
							AddUpdater (updaters, method, (CommandUpdateHandlerAttribute) attr);
						else {
							customHandlerChain = ChainHandler (customHandlerChain, attr);
							customArrayHandlerChain = ChainHandler (customArrayHandlerChain, attr);
							customTargetHandlerChain = ChainHandler (customTargetHandlerChain, attr);
							customArrayTargetHandlerChain = ChainHandler (customArrayTargetHandlerChain, attr);
						}
					}

					foreach (object attr in type.GetCustomAttributes (true)) {
						customHandlerChain = ChainHandler (customHandlerChain, attr);
						customArrayHandlerChain = ChainHandler (customArrayHandlerChain, attr);
						customTargetHandlerChain = ChainHandler (customTargetHandlerChain, attr);
						customArrayTargetHandlerChain = ChainHandler (customArrayTargetHandlerChain, attr);
					}
					
					if (handlers.Count > handlersStart) {
						if (customHandlerChain != null || customArrayHandlerChain != null) {
							// There are custom handlers. Create update handlers for all commands
							// that the method handles so the custom update handlers can be chained
							for (int i = handlersStart; i < handlers.Count; ++i) {
								CommandUpdaterInfo c = AddUpdateHandler (updaters, handlers[i].CommandId);
								c.AddCustomHandlers (customHandlerChain, customArrayHandlerChain);
							}
						}
						if (customTargetHandlerChain != null || customArrayTargetHandlerChain != null) {
							for (int i = handlersStart; i < handlers.Count; ++i)
								handlers[i].AddCustomHandlers (customTargetHandlerChain, customArrayTargetHandlerChain);
						}
					}
				}
				curType = curType.BaseType;
			}
			
			if (handlers.Count > 0)
				typeInfo.CommandHandlers = handlers.ToArray (); 
			if (updaters.Count > 0)
				typeInfo.CommandUpdaters = updaters.ToArray ();
				 
			handlerInfo [type] = typeInfo;
			return typeInfo;
		}

		CommandUpdaterInfo AddUpdateHandler (List<CommandUpdaterInfo> methodUpdaters, object cmdId)
		{
			foreach (CommandUpdaterInfo ci in methodUpdaters) {
				if (ci.CommandId.Equals (cmdId))
					return ci;
			}
			// Not found, it needs to be added
			CommandUpdaterInfo cinfo = new CommandUpdaterInfo (cmdId);
			methodUpdaters.Add (cinfo);
			return cinfo;
		}

		void AddUpdater (List<CommandUpdaterInfo> methodUpdaters, MethodInfo method, CommandUpdateHandlerAttribute attr)
		{
			var attrCommandId = CommandManager.ToCommandId (attr.CommandId);
			foreach (CommandUpdaterInfo ci in methodUpdaters) {
				if (ci.CommandId.Equals (attrCommandId)) {
					ci.Init (method, attr);
					return;
				}
			}
			// Not found, it needs to be added
			CommandUpdaterInfo cinfo = new CommandUpdaterInfo (method, attr);
			methodUpdaters.Add (cinfo);
		}

		ICommandArrayUpdateHandler ChainHandler (ICommandArrayUpdateHandler chain, object attr)
		{
			ICommandArrayUpdateHandler h = attr as ICommandArrayUpdateHandler;
			if (h == null) return chain;
			h.Next = chain ?? DefaultCommandHandler.Instance;
			return h;
		}

		ICommandUpdateHandler ChainHandler (ICommandUpdateHandler chain, object attr)
		{
			ICommandUpdateHandler h = attr as ICommandUpdateHandler;
			if (h == null) return chain;
			h.Next = chain ?? DefaultCommandHandler.Instance;
			return h;
		}

		ICommandTargetHandler ChainHandler (ICommandTargetHandler chain, object attr)
		{
			ICommandTargetHandler h = attr as ICommandTargetHandler;
			if (h == null) return chain;
			h.Next = chain ?? DefaultCommandHandler.Instance;
			return h;
		}

		ICommandArrayTargetHandler ChainHandler (ICommandArrayTargetHandler chain, object attr)
		{
			ICommandArrayTargetHandler h = attr as ICommandArrayTargetHandler;
			if (h == null) return chain;
			h.Next = chain ?? DefaultCommandHandler.Instance;
			return h;
		}

		Gtk.Window GetCurrentFocusedTopLevelWindow ()
		{
			foreach (var window in topLevelWindows) {
				if (window.HasToplevelFocus)
					return window;
			}
			return rootWidget;
		}
		
		object GetFirstCommandTarget (CommandTargetRoute targetRoute)
		{
			delegatorStack.Clear ();
			visitedTargets.Clear ();
			handlerFoundInMulticast = false;
			object cmdTarget;
			if (targetRoute.InitialTarget != null)
				cmdTarget = targetRoute.InitialTarget;
			else {
				cmdTarget = GetActiveWidget (GetCurrentFocusedTopLevelWindow ());
				if (cmdTarget == null) {
					cmdTarget = globalHandlerChain;
				}
			}
			visitedTargets.Add (cmdTarget);
			return cmdTarget;
		}
		
		object GetNextCommandTarget (CommandTargetRoute targetRoute, object cmdTarget, bool ignoreDelegator = false)
		{
			if (cmdTarget is IMultiCastCommandRouter) 
				cmdTarget = new MultiCastDelegator (this, (IMultiCastCommandRouter)cmdTarget, targetRoute);

			if (!ignoreDelegator && cmdTarget is ICommandDelegator) {
				if (cmdTarget is ICommandDelegatorRouter)
					throw new InvalidOperationException ("A type can't implement both ICommandDelegator and ICommandDelegatorRouter");
				object oldCmdTarget = cmdTarget;
				cmdTarget = ((ICommandDelegator)oldCmdTarget).GetDelegatedCommandTarget ();
				if (cmdTarget != null)
					delegatorStack.Push (oldCmdTarget);
				else
					cmdTarget = GetNextCommandTarget (targetRoute, oldCmdTarget, true);
			}
			else if (cmdTarget is ICommandDelegatorRouter) {
				object oldCmdTarget = cmdTarget;
				cmdTarget = ((ICommandDelegatorRouter)oldCmdTarget).GetDelegatedCommandTarget ();
				if (cmdTarget != null)
					delegatorStack.Push (oldCmdTarget);
				else
					cmdTarget = ((ICommandDelegatorRouter)oldCmdTarget).GetNextCommandTarget ();
			}
			else if (cmdTarget is ICommandRouter)
				cmdTarget = ((ICommandRouter)cmdTarget).GetNextCommandTarget ();
			else if (cmdTarget is Gtk.Widget)
				cmdTarget = ((Gtk.Widget)cmdTarget).Parent;
			#if MAC
			else if (cmdTarget is AppKit.NSView) {
				var v = (AppKit.NSView) cmdTarget;
				if (v.Superview != null && IsRootGdkQuartzView (v.Superview))
					// FIXME: We should get here the GTK parent of the superview. Since there is no api for this
					// right now, we rely on it being set by GetActiveWidget()
					cmdTarget = null;
				else
					cmdTarget = v.Superview;
			}
			#endif
			else
				cmdTarget = null;
			
			if (cmdTarget == null || !visitedTargets.Add (cmdTarget)) {
				while (delegatorStack.Count > 0) {
					var del = delegatorStack.Pop ();
					if (del is ICommandDelegatorRouter)
						cmdTarget = ((ICommandDelegatorRouter)del).GetNextCommandTarget ();
					else
						cmdTarget = GetNextCommandTarget (targetRoute, del, true);
					if (cmdTarget == CommandManager.CommandRouteTerminator)
						return null;
					if (cmdTarget != null)
						return cmdTarget;
				}
				return globalHandlerChain;
			} else
				return cmdTarget;
		}

		internal object NextMulticastTarget (CommandTargetRoute targetRoute)
		{
			while (delegatorStack.Count > 0) {
				MultiCastDelegator del = delegatorStack.Pop () as MultiCastDelegator;
				if (del != null) {
					object cmdTarget = GetNextCommandTarget (targetRoute, del);
					return cmdTarget == globalHandlerChain ? null : cmdTarget;
				}
			}
			return null;
		}
		
		Gtk.Window GetActiveWindow (Gtk.Window win)
		{
			Gtk.Window[] wins = Gtk.Window.ListToplevels ();
			
			bool hasFocus = false;
			bool lastFocusedExists = lastFocused == null;
			Gtk.Window newFocused = null;
			foreach (Gtk.Window w in wins) {
				if (w.Visible) {
					if (w.HasToplevelFocus) {
						hasFocus = true;
						newFocused = w;
					}
					if (w.IsActive && w.Type == Gtk.WindowType.Toplevel && !(w is Gtk.Dialog)) {
						if (win == null)
							win = w;
					}
					if (lastFocused == w) {
						lastFocusedExists = true;
					}
				}
			}

			lastFocused = newFocused;
			UpdateAppFocusStatus (hasFocus, lastFocusedExists);
			
			if (win != null && win.IsRealized) {
				RegisterTopWindow (win);
				return win;
			}
			else
				return null;
		}
		
		object GetActiveWidget (Gtk.Window win)
		{
			win = GetActiveWindow (win);

			Gtk.Widget widget = win;
			if (win != null) {

				#if MAC
				var nw = MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow (win);
				if (nw != null) {
					var v = nw.FirstResponder as AppKit.NSView;
					if (v != null && !IsRootGdkQuartzView (v)) {
						if (IsEmbeddedNSView (v))
							// FIXME: since there is no way to get the parent GTK widget of an embedded NSView,
							// here we return a ICommandDelegatorRouter object that will cause the command route
							// to continue with the active gtk widget once the NSView hierarchy has been inspected.
							return new NSViewCommandRouter { ActiveView = v, ParentWidget = GetFocusedChild (widget) };
						return v;
					}
				}
				#endif

				widget = GetFocusedChild (widget);
			}
			if (widget != lastActiveWidget) {
				if (ActiveWidgetChanged != null)
					ActiveWidgetChanged (this, new ActiveWidgetEventArgs () { OldActiveWidget = lastActiveWidget, NewActiveWidget = widget });
				lastActiveWidget = widget;
			}
			return widget;
		}

		Gtk.Widget GetFocusedChild (Gtk.Widget widget)
		{
			while (widget is Gtk.Container) {
				Gtk.Widget child = ((Gtk.Container)widget).FocusChild;
				if (child != null)
					widget = child;
				else
					break;
			}
			return widget;
		}

		#if MAC
		class NSViewCommandRouter: ICommandDelegatorRouter
		{
			public AppKit.NSView ActiveView;
			public Gtk.Widget ParentWidget;

			public object GetNextCommandTarget ()
			{
				return ParentWidget;
			}

			public object GetDelegatedCommandTarget ()
			{
				return ActiveView;
			}
		}

		bool IsRootGdkQuartzView (AppKit.NSView view)
		{
			return view.ToString ().Contains ("GdkQuartzView");
		}

		bool IsEmbeddedNSView (AppKit.NSView view)
		{
			if (IsRootGdkQuartzView (view))
				return true;
			if (view.Superview != null)
				return IsEmbeddedNSView (view.Superview);
			return false;
		}
		#endif

		bool UpdateStatus ()
		{
			if (!disposed && toolbarUpdaterRunning)
				UpdateToolbars ();
			else {
				toolbarUpdaterRunning = false;
				return false;
			}

			if (appHasFocus) {
				int x, y;
				Gdk.Display.Default.GetPointer (out x, out y);
				if (x != lastX || y != lastY) {
					// Mouse position has changed. The user is interacting.
					lastX = x;
					lastY = y;
					RegisterUserInteraction ();
				}
			}
			
			uint newWait;
			double secs = (DateTime.Now - lastUserInteraction).TotalSeconds;
			if (secs < 10)
				newWait = 500;
			else if (secs < 30)
				newWait = 700;
			else if (appHasFocus)
				newWait = 2000;
			else {
				// The application seems to be idle. Stop the status updater and
				// start a pasive wait for user interaction
				StartWaitingForUserInteraction ();
				return false;
			}
			
			if (newWait != statusUpdateWait && !waitingForUserInteraction) {
				statusUpdateWait = newWait;
				GLib.Timeout.Add (statusUpdateWait, new GLib.TimeoutHandler (UpdateStatus));
				return false;
			}
				
			return true;
		}
		
		bool waitingForUserInteraction;

		void StartStatusUpdater ()
		{
			if (enableToolbarUpdate && !toolbarUpdaterRunning && !waitingForUserInteraction) {
				lastUserInteraction = DateTime.Now;
				// Make sure the first update is done quickly
				statusUpdateWait = 1;
				GLib.Timeout.Add (statusUpdateWait, new GLib.TimeoutHandler (UpdateStatus));
				toolbarUpdaterRunning = true;
			}
		}

		void StopStatusUpdaterIfNeeded ()
		{
			if (toolbars.Count != 0 || visitors.Count != 0)
				return;

			StopStatusUpdater ();
		}
		
		void StopStatusUpdater ()
		{
			EndWaitingForUserInteraction ();
			toolbarUpdaterRunning = false;
		}
		
		void StartWaitingForUserInteraction ()
		{
			// Starts a pasive wait for user interaction.
			// To do it, it subscribes the MotionNotify event
			// of the main window. This event is unsubscribed when motion is detected
			// Keyboard events are already subscribed in RegisterTopWindow
			
			waitingForUserInteraction = true;
			toolbarUpdaterRunning = false;
			foreach (var win in topLevelWindows) {
				win.MotionNotifyEvent += HandleWinMotionNotifyEvent;
				win.FocusInEvent += HandleFocusInEventHandler;
			}
		}
		
		void EndWaitingForUserInteraction ()
		{
			if (!waitingForUserInteraction)
				return;
			waitingForUserInteraction = false;
			foreach (var win in topLevelWindows) {
				win.MotionNotifyEvent -= HandleWinMotionNotifyEvent;
				win.FocusInEvent -= HandleFocusInEventHandler;
			}

			StartStatusUpdater ();
		}
		
		internal void RegisterUserInteraction ()
		{
			if (enableToolbarUpdate) {
				lastUserInteraction = DateTime.Now;
				EndWaitingForUserInteraction ();
			}
		}

		void HandleFocusInEventHandler (object o, Gtk.FocusInEventArgs args)
		{
			RegisterUserInteraction ();
		}

		void HandleWinMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			RegisterUserInteraction ();
		}

		internal DateTime LastUserInteraction {
			get { return lastUserInteraction; }
		}
		
		public void RegisterCommandBar (ICommandBar commandBar)
		{
			if (toolbars.Contains (commandBar))
				return;
			
			toolbars.Add (commandBar);
			StartStatusUpdater ();
			
			commandBar.SetEnabled (guiLock == 0);
			
			object activeWidget = GetActiveWidget (rootWidget);
			commandBar.Update (activeWidget);
		}
		
		public void UnregisterCommandBar (ICommandBar commandBar)
		{
			toolbars.Remove (commandBar);

			StopStatusUpdaterIfNeeded ();
		}
		
		void UpdateToolbars ()
		{
			// This might get called after the app has exited, e.g. after executing the quit command
			// It then queries widgets, which resurrects widget wrappers, which breaks on managed widgets
			if (this.disposed)
				return;
			
			var activeWidget = GetActiveWidget (rootWidget);
			foreach (ICommandBar toolbar in toolbars) {
				toolbar.Update (activeWidget);
			}
			foreach (ICommandTargetVisitor v in visitors)
				VisitCommandTargets (v, null);
		}

		void UpdateAppFocusStatus (bool hasFocus, bool lastFocusedExists)
		{
			if (hasFocus != appHasFocus) {
				// The last focused window has been destroyed. Wait a few ms since another app's window
				// may gain focus again

				DateTime now = DateTime.Now;
				if (focusCheckDelayTimeout == DateTime.MinValue) {
					focusCheckDelayTimeout = now.AddMilliseconds (100);
					return;
				}

				if (now < focusCheckDelayTimeout)
					return;

				focusCheckDelayTimeout = DateTime.MinValue;
				
				appHasFocus = hasFocus;
				if (appHasFocus) {
					if (ApplicationFocusIn != null)
						ApplicationFocusIn (this, EventArgs.Empty);
				} else {
					if (ApplicationFocusOut != null)
						ApplicationFocusOut (this, EventArgs.Empty);
				}
			} else
				focusCheckDelayTimeout = DateTime.MinValue;
		}
		
		public void ReportError (object commandId, string message, Exception ex)
		{
			if (CommandError != null) {
				CommandErrorArgs args = new CommandErrorArgs (commandId, message, ex);
				CommandError (this, args);
			}
		}
		
		public static object ToCommandId (object ob)
		{
			// Include the type name when converting enum members to ids.
			if (ob == null)
				return null;
			else if (ob.GetType ().IsEnum)
				return ob.GetType ().FullName + "." + ob;
			else
				return ob;
		}
		
		void NotifyCommandTargetScanStarted ()
		{
			if (CommandTargetScanStarted != null)
				CommandTargetScanStarted (this, EventArgs.Empty);
		}
		
		void NotifyCommandTargetScanFinished ()
		{
			if (CommandTargetScanFinished != null)
				CommandTargetScanFinished (this, EventArgs.Empty);
		}

		internal bool ApplicationHasFocus {
			get { return appHasFocus; }
		}
		
		/// <summary>
		/// Raised when there is an exception while executing or updating the status of a command
		/// </summary>
		public event CommandErrorHandler CommandError;
		
		/// <summary>
		/// Raised when a command is highligted in a menu
		/// </summary>
		public event EventHandler<CommandSelectedEventArgs> CommandSelected;
		
		/// <summary>
		/// Raised when a command is deselected in a manu
		/// </summary>
		public event EventHandler CommandDeselected;
		
		/// <summary>
		/// Fired when the application gets the focus
		/// </summary>
		internal event EventHandler ApplicationFocusIn;
		
		/// <summary>
		/// Fired when the application loses the focus
		/// </summary>
		internal event EventHandler ApplicationFocusOut;
		
		/// <summary>
		/// Fired when the command route scan starts
		/// </summary>
		public event EventHandler CommandTargetScanStarted;
		
		/// <summary>
		/// Fired when the command route scan ends
		/// </summary>
		public event EventHandler CommandTargetScanFinished;
		
		/// <summary>
		/// Fired when a key is pressed
		/// </summary>
		public event EventHandler<KeyPressArgs> KeyPressed;

		/// <summary>
		/// Occurs when incomplete key is pressed.
		/// </summary>
		public event EventHandler<KeyPressArgs> IncompleteKeyPressed;

		/// <summary>
		/// Occurs when incomplete key is released.
		/// </summary>
		public event EventHandler<KeyPressArgs> IncompleteKeyReleased;

		/// <summary>
		/// Occurs when active widget (the current command target) changes
		/// </summary>
		public event EventHandler<ActiveWidgetEventArgs> ActiveWidgetChanged;
	}


	public class ActiveWidgetEventArgs: EventArgs
	{
		public Control OldActiveWidget { get; internal set; }
		public Control NewActiveWidget { get; internal set; }
	}

	internal class HandlerTypeInfo
	{
		public CommandHandlerInfo[] CommandHandlers;
		public CommandUpdaterInfo[] CommandUpdaters;
		
		public CommandHandlerInfo GetCommandHandler (object commandId)
		{
			if (CommandHandlers == null) return null;
			foreach (CommandHandlerInfo cui in CommandHandlers)
				if (cui.CommandId.Equals (commandId))
					return cui;
			return null;
		}
		
		public CommandUpdaterInfo GetCommandUpdater (object commandId)
		{
			if (CommandUpdaters == null) return null;
			foreach (CommandUpdaterInfo cui in CommandUpdaters)
				if (cui.CommandId.Equals (commandId))
					return cui;
			return null;
		}
	}

	
	internal class CommandMethodInfo
	{
		public object CommandId;
		protected MethodInfo Method;
		
		public CommandMethodInfo (MethodInfo method, CommandMethodAttribute attr)
		{
			Init (method, attr);
		}
		
		protected void Init (MethodInfo method, CommandMethodAttribute attr)
		{
			// Don't assign the method if there is already one assigned (maybe from a subclass)
			if (this.Method == null) {
				this.Method = method;
				CommandId = CommandManager.ToCommandId (attr.CommandId);
			}
		}
		
		public CommandMethodInfo (object commandId)
		{
			CommandId = CommandManager.ToCommandId (commandId);
		}
	}
	
	internal class CommandHandlerInfo: CommandMethodInfo
	{
		ICommandTargetHandler  customHandlerChain;
		ICommandArrayTargetHandler  customArrayHandlerChain;
		
		public CommandHandlerInfo (MethodInfo method, CommandHandlerAttribute attr): base (method, attr)
		{
			ParameterInfo[] pars = method.GetParameters ();
			if (pars.Length > 1)
				throw new InvalidOperationException ("Invalid signature for command handler: " + method.DeclaringType + "." + method.Name + "()");
		}
		
		public void Run (object cmdTarget, Command cmd)
		{
			if (customHandlerChain != null) {
				cmd.HandlerData = Method;
				customHandlerChain.Run (cmdTarget, cmd);
			}
			else
				Method.Invoke (cmdTarget, null);
		}
		
		public void Run (object cmdTarget, Command cmd, object dataItem)
		{
			if (customArrayHandlerChain != null) {
				cmd.HandlerData = Method;
				customArrayHandlerChain.Run (cmdTarget, cmd, dataItem);
			}
			else
				Method.Invoke (cmdTarget, new object[] {dataItem});
		}
		
		public void AddCustomHandlers (ICommandTargetHandler handlerChain, ICommandArrayTargetHandler arrayHandlerChain)
		{
			this.customHandlerChain = handlerChain;
			this.customArrayHandlerChain = arrayHandlerChain;
		}
	}
		
	internal class CommandUpdaterInfo: CommandMethodInfo
	{
		ICommandUpdateHandler customHandlerChain;
		ICommandArrayUpdateHandler customArrayHandlerChain;
		
		bool isArray;
		
		public CommandUpdaterInfo (object commandId): base (commandId)
		{
		}
		
		public CommandUpdaterInfo (MethodInfo method, CommandUpdateHandlerAttribute attr): base (method, attr)
		{
			Init (method, attr);
		}

		public void Init (MethodInfo method, CommandUpdateHandlerAttribute attr)
		{
			base.Init (method, attr);
			ParameterInfo[] pars = method.GetParameters ();
			if (pars.Length > 0 && pars.Length <= 2) {
				if (pars.Length == 2) {
					if (method.ReturnType != typeof (Task) || pars [1].ParameterType != typeof (CancellationToken))
						ReportInvalidSignature (method);
				}
				Type t = pars [0].ParameterType;
				if (t == typeof (CommandArrayInfo)) {
					isArray = true;
					return;
				} else if (t == typeof (CommandInfo))
					return;
			}
			ReportInvalidSignature (method);
		}

		void ReportInvalidSignature (MethodInfo method)
		{
			throw new InvalidOperationException ("Invalid signature for command update handler: " + method.DeclaringType + "." + method.Name + "()");
		}

		public void AddCustomHandlers (ICommandUpdateHandler handlerChain, ICommandArrayUpdateHandler arrayHandlerChain)
		{
			this.customHandlerChain = handlerChain;
			this.customArrayHandlerChain = arrayHandlerChain;
		}
		
		public void Run (object cmdTarget, CommandInfo info)
		{
			if (customHandlerChain != null) {
				info.UpdateHandlerData = Method;

				var sw = Stopwatch.StartNew ();
				customHandlerChain.CommandUpdate (cmdTarget, info);
				sw.Stop ();
				if (sw.ElapsedMilliseconds > CommandManager.SlowCommandWarningTime)
					LoggingService.LogWarning ("Slow command update ({0}ms): Command:{1}, CustomUpdater:{2}, CommandTargetType:{3}", (int)sw.ElapsedMilliseconds, CommandId, customHandlerChain, cmdTarget.GetType ());
			} else {
				if (Method == null)
					throw new InvalidOperationException ("Invalid custom update handler. An implementation of ICommandUpdateHandler was expected.");
				if (isArray)
					throw new InvalidOperationException ("Invalid signature for command update handler: " + Method.DeclaringType + "." + Method.Name + "()");

				var sw = Stopwatch.StartNew ();

				if (Method.ReturnType == typeof (Task)) {
					var t = (Task) Method.Invoke (cmdTarget, new object [] { info, info.AsyncUpdateCancellationToken });
					info.SetUpdateTask (t);
				}
				else
					Method.Invoke (cmdTarget, new object [] { info });

				sw.Stop ();
				if (sw.ElapsedMilliseconds > CommandManager.SlowCommandWarningTime)
					LoggingService.LogWarning ("Slow command update ({0}ms): Command:{1}, Method:{2}, CommandTargetType:{3}", (int)sw.ElapsedMilliseconds, CommandId, Method.DeclaringType + "." + Method.Name, cmdTarget.GetType ());
			}
		}
		
		public void Run (object cmdTarget, CommandArrayInfo info)
		{
			if (customArrayHandlerChain != null) {
				info.UpdateHandlerData = Method;

				var sw = Stopwatch.StartNew ();

				customArrayHandlerChain.CommandUpdate (cmdTarget, info);

				sw.Stop ();
				if (sw.ElapsedMilliseconds > CommandManager.SlowCommandWarningTime)
					LoggingService.LogWarning ("Slow command update ({0}ms): Command:{1}, Method:{2}, CommandTargetType:{3}", (int)sw.ElapsedMilliseconds, CommandId, Method.DeclaringType + "." + Method.Name, cmdTarget.GetType ());
			} else {
				if (Method == null)
					throw new InvalidOperationException ("Invalid custom update handler. An implementation of ICommandArrayUpdateHandler was expected.");
				if (!isArray)
					throw new InvalidOperationException ("Invalid signature for command update handler: " + Method.DeclaringType + "." + Method.Name + "()");

				var sw = Stopwatch.StartNew ();

				if (Method.ReturnType == typeof (Task)) {
					var t = (Task)Method.Invoke (cmdTarget, new object [] { info, info.AsyncUpdateCancellationToken });
					info.SetUpdateTask (t);
				} else
					Method.Invoke (cmdTarget, new object [] { info });

				sw.Stop ();
				if (sw.ElapsedMilliseconds > CommandManager.SlowCommandWarningTime)
					LoggingService.LogWarning ("Slow command update ({0}ms): Command:{1}, Method:{2}, CommandTargetType:{3}", (int)sw.ElapsedMilliseconds, CommandId, Method.DeclaringType + "." + Method.Name, cmdTarget.GetType ());
			}
		}
	}
	
	class DefaultCommandHandler: ICommandUpdateHandler, ICommandArrayUpdateHandler, ICommandTargetHandler, ICommandArrayTargetHandler
	{
		public static DefaultCommandHandler Instance = new DefaultCommandHandler ();
		
		public void CommandUpdate (object target, CommandInfo info)
		{
			MethodInfo mi = (MethodInfo) info.UpdateHandlerData;
			if (mi != null) {
				if (mi.ReturnType == typeof (Task)) {
					var t = (Task) mi.Invoke (target, new object [] { info, info.AsyncUpdateCancellationToken });
					info.SetUpdateTask (t);
				}
				else
					mi.Invoke (target, new object [] { info });
			}
		}
		
		public void CommandUpdate (object target, CommandArrayInfo info)
		{
			MethodInfo mi = (MethodInfo) info.UpdateHandlerData;
			if (mi != null) {
				if (mi.ReturnType == typeof (Task)) {
					var t = (Task)mi.Invoke (target, new object [] { info, info.AsyncUpdateCancellationToken });
					info.SetUpdateTask (t);
				} else
					mi.Invoke (target, new object [] { info });
			}
		}

		public void Run (object target, Command cmd)
		{
			MethodInfo mi = (MethodInfo) cmd.HandlerData;
			if (mi != null)
				mi.Invoke (target, new object[0] );
		}
		
		public void Run (object target, Command cmd, object data)
		{
			MethodInfo mi = (MethodInfo) cmd.HandlerData;
			if (mi != null)
				mi.Invoke (target, new object[] {data} );
		}

		ICommandArrayTargetHandler ICommandArrayTargetHandler.Next {
			get {
				return null;
			}
			set {
			}
		}
		
		ICommandTargetHandler ICommandTargetHandler.Next {
			get {
				return null;
			}
			set {
			}
		}
		
		ICommandArrayUpdateHandler ICommandArrayUpdateHandler.Next {
			get {
				// Last one in the chain
				return null;
			}
			set {
			}
		}
		
		public ICommandUpdateHandler Next {
			get {
				// Last one in the chain
				return null;
			}
			set {
			}
		}
	}

	internal class ToolbarTracker
	{
		Gtk.IconSize lastSize;
		 
		public void Track (Gtk.Toolbar toolbar)
		{
			lastSize = toolbar.IconSize;
			toolbar.AddNotification ("icon-size", IconSizeChanged);
			toolbar.OrientationChanged += HandleToolbarOrientationChanged;
			toolbar.StyleChanged += HandleToolbarStyleChanged;
			
			toolbar.Destroyed += delegate {
				toolbar.StyleChanged -= HandleToolbarStyleChanged;
				toolbar.OrientationChanged -= HandleToolbarOrientationChanged;
				toolbar.RemoveNotification ("icon-size", IconSizeChanged);
			};
		}

		void HandleToolbarStyleChanged (object o, Gtk.StyleChangedArgs args)
		{
			Gtk.Toolbar t = (Gtk.Toolbar) o;
			if (lastSize != t.IconSize)
				UpdateCustomItems (t);
		}

		void HandleToolbarOrientationChanged (object o, Gtk.OrientationChangedArgs args)
		{
			Gtk.Toolbar t = (Gtk.Toolbar) o;
			if (lastSize != t.IconSize)
				UpdateCustomItems (t);
		}

		void IconSizeChanged (object o, GLib.NotifyArgs args)
		{
			this.lastSize = ((Gtk.Toolbar) o).IconSize;
			UpdateCustomItems ((Gtk.Toolbar) o);
		}
		
		void UpdateCustomItems (Gtk.Toolbar t)
		{
			foreach (Gtk.ToolItem ti in t.Children) {
				CustomItem ci = ti.Child as CustomItem;
				if (ci != null)
					ci.SetToolbarStyle (t);
			}
		}
	}

	class MultiCastDelegator: ICommandDelegatorRouter
	{
		IEnumerator enumerator;
		object nextTarget;
		CommandManager manager;
		bool done;
		CommandTargetRoute route;
		
		public MultiCastDelegator (CommandManager manager, IMultiCastCommandRouter mcr, CommandTargetRoute route)
		{
			this.manager = manager;
			enumerator = mcr.GetCommandTargets ().GetEnumerator ();
			this.route = route;
		}
		
		public object GetNextCommandTarget ()
		{
			if (nextTarget != null)
				return this;
			else {
				if (manager.handlerFoundInMulticast)
					return manager.NextMulticastTarget (route);
				else
					return null;
			}
		}
		
		public object GetDelegatedCommandTarget ()
		{
			object currentTarget;
			if (done)
				return null;
			if (nextTarget != null) {
				currentTarget = nextTarget;
				nextTarget = null;
			} else {
				if (enumerator.MoveNext ())
					currentTarget = enumerator.Current;
				else
					return null;
			}
			
			if (enumerator.MoveNext ())
				nextTarget = enumerator.Current;
			else {
				done = true;
				nextTarget = null;
			}

			return currentTarget;
		}
	}

	class CommandTargetChain: ICommandDelegatorRouter
	{
		object target;
		internal CommandTargetChain Next;

		public CommandTargetChain (object target)
		{
			this.target = target;
		}
		
		public object GetNextCommandTarget ()
		{
			if (Next == null)
				return CommandManager.CommandRouteTerminator;
			else
				return Next;
		}
		
		public object GetDelegatedCommandTarget ()
		{
			return target;
		}

		public static CommandTargetChain RemoveTarget (CommandTargetChain chain, object target)
		{
			if (chain == null)
				return null;
			if (chain.target == target)
				return chain.Next;
			else if (chain.Next != null)
				chain.Next = CommandTargetChain.RemoveTarget (chain.Next, target);
			return chain;
		}

		public static CommandTargetChain AddTarget (CommandTargetChain chain, object target)
		{
			if (chain == null)
				return new CommandTargetChain (target);
			else {
				chain.Next = AddTarget (chain.Next, target);
				return chain;
			}
		}
	}

	delegate void HandlerCallback ();
		
	public class CommandActivationEventArgs : EventArgs
	{
		public CommandActivationEventArgs (object commandId, CommandInfo commandInfo, object dataItem, object target, CommandSource source, TimeSpan executionTime = default(TimeSpan))
		{
			this.CommandId = commandId;
			this.CommandInfo = commandInfo;
			this.Target = target;
			this.Source = source;
			this.DataItem = dataItem;
			this.ExecutionTime = executionTime;
		}			
		
		public object CommandId  { get; private set; }
		public CommandInfo CommandInfo  { get; private set; }
		public object Target  { get; private set; }
		public CommandSource Source { get; private set; }
		public object DataItem  { get; private set; }
		public TimeSpan ExecutionTime { get; private set; }
	}
	
	public enum CommandSource
	{
		MainMenu,
		ContextMenu,
		MainToolbar,
		Keybinding,
		Unknown,
		MacroPlayback,
		WelcomePage
	}
	
	public class CommandTargetRoute
	{
		List<object> targets = new List<object> ();
		
		public CommandTargetRoute ()
		{
		}
		
		public CommandTargetRoute (object initialTarget)
		{
			InitialTarget = initialTarget;
		}
		
		public object InitialTarget { get; internal set; }
		
		internal bool Initialized { get; set; }
		
		internal void AddTarget (object obj)
		{
			targets.Add (obj);
		}
		
		internal IEnumerable<object> Targets {
			get { return targets; }
		}
	}
	
	public class KeyPressArgs: EventArgs
	{
		public Gdk.Key Key { get; internal set; }
		public uint KeyValue { get; internal set; }
		public Gdk.ModifierType Modifiers { get; internal set; }
	}
	
	public class KeyBindingFailedEventArgs : EventArgs
	{
		public string Message { get; private set; }
		
		public KeyBindingFailedEventArgs (string message)
		{
			Message = message;
		}
	}
}

