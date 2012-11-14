// CustomCommandPanelWidget.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class CustomCommandPanelWidget : Gtk.Bin
	{
		CustomCommandCollection commands;
		CustomCommandWidget lastSlot;
		SolutionEntityItem entry;
		ConfigurationSelector configSelector;
		CustomCommandType[] supportedTypes;
		
		public CustomCommandPanelWidget ()
		{
			this.Build();
			label3.LabelProp = MonoDevelop.Core.BrandingService.BrandApplicationName (label3.LabelProp);
		}
		
		public void Load (SolutionEntityItem entry, CustomCommandCollection commands, ConfigurationSelector configSelector, CustomCommandType[] supportedTypes)
		{
			this.entry = entry;
			this.commands = commands;
			this.configSelector = configSelector;
			this.supportedTypes = supportedTypes;
			
			// Clean the list
			foreach (CustomCommandWidget ccw in vboxCommands.Children) {
				ccw.CommandCreated -= OnCommandCreated;
				ccw.CommandRemoved -= OnCommandRemoved;
				vboxCommands.Remove (ccw);
				ccw.Destroy ();
			}
			
			foreach (CustomCommand cmd in commands) {
				if (supportedTypes.Contains (cmd.Type))
					AddCommandSlot (cmd);
			}
			// Add an empty slot to allow adding more commands.
			AddCommandSlot (null);
		}
		
		void AddCommandSlot (CustomCommand cmd)
		{
			CustomCommandWidget widget = new CustomCommandWidget (entry, cmd, configSelector, supportedTypes);
			vboxCommands.PackStart (widget, false, false, 0);
			widget.CommandCreated += OnCommandCreated;
			widget.CommandRemoved += OnCommandRemoved;
			widget.Show ();
			lastSlot = widget;
		}
		
		void OnCommandCreated (object s, EventArgs args)
		{
			CustomCommandWidget widget = (CustomCommandWidget) s;
			commands.Add (widget.CustomCommand);
			
			// Add an empty slot to allow adding more commands.
			AddCommandSlot (null);
		}
		
		void OnCommandRemoved (object s, EventArgs args)
		{
			CustomCommandWidget widget = (CustomCommandWidget) s;
			commands.Remove (widget.CustomCommand);
			if (lastSlot != widget)
				vboxCommands.Remove (widget);
		}
	}
}
