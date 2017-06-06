// KeyBindingSet.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//   Jeffrey Stedfast <fejj@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using MonoDevelop.Core;
using Unix = Mono.Unix.Native;
using System.Linq;

namespace MonoDevelop.Components.Commands
{
	public class KeyBindingSet
	{
		const string version = "1.0";
		Dictionary<string, string> bindings = new Dictionary<string, string> ();
		KeyBindingSet parent;
		
		public KeyBindingSet()
		{
		}
		
		public KeyBindingSet (KeyBindingSet parent)
		{
			this.parent = parent;
		}

		public KeyBindingConflict[] CheckKeyBindingConflicts (IEnumerable<Command> commands)
		{
			Dictionary<string, List<Command>> bindings = new Dictionary<string, List<Command>> ();
			List<KeyBindingConflict> conflicts = new List<KeyBindingConflict> ();

			// Load all bindings
			foreach (Command cmd in commands) {
				foreach (var key in GetBindings (cmd)) {
					if (key.Length == 0)
						continue;
					List<Command> clist;
					if (!bindings.TryGetValue (key, out clist)) {
						clist = new List<Command> ();
						bindings.Add (key, clist);
					}
					clist.Add (cmd);
					if (clist.Count == 2)
						// We have a conflict
						conflicts.Add (new KeyBindingConflict (key, clist));
				}
			}

			// Check if there are conflicts with key combinations prefixes.
			// For example, ctrl+X would conflict with ctrl+X|A
			foreach (Command cmd in commands) {
				foreach (var key in GetBindings (cmd).Where (key => key.Contains ('|')).Select (key => key.Substring (0, key.IndexOf ('|')))) {
					List<Command> clist;
					if (bindings.TryGetValue (key, out clist)) {
						if (!clist.Contains (cmd))
							clist.Add (cmd);
						if (clist.Count == 2)
							// We have a conflict
							conflicts.Add (new KeyBindingConflict (key, clist));
					}
				}
			}
			return conflicts.ToArray ();
		}
		
		public KeyBindingSet Clone ()
		{
			KeyBindingSet kset = new KeyBindingSet (parent);
			kset.bindings = new Dictionary<string, string> (bindings);
			return kset;
		}

		public bool Equals (KeyBindingSet other)
		{
			if (parent != other && bindings.Count != other.bindings.Count)
				return false;
			foreach (KeyValuePair<string, string> binding in bindings) {
				string accel;
				if (!other.bindings.TryGetValue (binding.Key, out accel) || accel != binding.Value)
					return false;
			}
			return true;
		}

		public void ClearBindings ()
		{
			bindings.Clear ();
		}
		
		public void StoreBinding (Command cmd)
		{
			if (cmd.AlternateAccelKeys != null)
				SetBinding (cmd, new [] { cmd.AccelKey }.Concat (cmd.AlternateAccelKeys).ToArray ());
			else
				SetBinding (cmd, cmd.AccelKey);
		}
		
		public void SetBinding (Command cmd, params string[] accelKeys)
		{
			string key = KeyBindingService.GetCommandKey (cmd);
			accelKeys = accelKeys?.Distinct ().ToArray () ?? new string [0];
			if (parent == null) {
				if (accelKeys.Length == 0)
					bindings.Remove (key);
				else
					bindings [key] = string.Join (" ", accelKeys);
			} else {
				// If the key is the same as the default, remove it from the scheme
				var pbind = parent.GetBindings (cmd).Distinct ();
				if (new HashSet<string> (accelKeys).SetEquals (pbind))
					bindings.Remove (key);
				else
					bindings [key] = string.Join (" ", accelKeys);
			}
		}
		
		public void LoadBinding (Command cmd)
		{
			var cmdAccesls = GetBindings (cmd);
			if (cmdAccesls == null || cmdAccesls.Length == 0) {
				cmd.AccelKey = String.Empty;
				cmd.AlternateAccelKeys = null;
				return;
			}
			
			cmd.AccelKey = cmdAccesls [0];
			if (cmdAccesls.Length > 1)
				cmd.AlternateAccelKeys = cmdAccesls.Skip (1).ToArray ();
			else
				cmd.AlternateAccelKeys = null;
		}
		
		public string [] GetBindings (Command cmd)
		{
			string key = KeyBindingService.GetCommandKey (cmd);

			// Schemes use default key bindings when not explicitly overriden
			string accel;
			if (bindings.TryGetValue (key, out accel))
				return (accel ?? "").Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			else if (parent != null)
				return parent.GetBindings (cmd);
			else
				return new string[0];
		}
		
		const string commandAttr = "command";
		const string shortcutAttr = "shortcut";
		
		internal bool LoadScheme (XmlTextReader reader, string id)
		{
			bool foundSchemes = false;
			bool foundScheme = false;
			string command;
			string binding;

			bindings.Clear ();
			
			while (reader.Read ()) {
				if (reader.IsStartElement ("schemes") || reader.IsStartElement ("scheme")) {
					foundSchemes = true;
					break;
				}
			}
			
			if (!foundSchemes || reader.GetAttribute ("version") != version)
				return false;
			
			if (reader.IsStartElement ("schemes")) {
				while (reader.Read ()) {
					if (reader.IsStartElement ("scheme") && reader.GetAttribute ("name") == id) {
						foundScheme = true;
						break;
					}
				}
				if (!foundScheme)
					return false;
			}
			
			while (reader.Read ()) {
				if (reader.IsStartElement ()) {
					switch (reader.LocalName) {
					case "scheme":
						// this is the beginning of the next scheme
						return true;
					case "binding":
						command = reader.GetAttribute (commandAttr);
						binding = reader.GetAttribute (shortcutAttr);
						
						if (!string.IsNullOrEmpty (command))
							bindings.Add (command, KeyBindingManager.FixChordSeparators(binding));
						
						break;
					}
				}
			}
			
			return true;
		}
		
		public void Save (XmlTextWriter writer, string id)
		{
			writer.WriteStartElement ("scheme");
			writer.WriteAttributeString ("name", id);
			
			foreach (KeyValuePair<string, string> binding in bindings) {

				string pbind;
				if (parent?.bindings != null && parent.bindings.TryGetValue (binding.Key, out pbind)) {
					if (binding.Value == pbind)
						continue;
				} else if (string.IsNullOrEmpty (binding.Value))
					continue;

				writer.WriteStartElement ("binding");
				writer.WriteAttributeString (commandAttr, binding.Key);
				writer.WriteAttributeString (shortcutAttr, binding.Value);
				writer.WriteEndElement ();
			}
			
			writer.WriteEndElement ();
		}
		
		public void Save (string fileName, string id)
		{
			XmlTextWriter writer = null;
			Stream stream = null;
			bool success = false;
			
			try {
				stream = new FileStream (fileName + '~', FileMode.Create);
				writer = new XmlTextWriter (stream, Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				writer.IndentChar = ' ';
				writer.Indentation = 2;
				
				writer.WriteStartElement ("schemes");
				writer.WriteAttributeString ("version", version);
				Save (writer, id);
				writer.WriteEndElement ();
				
				success = true;
			} catch (Exception e) {
				LoggingService.LogError (e.ToString ());
			} finally {
				if (writer != null)
					writer.Close ();
				
				if (stream != null)
					stream.Close ();
			}
			
			if (success)
				FileService.SystemRename (fileName + '~', fileName);
		}
	}

	public class KeyBindingConflict
	{
		internal KeyBindingConflict (string key, IEnumerable<Command> cmds)
		{
			Key = key;
			Commands = cmds;
		}
		
		public string Key { get; internal set; }
		public IEnumerable<Command> Commands { get; internal set; }
	}
}
