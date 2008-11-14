// KeyBindingService.cs
//
// Author: Jeffrey Stedfast <fejj@novell.com>
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

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Mono.Addins;

using Unix = Mono.Unix.Native;

using MonoDevelop.Core;
using MonoDevelop.Components.Commands.ExtensionNodes;

namespace MonoDevelop.Components.Commands {
	public class KeyBindingService {
		const string configFileName = "KeyBindings.xml";
		const string version = "1.0";
		const string DefaultSchemeName = "Default";
		
		static SortedDictionary<string, string> current;
		static SortedDictionary<string, string> scheme;
		static SortedDictionary<string, SchemeExtensionNode> schemes;
		static SortedDictionary<string, string> defaultScheme;

		static string schemeName;
		
		static KeyBindingService ()
		{
			current = new SortedDictionary<string, string> ();
			LoadCurrentBindings ();
			
			scheme = new SortedDictionary<string, string> ();
			schemes = new SortedDictionary<string,SchemeExtensionNode> ();
			defaultScheme = new SortedDictionary<string, string> ();
		}
		
		static string ConfigFileName {
			get {
				return Path.Combine (PropertyService.ConfigPath, configFileName);
			}
		}
		
		public static IEnumerable<string> SchemeNames {
			get {
				yield return DefaultSchemeName;
				foreach (string s in schemes.Keys)
					yield return s;
			}
		}
		
		public static void LoadBindingsFromExtensionPath (string path)
		{
			AddinManager.AddExtensionNodeHandler (path, OnBindingExtensionChanged);
		}
		
		static void OnBindingExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				SchemeExtensionNode node = (SchemeExtensionNode) args.ExtensionNode;
				schemes.Add (node.Name, node);
			}
			else {
				SchemeExtensionNode node = (SchemeExtensionNode) args.ExtensionNode;
				schemes.Remove (node.Name);
			}
		}
		
		public static void LoadBinding (Command cmd)
		{
			if (cmd == null)
				return;
			
			string key = GetCommandKey (cmd);
			
			// Note: Only override the cmd.AccelKey if the scheme had the binding defined,
			// this way if an Addin later gets installed which has its own set of Commands
			// w/ key-bindings, we won't inadvertantly override them.
			string accel;
			if (current.TryGetValue (key, out accel))
				cmd.AccelKey = accel;
		}
		
		public static void LoadDefaultBinding (Command cmd)
		{
			if (cmd == null)
				return;
			
			string key = GetCommandKey (cmd);
			defaultScheme [key] = cmd.AccelKey;
		}
		
		public static void StoreBinding (Command cmd)
		{
			if (cmd == null)
				return;
			
			string key = GetCommandKey (cmd);

			string defaultAccel;
			defaultScheme.TryGetValue (key, out defaultAccel);
			
			// If the key is the same as the default, remove it from the scheme
			if (cmd.AccelKey == defaultAccel)
				current.Remove (key);
			else
				current[key] = cmd.AccelKey;
		}
		
		public static string SchemeBinding (Command cmd)
		{
			if (schemeName == null || cmd == null)
				return null;
			
			string key = GetCommandKey (cmd);

			// Schemes use default key bindings when not explicitly overriden
			string accel;
			if (scheme.TryGetValue (key, out accel))
				return accel;
			else if (defaultScheme.TryGetValue (key, out accel))
				return accel;
			else
				return cmd.AccelKey;
		}
		
		public static string SchemeBinding (string name, Command cmd)
		{
			if (cmd == null || name == null)
				return null;
			
			if (schemeName != name)
				LoadScheme (name);
			
			return SchemeBinding (cmd);
		}

		static string GetCommandKey (Command cmd)
		{
			if (cmd.Id is Enum)
				return cmd.Id.GetType () + "." + cmd.Id;
			else
				return cmd.Id.ToString ();
		}
		
		const string commandAttr = "command";
		const string shortcutAttr = "shortcut";
		
		static bool Load (XmlTextReader reader, string name, ref SortedDictionary<string, string> bindings)
		{
			bool foundSchemes = false;
			bool foundScheme = false;
			string command;
			string binding;
			
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
					if (reader.IsStartElement ("scheme") && reader.GetAttribute ("name") == name) {
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
						
						if (command != null && command != String.Empty)
							bindings.Add (command, binding);
						
						break;
					}
				}
			}
			
			return true;
		}
		
		static void LoadCurrentBindings ()
		{
			XmlTextReader reader = null;
			
			try {
				reader = new XmlTextReader (ConfigFileName);
				Load (reader, "current", ref current);
			} catch {
				current.Clear ();
			} finally {
				if (reader != null)
					reader.Close ();
			}
		}
		
		public static void LoadScheme (string name)
		{
			XmlTextReader reader = null;
			Stream stream;
			
			if (schemeName != null)
				UnloadScheme ();
			
			if (name == null)
				return;

			if (name == DefaultSchemeName) {
				schemeName = DefaultSchemeName;
				scheme = defaultScheme;
				return;
			}
			
			SchemeExtensionNode node = schemes [name];
			
			try {
				stream = node.GetKeyBindingsSchemeStream ();
				if (stream != null) {
					reader = new XmlTextReader (stream);
					Load (reader, name, ref scheme);
					schemeName = name;
				}
			} catch (Exception e) {
				LoggingService.LogError (e.ToString ());
			} finally {
				if (reader != null)
					reader.Close ();
			}
		}
		
		public static void UnloadScheme ()
		{
			// The default scheme is kept in memory, so don't clear it here.
			schemeName = null;
			scheme = new SortedDictionary<string, string> ();
		}

		static void Save (XmlTextWriter writer, string name, ref SortedDictionary<string, string> bindings)
		{
			writer.WriteStartElement ("scheme");
			writer.WriteAttributeString ("name", name);
			
			foreach (KeyValuePair<string, string> binding in bindings) { 
				writer.WriteStartElement ("binding");
				writer.WriteAttributeString (commandAttr, binding.Key);
				writer.WriteAttributeString (shortcutAttr, binding.Value);
				writer.WriteEndElement ();
			}
			
			writer.WriteEndElement ();
		}
		
		static void Save (string fileName, string name, ref SortedDictionary<string, string> bindings)
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
				Save (writer, name, ref bindings);
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
				Unix.Syscall.rename (fileName + '~', fileName);
		}
		
		public static void SaveCurrentBindings ()
		{
			Save (ConfigFileName, "current", ref current);
		}
	}
}
