// 
// CustomExecutionMode.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using Mono.Addins;
using System.Linq;

namespace MonoDevelop.Ide.Execution
{
	class CustomExecutionModes
	{
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
		public string ModeId { get; set; }
		
		[ItemProperty (FallbackType=typeof(UnknownModeData))]
		public object Data { get; set; }
		
		[ItemProperty ("CommandData")]
		[ItemProperty (Scope="value", FallbackType=typeof(UnknownModeData))]
		Dictionary<string,object> commandData;
		
		[ItemProperty (DefaultValue=false)]
		public bool PromptForParameters { get; set; }
		
		public SolutionEntityItem Project { get; set; }
		
		public CustomModeScope Scope { get; set; }
		
		IExecutionMode mode;
		
		public object GetCommandData (string editorId)
		{
			object data = null;
			if (commandData != null)
				commandData.TryGetValue (editorId, out data);
			return data;
		}
		
		public void SetCommandData (string editorId, object data)
		{
			if (commandData == null)
				commandData = new Dictionary<string, object> ();
			commandData [editorId] = data;
		}
		
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
				return Mode.ExecutionHandler.CanExecute (command)
					&& GetCachedCustomizers ().All (c => c.Item1.CanCustomize (command));
			return false;
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			return Execute (command, console, true, false);
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console, bool allowPrompt, bool forcePrompt)
		{
			if ((PromptForParameters || forcePrompt) && allowPrompt) {
				var ctx = new CommandExecutionContext (Project, command);
				CustomExecutionMode customMode = ExecutionModeCommandService.ShowParamtersDialog (ctx, Mode, this);
				if (customMode == null)
					return new CancelledProcessAsyncOperation ();
				return customMode.Execute (command, console, false, false);
			}

			foreach (var cc in GetCachedCustomizers ()) {
				cc.Item1.Customize (command, cc.Item2);
			}

			var cmode = Mode.ExecutionHandler as ParameterizedExecutionHandler;
			if (cmode != null) {
				CommandExecutionContext ctx = new CommandExecutionContext (Project, command);
				return cmode.Execute (command, console, ctx, Data);
			}

			return Mode.ExecutionHandler.Execute (command, console);
		}
		#endregion

		IList<Tuple<ExecutionCommandCustomizer,object>> cachedCustomizers;

		IList<Tuple<ExecutionCommandCustomizer,object>> GetCachedCustomizers ()
		{
			if (cachedCustomizers != null)
				return cachedCustomizers;

			if (commandData == null)
				return cachedCustomizers = new Tuple<ExecutionCommandCustomizer,object>[0];

			return cachedCustomizers = commandData
					.Select (cmdData => Tuple.Create (
						ExecutionModeCommandService.GetExecutionCommandCustomizer (cmdData.Key),
						cmdData.Value))
					.Where (cc => cc != null)
					.ToList();
		}
	}
		
	class UnknownModeData
	{
	}
	
	enum CustomModeScope
	{
		Project = 0,
		Solution = 1,
		Global = 2
	}
}
