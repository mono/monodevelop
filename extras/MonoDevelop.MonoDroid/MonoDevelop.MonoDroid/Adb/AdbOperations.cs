// 
// AdbOperations.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core;
using System.Collections.Generic;
using System.IO;

namespace MonoDevelop.MonoDroid
{
	public sealed class AdbCheckVersionOperation : AdbOperation
	{
		const string SUPPORTED_PROTOCOL = "001a";
		
		public AdbCheckVersionOperation ()
		{
			BeginConnect ();
		}
		
		protected override void OnConnected ()
		{
			WriteCommand ("host:version", () => GetStatus (() => ReadResponseWithLength (OnGotResponse)));
		}
		
		void OnGotResponse (string response)
		{
			if (response != SUPPORTED_PROTOCOL)
				throw new Exception ("Unsupported adb protocol: " + response);
			else
				SetCompleted (true);
		}
	}
	
	public sealed class AdbKillServerOperation : AdbOperation
	{
		public AdbKillServerOperation ()
		{
			BeginConnect ();
		}
		
		protected override void OnConnected ()
		{
			WriteCommand ("host:kill", () => SetCompleted (true));
		}
	}
	
	public sealed class AdbTrackDevicesOperation : AdbOperation
	{
		public AdbTrackDevicesOperation ()
		{
			BeginConnect ();
		}
		
		protected override void OnConnected ()
		{
			WriteCommand ("host:track-devices", () => GetStatus (() => ReadResponseWithLength (OnGotResponse)));
		}
		
		void OnGotResponse (string response)
		{
			Devices = Parse (response);
			var ev = DevicesChanged;
			if (ev != null)
				ev (Devices);
			ReadResponseWithLength (OnGotResponse);
		}
		
		List<AndroidDevice> Parse (string response)
		{
			var devices = new List<AndroidDevice> ();
			using (var sr = new StringReader (response)) {
				string s;
				while ((s = sr.ReadLine ()) != null) {
					s = s.Trim ();
					if (s.Length == 0)
						continue;
					string[] data = s.Split ('\t');
					devices.Add (new AndroidDevice (data[0].Trim (), data[1].Trim ()));
				}
			}
			return devices;
		}
		
		public List<AndroidDevice> Devices { get; private set; }
		public event Action<List<AndroidDevice>> DevicesChanged;
	}

	public sealed class AdbTrackLogOperation : AdbTransportOperation
	{	
		Action<string> output;
		string [] parameters;

		public AdbTrackLogOperation (AndroidDevice device, Action<string> output, params string [] parameters) : base (device)
		{
			if (output == null)
				throw new ArgumentNullException ("output");
			
			this.output = output;
			this.parameters = parameters;
			
			BeginConnect ();
		}

		protected override void OnGotTransport ()
		{
			string command = "shell:logcat " + string.Join (" ", parameters);

			WriteCommand (command, () => GetStatus (() => ReadResponseContinuous (GotResponseContinuous)));
		}

		static readonly char [] newLine = new char [] { '\n' };

		void GotResponseContinuous (string response)
		{
			//FIXME: is each read guaranteed to get a full line? or do we need to cache and merge partial lines?
			foreach (string line in response.Split (newLine, StringSplitOptions.RemoveEmptyEntries))
				output (line);
		}
	}
	
	public sealed class AdbGetPackagesOperation : AdbTransportOperation
	{
		public AdbGetPackagesOperation (AndroidDevice device) : base (device)
		{
			BeginConnect ();
		}
		
		protected override void OnGotTransport ()
		{
			var sr = new StringWriter ();
			WriteCommand ("shell:pm list packages", () => GetStatus (() => ReadResponse (sr, OnGotResponse)));
		}
		
		void OnGotResponse (TextWriter tw)
		{
			var sr = new StringReader (tw.ToString ());
			var list = new List<string> ();
			
			string s;
			while ((s = sr.ReadLine ()) != null) {
				//"Error: Could not access the Package Manager.  Is the system running?"
				if (s.StartsWith ("Error:"))
					throw new GetPackageListException (s.Substring ("Error:".Length));
				if (!s.StartsWith ("package:"))
					throw new GetPackageListException ("Unexpected package list output: '" + s + "'");
				s = s.Substring ("package:".Length);
				if (!string.IsNullOrEmpty (s))
					list.Add (s);
			}
			Packages = list;
			
			SetCompleted (true);
		}
		
		public List<string> Packages { get; private set; }
	}
	
	class GetPackageListException : Exception
	{
		public GetPackageListException (string message) : base (message)
		{
		}
	}
	
	public sealed class AdbGetPropertiesOperation : AdbTransportOperation
	{
		public AdbGetPropertiesOperation (AndroidDevice device) : base (device)
		{
			BeginConnect ();
		}
		
		protected override void OnGotTransport ()
		{
			var sr = new StringWriter ();
			WriteCommand ("shell:getprop", () => GetStatus (() => ReadResponse (sr, OnGotResponse)));
		}
		
		void OnGotResponse (TextWriter tw)
		{
			var sr = new StringReader (tw.ToString ());
			var props = new Dictionary<string,string> ();
			
			string s;
			var split = new char[] { '[', ']' };
			while ((s = sr.ReadLine ()) != null) {
				var arr = s.Split (split);
				if (arr.Length != 5 || string.IsNullOrEmpty (arr[1]) || arr[2] != ": ")
					throw new Exception ("Unknown property format: '" + s + "'");
				props [arr[1]] = arr[3];
			}
			Properties = props;
			
			SetCompleted (true);
		}
		
		public Dictionary<string,string> Properties { get; private set; }
	}

	public sealed class AdbGetProcessIdOperation : AdbTransportOperation
	{
		int pid = -1;
		string packageName;

		public AdbGetProcessIdOperation (AndroidDevice device, string packageName) :
			base (device)
		{
			if (packageName == null)
				throw new ArgumentNullException ("packageName");

			this.packageName = packageName;
			BeginConnect ();
		}

		protected override void OnGotTransport ()
		{
			// Valid package names never start with a '.'
			// Process names use the format ".lastPartOfPackageName"
			int idx = packageName.LastIndexOf ('.');
			var processName = idx > -1 ? packageName.Substring (idx + 1) : packageName;

			var sr = new StringWriter ();
			WriteCommand ("shell:ps " + processName, () => GetStatus (() => ReadResponse (sr, OnGotResponse)));
		}

		void OnGotResponse (TextWriter tw)
		{
			pid = ParseResponse (packageName, tw.ToString ());
			SetCompleted (true);
		}

		// Values:
		// -1 - value hasn't been computed, or an error happened
		// 0 - no associated process was found
		// any other - actual pid
		public int ProcessId {
			get {
				return pid;
			}
		}

		// The first line is the header, so we ignore it.
		// The information we are looking are is contained in the members:
		// 1 - Process ID
		// 8 - Package name
		static int ParseResponse (string packageName, string response)
		{
			using (var sr = new StringReader (response)) {
				string line = sr.ReadLine (); // ignore header
				if (line == null)
					throw new Exception ("'ps' output not recognized: '" + response + "'");

				char [] space = new char [] { ' ' };
				while ((line = sr.ReadLine ()) != null) {
					line = line.Trim ();
					if (line.Length == 0)
						continue;

					string [] stats = line.Split (space, StringSplitOptions.RemoveEmptyEntries);
					if (stats.Length != 9)
						throw new Exception ("'ps' output not recognized: '" + response + "'");

					if (stats [8].Trim () == packageName)
						return Int32.Parse (stats [1]);
				}
			}

			return 0;
		}

	}
	
	public sealed class AdbGetDateOperation : AdbBaseShellOperation
	{
		public AdbGetDateOperation (AndroidDevice device) : base (device, "date +%s")
		{
			BeginConnect ();
		}
		
		long? date;
		
		public override bool Success {
			get {
				if (!base.Success)
					return false;
				if (!date.HasValue) {
					long value;
					date = long.TryParse (Output, out value)? value : -1;
				}
				return date >= 0;
			}
		}
			
		public long Date {
			get {
				if (!Success)
					throw new InvalidOperationException ("Error getting date from device:\n" + Output);
				//Success will have parsed the value 
				return date.Value;
			}
		}
	}

	public abstract class AdbBaseShellOperation : AdbTransportOperation
	{
		string command;
		
		public AdbBaseShellOperation (AndroidDevice device, string command) : base (device)
		{
			this.command = command;
		}
		
		protected override void OnGotTransport ()
		{
			var sr = new StringWriter ();
			WriteCommand ("shell:" + command, () => GetStatus (() => ReadResponse (sr, OnGotResponse)));
		}

		void OnGotResponse (TextWriter tw)
		{
			Output = tw.ToString ();
			SetCompleted (true);
		}
		
		public string Output { get; private set; }
	}
	
	public sealed class AdbShellOperation : AdbBaseShellOperation
	{
		public AdbShellOperation (AndroidDevice device, string command) : base (device, command)
		{
			BeginConnect ();
		}
	}
}
