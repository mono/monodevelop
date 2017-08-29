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

namespace MonoDevelop.Components.Commands
{
	public class KeyBindingService
	{
		const string configFileName = "KeyBindings.xml";
		const string configFileNameMac = "KeyBindingsMac.xml";
		
		static KeyBindingSet current;
		static SortedDictionary<string, KeyBindingScheme> schemes;
		static KeyBindingSet defaultSchemeBindings;
		static DefaultScheme defaultScheme;

		static KeyBindingService ()
		{
			schemes = new SortedDictionary<string,KeyBindingScheme> ();

			// Initialize the default scheme
			defaultSchemeBindings = new KeyBindingSet ();
			defaultScheme = new DefaultScheme (defaultSchemeBindings);
			schemes.Add (defaultScheme.Id, defaultScheme);

			// Initialize the current bindings
			current = new KeyBindingSet (defaultSchemeBindings);
		}
		
		static string ConfigFileName {
			get {
				string file = Platform.IsMac? "Custom.mac-kb.xml" : "Custom.kb.xml";
				return UserProfile.Current.UserDataRoot.Combine ("KeyBindings", file);
			}
		}

		internal static KeyBindingSet DefaultKeyBindingSet {
			get { return defaultSchemeBindings; }
		}

		public static KeyBindingSet CurrentKeyBindingSet {
			get { return current; }
		}

		public static KeyBindingScheme GetScheme (string id)
		{
			KeyBindingScheme scheme;
			if (schemes.TryGetValue (id, out scheme))
				return scheme;
			else
				return null;
		}

		public static KeyBindingScheme GetSchemeByName (string name)
		{
			foreach (KeyBindingScheme scheme in schemes.Values)
				if (scheme.Name == name)
					return scheme;
			return null;
		}
		
		public static IEnumerable<KeyBindingScheme> Schemes {
			get {
				foreach (KeyBindingScheme s in schemes.Values)
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
				if (node.IsForMac == Platform.IsMac)
					schemes.Add (node.Id, node);
			}
			else {
				SchemeExtensionNode node = (SchemeExtensionNode) args.ExtensionNode;
				if (node.IsForMac == Platform.IsMac)
					schemes.Remove (node.Name);
			}
		}
		
		public static void LoadBinding (Command cmd)
		{
			current.LoadBinding (cmd);
		}
		
		public static void StoreDefaultBinding (Command cmd)
		{
			defaultSchemeBindings.StoreBinding (cmd);
		}

		public static void ResetCurrent (KeyBindingSet kbset)
		{
			current = kbset.Clone ();
		}
		
		public static void ResetCurrent ()
		{
			ResetCurrent ((string)null);
		}
		
		public static void ResetCurrent (string schemeId)
		{
			if (schemeId != null) {
				KeyBindingScheme scheme = GetScheme (schemeId);
				if (scheme != null) {
					current = scheme.GetKeyBindingSet ().Clone ();
					return;
				}
			}

			current.ClearBindings ();
		}
		
		public static void StoreBinding (Command cmd)
		{
			current.StoreBinding (cmd);
		}
		
		internal static string GetCommandKey (Command cmd)
		{
			if (cmd.Id is Enum)
				return cmd.Id.GetType () + "." + cmd.Id;
			else
				return cmd.Id.ToString ();
		}
		
		public static void LoadCurrentBindings (string defaultSchemaId)
		{
			XmlTextReader reader = null;

			if (!File.Exists (ConfigFileName)) {
				ResetCurrent (defaultSchemaId);
				return;
			}

			try {
				reader = new XmlTextReader (ConfigFileName);
				current.LoadScheme (reader, "current");
			} catch {
				ResetCurrent (defaultSchemaId);
			} finally {
				if (reader != null)
					reader.Close ();
			}
		}
				
		public static void SaveCurrentBindings ()
		{
			string dir = Path.GetDirectoryName (ConfigFileName);
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);
			current.Save (ConfigFileName, "current");
		}
	}

	class DefaultScheme: KeyBindingScheme
	{
		KeyBindingSet bindings;

		public DefaultScheme (KeyBindingSet bindings)
		{
			this.bindings = bindings;
		}
		
		public string Id {
			get { return "Default"; }
		}
		
		public string Name {
			get {
				if (BrandingService.ApplicationName == "MonoDevelop")
					return AddinManager.CurrentLocalizer.GetString ("Default");
				else
					return BrandingService.ApplicationName;
			}
		}
		
		public KeyBindingSet GetKeyBindingSet ()
		{
			return bindings;
		}
	}
}
