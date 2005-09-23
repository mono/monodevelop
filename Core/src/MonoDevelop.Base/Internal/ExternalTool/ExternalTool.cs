// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Diagnostics;
using System.Xml;

using MonoDevelop.Services;

namespace MonoDevelop.Internal.ExternalTool
{
	/// <summary>
	/// This class describes an external tool, which is a external program
	/// that can be launched from the toolmenu inside Sharp Develop.
	/// </summary>
	internal class ExternalTool
	{
		string menuCommand       = GettextCatalog.GetString ("New Tool");
		string command           = "";
		string arguments         = "";
		string initialDirectory  = "";
		bool   promptForArguments = false;
		bool   useOutputPad       = false;
		
		public string MenuCommand {
			get {
				return menuCommand;
			}
			set {
				menuCommand = value;
				Debug.Assert(menuCommand != null, "MonoDevelop.Internal.ExternalTool.ExternalTool : string MenuCommand == null");
			}
		}
		
		public string Command {
			get {
				return command;
			}
			set {
				command = value;
				Debug.Assert(command != null, "MonoDevelop.Internal.ExternalTool.ExternalTool : string Command == null");
			}
		}
		
		public string Arguments {
			get {
				return arguments;
			}
			set {
				arguments = value;
				Debug.Assert(arguments != null, "MonoDevelop.Internal.ExternalTool.ExternalTool : string Arguments == null");
			}
		}
		
		public string InitialDirectory {
			get {
				return initialDirectory;
			}
			set {
				initialDirectory = value;
				Debug.Assert(initialDirectory != null, "MonoDevelop.Internal.ExternalTool.ExternalTool : string InitialDirectory == null");
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
		
		public ExternalTool() 
		{
		}
		
		public ExternalTool(XmlElement el)
		{
			if (el == null) {
				throw new ArgumentNullException("ExternalTool(XmlElement el) : el can't be null");
			}
			
			if (el["INITIALDIRECTORY"] == null ||
				el["ARGUMENTS"] == null ||
				el["COMMAND"] == null ||
				el["MENUCOMMAND"] == null || 
				el["PROMPTFORARGUMENTS"] == null ||
				el["USEOUTPUTPAD"] == null) {
				throw new Exception("ExternalTool(XmlElement el) : INITIALDIRECTORY and ARGUMENTS and COMMAND and MENUCOMMAND and PROMPTFORARGUMENTS and USEOUTPUTPAD attributes must exist.(check the ExternalTool XML)");
			}
			
			InitialDirectory  = el["INITIALDIRECTORY"].InnerText;
			Arguments         = el["ARGUMENTS"].InnerText;
			Command           = el["COMMAND"].InnerText;
			MenuCommand       = el["MENUCOMMAND"].InnerText;
						
			PromptForArguments = Boolean.Parse(el["PROMPTFORARGUMENTS"].InnerText);
			UseOutputPad       = Boolean.Parse(el["USEOUTPUTPAD"].InnerText);
			
		}
		
		public override string ToString()
		{
			return menuCommand;
		}
		
		public XmlElement ToXmlElement(XmlDocument doc)
		{
			if (doc == null) {
				throw new ArgumentNullException("ExternalTool.ToXmlElement(XmlDocument doc) : doc can not be null");
			}
			
			XmlElement el = doc.CreateElement("TOOL");
			
			XmlElement x = doc.CreateElement("INITIALDIRECTORY");
			x.InnerText = InitialDirectory;
			el.AppendChild(x);
			
			x = doc.CreateElement("ARGUMENTS");
			x.InnerText = Arguments;
			el.AppendChild(x);
			
			x = doc.CreateElement("COMMAND");
			x.InnerText = command;
			el.AppendChild(x);
			
			x = doc.CreateElement("MENUCOMMAND");
			x.InnerText = MenuCommand;
			el.AppendChild(x);
			
			x = doc.CreateElement("PROMPTFORARGUMENTS");
			x.InnerText = PromptForArguments.ToString();
			el.AppendChild(x);

			x = doc.CreateElement("USEOUTPUTPAD");
			x.InnerText = UseOutputPad.ToString();
			el.AppendChild(x);
			
			return el;
		}
	}
}
