// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

using MonoDevelop.Core.AddIns;

using MonoDevelop.Internal.Parser;
using MonoDevelop.Internal.Project;

using MonoDevelop.Gui;

namespace MonoDevelop.Services
{
	public delegate void ParseInformationEventHandler(object sender, ParseInformationEventArgs e);
	
	public class ParseInformationEventArgs : EventArgs
	{
		string fileName;
		IParseInformation parseInformation;
				
		public string FileName {
			get {
				return fileName;
			}
		}
		
		public IParseInformation ParseInformation {
			get {
				return parseInformation;
			}
		}
		
		public ParseInformationEventArgs(string fileName, IParseInformation parseInformation)
		{
			this.fileName         = fileName;
			this.parseInformation = parseInformation;
		}
	}
}
