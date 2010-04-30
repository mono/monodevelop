// 
// CustomToolService.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Ide.Extensions;
using System.Collections.Generic;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects;
using System.IO;
using MonoDevelop.Ide.Tasks;
using System.CodeDom.Compiler;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.CustomTools
{
	public static class CustomToolService
	{
		static Dictionary<string,CustomToolExtensionNode> nodes = new Dictionary<string,CustomToolExtensionNode> ();
		
		static CustomToolService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/CustomTools", delegate(object sender, ExtensionNodeEventArgs args) {
				var node = (CustomToolExtensionNode)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					if (nodes.ContainsKey (node.Name))
						LoggingService.LogError ("Duplicate custom tool name '{0}'", node.Name);
					else
						nodes.Add (node.Name, node);
					break;
				case ExtensionChange.Remove:
					nodes.Remove (node.Name);
					break;
				}
			});
			IdeApp.Workspace.FileChangedInProject += delegate (object sender, ProjectFileEventArgs e) {
				Update (e.ProjectFile);
			};
			IdeApp.Workspace.FilePropertyChangedInProject += delegate (object sender, ProjectFileEventArgs e) {
				Update (e.ProjectFile);
			};
			//FIXME: handle the rename
			//MonoDevelop.Ide.Gui.IdeApp.Workspace.FileRenamedInProject
		}
		
		internal static void Init ()
		{
			//forces static ctor to run
		}
		
		static CustomTool GetGenerator (ProjectFile file)
		{
			CustomToolExtensionNode node;
			if (!string.IsNullOrEmpty (file.Generator) && nodes.TryGetValue (file.Generator, out node))
				return node.Tool;
			return null;
		}
		
		public static void Update (ProjectFile file)
		{
			var tool = GetGenerator (file);
			if (tool == null)
				return;
			ProjectFile genFile = null;
			if (!string.IsNullOrEmpty (file.LastGenOutput))
				genFile = file.Project.Files.GetFile (file.FilePath.ParentDirectory.Combine (file.LastGenOutput));
			
			if (genFile != null && File.Exists (genFile.FilePath) && 
			    File.GetLastWriteTime (file.FilePath) < File.GetLastWriteTime (genFile.FilePath)) {
				return;
			}
			
			CompilerErrorCollection errors = null;
			bool broken = false;
			FilePath genFilePath = FilePath.Null;
			string genFileName = null;
			try {
				errors = tool.Generate (file, out genFileName);
				genFilePath = genFileName;
				if (genFilePath.IsNullOrEmpty) {
					genFileName = null;
				} else {
					genFileName = genFilePath.ToRelative (file.FilePath.ParentDirectory).ToString ();
				}
			} catch (Exception ex) {
				broken = true;
				(errors ?? (errors = new CompilerErrorCollection ())) .Add (
					new CompilerError (file.Name, 0, 0, "",
						string.Format ("The '{0}' code generator crashed: {1}", file.Generator, ex.Message)));
				LoggingService.LogError (string.Format ("The '{0}' code generator crashed: {1}", file.Generator), ex);
			}
			if (string.IsNullOrEmpty (genFileName) ||
				genFileName.IndexOfAny (new char[] { '/', '\\' }) >= 0 ||
				!FileService.IsValidFileName (genFileName))
			{
				broken = true;
				(errors ?? (errors = new CompilerErrorCollection ())).Add (
					new CompilerError (file.Name, 0, 0, "",
						string.Format ("The '{0}' code generator output invalid filename '{1}'", file.Generator, genFileName)));
			}
			
			TaskService.Errors.ClearByOwner (file);
			if (errors.Count > 0) {
				foreach (CompilerError err in errors)
					TaskService.Errors.Add (new Task (file.FilePath, err.ErrorText, err.Column, err.Line,
						                                  err.IsWarning? TaskSeverity.Warning : TaskSeverity.Error,
						                                  TaskPriority.Normal, file.Project.ParentSolution, file));
			}
			if (broken)
				return;
			
			if (!genFilePath.IsNullOrEmpty && File.Exists (genFilePath)) {
				if (genFile == null) {
					genFile = file.Project.AddFile (genFilePath);
				} else if (genFilePath != genFile.FilePath) {
					genFile.Name = genFilePath;
				}
				file.LastGenOutput =  genFileName;
				genFile.DependsOn = file.FilePath.FileName; 
			}
		}
		
		public static void HandleRename (ProjectFileRenamedEventArgs args)
		{
			var file = args.ProjectFile;
			var tool = GetGenerator (file);
			if (tool == null)
				return;
		}
	}
}

