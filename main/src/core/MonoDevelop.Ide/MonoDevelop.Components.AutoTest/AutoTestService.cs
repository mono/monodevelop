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
using MonoDevelop.Core;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;

namespace MonoDevelop.Components.AutoTest
{
	public static class AutoTestService
	{
		static CommandManager commandManager;
		static AutoTestServiceManager manager = new AutoTestServiceManager ();
		public static AutoTestSession CurrentSession { 
			get { return manager.currentSession; } 
		}

		static SessionRecord currentRecordSession;
		public static SessionRecord CurrentRecordSession {
			get { return currentRecordSession; }
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

		public static void ReplaySessionFromFile (string filename)
		{
			currentRecordSession = new SessionRecord (commandManager, filename);
			currentRecordSession.ReplayEvents (() => {
				currentRecordSession = null;
			});
		}

		public static SessionRecord StartRecordingSession ()
		{
			currentRecordSession = new SessionRecord (commandManager);
			return currentRecordSession;
		}

		public static void StopRecordingSession (string filename = null)
		{
			if (currentRecordSession == null) {
				return;
			}

			currentRecordSession.Pause ();

			if (filename != null) {
				currentRecordSession.WriteLogToFile (filename);
			}
			currentRecordSession = null;
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
			if (client == this.client) {
				this.client = null;
				currentSession?.Dispose ();
				currentSession = null;
			} else
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
		enum State {
			Idle,
			Recording,
			Replaying
		};
		State state;

		StringBuilder pendingText;
		Gdk.ModifierType pendingModifiers = Gdk.ModifierType.None;

		public abstract class RecordEvent
		{
			public abstract XElement ToXML ();
			public abstract void ParseXML (XElement element);
			public abstract void Replay (AutoTestSession testSession);
		}
		
		internal class KeyPressEvent: RecordEvent
		{
			public Gdk.Key Key { get; set; }
			public Gdk.ModifierType Modifiers { get; set; }

			public override XElement ToXML ()
			{
				return new XElement ("event", new XAttribute ("type", "KeyPressEvent"),
						new XElement ("key", Key.ToString ()),
						new XElement ("modifier", Modifiers.ToString ()));
			}

			public override void ParseXML (XElement element)
			{
				foreach (var e in element.Elements ()) {
					if (e.Name == "key") {
						Key = (Gdk.Key)Enum.Parse (typeof (Gdk.Key), e.Value);
					} else if (e.Name == "modifier") {
						Modifiers = (Gdk.ModifierType)Enum.Parse (typeof (Gdk.ModifierType), e.Value);
					}
				}
			}

			public override void Replay (AutoTestSession testSession)
			{
				// Select the main window and then we can push key events to it.
				AppQuery query = testSession.CreateNewQuery ();
				AppResult[] results = query.Window ().Marked ("MonoDevelop.Ide.Gui.DefaultWorkbench").Execute ();
				if (results.Length == 0) {
					return;
				}

				testSession.Select (results[0]);
				// We need the GtkWidgetResult for the main window as we only have the keys as a Gdk key
				if (results [0] is AutoTest.Results.GtkWidgetResult) {
					AutoTest.Results.GtkWidgetResult widgetResult = (AutoTest.Results.GtkWidgetResult) results[0];
					widgetResult.RealTypeKey (Key, Modifiers);
				}
			}
		}

		internal class StringEvent: RecordEvent
		{
			internal string Text;
			internal Gdk.ModifierType Modifiers { get; set; }

			public override XElement ToXML ()
			{
				return new XElement ("event", new XAttribute ("type", "StringEvent"),
					new XElement ("text", Text),
					new XElement ("modifier", Modifiers.ToString ()));
			}

			public override void ParseXML (XElement element)
			{
				foreach (var e in element.Elements ()) {
					if (e.Name == "text") {
						Text = e.Value;
					} else if (e.Name == "modifier") {
						Modifiers = (Gdk.ModifierType)Enum.Parse (typeof(Gdk.ModifierType), e.Value);
					}
				}
			}

			public override void Replay (AutoTestSession testSession)
			{
				AppQuery query = testSession.CreateNewQuery ();
				AppResult[] results = query.Window ().Marked ("MonoDevelop.Ide.Gui.DefaultWorkbench").Execute ();
				if (results.Length == 0) {
					return;
				}

				testSession.Select (results [0]);

				if (results [0] is AutoTest.Results.GtkWidgetResult) {
					AutoTest.Results.GtkWidgetResult widgetResult = (AutoTest.Results.GtkWidgetResult)results [0];
					widgetResult.EnterText (Text);
				}
			}
		}

		internal class CommandEvent: RecordEvent
		{
			public object CommandId { get; set; }
			public int DataItemIndex { get; set; }
			public bool IsCommandArray {
				get { return DataItemIndex != -1; }
			}

			public override XElement ToXML ()
			{
				return new XElement ("event", new XAttribute ("type", "CommandEvent"),
						new XElement ("commandID", CommandId.ToString ()),
						new XElement ("dataItemIndex", DataItemIndex));
			}

			public override void ParseXML (XElement element)
			{
				foreach (var e in element.Elements ()) {
					if (e.Name == "commandID") {
						CommandId = e.Value;
					} else if (e.Name == "dataItemIndex") {
						DataItemIndex = Convert.ToInt32 (e.Value);
					}
				}
			}

			public override void Replay (AutoTestSession testSession)
			{
				testSession.ExecuteCommand (CommandId);
			}
		}
		
		internal SessionRecord (CommandManager commandManager)
		{
			this.commandManager = commandManager;
			Resume ();
		}

		internal SessionRecord (CommandManager commandManager, string logFile)
		{
			state = State.Idle;
			this.commandManager = commandManager;
			LoadFromLogFile (logFile);
		}
		
		public IEnumerable<RecordEvent> Events {
			get {
				if (state == State.Recording)
					throw new InvalidOperationException ("The record session must be paused before getting the recorded events.");
				return events; 
			}
		}
		
		public bool IsPaused {
			get { return state == State.Idle; }
		}

		public bool IsReplaying {
			get { return state == State.Replaying; }
		}
		
		public void Pause ()
		{
			if (state == State.Recording) {
				commandManager.KeyPressed -= HandleCommandManagerKeyPressed;
				commandManager.CommandActivated -= HandleCommandManagerCommandActivated;
				state = State.Idle;
			}
		}
		
		public void Resume ()
		{
			if (state == State.Idle) {
				commandManager.KeyPressed += HandleCommandManagerKeyPressed;
				commandManager.CommandActivated += HandleCommandManagerCommandActivated;
				state = State.Recording;
				LoggingService.LogError ("Starting up session recording");
			}
		}
		
		void HandleCommandManagerCommandActivated (object sender, CommandActivationEventArgs e)
		{
			if ((string)e.CommandId == "MonoDevelop.Ide.Commands.ToolCommands.ToggleSessionRecorder" ||
				(string)e.CommandId == "MonoDevelop.Ide.Commands.ToolCommands.ReplaySession") {
				return;
			}

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

		void CompleteStringEvent (string s, Gdk.ModifierType modifiers)
		{
			events.Add (new StringEvent { Text = pendingText.ToString (), Modifiers = pendingModifiers });
			pendingText = null;
			pendingModifiers = Gdk.ModifierType.None;
		}

		void HandleCommandManagerKeyPressed (object sender, KeyPressArgs e)
		{
			uint unicode = Gdk.Keyval.ToUnicode (e.KeyValue);
			if (pendingText != null) {
				if (pendingModifiers != e.Modifiers || unicode == 0) {
					CompleteStringEvent (pendingText.ToString (), pendingModifiers);
				} else {
					pendingText.Append ((char)unicode);
					return;
				}

				// If text event has been completed, then we need to reset the pending events
				if (unicode != 0) {
					pendingText = new StringBuilder ();
					pendingText.Append ((char)unicode);
					pendingModifiers = e.Modifiers;
				} else {
					// Don't have a unicode key, so just issue a standard key event
					events.Add (new KeyPressEvent { Key = e.Key, Modifiers = e.Modifiers });
					pendingText = null;
					pendingModifiers = Gdk.ModifierType.None;
				}
			} else {
				if (unicode == 0) {
					events.Add (new KeyPressEvent () { Key = e.Key, Modifiers = e.Modifiers });
					return;
				}

				pendingText = new StringBuilder ();
				pendingText.Append ((char)unicode);
				pendingModifiers = e.Modifiers;
			}
		}

		public void WriteLogToFile (string filepath)
		{
			var doc = new XDocument (new XElement ("xs-event-replay-log",
				          from ev in events
						  select ev.ToXML ()));

			using (XmlWriter xw = XmlWriter.Create (filepath, new XmlWriterSettings { Indent = true })) {
				doc.Save (xw);
			}
		}

		public bool LoadFromLogFile (string filepath)
		{
			XDocument doc = XDocument.Load (filepath);
			foreach (XElement element in doc.Element("xs-event-replay-log").Elements ()) {
				if (element == null) {
					continue;
				}

				string evType = element.Attribute ("type").Value;
				RecordEvent ev = null;
				if (evType == "KeyPressEvent") {
					ev = new KeyPressEvent ();
				} else if (evType == "CommandEvent") {
					ev = new CommandEvent ();
				} else if (evType == "StringEvent") {
					ev = new StringEvent ();
				}

				if (ev == null) {
					return false;
				}

				ev.ParseXML (element);
				events.Add (ev);
			}

			return true;
		}

		public void ReplayEvents (Action completionHandler = null)
		{
			AutoTestSession testSession = new AutoTestSession ();
			Stopwatch sw = new Stopwatch ();
			int eventCount = events.Count;

			state = State.Replaying;
			
			sw.Start ();
			// Each spin of the main loop, remove an event from the queue and replay it.
			GLib.Idle.Add (() => {
				RecordEvent ev = events[0];
				events.RemoveAt (0);

				ev.Replay (testSession);

				if (events.Count > 0) {
					return true;
				}

				sw.Stop ();
				LoggingService.LogInfo ("Time elapsed to replay {0} events: {1}", eventCount, sw.Elapsed);
				state = State.Idle;

				if (completionHandler != null) {
					completionHandler ();
				}

				return false;
			});
		}
	}
}
