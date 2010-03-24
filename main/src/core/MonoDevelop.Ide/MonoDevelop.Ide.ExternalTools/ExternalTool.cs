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
using System.Xml;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.ExternalTools
{
	public class ExternalTool
	{
		string menuCommand;
		string command;
		string arguments;
		string initialDirectory;
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
		
#region I/O
		public const string Node = "ExternalTool";
		
		const string menuCommandAttribute        = "menuCommand";
		const string commandAttribute            = "command";
		const string argumentsAttribute          = "arguments";
		const string initialDirectoryAttribute   = "initialDirectory";
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
			result.menuCommand      = reader.GetAttribute (menuCommandAttribute);
			
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