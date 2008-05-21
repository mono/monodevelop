//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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

using Gtk;
using System;
using MonoDevelop.Database.Sql;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;

namespace MonoDevelop.Database.CodeGenerator
{
	public enum ToolCommands
	{
		GenerateDataClasses
	}
	
	public class GenerateDataClassesHandler : CommandHandler
	{
		protected override void Run ()
		{
			GenerateDataClassesDialog dlg = new GenerateDataClassesDialog ();
			try {
				dlg.Run ();
			} finally {
				dlg.Destroy ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			int count = ConnectionContextService.DatabaseConnections.Count;
			Solution combine = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (count == 0 || combine == null) {
				info.Enabled = false;
				return;
			}

			//only enable the command if 1 or more code projects exist 
			foreach (SolutionItem entry in combine.Items) {
				if (!(entry is DotNetProject))
					continue;
				
				DotNetProject proj = (DotNetProject)entry;
				if (proj.LanguageBinding != null) {
					info.Enabled = true;
					break;
				}
			}
		}
	}
}