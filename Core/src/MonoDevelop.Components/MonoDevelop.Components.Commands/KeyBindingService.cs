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

using Unix = Mono.Unix.Native;

using MonoDevelop.Core;

namespace MonoDevelop.Components.Commands {
	public class KeyBindingService {
		const string schemesFileName = "KeyBindingSchemes.xml";
		const string configFileName = "KeyBindings.xml";
		const string version = "1.0";
		
		static SortedDictionary<string, string> current;
		static SortedDictionary<string, string> scheme;
		static List<string> schemes;
		static string schemeName;
		
		static KeyBindingService ()
		{
			current = new SortedDictionary<string, string> ();
			LoadCurrentBindings ();
			
			scheme = new SortedDictionary<string, string> ();
			schemes = new List<string> ();
			LoadSchemes ();
		}
		
		static string ConfigFileName {
			get {
				PropertyService propertyService = (PropertyService) ServiceManager.GetService (typeof (PropertyService));
				return Path.Combine (propertyService.ConfigDirectory, configFileName);
			}
		}
		
		public static List<string> SchemeNames {
			get { return schemes; }
		}
		
		public static void LoadBinding (Command cmd)
		{
			if (cmd == null)
				return;
			
			string key;
			if (cmd.Id is Enum)
				key = cmd.Id.GetType () + "." + cmd.Id;
			else
				key = cmd.Id.ToString ();
			
			// Note: Only override the cmd.AccelKey if the scheme had the binding defined,
			// this way if an Addin later gets installed which has its own set of Commands
			// w/ key-bindings, we won't inadvertantly override them.
			if (current.ContainsKey (key))
				cmd.AccelKey = current[key];
		}
		
		public static void StoreBinding (Command cmd)
		{
			if (cmd == null)
				return;
			
			string key;
			if (cmd.Id is Enum)
				key = cmd.Id.GetType () + "." + cmd.Id;
			else
				key = cmd.Id.ToString ();
			
			if (!current.ContainsKey (key))
				current.Add (key, cmd.AccelKey);
			else
				current[key] = cmd.AccelKey;
		}
		
		public static string SchemeBinding (Command cmd)
		{
			if (schemeName == null || cmd == null)
				return null;
			
			string key;
			if (cmd.Id is Enum)
				key = cmd.Id.GetType () + "." + cmd.Id;
			else
				key = cmd.Id.ToString ();
			
			if (scheme.ContainsKey (key))
				return scheme[key];
			
			return null;
		}
		
		public static string SchemeBinding (string name, Command cmd)
		{
			if (cmd == null || name == null)
				return null;
			
			if (schemeName != name)
				LoadScheme (name);
			
			return SchemeBinding (cmd);
		}
		
		static void LoadSchemes ()
		{
			Stream stream = typeof (KeyBindingService).Assembly.GetManifestResourceStream (schemesFileName);
			bool inSchemes = false;
			string name;
			
			if (stream != null) {
				XmlTextReader reader = new XmlTextReader (stream);
				try {
					while (reader.Read ()) {
						if (reader.IsStartElement ()) {
							switch (reader.LocalName) {
							case "schemes":
								if (reader.GetAttribute ("version") != version)
									return;
								inSchemes = true;
								break;
							case "scheme":
								if (inSchemes) {
									name = reader.GetAttribute ("name");
									schemes.Add (name);
								}
								break;
							default:
								break;
							}
						}
					}
				} finally {
					reader.Close ();
				}
			}
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
				if (reader.IsStartElement ("schemes")) {
					foundSchemes = true;
					break;
				}
			}
			
			if (!foundSchemes || reader.GetAttribute ("version") != version)
				return false;
			
			while (reader.Read ()) {
				if (reader.IsStartElement ("scheme") && reader.GetAttribute ("name") == name) {
					foundScheme = true;
					break;
				}
			}
			
			if (!foundScheme)
				return false;
			
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
			
			stream = typeof (KeyBindingService).Assembly.GetManifestResourceStream (schemesFileName);
			if (stream != null) {
				try {
					reader = new XmlTextReader (stream);
					Load (reader, name, ref scheme);
					schemeName = name;
				} catch (Exception e) {
					Runtime.LoggingService.Error (e);
				} finally {
					if (reader != null)
						reader.Close ();
				}
			}
		}
		
		public static void UnloadScheme ()
		{
			schemeName = null;
			scheme.Clear ();
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
				Runtime.LoggingService.Error (e);
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
