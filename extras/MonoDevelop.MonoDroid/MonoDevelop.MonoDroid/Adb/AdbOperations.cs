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
	public class AdbCheckVersionOperation : AdbOperation
	{
		const string SUPPORTED_PROTOCOL = "001a";
		
		public AdbCheckVersionOperation () : base ("host:version") {}
		
		protected override void OnGotResponse (string response, ref bool readAgain)
		{
			if (response != SUPPORTED_PROTOCOL)
				throw new Exception ("Unsupported adb protocol: " + response);
		}
	}
	
	public class AdbTrackDevicesOperation : AdbOperation
	{
		public AdbTrackDevicesOperation () : base ("host:track-devices") {}
		
		protected override void OnGotResponse (string response, ref bool readAgain)
		{
			readAgain = true;
			Devices = Parse (response);
			var ev = DevicesChanged;
			if (ev != null)
				ev (Devices);
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
}