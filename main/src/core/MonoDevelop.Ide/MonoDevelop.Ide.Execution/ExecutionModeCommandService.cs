// 
// ExecutionModeCommandService.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using Mono.Addins;

namespace MonoDevelop.Ide.Execution
{
	public static class ExecutionModeCommandService
	{
		static CustomExecutionModes globalModes;
		
		internal static IEnumerable<ExecutionCommandCustomizer> GetExecutionCommandCustomizers (CommandExecutionContext ctx)
		{
			ExecutionCommand cmd = ctx.GetTargetCommand ();
			if (cmd == null)
				yield break;
			foreach (ExecutionCommandCustomizer customizer in AddinManager.GetExtensionNodes ("/MonoDevelop/Ide/ExecutionCommandEditors", typeof(ExecutionCommandCustomizer))) {
				if (customizer.CanCustomize (cmd))
					yield return customizer;
			}
		}
		
		internal static ExecutionCommandCustomizer GetExecutionCommandCustomizer (string id)
		{
			foreach (ExecutionCommandCustomizer customizer in AddinManager.GetExtensionNodes ("/MonoDevelop/Ide/ExecutionCommandEditors", typeof(ExecutionCommandCustomizer))) {
				if (customizer.Id == id)
					return customizer;
			}
			return null;
		}
		
		public static void GenerateExecutionModeCommands (SolutionEntityItem project, CanExecuteDelegate runCheckDelegate, CommandArrayInfo info)
		{
			CommandExecutionContext ctx = new CommandExecutionContext (project, runCheckDelegate);
			bool supportsParameterization = false;
			
			foreach (List<IExecutionMode> modes in GetExecutionModeCommands (ctx, false, true)) {
				foreach (IExecutionMode mode in modes) {
					CommandInfo ci = info.Add (mode.Name, new CommandItem (ctx, mode));
					if ((mode.ExecutionHandler is ParameterizedExecutionHandler) || ((mode is CustomExecutionMode) && ((CustomExecutionMode)mode).PromptForParameters)) {
						// It will prompt parameters, so we need command to end with '..'.
						// However, some commands may end with '...' already and we don't want to break 
						// already-translated strings by altering them
						if (!ci.Text.EndsWith ("...")) 
							ci.Text += "...";
						supportsParameterization = true;
					} else {
						// The parameters window will be shown if ctrl is pressed
						ci.Description = GettextCatalog.GetString ("Run With: {0}", ci.Text);
						if (SupportsParameterization (mode, ctx)) {
							ci.Description += " - " + GettextCatalog.GetString ("Hold Control key to display the execution parameters dialog.");
							supportsParameterization = true;
						}
					}
				}
				if (info.Count > 0)
					info.AddSeparator ();
			}

			var targets = new List<ExecutionTarget> ();
			if (project != null)
				FlattenExecutionTargets (targets, project.GetExecutionTargets (IdeApp.Workspace.ActiveConfiguration));

			if (targets.Count > 1) {
				foreach (var t in targets) {
					var h = new TargetedExecutionHandler (Runtime.ProcessService.DefaultExecutionHandler, t);
					CommandInfo ci = info.Add (t.FullName, new CommandItem (ctx, new ExecutionMode (t.Id, t.FullName, h)));
					ci.Description = GettextCatalog.GetString ("Run With: {0}", ci.Text);
				}
				info.AddSeparator ();
			}

			if (supportsParameterization) {
				info.AddSeparator ();
				info.Add (GettextCatalog.GetString ("Edit Custom Modes..."), new CommandItem (ctx, null));
			}
		}

		static void FlattenExecutionTargets (List<ExecutionTarget> addToList, IEnumerable<ExecutionTarget> targets)
		{
			foreach (var t in targets) {
				var group = t as ExecutionTargetGroup;
				if (group != null) {
					FlattenExecutionTargets (addToList, group);
				} else {
					addToList.Add (t);
				}
			}
		}
		
		public static IExecutionHandler GetExecutionModeForCommand (object data)
		{
			CommandItem item = (CommandItem) data;
			if (item.Mode == null) {
				using (var dlg = new CustomExecutionModeManagerDialog (item.Context))
					MessageService.ShowCustomDialog (dlg);
				return null;
			}
			
			if (item.Mode.ExecutionHandler is ParameterizedExecutionHandler) {
				ParameterizedExecutionHandler cmode = (ParameterizedExecutionHandler) item.Mode.ExecutionHandler;
				ParameterizedExecutionHandlerWrapper pw = new ParameterizedExecutionHandlerWrapper ();
				pw.Handler = cmode;
				pw.Context = item.Context;
				pw.ParentMode = item.Mode;
				return pw;
			}
			
			// If control key is pressed, show the parameters dialog
			Gdk.ModifierType mtype;
			if (Gtk.Global.GetCurrentEventState (out mtype) && (mtype & Gdk.ModifierType.ControlMask) != 0) {
				RunWithPromptHandler cmode = new RunWithPromptHandler ();
				cmode.Context = item.Context;
				cmode.Mode = item.Mode;
				return cmode;
			}
			
			return item.Mode.ExecutionHandler;
		}
		
		static bool SupportsParameterization (IExecutionMode mode, CommandExecutionContext ctx)
		{
			if (ExecutionModeCommandService.GetExecutionCommandCustomizers (ctx).Any ())
				return true;
			return mode.ExecutionHandler is ParameterizedExecutionHandler;
		}
		
		internal static List<List<IExecutionMode>> GetExecutionModeCommands (CommandExecutionContext ctx, bool includeDefault, bool includeDefaultCustomizer)
		{
			List<List<IExecutionMode>> itemGroups = new List<List<IExecutionMode>> ();
			
			List<CustomExecutionMode> customModes = new List<CustomExecutionMode> (GetCustomModes (ctx));
			
			foreach (IExecutionModeSet mset in Runtime.ProcessService.GetExecutionModes ()) {
				List<IExecutionMode> items = new List<IExecutionMode> ();
				HashSet<string> setModes = new HashSet<string> ();
				foreach (IExecutionMode mode in mset.ExecutionModes) {
					if (!ctx.CanExecute (mode.ExecutionHandler))
						continue;
					setModes.Add (mode.Id);
					if (mode.Id != "Default" || includeDefault)
						items.Add (mode);
					if (mode.Id == "Default" && includeDefaultCustomizer && SupportsParameterization (mode, ctx)) {
						CustomExecutionMode cmode = new CustomExecutionMode ();
						cmode.Mode = mode;
						cmode.Project = ctx.Project;
						cmode.PromptForParameters = true;
						cmode.Name = GettextCatalog.GetString ("Custom Parameters...");
						items.Add (cmode);
					}
				}
				List<CustomExecutionMode> toRemove = new List<CustomExecutionMode> ();
				foreach (CustomExecutionMode cmode in customModes) {
					if (setModes.Contains (cmode.Mode.Id)) {
						if (ctx.CanExecute (cmode.Mode.ExecutionHandler))
							items.Add (cmode);
						toRemove.Add (cmode);
					}
				}
				foreach (CustomExecutionMode cmode in toRemove)
					customModes.Remove (cmode);
				
				if (items.Count > 0)
					itemGroups.Add (items);
			}
			
			if (customModes.Count > 0) {
				List<IExecutionMode> items = new List<IExecutionMode> ();
				foreach (CustomExecutionMode cmode in customModes) {
					if (ctx.CanExecute (cmode.ExecutionHandler))
						items.Add (cmode);
				}
				if (items.Count > 0)
					itemGroups.Add (items);
			}
			return itemGroups;
		}
		
		internal static IEnumerable<CustomExecutionMode> GetCustomModes (CommandExecutionContext ctx)
		{
			if (ctx.Project != null) {
				CustomExecutionModes modes = ctx.Project.UserProperties.GetValue<CustomExecutionModes> ("MonoDevelop.Ide.CustomExecutionModes", GetDataContext ());
				if (modes != null) {
					foreach (CustomExecutionMode mode in modes.Data) {
						mode.Project = ctx.Project;
						if (ctx.CanExecute (mode.ExecutionHandler)) {
							mode.Scope = CustomModeScope.Project;
							yield return mode;
						}
					}
				}
				modes = ctx.Project.ParentSolution.UserProperties.GetValue<CustomExecutionModes> ("MonoDevelop.Ide.CustomExecutionModes", GetDataContext ());
				if (modes != null) {
					foreach (CustomExecutionMode mode in modes.Data) {
						mode.Project = ctx.Project;
						if (ctx.CanExecute (mode.ExecutionHandler)) {
							mode.Scope = CustomModeScope.Solution;
							yield return mode;
						}
					}
				}
			}
			foreach (CustomExecutionMode mode in GetGlobalCustomExecutionModes ().Data) {
				if (ctx.CanExecute (mode.ExecutionHandler)) {
					mode.Scope = CustomModeScope.Global;
					yield return mode;
				}
			}
		}
		
		internal static CustomExecutionMode ShowParamtersDialog (CommandExecutionContext ctx, IExecutionMode mode, CustomExecutionMode currentMode)
		{
			CustomExecutionMode cmode = null;
			
			DispatchService.GuiSyncDispatch (delegate {
				CustomExecutionModeDialog dlg = new CustomExecutionModeDialog ();
				try {
					dlg.Initialize (ctx, mode, currentMode);
					if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
						cmode = dlg.GetConfigurationData ();
						cmode.Project = ctx.Project;
						if (dlg.Save)
							SaveCustomCommand (ctx.Project, cmode);
					}
				} finally {
					dlg.Destroy ();
					dlg.Dispose ();
				}
			});
			return cmode;
		}
		
		internal class CommandItem
		{
			public IExecutionMode Mode;
			public CommandExecutionContext Context;
			
			public CommandItem (CommandExecutionContext context, IExecutionMode mode)
			{
				Context = context;
				Mode = mode;
			}
			
			public override bool Equals (object obj)
			{
				CommandItem other = obj as CommandItem;
				if (other == null)
					return false;
				if (Mode == null || other.Mode == null)
					return Mode == other.Mode;
				return other.Mode.Id == Mode.Id;
			}
			
			public override int GetHashCode ()
			{
				return Mode.Id.GetHashCode ();
			}
		}
		
		internal static void SaveCustomCommand (SolutionEntityItem project, CustomExecutionMode cmode)
		{
			CustomExecutionModes modes = GetCustomExecutionModeList (project, cmode.Scope);
			bool found = false;
			if (!string.IsNullOrEmpty (cmode.Id)) {
				for (int n=0; n<modes.Data.Count; n++) {
					if (modes.Data[n].Id == cmode.Id) {
						modes.Data[n] = cmode;
						found = true;
						break;
					}
				}
			}
			if (!found) {
				cmode.Id = Guid.NewGuid ().ToString ();
				modes.Data.Add (cmode);
			}
			
			if (cmode.Scope == CustomModeScope.Global)
				SaveGlobalCustomExecutionModes ();
			else
				IdeApp.Workspace.SavePreferences ();
		}
		
		static CustomExecutionModes GetCustomExecutionModeList (SolutionEntityItem project, CustomModeScope scope)
		{
			CustomExecutionModes modes;
			if (scope == CustomModeScope.Global) {
				modes = GetGlobalCustomExecutionModes ();
			}
			else {
				PropertyBag props;
				if (scope == CustomModeScope.Project)
					props = project.UserProperties;
				else
					props = project.ParentSolution.UserProperties;
				
				if (props.HasValue ("MonoDevelop.Ide.CustomExecutionModes"))
					modes = props.GetValue<CustomExecutionModes> ("MonoDevelop.Ide.CustomExecutionModes", GetDataContext ());
				else {
					modes = new CustomExecutionModes ();
					props.SetValue<CustomExecutionModes> ("MonoDevelop.Ide.CustomExecutionModes", modes);
				}
			}
			return modes;
		}
		
		internal static void RemoveCustomCommand (SolutionEntityItem project, CustomExecutionMode cmode)
		{
			CustomExecutionModes modes = GetCustomExecutionModeList (project, cmode.Scope);
			modes.Data.Remove (cmode);
			if (cmode.Scope == CustomModeScope.Global)
				SaveGlobalCustomExecutionModes ();
			else
				IdeApp.Workspace.SavePreferences ();
		}
		
		static CustomExecutionModes GetGlobalCustomExecutionModes ()
		{
			if (globalModes == null) {
				try {
					XmlDataSerializer ser = new XmlDataSerializer (GetDataContext ());
					FilePath file = UserProfile.Current.ConfigDir.Combine ("custom-command-modes.xml");
					if (File.Exists (file))
						globalModes = (CustomExecutionModes) ser.Deserialize (file, typeof(CustomExecutionModes));
				} catch (Exception ex) {
					LoggingService.LogError ("Could not load global custom execution modes.", ex);
				}
				
				if (globalModes == null)
					globalModes = new CustomExecutionModes ();
			}
			return globalModes;
		}
		
		static void SaveGlobalCustomExecutionModes ()
		{
			if (globalModes == null)
				return;
			try {
				XmlDataSerializer ser = new XmlDataSerializer (GetDataContext ());
				FilePath file = UserProfile.Current.ConfigDir.Combine ("custom-command-modes.xml");
				ser.Serialize (file, globalModes, typeof(CustomExecutionModes));
			} catch (Exception ex) {
				LoggingService.LogError ("Could not save global custom execution modes.", ex);
			}
		}
		
		static DataContext dataContext = new DataContext ();
		
		static DataContext GetDataContext ()
		{
			foreach (IExecutionModeSet mset in Runtime.ProcessService.GetExecutionModes ()) {
				foreach (IExecutionMode mode in mset.ExecutionModes) {
					if (mode.ExecutionHandler is ParameterizedExecutionHandler)
						dataContext.IncludeType (mode.ExecutionHandler.GetType ());
				}
			}
			foreach (ExecutionCommandCustomizer customizer in AddinManager.GetExtensionNodes ("/MonoDevelop/Ide/ExecutionCommandEditors", typeof(ExecutionCommandCustomizer))) {
				dataContext.IncludeType (customizer.Type);
			}
			return dataContext;
		}
		
		
		public static IExecutionMode GetExecutionMode (CommandExecutionContext ctx, string id)
		{
			foreach (IExecutionMode mode in GetExecutionModes (ctx)) {
				if (mode.Id == id)
					return mode;
			}
			return null;
		}
		
		public static IEnumerable<IExecutionMode> GetExecutionModes (CommandExecutionContext ctx)
		{
			foreach (IExecutionModeSet mset in Runtime.ProcessService.GetExecutionModes ()) {
				foreach (IExecutionMode mode in mset.ExecutionModes) {
					if (ctx.CanExecute (mode.ExecutionHandler))
						yield return mode;
				}
			}
			
			foreach (CustomExecutionMode mode in GetCustomModes (ctx)) {
				if (ctx.CanExecute (mode))
					yield return mode;
			}
		}
	}
	
	class ExecutionCommandCustomizer: TypeExtensionNode, IExecutionCommandCustomizer
	{
		IExecutionCommandCustomizer customizer;
		
		[NodeAttribute ("_name")]
		string name;
		
		protected override void Read (Mono.Addins.NodeElement elem)
		{
			base.Read (elem);
			customizer = (IExecutionCommandCustomizer) GetInstance (typeof(IExecutionCommandCustomizer));
		}

		public bool CanCustomize (ExecutionCommand cmd)
		{
			return customizer.CanCustomize (cmd);
		}
		
		public void Customize (ExecutionCommand cmd, object data)
		{
			customizer.Customize (cmd, data);
		}
		
		public IExecutionConfigurationEditor CreateEditor ()
		{
			return customizer.CreateEditor ();
		}
		
		public string Name {
			get {
				return name;
			}
		}
	}
	
	class RunWithPromptHandler: IExecutionHandler
	{
		public IExecutionMode Mode;
		public CommandExecutionContext Context;
		
		public bool CanExecute (ExecutionCommand command)
		{
			return Mode.ExecutionHandler.CanExecute (command);
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			if (Mode is CustomExecutionMode)
				return ((CustomExecutionMode)Mode).Execute (command, console, true, true);
			else {
				CustomExecutionMode cmode = new CustomExecutionMode ();
				cmode.Mode = Mode;
				cmode.Project = Context.Project;
				cmode.PromptForParameters = true;
				return cmode.ExecutionHandler.Execute (command, console);
			}
		}
	}
	
	class ParameterizedExecutionHandlerWrapper: IExecutionHandler
	{
		public ParameterizedExecutionHandler Handler;
		public CommandExecutionContext Context;
		public IExecutionMode ParentMode;
		
		public bool CanExecute (ExecutionCommand command)
		{
			return Handler.CanExecute (command);
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			return Handler.InternalExecute (Context, ParentMode, command, console);
		}
	}
}
