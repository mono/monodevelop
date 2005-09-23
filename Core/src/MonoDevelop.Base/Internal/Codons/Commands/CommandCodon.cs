//
// CommandCodon.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Commands;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;

namespace MonoDevelop.Core.AddIns.Codons
{
	[CodonNameAttribute ("Command")]
	public class CommandCodon : AbstractCodon
	{
		[XmlMemberAttribute ("_label", IsRequired=true)]
		string label;
		
		[XmlMemberAttribute ("description")]
		string description;
		
		[XmlMemberAttribute ("shortcut")]
		string shortcut;
		
		[XmlMemberAttribute("icon")]
		string icon;
		
		[XmlMemberAttribute("disabledVisible")]
		bool disabledVisible = true;
		
		[XmlMemberAttribute("type")]
		string type = "normal";
		
		[XmlMemberAttribute("widget")]
		string widget = null;
		
		[XmlMemberAttribute("defaultHandler")]
		string defaultHandler;
		
		public override object BuildItem (object owner, ArrayList subItems, ConditionCollection conditions)
		{
			ActionType ct = ActionType.Normal;
			bool isArray = false;
			bool custom = false;
			bool isAction = false;

			foreach (string p in type.Split ('|')) {
				switch (p) {
					case "check":
						ct = ActionType.Check;
						if (isAction)
							throw new InvalidOperationException ("Action type specified twice.");
						isAction = true;
						break;

					case "radio":
						ct = ActionType.Radio;
						if (isAction)
							throw new InvalidOperationException ("Action type specified twice.");
						isAction = true;
						break;

					case "normal":
						ct = ActionType.Normal;
						if (isAction)
							throw new InvalidOperationException ("Action type specified twice.");
						isAction = true;
						break;

					case "custom":
						if (widget == null)
							throw new InvalidOperationException ("Widget type not specified in custom command.");
						custom = true;
						break;
						
					case "array":
						isArray = true;
						break;
						
					default:
						throw new InvalidOperationException ("Unknown command type: " + p);
				}
			}
			
			if (isAction && custom)
				throw new InvalidOperationException ("Invalid command type combination: " + type);

			Command cmd;

			if (custom) {
				if (isArray)
					throw new InvalidOperationException ("Array custom commands are not allowed.");
					
				CustomCommand ccmd = new CustomCommand ();
				ccmd.Text = label;
				ccmd.Description = description;
				ccmd.WidgetType = AddIn.GetType (widget);
				if (ccmd.WidgetType == null)
					throw new InvalidOperationException ("Could not find command type '" + widget + "'.");
				cmd = ccmd;
			} else {
				if (widget != null)
					throw new InvalidOperationException ("Widget type can only be specified for custom commands.");
					
				ActionCommand acmd = new ActionCommand ();
				acmd.ActionType = ct;
				acmd.CommandArray = isArray;
				
				if (defaultHandler != null) {
					acmd.DefaultHandlerType = AddIn.GetType (defaultHandler);
					if (acmd.DefaultHandlerType == null)
						throw new InvalidOperationException ("Could not find handler type '" + defaultHandler + "' for command " + ID);
				}
				
				cmd = acmd;
			}
			
			cmd.Id = ParseCommandId (this);
			cmd.Text = Runtime.StringParserService.Parse (GettextCatalog.GetString (label));
			cmd.Description = GettextCatalog.GetString (description);
			if (icon != null)
				cmd.Icon = ResourceService.GetStockId (AddIn, icon);
			cmd.AccelKey = shortcut;
			cmd.DisabledVisible = disabledVisible;
			
			return cmd;
		}
		
		internal static object ParseCommandId (ICodon codon)
		{
			string id = codon.ID;
			if (id.StartsWith ("@"))
				return id.Substring (1);

			Type enumType = null;
			string typeName = id;
			
			int i = id.LastIndexOf (".");
			if (i != -1)
				typeName = id.Substring (0,i);
				
			enumType = codon.AddIn.GetType (typeName);
				
			if (enumType == null)
				enumType = Type.GetType (typeName);

			if (enumType == null)
				enumType = typeof(Command).Assembly.GetType (typeName);

			if (enumType == null || !enumType.IsEnum)
				throw new InvalidOperationException ("Could not find an enum type for the command '" + id + "'.");
				
			try {
				return Enum.Parse (enumType, id.Substring (i+1));
			} catch {
				throw new InvalidOperationException ("Could not find an enum value for the command '" + id + "'.");
			}
		}
	}
}
