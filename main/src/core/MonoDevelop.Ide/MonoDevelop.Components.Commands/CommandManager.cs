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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Components.Commands.ExtensionNodes;
using Mono.TextEditor;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Components.Commands
{
	public class CommandManager: IDisposable
	{
		public static CommandManager Main = new CommandManager ();
		Gtk.Window rootWidget;
		KeyBindingManager bindings;
		Gtk.AccelGroup accelGroup;
		uint statusUpdateWait = 500;
		DateTime lastUserInteraction;
		KeyboardShortcut[] chords;
		string chord;
		
		Dictionary<object,Command> cmds = new Dictionary<object,Command> ();
		Hashtable handlerInfo = new Hashtable ();
		List<ICommandBar> toolbars = new List<ICommandBar> ();
		CommandTargetChain globalHandlerChain;
		ArrayList commandUpdateErrors = new ArrayList ();
		ArrayList visitors = new ArrayList ();
		Dictionary<Gtk.Window,Gtk.Window> topLevelWindows = new Dictionary<Gtk.Window,Gtk.Window> ();
		Stack delegatorStack = new Stack ();
		
		HashSet<object> visitedTargets = new HashSet<object> ();
		
		bool disposed;
		bool toolbarUpdaterRunning;
		bool enableToolbarUpdate;
		int guiLock;
		
		// Fields used to keep track of the application focus
		bool appHasFocus;
		Gtk.Window lastFocused;
		DateTime focusCheckDelayTimeout = DateTime.MinValue;
		
		internal static readonly object CommandRouteTerminator = new object ();
		
		internal bool handlerFoundInMulticast;
		
		public CommandManager (): this (null)
		{
		}
		
		public CommandManager (Gtk.Window root)
		{
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
		public Gtk.MenuBar CreateMenuBar (string addinPath)
		{
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			return CreateMenuBar (addinPath, cset);
		}
		
		/// <summary>
		/// Creates a set of toolbars from the provided extension path
		/// </summary>
		public Gtk.Toolbar[] CreateToolbarSet (string addinPath)
		{
			ArrayList bars = new ArrayList ();
			
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			foreach (CommandEntry ce in cset) {
				CommandEntrySet ces = ce as CommandEntrySet;
				if (ces != null)
					bars.Add (CreateToolbar (addinPath + "/" + ces.CommandId, ces));
			}
			return (Gtk.Toolbar[]) bars.ToArray (typeof(Gtk.Toolbar));
		}
		
		/// <summary>
		/// Creates a toolbar from the provided extension path
		/// </summary>
		public Gtk.Toolbar CreateToolbar (string addinPath)
		{
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			return CreateToolbar (addinPath, cset);
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
		public void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, string addinPath)
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
		public void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt,
			ExtensionContext ctx, string addinPath)
		{
			ShowContextMenu (parent, evt, CreateCommandEntrySet (ctx, addinPath));
		}
		
		[Obsolete("Use ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ...)")]
		public void ShowContextMenu (string addinPath)
		{
			ShowContextMenu (CreateCommandEntrySet (addinPath));
		}
		
		[Obsolete("Use ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ...)")]
		public void ShowContextMenu (ExtensionContext ctx, string addinPath)
		{
			ShowContextMenu (CreateCommandEntrySet (ctx, addinPath));
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
		
		[GLib.ConnectBefore]
		void OnKeyPressed (object o, Gtk.KeyPressEventArgs e)
		{
			if (!IsEnabled)
				return;
			
			RegisterUserInteraction ();
			
			bool complete;
			KeyboardShortcut[] accels = KeyBindingManager.AccelsFromKey (e.Event, out complete);
			
			if (!complete) {
				// incomplete accel
				e.RetVal = true;
				return;
			}
			
			List<Command> commands = null;
			KeyBinding binding;
			bool isChord;
			
			if (CanUseBinding (chords, accels, out binding, out isChord)) {
				commands = bindings.Commands (binding);
				e.RetVal = true;
				chords = null;
				chord = null;
			} else if (isChord) {
				chord = KeyBindingManager.AccelLabelFromKey (e.Event);
				e.RetVal = true;
				chords = accels;
				return;
			} else if (chords != null) {
				// Note: The user has entered a valid chord but the accel was invalid.
				if (KeyBindingFailed != null) {
					string accel = KeyBindingManager.AccelLabelFromKey (e.Event);
					
					KeyBindingFailed (this, new KeyBindingFailedEventArgs (GettextCatalog.GetString ("The key combination ({0}, {1}) is not a command.", chord, accel)));
				}
				
				e.RetVal = true;
				chords = null;
				chord = null;
				return;
			} else {
				e.RetVal = false;
				chords = null;
				chord = null;
				
				NotifyKeyPressed (e);
				return;
			}
			
			bool bypass = false;
			for (int i = 0; i < commands.Count; i++) {
				CommandInfo cinfo = GetCommandInfo (commands[i].Id, new CommandTargetRoute ());
				if (cinfo.Bypass) {
					bypass = true;
					continue;
				}
				if (cinfo.Enabled && cinfo.Visible && DispatchCommand (commands[i].Id, CommandSource.Keybinding))
					return;
			}

			// The command has not been handled.
			// If there is at least a handler that sets the bypass flag, allow gtk to execute the default action
			
			if (commands.Count > 0 && !bypass) {
				e.RetVal = true;
			} else {
				e.RetVal = false;
				NotifyKeyPressed (e);
			}
			
			chords = null;
		}
		
		void NotifyKeyPressed (Gtk.KeyPressEventArgs e)
		{
			if (KeyPressed != null)
				KeyPressed (this, new KeyPressArgs () { Key = e.Event.Key, Modifiers = e.Event.State });
		}
		
		/// <summary>
		/// Sets the root window. The manager will start the command route at this window, if no other is active.
		/// </summary>
		public void SetRootWindow (Gtk.Window root)
		{
			if (rootWidget != null)
				rootWidget.KeyPressEvent -= OnKeyPressed;
			
			rootWidget = root;
			rootWidget.AddAccelGroup (AccelGroup);
			RegisterTopWindow (rootWidget);
		}
		
		void RegisterTopWindow (Gtk.Window win)
		{
			if (!topLevelWindows.ContainsKey (win)) {
				topLevelWindows.Add (win, win);
				win.KeyPressEvent += OnKeyPressed;
				win.Destroyed += TopLevelDestroyed;
			}
		}
		
		void TopLevelDestroyed (object o, EventArgs args)
		{
			Gtk.Window w = (Gtk.Window) o;
			w.Destroyed -= TopLevelDestroyed;
			w.KeyPressEvent -= OnKeyPressed;
			topLevelWindows.Remove (w);
			if (w == lastFocused)
				lastFocused = null;
			RegisterUserInteraction ();
		}
		
		public void Dispose ()
		{
			disposed = true;
			bindings.Dispose ();
			lastFocused = null;
		}
		
		/// <summary>
		/// Disables all commands
		/// </summary>
		public void LockAll ()
		{
			guiLock++;
			if (guiLock == 1) {
				foreach (ICommandBar toolbar in toolbars)
					toolbar.SetEnabled (false);
			}
		}
		
		/// <summary>
		/// Unlocks the command manager
		/// </summary>
		public void UnlockAll ()
		{
			if (guiLock == 1) {
				foreach (ICommandBar toolbar in toolbars)
					toolbar.SetEnabled (true);
			}
			
			if (guiLock > 0)
				guiLock--;
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
		public Gtk.MenuBar CreateMenuBar (string name, CommandEntrySet entrySet)
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
		public Gtk.Menu CreateMenu (CommandEntrySet entrySet, CommandMenu menu)
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
			var menu = (CommandMenu) CreateMenu (entrySet, new CommandMenu (this));
			menu.InitialCommandTarget = initialTarget;
			return menu;
		}
		
		[Obsolete("Unused. To be removed")]
		public void InsertOptions (Gtk.Menu menu, CommandEntrySet entrySet, int index)
		{
			CommandTargetRoute route = new CommandTargetRoute ();
			foreach (CommandEntry entry in entrySet) {
				Gtk.MenuItem item = entry.CreateMenuItem (this);
				CustomItem ci = item.Child as CustomItem;
				if (ci != null)
					ci.SetMenuStyle (menu);
				int n = menu.Children.Length;
				menu.Insert (item, index);
				if (item is ICommandUserItem)
					((ICommandUserItem)item).Update (route);
				else
					item.Show ();
				index += menu.Children.Length - n;
			}
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
		public void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, CommandEntrySet entrySet,
			object initialCommandTarget = null)
		{
			var menu = CreateMenu (entrySet);
			if (menu != null)
				ShowContextMenu (parent, evt, menu, initialCommandTarget);
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
		public void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, Gtk.Menu menu,
			object initialCommandTarget = null)
		{
			if (menu is CommandMenu) {
				((CommandMenu)menu).InitialCommandTarget = initialCommandTarget ?? parent;
			}
			
			Mono.TextEditor.GtkWorkarounds.ShowContextMenu (menu, parent, evt);
		}
		
		[Obsolete ("Use ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ...)")]
		public void ShowContextMenu (Gtk.Menu menu, object initialCommandTarget, Gdk.EventButton evt)
		{
			if (menu is CommandMenu) {
				((CommandMenu)menu).InitialCommandTarget = initialCommandTarget;
			}
			ShowContextMenu (null, evt, menu, initialCommandTarget);
		}
		
		[Obsolete ("Use ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ...)")]
		public void ShowContextMenu (CommandEntrySet entrySet)
		{
			ShowContextMenu (entrySet, null);
		}
		
		[Obsolete ("Use ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ...)")]
		public void ShowContextMenu (CommandEntrySet entrySet, object initialTarget)
		{
			ShowContextMenu (CreateMenu (entrySet, initialTarget));
		}
		
		[Obsolete ("Use ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ...)")]
		public void ShowContextMenu (Gtk.Menu menu)
		{
			ShowContextMenu (menu, null, (Gdk.EventButton) null);
		}
		
		[Obsolete ("Use ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ...)")]
		public void ShowContextMenu (Gtk.Menu menu, object initialCommandTarget)
		{
			ShowContextMenu (menu, initialCommandTarget, null);
		}
		
		/// <summary>
		/// Creates a toolbar.
		/// </summary>
		/// <returns>
		/// The toolbar.
		/// </returns>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		public Gtk.Toolbar CreateToolbar (CommandEntrySet entrySet)
		{
			return CreateToolbar ("", entrySet, null);
		}
		
		/// <summary>
		/// Creates a toolbar.
		/// </summary>
		/// <returns>
		/// The toolbar.
		/// </returns>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		/// <param name='initialTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		public Gtk.Toolbar CreateToolbar (CommandEntrySet entrySet, object initialTarget)
		{
			return CreateToolbar ("", entrySet, initialTarget);
		}
		
		/// <summary>
		/// Creates a toolbar.
		/// </summary>
		/// <returns>
		/// The toolbar.
		/// </returns>
		/// <param name='id'>
		/// Identifier of the toolbar
		/// </param>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		public Gtk.Toolbar CreateToolbar (string id, CommandEntrySet entrySet)
		{
			return CreateToolbar (id, entrySet, null);
		}
		
		/// <summary>
		/// Creates a toolbar.
		/// </summary>
		/// <returns>
		/// The toolbar.
		/// </returns>
		/// <param name='id'>
		/// Identifier of the toolbar
		/// </param>
		/// <param name='entrySet'>
		/// Entry with the command definitions
		/// </param>
		/// <param name='initialTarget'>
		/// Initial command route target. The command handler will start looking for command handlers in this object.
		/// </param>
		public Gtk.Toolbar CreateToolbar (string id, CommandEntrySet entrySet, object initialTarget)
		{
			CommandToolbar toolbar = new CommandToolbar (this, id, entrySet.Name);
			toolbar.InitialCommandTarget = initialTarget;
			
			foreach (CommandEntry entry in entrySet) {
				Gtk.ToolItem ti = entry.CreateToolItem (this);
				CustomItem ci = ti.Child as CustomItem;
				if (ci != null)
					ci.SetToolbarStyle (toolbar);
				toolbar.Add (ti);
			}
			ToolbarTracker tt = new ToolbarTracker ();
			tt.Track (toolbar);
			return toolbar;
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
			RegisterUserInteraction ();
			
			if (guiLock > 0)
				return false;

			commandId = CommandManager.ToCommandId (commandId);
			
			List<HandlerCallback> handlers = new List<HandlerCallback> ();
			ActionCommand cmd = null;
			try {
				cmd = GetActionCommand (commandId);
				if (cmd == null)
					return false;
				
				CommandTargetRoute targetRoute = new CommandTargetRoute (initialTarget);
				object cmdTarget = GetFirstCommandTarget (targetRoute);
				CommandInfo info = new CommandInfo (cmd);

				while (cmdTarget != null)
				{
					HandlerTypeInfo typeInfo = GetTypeHandlerInfo (cmdTarget);
					
					bool bypass = false;
					
					CommandUpdaterInfo cui = typeInfo.GetCommandUpdater (commandId);
					if (cui != null) {
						if (cmd.CommandArray) {
							// Make sure that the option is still active
							info.ArrayInfo = new CommandArrayInfo (info);
							cui.Run (cmdTarget, info.ArrayInfo);
							if (!info.ArrayInfo.Bypass) {
								if (info.ArrayInfo.FindCommandInfo (dataItem) == null)
									return false;
							} else
								bypass = true;
						} else {
							info.Bypass = false;
							cui.Run (cmdTarget, info);
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
									chi.Run (localTarget, cmd, dataItem);
									OnCommandActivated (commandId, info, dataItem, localTarget, source);
								});
							}
							else {
								handlers.Add (delegate {
									OnCommandActivating (commandId, info, dataItem, localTarget, source);
									chi.Run (localTarget, cmd);
									OnCommandActivated (commandId, info, dataItem, localTarget, source);
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
			return false;
		}
		
		bool DefaultDispatchCommand (ActionCommand cmd, CommandInfo info, object dataItem, object target, CommandSource source)
		{
			DefaultUpdateCommandInfo (cmd, info);
			
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
			cmd.DefaultHandler.InternalRun (dataItem);
			OnCommandActivated (cmd.Id, info, dataItem, target, source);
			return true;
		}
		
		void OnCommandActivating (object commandId, CommandInfo commandInfo, object dataItem, object target, CommandSource source)
		{
			if (CommandActivating != null)
				CommandActivating (this, new CommandActivationEventArgs (commandId, commandInfo, dataItem, target, source));
		}
		
		void OnCommandActivated (object commandId, CommandInfo commandInfo, object dataItem, object target, CommandSource source)
		{
			if (CommandActivated != null)
				CommandActivated (this, new CommandActivationEventArgs (commandId, commandInfo, dataItem, target, source));
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
			commandId = CommandManager.ToCommandId (commandId);
			ActionCommand cmd = GetActionCommand (commandId);
			if (cmd == null)
				throw new InvalidOperationException ("Invalid action command id: " + commandId);

			NotifyCommandTargetScanStarted ();
			CommandInfo info = new CommandInfo (cmd);
			
			try {
				bool multiCastEnabled = true;
				bool multiCastVisible = false;
				
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
					List<CommandHandlerInfo> methodHandlers = new List<CommandHandlerInfo> ();
					
					foreach (object attr in method.GetCustomAttributes (true)) {
						if (attr is CommandHandlerAttribute)
							methodHandlers.Add (new CommandHandlerInfo (method, (CommandHandlerAttribute) attr));
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
					
					if (methodHandlers.Count > 0) {
						if (customHandlerChain != null || customArrayHandlerChain != null) {
							// There are custom handlers. Create update handlers for all commands
							// that the method handles so the custom update handlers can be chained
							foreach (CommandHandlerInfo ci in methodHandlers) {
								CommandUpdaterInfo c = AddUpdateHandler (updaters, ci.CommandId);
								c.AddCustomHandlers (customHandlerChain, customArrayHandlerChain);
							}
						}
						if (customTargetHandlerChain != null || customArrayTargetHandlerChain != null) {
							foreach (CommandHandlerInfo ci in methodHandlers)
								ci.AddCustomHandlers (customTargetHandlerChain, customArrayTargetHandlerChain);
						}
					}
					handlers.AddRange (methodHandlers);
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
			foreach (CommandUpdaterInfo ci in methodUpdaters) {
				if (ci.CommandId.Equals (CommandManager.ToCommandId (attr.CommandId))) {
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
		
		object GetFirstCommandTarget (CommandTargetRoute targetRoute)
		{
			delegatorStack.Clear ();
			visitedTargets.Clear ();
			handlerFoundInMulticast = false;
			object cmdTarget;
			if (targetRoute.InitialTarget != null)
				cmdTarget = targetRoute.InitialTarget;
			else {
				cmdTarget = GetActiveWidget (rootWidget);
				if (cmdTarget == null) {
					cmdTarget = globalHandlerChain;
				}
			}
			visitedTargets.Add (cmdTarget);
			return cmdTarget;
		}
		
		object GetNextCommandTarget (CommandTargetRoute targetRoute, object cmdTarget)
		{
			if (cmdTarget is IMultiCastCommandRouter) 
				cmdTarget = new MultiCastDelegator (this, (IMultiCastCommandRouter)cmdTarget, targetRoute);
			
			if (cmdTarget is ICommandDelegatorRouter) {
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
			else
				cmdTarget = null;
			
			if (cmdTarget == null || !visitedTargets.Add (cmdTarget)) {
				if (delegatorStack.Count > 0) {
					ICommandDelegatorRouter del = (ICommandDelegatorRouter) delegatorStack.Pop ();
					cmdTarget = del.GetNextCommandTarget ();
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
		
		Gtk.Widget GetActiveWidget (Gtk.Window win)
		{
			win = GetActiveWindow (win);
			if (win != null) {
				Gtk.Widget widget = win;
				while (widget is Gtk.Container) {
					Gtk.Widget child = ((Gtk.Container)widget).FocusChild;
					if (child != null)
						widget = child;
					else
						break;
				}
				return widget;
			}
			return win;
		}
		
		bool UpdateStatus ()
		{
			if (!disposed && toolbarUpdaterRunning)
				UpdateToolbars ();
			else {
				toolbarUpdaterRunning = false;
				return false;
			}
			
			uint newWait;
			double secs = (DateTime.Now - lastUserInteraction).TotalSeconds;
			if (secs < 10)
				newWait = 500;
			else if (secs < 30)
				newWait = 700;
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
		Gtk.Window suspendedActiveWindow;
		
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
			Gtk.Window win = GetActiveWindow (rootWidget);
			suspendedActiveWindow = win;
			if (win != null) {
				win.MotionNotifyEvent += HandleWinMotionNotifyEvent;
				win.Destroyed += HandleWinDestroyed;
			}
		}
		
		void EndWaitingForUserInteraction ()
		{
			if (!waitingForUserInteraction)
				return;
			waitingForUserInteraction = false;
			if (suspendedActiveWindow != null) {
				suspendedActiveWindow.MotionNotifyEvent -= HandleWinMotionNotifyEvent;
				suspendedActiveWindow.Destroyed -= HandleWinDestroyed;
				suspendedActiveWindow = null;
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

		void HandleWinDestroyed (object sender, EventArgs e)
		{
			suspendedActiveWindow = null;
		}
		
		void HandleWinMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			RegisterUserInteraction ();
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
		}
		
		void UpdateToolbars ()
		{
			// This might get called after the app has exited, e.g. after executing the quit command
			// It then queries widgets, which resurrects widget wrappers, which breaks on managed widgets
			if (this.disposed)
				return;
			
			object activeWidget = GetActiveWidget (rootWidget);
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
			if (pars.Length == 1) {
				Type t = pars[0].ParameterType;
				
				if (t == typeof(CommandArrayInfo)) {
					isArray = true;
					return;
				} else if (t == typeof(CommandInfo))
					return;
			}
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
				customHandlerChain.CommandUpdate (cmdTarget, info);
			} else {
				if (Method == null)
					throw new InvalidOperationException ("Invalid custom update handler. An implementation of ICommandUpdateHandler was expected.");
				if (isArray)
					throw new InvalidOperationException ("Invalid signature for command update handler: " + Method.DeclaringType + "." + Method.Name + "()");
				Method.Invoke (cmdTarget, new object[] {info} );
			}
		}
		
		public void Run (object cmdTarget, CommandArrayInfo info)
		{
			if (customArrayHandlerChain != null) {
				info.UpdateHandlerData = Method;
				customArrayHandlerChain.CommandUpdate (cmdTarget, info);
			} else {
				if (Method == null)
					throw new InvalidOperationException ("Invalid custom update handler. An implementation of ICommandArrayUpdateHandler was expected.");
				if (!isArray)
					throw new InvalidOperationException ("Invalid signature for command update handler: " + Method.DeclaringType + "." + Method.Name + "()");
				Method.Invoke (cmdTarget, new object[] {info} );
			}
		}
	}
	
	class DefaultCommandHandler: ICommandUpdateHandler, ICommandArrayUpdateHandler, ICommandTargetHandler, ICommandArrayTargetHandler
	{
		public static DefaultCommandHandler Instance = new DefaultCommandHandler ();
		
		public void CommandUpdate (object target, CommandInfo info)
		{
			MethodInfo mi = (MethodInfo) info.UpdateHandlerData;
			if (mi != null)
				mi.Invoke (target, new object[] {info} );
		}
		
		public void CommandUpdate (object target, CommandArrayInfo info)
		{
			MethodInfo mi = (MethodInfo) info.UpdateHandlerData;
			if (mi != null)
				mi.Invoke (target, new object[] {info} );
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
		public CommandActivationEventArgs (object commandId, CommandInfo commandInfo, object dataItem, object target, CommandSource source)
		{
			this.CommandId = commandId;
			this.CommandInfo = commandInfo;
			this.Target = target;
			this.Source = source;
			this.DataItem = dataItem;
		}			
		
		public object CommandId  { get; private set; }
		public CommandInfo CommandInfo  { get; private set; }
		public object Target  { get; private set; }
		public CommandSource Source { get; private set; }
		public object DataItem  { get; private set; }
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

