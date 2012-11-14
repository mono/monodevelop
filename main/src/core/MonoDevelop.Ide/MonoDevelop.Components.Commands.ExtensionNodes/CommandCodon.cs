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
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using Mono.Addins;
using System.ComponentModel;

namespace MonoDevelop.Components.Commands.ExtensionNodes
{
	[ExtensionNode (Description="A user interface command. The 'id' of the command must match the full name of an existing enumeration. An arbitrary string can also be used as an id for the command by just using '@' as prefix for the string.")]
	internal class CommandCodon : TypeExtensionNode
	{
		[NodeAttribute ("_label", true, "Label", Localizable=true)]
		string label;
		
		[NodeAttribute ("_description", "Description of the command", Localizable=true)]
		string _description;
		
		[NodeAttribute ("shortcut", "Key combination that triggers the command. Control, Alt, Meta, Super and Shift modifiers can be specified using '+' as a separator. Multi-state key bindings can be specified using a '|' between the mode and accel. For example 'Control+D' or 'Control+X|Control+S'")]
		string shortcut;
		
		[NodeAttribute ("macShortcut", "Mac version of the shortcut. Format is that same as 'shortcut', but the 'Meta' modifier corresponds to the Command key.")]
		string macShortcut;
		
		[NodeAttribute("icon", "Icon of the command. The provided value must be a registered stock icon. A resource icon can also be specified using 'res:' as prefix for the name, for example: 'res:customIcon.png'")]
		string icon;
		
		[NodeAttribute("disabledVisible", "Set to 'false' if the command has to be hidden when disabled. 'true' by default.")]
		bool disabledVisible = true;
		
		[NodeAttribute("type", "Type of the command. It can be: normal (the default), check, radio or array.")]
		string type = "normal";
		
		[NodeAttribute("widget", "Class of the widget to create when type is 'custom'.")]
		string widget = null;
		
		[NodeAttribute("defaultHandler", "Class that handles this command. This property is optional.")]
		string defaultHandler;
		
		public override object CreateInstance ()
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
				ccmd.WidgetType = Addin.GetType (widget);
				if (ccmd.WidgetType == null)
					throw new InvalidOperationException ("Could not find command type '" + widget + "'.");
				cmd = ccmd;
			} else {
				if (widget != null)
					throw new InvalidOperationException ("Widget type can only be specified for custom commands.");
					
				ActionCommand acmd = new ActionCommand ();
				acmd.ActionType = ct;
				acmd.CommandArray = isArray;
				
				if (defaultHandler != null)
					acmd.SetDefaultHandlerTypeInfo (Addin, defaultHandler);
				
				cmd = acmd;
			}
			
			cmd.Id = ParseCommandId (this);
			cmd.Text = StringParserService.Parse (BrandingService.BrandApplicationName (label));
			if ((_description != null) && (_description.Length > 0)){
				cmd.Description = BrandingService.BrandApplicationName (_description);				
			}
			cmd.Description = cmd.Description;
			
			if (icon != null)
				cmd.Icon = GetStockId (Addin, icon);
			
			cmd.AccelKey = KeyBindingManager.CanonicalizeBinding (Platform.IsMac? macShortcut : shortcut);
			
			cmd.DisabledVisible = disabledVisible;
			
			// Assign the category of the command
			CommandCategoryCodon cat = Parent as CommandCategoryCodon;
			if (cat != null)
				cmd.Category = cat.Name;
			
			return cmd;
		}

		internal static object ParseCommandId (ExtensionNode codon)
		{
			string id = codon.Id;
			if (id.StartsWith ("@"))
				return id.Substring (1);

			return id;
/*			Type enumType = null;
			string typeName = id;
			
			int i = id.LastIndexOf (".");
			if (i != -1)
				typeName = id.Substring (0,i);
				
			enumType = codon.Addin.GetType (typeName);
				
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
			}*/
		}
		
		internal static string GetStockId (RuntimeAddin addin, string icon)
		{
			return icon;
		}
	}
}
