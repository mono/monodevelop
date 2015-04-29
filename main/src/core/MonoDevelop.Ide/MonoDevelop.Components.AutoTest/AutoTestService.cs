// 
// AutoTestService.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System;
using MonoDevelop.Components.Commands;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Collections.Generic;

namespace MonoDevelop.Components.AutoTest
{
	public static class AutoTestService
	{
		static CommandManager commandManager;
		static AutoTestServiceManager manager = new AutoTestServiceManager ();
		public static AutoTestSession CurrentSession { 
			get { return manager.currentSession; } 
		}

		public static void Start (CommandManager commandManager, bool publishServer)
		{
			AutoTestService.commandManager = commandManager;
			
			string sref = Environment.GetEnvironmentVariable ("MONO_AUTOTEST_CLIENT");
			if (!string.IsNullOrEmpty (sref)) {
				Console.WriteLine ("AutoTest service starting");
				MonoDevelop.Core.Execution.RemotingService.RegisterRemotingChannel ();
				byte[] data = Convert.FromBase64String (sref);
				MemoryStream ms = new MemoryStream (data);
				BinaryFormatter bf = new BinaryFormatter ();
				IAutoTestClient client = (IAutoTestClient) bf.Deserialize (ms);
				client.Connect (manager.AttachClient (client));
			}
			if (publishServer && !manager.IsClientConnected) {
				MonoDevelop.Core.Execution.RemotingService.RegisterRemotingChannel ();
				BinaryFormatter bf = new BinaryFormatter ();
				ObjRef oref = RemotingServices.Marshal (manager);
				MemoryStream ms = new MemoryStream ();
				bf.Serialize (ms, oref);
				sref = Convert.ToBase64String (ms.ToArray ());
				File.WriteAllText (SessionReferenceFile, sref);
			}
		}
		
		public static SessionRecord StartRecordingSession ()
		{
			return new SessionRecord (commandManager);
		}
		
		internal static string SessionReferenceFile {
			get {
				return Path.Combine (Path.GetTempPath (), "monodevelop-autotest-objref");
			}
		}
		
		internal static CommandManager CommandManager {
			get { return commandManager; }
		}
		
		public static void NotifyEvent (string eventName)
		{
			if (manager.IsClientConnected)
				manager.NotifyEvent (eventName);
		}
	}
	
	class AutoTestServiceManager: MarshalByRefObject, IAutoTestService
	{
		IAutoTestClient client;
		public AutoTestSession currentSession;
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}
		
		public bool IsClientConnected {
			get { return client != null; }
		}
		
		public void NotifyEvent (string eventName)
		{
			try {
				client.NotifyEvent (eventName);
			}
			catch (Exception ex) {
				Console.WriteLine ("Dropping autotest client: " + ex.Message);
				client = null;
			}
		}
			
		public AutoTestSession AttachClient (IAutoTestClient client)
		{
			if (this.client != null) {
				// Make sure the current client is alive
				NotifyEvent ("Ping");
				if (this.client != null)
					throw new InvalidOperationException ("A client is already connected");
			}
			this.client = client;
			if (currentSession == null)
				currentSession = new AutoTestSession ();
			return currentSession;
		}
		
		public void DetachClient (IAutoTestClient client)
		{
			if (client == this.client)
				this.client = null;
			else
				throw new InvalidOperationException ("Not connected");
		}
	}
	
	public interface IAutoTestService
	{
		AutoTestSession AttachClient (IAutoTestClient client);
		void DetachClient (IAutoTestClient client);
	}
	
	public class SessionRecord
	{
		CommandManager commandManager;
		List<RecordEvent> events = new List<RecordEvent> ();
		bool recording;
		
		public class RecordEvent
		{
		}
		
		public class KeyPressEvent: RecordEvent
		{
			public Gdk.Key Key { get; set; }
			public Gdk.ModifierType Modifiers { get; set; }
		}
		
		public class CommandEvent: RecordEvent
		{
			public object CommandId { get; set; }
			public int DataItemIndex { get; set; }
			public bool IsCommandArray {
				get { return DataItemIndex != -1; }
			}
		}
		
		internal SessionRecord (CommandManager commandManager)
		{
			this.commandManager = commandManager;
			Resume ();
		}
		
		public IEnumerable<RecordEvent> Events {
			get {
				if (recording)
					throw new InvalidOperationException ("The record session must be paused before getting the recorded events.");
				return events; 
			}
		}
		
		public bool IsPaused {
			get { return !recording; }
		}
		
		public void Pause ()
		{
			if (recording) {
				commandManager.KeyPressed -= HandleCommandManagerKeyPressed;
				commandManager.CommandActivated -= HandleCommandManagerCommandActivated;
				recording = false;
			}
		}
		
		public void Resume ()
		{
			if (!recording) {
				commandManager.KeyPressed += HandleCommandManagerKeyPressed;
				commandManager.CommandActivated += HandleCommandManagerCommandActivated;
				recording = true;
			}
		}
		
		void HandleCommandManagerCommandActivated (object sender, CommandActivationEventArgs e)
		{
			CommandEvent cme = new CommandEvent () { CommandId = e.CommandId };
			cme.DataItemIndex = -1;
			
			if (e.DataItem != null && e.CommandInfo.ArrayInfo != null) {
				for (int n=0; n<e.CommandInfo.ArrayInfo.Count; n++) {
					if (e.CommandInfo.ArrayInfo[n].HandlesItem (e.DataItem)) {
						cme.DataItemIndex = n;
						break;
					}
				}
			}
			events.Add (cme);
		}

		void HandleCommandManagerKeyPressed (object sender, KeyPressArgs e)
		{
			events.Add (new KeyPressEvent () { Key = e.Key, Modifiers = e.Modifiers });
		}
	}
}
