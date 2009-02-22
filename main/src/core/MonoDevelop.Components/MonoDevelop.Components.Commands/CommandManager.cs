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
using Mono.Addins;

namespace MonoDevelop.Components.Commands
{
	public class CommandManager: IDisposable
	{
		public static CommandManager Main = new CommandManager ();
		Gtk.Window rootWidget;
		KeyBindingManager bindings;
		Gtk.AccelGroup accelGroup;
		string mode;
		
		Dictionary<object,Command> cmds = new Dictionary<object,Command> ();
		Hashtable handlerInfo = new Hashtable ();
		ArrayList toolbars = new ArrayList ();
		CommandTargetChain globalHandlerChain;
		ArrayList commandUpdateErrors = new ArrayList ();
		ArrayList visitors = new ArrayList ();
		Dictionary<Gtk.Window,Gtk.Window> topLevelWindows = new Dictionary<Gtk.Window,Gtk.Window> ();
		Stack delegatorStack = new Stack ();
		bool disposed;
		bool toolbarUpdaterRunning;
		bool enableToolbarUpdate;
		int guiLock;
		
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
		
		public void LoadCommands (string addinPath)
		{
			AddinManager.AddExtensionNodeHandler (addinPath, OnExtensionChange);
		}
		
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
		
		public Gtk.MenuBar CreateMenuBar (string addinPath)
		{
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			return CreateMenuBar (addinPath, cset);
		}
		
		public Gtk.Toolbar[] CreateToolbarSet (string addinPath)
		{
			ArrayList bars = new ArrayList ();
			
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			foreach (CommandEntry ce in cset) {
				CommandEntrySet ces = ce as CommandEntrySet;
				if (ces != null)
					bars.Add (CreateToolbar (addinPath + "/" + ces.Name, ces));
			}
			return (Gtk.Toolbar[]) bars.ToArray (typeof(Gtk.Toolbar));
		}
		
		public Gtk.Toolbar CreateToolbar (string addinPath)
		{
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			return CreateToolbar (addinPath, cset);
		}
		
		public Gtk.Menu CreateMenu (string addinPath)
		{
			CommandEntrySet cset = CreateCommandEntrySet (addinPath);
			return CreateMenu (cset);
		}
		
		public void ShowContextMenu (string addinPath)
		{
			ShowContextMenu (CreateCommandEntrySet (addinPath));
		}
		
		public CommandEntrySet CreateCommandEntrySet (ExtensionContext ctx, string addinPath)
		{
			CommandEntrySet cset = new CommandEntrySet ();
			object[] items = ctx.GetExtensionObjects (addinPath, false);
			foreach (CommandEntry e in items)
				cset.Add (e);
			return cset;
		}
		
		public CommandEntrySet CreateCommandEntrySet (string addinPath)
		{
			CommandEntrySet cset = new CommandEntrySet ();
			object[] items = AddinManager.GetExtensionObjects (addinPath, false);
			foreach (CommandEntry e in items)
				cset.Add (e);
			return cset;
		}
		
		bool CanUseBinding (string mode, string binding)
		{
			if (!bindings.BindingExists (binding))
				return false;
			
			if (mode == null && bindings.ModeExists (binding)) {
				// binding is a simple accel and is registered as a mode... modes take precedence
				return false;
			}
			
			return true;
		}
		
		[GLib.ConnectBefore]
		void OnKeyPressed (object o, Gtk.KeyPressEventArgs e)
		{
			string accel = KeyBindingManager.AccelFromKey (e.Event.Key, e.Event.State);
			
			if (accel == null) {
				// incomplete accel
				e.RetVal = true;
				return;
			}
			
			string binding = KeyBindingManager.Binding (mode, accel);
			List<Command> commands = null;
			
			if (CanUseBinding (mode, binding)) {
				commands = bindings.Commands (binding);
				e.RetVal = true;
				mode = null;
			} else if (mode != null && CanUseBinding (null, accel)) {
				// fall back to accel
				commands = bindings.Commands (accel);
				e.RetVal = true;
				mode = null;
			} else if (bindings.ModeExists (accel)) {
				e.RetVal = true;
				mode = accel;
				return;
			} else {
				e.RetVal = false;
				mode = null;
				return;
			}
			
			for (int i = 0; i < commands.Count; i++) {
				CommandInfo cinfo = GetCommandInfo (commands[i].Id, null);
				if (cinfo.Enabled && cinfo.Visible && DispatchCommand (commands[i].Id))
					return;
			}
			
			e.RetVal = false;
			mode = null;
		}
		
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
		}
		
		public void Dispose ()
		{
			disposed = true;
			bindings.Dispose ();
		}
		
		public void LockAll ()
		{
			guiLock++;
			if (guiLock == 1) {
				foreach (CommandToolbar toolbar in toolbars)
					toolbar.SetEnabled (false);
			}
		}
		
		public void UnlockAll ()
		{
			if (guiLock == 1) {
				foreach (CommandToolbar toolbar in toolbars)
					toolbar.SetEnabled (true);
			}
			
			if (guiLock > 0)
				guiLock--;
		}
		
		public bool EnableIdleUpdate {
			get { return enableToolbarUpdate; }
			set {
				if (enableToolbarUpdate != value) {
					if (value) {
						if (toolbars.Count > 0 || visitors.Count > 0) {
							if (!toolbarUpdaterRunning) {
								GLib.Timeout.Add (500, new GLib.TimeoutHandler (UpdateStatus));
								toolbarUpdaterRunning = true;
							}
						}
					} else {
						toolbarUpdaterRunning = false;
					}
					enableToolbarUpdate = value;
				}
			}
		}
		
		public void RegisterCommand (Command cmd)
		{
			KeyBindingService.StoreDefaultBinding (cmd);
			KeyBindingService.LoadBinding (cmd);
			
			cmds[cmd.Id] = cmd;
			bindings.RegisterCommand (cmd);
		}
		
		public void UnregisterCommand (Command cmd)
		{
			bindings.UnregisterCommand (cmd);
			cmds.Remove (cmd.Id);
		}

		public void LoadUserBindings ()
		{
			foreach (Command cmd in cmds.Values)
				KeyBindingService.LoadBinding (cmd);
		}
		
		public void RegisterGlobalHandler (object handler)
		{
			globalHandlerChain = CommandTargetChain.AddTarget (globalHandlerChain, handler);
		}
		
		public void UnregisterGlobalHandler (object handler)
		{
			globalHandlerChain = CommandTargetChain.RemoveTarget (globalHandlerChain, handler);
		}
		
		public void RegisterCommandTargetVisitor (ICommandTargetVisitor visitor)
		{
			visitors.Add (visitor);
			if (enableToolbarUpdate && !toolbarUpdaterRunning) {
				GLib.Timeout.Add (500, new GLib.TimeoutHandler (UpdateStatus));
				toolbarUpdaterRunning = true;
			}
		}
		
		public void UnregisterCommandTargetVisitor (ICommandTargetVisitor visitor)
		{
			visitors.Remove (visitor);
		}
		
		public Command GetCommand (object cmdId)
		{
			Command cmd;
			if (cmds.TryGetValue (cmdId, out cmd))
				return cmd;
			else
				return null;
		}
		
		public IEnumerable<Command> GetCommands ()
		{
			return cmds.Values;
		}
		
		public ActionCommand GetActionCommand (object cmdId)
		{
			Command cmd;
			if (cmds.TryGetValue (cmdId, out cmd))
				return cmd as ActionCommand;
			else
				return null;
		}
		
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
		
*/		public Gtk.Menu CreateMenu (CommandEntrySet entrySet)
		{
			CommandMenu menu = new CommandMenu (this);
			foreach (CommandEntry entry in entrySet) {
				Gtk.MenuItem mi = entry.CreateMenuItem (this);
				CustomItem ci = mi.Child as CustomItem;
				if (ci != null)
					ci.SetMenuStyle (menu);
				menu.Append (mi);
			}
			return menu;
		}
		
		public void InsertOptions (Gtk.Menu menu, CommandEntrySet entrySet, int index)
		{
			foreach (CommandEntry entry in entrySet) {
				Gtk.MenuItem item = entry.CreateMenuItem (this);
				CustomItem ci = item.Child as CustomItem;
				if (ci != null)
					ci.SetMenuStyle (menu);
				int n = menu.Children.Length;
				menu.Insert (item, index);
				if (item is ICommandUserItem)
					((ICommandUserItem)item).Update (null);
				else
					item.Show ();
				index += menu.Children.Length - n;
			}
		}
		
		public void ShowContextMenu (CommandEntrySet entrySet)
		{
			ShowContextMenu (entrySet, null);
		}
		
		public void ShowContextMenu (CommandEntrySet entrySet, object initialTarget)
		{
			CommandMenu menu = (CommandMenu) CreateMenu (entrySet);
			ShowContextMenu (menu, initialTarget);
		}
		
		public void ShowContextMenu (Gtk.Menu menu)
		{
			menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
		}
		
		public void ShowContextMenu (Gtk.Menu menu, object initialCommandTarget)
		{
			if (menu is CommandMenu) {
				((CommandMenu)menu).InitialCommandTarget = initialCommandTarget;
			}
			ShowContextMenu (menu);
		}
		
		public Gtk.Toolbar CreateToolbar (CommandEntrySet entrySet)
		{
			return CreateToolbar ("", entrySet, null);
		}
		
		public Gtk.Toolbar CreateToolbar (CommandEntrySet entrySet, object initialTarget)
		{
			return CreateToolbar ("", entrySet, initialTarget);
		}
		
		public Gtk.Toolbar CreateToolbar (string id, CommandEntrySet entrySet)
		{
			return CreateToolbar (id, entrySet, null);
		}
		
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
		
		public bool DispatchCommand (object commandId)
		{
			return DispatchCommand (commandId, null, null);
		}
		
		public bool DispatchCommand (object commandId, object dataItem)
		{
			return DispatchCommand (commandId, dataItem, null);
		}

		public bool DispatchCommand (object commandId, object dataItem, object initialTarget)
		{
			if (guiLock > 0)
				return false;

			List<HandlerCallback> handlers = new List<HandlerCallback> ();
			ActionCommand cmd = null;
			try {
				cmd = GetActionCommand (commandId);
				if (cmd == null)
					return false;
				
				object cmdTarget = GetFirstCommandTarget (initialTarget);
				CommandInfo info = new CommandInfo (cmd);

				while (cmdTarget != null)
				{
					HandlerTypeInfo typeInfo = GetTypeHandlerInfo (cmdTarget);
					
					bool bypass = false;
					
					CommandUpdaterInfo cui = typeInfo.GetCommandUpdater (commandId);
					if (cui != null) {
						if (cmd.CommandArray) {
							// Make sure that the option is still active
							CommandArrayInfo ainfo = new CommandArrayInfo (info);
							cui.Run (cmdTarget, ainfo);
							if (!ainfo.Bypass) {
								bool found = false;
								foreach (CommandInfo ci in ainfo) {
									if (Object.Equals (dataItem, ci.DataItem)) {
										found = true;
										break;
									}
								}
								
								if (!found)
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
									chi.Run (localTarget, cmd, dataItem);
								});
							}
							else {
								handlers.Add (delegate {
									chi.Run (localTarget, cmd);
								});
							}
							handlerFoundInMulticast = true;
							cmdTarget = NextMulticastTarget ();
							if (cmdTarget == null)
								break;
							else
								continue;
						}
					}
					cmdTarget = GetNextCommandTarget (cmdTarget);
				}

				if (handlers.Count > 0) {
					foreach (HandlerCallback c in handlers)
						c ();
					UpdateToolbars ();
					return true;
				}
	
				if (cmd.DispatchCommand (dataItem)) {
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
		
		internal CommandInfo GetCommandInfo (object commandId, object initialTarget)
		{
			ActionCommand cmd = GetActionCommand (commandId);
			if (cmd == null)
				throw new InvalidOperationException ("Invalid action command id: " + commandId);
			
			CommandInfo info = new CommandInfo (cmd);
			
			try {
				object cmdTarget = GetFirstCommandTarget (initialTarget);
				bool multiCastEnabled = true;
				bool multiCastVisible = false;
				
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
						cmdTarget = NextMulticastTarget ();
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
					
					cmdTarget = GetNextCommandTarget (cmdTarget);
				}
				
				cmd.UpdateCommandInfo (info);
			}
			catch (Exception ex) {
				if (!commandUpdateErrors.Contains (commandId)) {
					commandUpdateErrors.Add (commandId);
					ReportError (commandId, "Error while updating status of command: " + commandId, ex);
				}
				info.Enabled = false;
				info.Visible = true;
			}
			if (guiLock > 0)
				info.Enabled = false;
			return info;
		}
		
		public object VisitCommandTargets (ICommandTargetVisitor visitor, object initialTarget)
		{
			object cmdTarget = GetFirstCommandTarget (initialTarget);
			
			while (cmdTarget != null)
			{
				if (visitor.Visit (cmdTarget))
					return cmdTarget;

				cmdTarget = GetNextCommandTarget (cmdTarget);
			}
			
			visitor.Visit (null);
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
			if (accel == null)
				return DispatchCommand (commandId, dataItem, initialTarget);
			
			List<Command> list = bindings.Commands (accel);
			if (list == null || list.Count == 1)
				// The command is not overloaded, so it can be handled normally.
				return DispatchCommand (commandId, dataItem, initialTarget);
			
			// Get the accelerator used to fire the command and make sure it has not changed.
			CommandInfo accelInfo = GetCommandInfo (commandId, initialTarget);
			bool res = DispatchCommand (commandId, accelInfo.DataItem, initialTarget);

			// If the accelerator has changed, we can't handle overloading.
			if (res || accel != accelInfo.AccelKey)
				return res;
			
			// Execution failed. Now try to execute alternate commands
			// bound to the same key.
			
			for (int i = 0; i < list.Count; i++) {
				if (list[i].Id == commandId) // already handled above.
					continue;
				
				CommandInfo cinfo = GetCommandInfo (list[i].Id, initialTarget);
				if (cinfo.AccelKey != accel) // Key changed by a handler, just ignore the command.
					continue;
				
				if (DispatchCommand (list[i].Id, cinfo.DataItem, initialTarget))
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
				if (ci.CommandId.Equals (attr.CommandId)) {
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
		
		object GetFirstCommandTarget (object initialTarget)
		{
			delegatorStack.Clear ();
			handlerFoundInMulticast = false;
			if (initialTarget != null)
				return initialTarget;
			object cmdTarget = GetActiveWidget (rootWidget);
			if (cmdTarget != null)
				return cmdTarget;
			else
				return globalHandlerChain;
		}
		
		object GetNextCommandTarget (object cmdTarget)
		{
			if (cmdTarget is IMultiCastCommandRouter) 
				cmdTarget = new MultiCastDelegator (this, (IMultiCastCommandRouter)cmdTarget);
			
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
			
			if (cmdTarget == null) {
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

		internal object NextMulticastTarget ()
		{
			while (delegatorStack.Count > 0) {
				MultiCastDelegator del = delegatorStack.Pop () as MultiCastDelegator;
				if (del != null) {
					object cmdTarget = GetNextCommandTarget (del);
					return cmdTarget == globalHandlerChain ? null : cmdTarget;
				}
			}
			return null;
		}
		
		Gtk.Widget GetActiveWidget (Gtk.Window win)
		{
			Gtk.Window[] wins = Gtk.Window.ListToplevels ();
			
			foreach (Gtk.Window w in wins) {
				if (w.IsActive && w.Visible && w.Type == Gtk.WindowType.Toplevel && !(w is Gtk.Dialog)) {
					win = w;
					break;
				}
			}
			
			if (win != null) {
				RegisterTopWindow (win);
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
			if (!disposed)
				UpdateToolbars ();
			else
				toolbarUpdaterRunning = false;
				
			return toolbarUpdaterRunning;
		}
		
		internal void RegisterToolbar (CommandToolbar toolbar)
		{
			toolbars.Add (toolbar);
			if (enableToolbarUpdate && !toolbarUpdaterRunning) {
				GLib.Timeout.Add (500, new GLib.TimeoutHandler (UpdateStatus));
				toolbarUpdaterRunning = true;
			}
			if (guiLock > 0)
				toolbar.SetEnabled (false);
		}
		
		void UpdateToolbars ()
		{
			object activeWidget = GetActiveWidget (rootWidget);
			foreach (CommandToolbar toolbar in toolbars) {
				if (toolbar.Visible)
					toolbar.Update (activeWidget);
			}
			foreach (ICommandTargetVisitor v in visitors)
				VisitCommandTargets (v, null);
				
		}
		
		public void ReportError (object commandId, string message, Exception ex)
		{
			if (CommandError != null) {
				CommandErrorArgs args = new CommandErrorArgs (commandId, message, ex);
				CommandError (this, args);
			}
		}
		
		public event CommandErrorHandler CommandError;
		public event EventHandler<CommandSelectedEventArgs> CommandSelected;
		public event EventHandler CommandDeselected;
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
			this.Method = method;
			CommandId = attr.CommandId;
		}
		
		public CommandMethodInfo (object commandId)
		{
			CommandId = commandId;
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
			toolbar.AddNotification (OnToolbarPropChanged);
		}
		
		void OnToolbarPropChanged (object ob, GLib.NotifyArgs args)
		{
			Gtk.Toolbar t = (Gtk.Toolbar) ob;
			if (lastSize != t.IconSize || args.Property == "orientation" || args.Property == "toolbar-style")
				UpdateCustomItems (t);
			lastSize = t.IconSize;
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
		
		public MultiCastDelegator (CommandManager manager, IMultiCastCommandRouter mcr)
		{
			this.manager = manager;
			enumerator = mcr.GetCommandTargets ().GetEnumerator ();
		}
		
		public object GetNextCommandTarget ()
		{
			if (nextTarget != null)
				return this;
			else {
				if (manager.handlerFoundInMulticast)
					return manager.NextMulticastTarget ();
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
}

