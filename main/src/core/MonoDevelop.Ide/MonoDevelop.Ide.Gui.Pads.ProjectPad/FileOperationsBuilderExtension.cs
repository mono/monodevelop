// 
// FileOperationsBuilderExtension.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class FileOperationsBuilderExtension: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(IFileItem).IsAssignableFrom (dataType) ||
					typeof(IFolderItem).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(FileOperationsCommandHandler); }
		}
	}
	
	class FileOperationsCommandHandler: NodeCommandHandler
	{
		string GetDir (object ob)
		{
			if (ob is IFolderItem)
				return ((IFolderItem)ob).BaseDirectory;
			else if (ob is IFileItem) {
				string path = ((IFileItem)ob).FileName;
				if (!string.IsNullOrEmpty (path))
					return System.IO.Path.GetDirectoryName (path);
			}
			return string.Empty;
		}
		
		[CommandHandler (FileCommands.OpenContainingFolder)]
		[AllowMultiSelection]
		public void OnOpenFolder ()
		{
			HashSet<string> paths = new HashSet<string> ();
			foreach (ITreeNavigator node in CurrentNodes) {
				string path = GetDir (node.DataItem);
				if (!string.IsNullOrEmpty (path) && paths.Add (path))
					System.Diagnostics.Process.Start ("file://" + path);
			}
		}
		
		[CommandHandler (SearchCommands.FindInFiles)]
		public void OnFindInFiles ()
		{
			string path = GetDir (CurrentNode.DataItem);
			SearchReplaceInFilesManager.ShowFindDialog (path);
		}
		
		public static string TerminalCommand {
			get {
				return PropertyService.Get ("MonoDevelop.Shell", "gnome-terminal");
			}
		}
		
		[CommandHandler (FileCommands.OpenInTerminal)]
		[AllowMultiSelection]
		public void OnOpenInTerminal ()
		{
			HashSet<string> paths = new HashSet<string> ();
			foreach (ITreeNavigator node in CurrentNodes) {
				string path = GetDir (node.DataItem);
				string terminal = TerminalCommand;
				if (paths.Add (path))
					Runtime.ProcessService.StartProcess (terminal, "", path, null);
			}
		}
		
	}
}
