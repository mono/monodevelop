//
// ExternalTool.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Ide.ExternalTools
{
	public class ExternalTool
	{
		string menuCommand;
		string command;
		string arguments;
		string initialDirectory;
		string accelKey;
		bool   promptForArguments;
		bool   useOutputPad = true;
		bool   saveCurrentFile;

		public string MenuCommand {
			get {
				return menuCommand;
			}
			set {
				menuCommand = value;
			}
		}

		public string Command {
			get {
				return command;
			}
			set {
				command = value;
			}
		}
		
		public string Arguments {
			get {
				return arguments;
			}
			set {
				arguments = value;
			}
		}

		public string InitialDirectory {
			get {
				return initialDirectory;
			}
			set {
				initialDirectory = value;
			}
		}

		public string AccelKey {
			get {
				return accelKey;
			}
			set {
				accelKey = value;
			}
		}

		public bool PromptForArguments {
			get {
				return promptForArguments;
			}
			set {
				promptForArguments = value;
			}
		}

		public bool UseOutputPad {
			get {
				return useOutputPad;
			}
			set {
				useOutputPad = value;
			}
		}

		public bool SaveCurrentFile {
			get {
				return saveCurrentFile;
			}
			set {
				saveCurrentFile = value;
			}
		}
		
		public ExternalTool ()
		{
			this.menuCommand = GettextCatalog.GetString ("New Tool");
		}

		internal void Run ()
		{
			string argumentsTool = StringParserService.Parse (Arguments, IdeApp.Workbench.GetStringTagModel ());

			//Save current file checkbox
			if (SaveCurrentFile && IdeApp.Workbench.ActiveDocument != null)
				IdeApp.Workbench.ActiveDocument.Save ();

			if (PromptForArguments) {
				string customerArguments = MessageService.GetTextResponse (GettextCatalog.GetString ("Enter any arguments you want to use while launching tool, {0}:", MenuCommand), GettextCatalog.GetString ("Command Arguments for {0}", MenuCommand), "");
				if (customerArguments != String.Empty)
					argumentsTool = StringParserService.Parse (customerArguments, IdeApp.Workbench.GetStringTagModel ());
			}

			Task.Run (delegate {
				RunExternalTool (this, argumentsTool);
			});
		}

		static void RunExternalTool (ExternalTools.ExternalTool tool, string argumentsTool)
		{
			string commandTool = StringParserService.Parse (tool.Command, IdeApp.Workbench.GetStringTagModel ());
			string initialDirectoryTool = StringParserService.Parse (tool.InitialDirectory, IdeApp.Workbench.GetStringTagModel ());

			//Execute tool
			ProgressMonitor progressMonitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();
			try {
				progressMonitor.Log.WriteLine (GettextCatalog.GetString ("Running: {0} {1}", (commandTool), (argumentsTool)));
				progressMonitor.Log.WriteLine ();

				ProcessWrapper processWrapper;
				if (tool.UseOutputPad)
					processWrapper = Runtime.ProcessService.StartProcess (commandTool, argumentsTool, initialDirectoryTool, progressMonitor.Log, progressMonitor.Log, null);
				else
					processWrapper = Runtime.ProcessService.StartProcess (commandTool, argumentsTool, initialDirectoryTool, null);

				string processName = System.IO.Path.GetFileName (commandTool);
				try {
					processName = processWrapper.ProcessName;
				} catch (SystemException) {
				}

				processWrapper.WaitForOutput ();

				if (processWrapper.ExitCode == 0) {
					progressMonitor.Log.WriteLine (GettextCatalog.GetString ("Process '{0}' has completed succesfully", processName));
				} else {
					progressMonitor.Log.WriteLine (GettextCatalog.GetString ("Process '{0}' has exited with error code {1}", processName, processWrapper.ExitCode));
				}
			} catch (Exception ex) {
				progressMonitor.ReportError (GettextCatalog.GetString ("External program execution failed.\nError while starting:\n '{0} {1}'", commandTool, argumentsTool), ex);
			} finally {
				progressMonitor.Dispose ();
			}
		}
		
#region I/O
		public const string Node = "ExternalTool";
		
		const string menuCommandAttribute        = "menuCommand";
		const string commandAttribute            = "command";
		const string argumentsAttribute          = "arguments";
		const string initialDirectoryAttribute   = "initialDirectory";
		const string accelKeyAttribute           = "accelKey";
		const string promptForArgumentsAttribute = "promptForArguments";
		const string useOutputPadAttribute       = "useOutputPad";
		const string saveCurrentFileAttribute    = "saveCurrentFile";
		
		public void Write (XmlWriter writer)
		{
			writer.WriteStartElement (Node);
			writer.WriteAttributeString (menuCommandAttribute, this.menuCommand);
			writer.WriteAttributeString (commandAttribute, this.command);
			writer.WriteAttributeString (argumentsAttribute, this.arguments);
			writer.WriteAttributeString (initialDirectoryAttribute, this.initialDirectory);
			writer.WriteAttributeString (accelKeyAttribute, this.accelKey);
			writer.WriteAttributeString (promptForArgumentsAttribute, this.promptForArguments.ToString ());
			writer.WriteAttributeString (useOutputPadAttribute, this.useOutputPad.ToString ());
			writer.WriteAttributeString (saveCurrentFileAttribute, this.saveCurrentFile.ToString ());
			writer.WriteEndElement (); // Node
		}
		
		public static ExternalTool Read (XmlReader reader)
		{
			Debug.Assert (reader.LocalName == Node);
			
			ExternalTool result = new ExternalTool ();
			result.menuCommand      = reader.GetAttribute (menuCommandAttribute);
			result.command          = reader.GetAttribute (commandAttribute);
			result.arguments        = reader.GetAttribute (argumentsAttribute);
			result.initialDirectory = reader.GetAttribute (initialDirectoryAttribute);
			result.accelKey         = reader.GetAttribute (accelKeyAttribute);
			
			if (!String.IsNullOrEmpty (reader.GetAttribute (promptForArgumentsAttribute)))
			    result.promptForArguments = Boolean.Parse (reader.GetAttribute (promptForArgumentsAttribute));
			if (!String.IsNullOrEmpty (reader.GetAttribute (useOutputPadAttribute)))
			    result.useOutputPad = Boolean.Parse (reader.GetAttribute (useOutputPadAttribute));
			if (!String.IsNullOrEmpty (reader.GetAttribute (saveCurrentFileAttribute)))
			    result.saveCurrentFile = Boolean.Parse (reader.GetAttribute (saveCurrentFileAttribute));
			
			// Some tag names have changed. Update them now.
			
			result.arguments = UpgradeTags (result.arguments);
			result.initialDirectory = UpgradeTags (result.initialDirectory);
			
			return result;
		}
		
		static string UpgradeTags (string s)
		{
			s = s.Replace ("${ItemPath}","${FilePath}");
			s = s.Replace ("${ItemDir}","${FileDir}");
			s = s.Replace ("${ItemFileName}","${FileName}");
			s = s.Replace ("${ItemExt}","${FileExt}");
			return s;
		}
#endregion
		
	}
}