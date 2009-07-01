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
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Ide.Execution
{
	public class ExecutionModeCommandService
	{
		public static void GenerateExecutionModeCommands (Project project, CanExecuteDelegate runCheckDelegate, CommandArrayInfo info)
		{
			CommandExecutionContext ctx = new CommandExecutionContext (project, runCheckDelegate);
			
			List<CustomExecutionMode> customModes = new List<CustomExecutionMode> ();
			if (project != null) {
				CustomExecutionModes modes = project.UserProperties.GetValue<CustomExecutionModes> ("MonoDevelop.Ide.CustomExecutionModes", GetDataContext ());
				if (modes != null) {
					foreach (CustomExecutionMode mode in modes.Data) {
						mode.Project = project;
						if (runCheckDelegate (mode.ExecutionHandler))
							customModes.Add (mode);
					}
				}
			}
			
			foreach (IExecutionModeSet mset in Runtime.ProcessService.GetExecutionModes ()) {
				HashSet<IExecutionMode> setModes = new HashSet<IExecutionMode> (mset.ExecutionModes);
				foreach (IExecutionMode mode in mset.ExecutionModes) {
					if (runCheckDelegate (mode.ExecutionHandler))
						info.Add (mode.Name, new CommandItem (ctx, mode));
				}
				List<CustomExecutionMode> toRemove = new List<CustomExecutionMode> ();
				foreach (CustomExecutionMode cmode in customModes) {
					if (setModes.Contains ((IExecutionMode)cmode.Mode)) {
						info.Add (cmode.Name, new CommandItem (ctx, cmode));
						toRemove.Add (cmode);
					}
				}
				foreach (CustomExecutionMode cmode in toRemove)
					customModes.Remove (cmode);
				
				info.AddSeparator ();
			}
			
			if (customModes.Count > 0) {
				info.AddSeparator ();
				foreach (CustomExecutionMode cmode in customModes)
					info.Add (cmode.Name, new CommandItem (ctx, cmode));
			}
		}
		
		public static IExecutionHandler GetExecutionModeForCommand (object data)
		{
			CommandItem item = (CommandItem) data;
			if (item.Mode.ExecutionHandler is ParameterizedExecutionHandler) {
				ParameterizedExecutionHandler cmode = (ParameterizedExecutionHandler) item.Mode.ExecutionHandler;
				ParameterizedExecutionHandlerWrapper pw = new ParameterizedExecutionHandlerWrapper ();
				pw.Handler = cmode;
				pw.Context = item.Context;
				pw.ParentMode = item.Mode;
				return pw;
			}
			return item.Mode.ExecutionHandler;
		}
		
		class CommandItem
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
				return other != null && other.Mode.Id == Mode.Id;
			}
			
			public override int GetHashCode ()
			{
				return Mode.Id.GetHashCode ();
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
		
		internal static void SaveCustomCommand (Project project, IExecutionMode mode, string name, object data)
		{
			CustomExecutionModes modes;
			if (project.UserProperties.HasValue ("MonoDevelop.Ide.CustomExecutionModes"))
				modes = project.UserProperties.GetValue<CustomExecutionModes> ("MonoDevelop.Ide.CustomExecutionModes", GetDataContext ());
			else {
				modes = new CustomExecutionModes ();
				project.UserProperties.SetValue<CustomExecutionModes> ("MonoDevelop.Ide.CustomExecutionModes", modes);
			}
			CustomExecutionMode cmode = new CustomExecutionMode ();
			cmode.Mode = mode;
			cmode.Data = data;
			cmode.Name = name;
			cmode.Id = "__PRJ_" + mode.Id + "_" + (++modes.LastId);
			modes.Data.Add (cmode);
			Ide.Gui.IdeApp.Workspace.SavePreferences ();
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
			if (ctx.Project != null) {
				CustomExecutionModes modes = ctx.Project.UserProperties.GetValue<CustomExecutionModes> ("MonoDevelop.Ide.CustomExecutionModes", GetDataContext ());
				if (modes != null) {
					foreach (CustomExecutionMode mode in modes.Data) {
						if (ctx.CanExecute (mode))
							yield return mode;
					}
				}
			}
		}
		
		class CustomExecutionModes
		{
			[ItemProperty]
			public int LastId;
			
			[ItemProperty]
			[ItemProperty ("Mode", Scope="*")]
			[ExpandedCollection]
			public List<CustomExecutionMode> Data = new List<CustomExecutionMode> ();
		}

		class CustomExecutionMode: IExecutionMode, IExecutionHandler
		{
			[ItemProperty]
			public string Name { get; set; }
			
			[ItemProperty]
			public string Id { get; set; }
			
			[ItemProperty]
			public string ModeId;
			
			[ItemProperty]
			public object Data;
			
			public Project Project;
			
			IExecutionMode mode;
			
			public IExecutionHandler ExecutionHandler {
				get { return this; }
			}
			
			public IExecutionMode Mode {
				get {
					if (mode != null)
						return mode;
					foreach (IExecutionModeSet mset in Runtime.ProcessService.GetExecutionModes ()) {
						foreach (IExecutionMode m in mset.ExecutionModes) {
							if (m.Id == ModeId)
								return mode = m;
						}
					}
					return null;
				}
				set {
					mode = value;
					ModeId = value.Id;
				}
			}
			
			#region IExecutionHandler implementation
			public bool CanExecute (ExecutionCommand command)
			{
				if (Mode != null)
					return Mode.ExecutionHandler.CanExecute (command);
				return false;
			}
			
			public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
			{
				ParameterizedExecutionHandler cmode = Mode.ExecutionHandler as ParameterizedExecutionHandler;
				CommandExecutionContext ctx = new CommandExecutionContext (Project, command);
				return cmode.Execute (command, console, ctx, Data);
			}
			#endregion
		}
	}
}
