// 
// MobileProvision.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Linq;
using System.IO;
using PropertyList;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Security.Cryptography.X509Certificates;

namespace MonoDevelop.IPhone
{


	public class MobileProvision
	{
		public static MobileProvision LoadFromFile (string fileName)
		{
			var m = new MobileProvision ();
			m.Load (fileName);
			return m;
		}
		
		public static IList<MobileProvision> GetAllInstalledProvisions ()
		{
			FilePath directory = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			directory.Combine ("Library", "MobileDevice", "Provisioning Profiles");
			if (!Directory.Exists (directory))
				return new MobileProvision[0];
			
			var list = new List<MobileProvision> ();
			
			foreach (string file in Directory.GetFiles (directory, "*.mobileprovision")) {
				var m = new MobileProvision ();
				try {
					m.Load (file);
				} catch (Exception ex) {
					LoggingService.LogWarning ("Error reading iPhone mobile provision file '" + file +"'", ex);
				}
				list.Add (m);
			}
			return list;
		}

		MobileProvision ()
		{
		}
		
		void Load (string fileName)
		{
			this.FileName = fileName;
			string fileText = File.ReadAllText (fileName);
			
			//find the raw plist within the .mobileprovision file
			int start = fileText.IndexOf ("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			int length;
			if (start < 0 || (length = (fileText.IndexOf ("</plist>", start) - start)) < 1)
				throw new Exception ("Did not find XML plist in '" + fileName + "'");
			
			length += "</plist>".Length;
			string rawPlist = fileText.Substring (start, length);
			
			
			var doc = new PlistDocument ();
			doc.LoadFromXml (rawPlist);
			var rootDict = (PlistDictionary) doc.Root;
			
			var prefixes = rootDict["ApplicationIdentifierPrefix"] as PlistArray;
			if (prefixes != null)
				this.ApplicationIdentifierPrefix = prefixes.OfType<PlistString> ().Select (x => x.Value).ToArray ();
			
			var creationDate = rootDict ["CreationDate"] as PlistDate;
			if (creationDate != null)
				this.CreationDate = creationDate.Value;
			
			var devCerts = rootDict ["DeveloperCertificates"] as PlistArray;
			if (devCerts != null)
				this.DeveloperCertificates = devCerts.OfType<PlistData> ().Select (x => new X509Certificate (x.Value)).ToArray ();
			
			var entl = rootDict ["Entitlements"] as PlistDictionary;
			if (entl != null)
				this.Entitlements = entl; //string application-identifier, bool get-task-allow, string[] keychain-access-groups
			
			var expirationDate = rootDict ["ExpirationDate"] as PlistDate;
			if (expirationDate != null)
				this.ExpirationDate = expirationDate.Value;
			
			var name = rootDict ["Name"] as PlistString;
			if (name != null)
				this.Name = name.Value;
			
			var provDevs = rootDict ["ProvisionedDevices"] as PlistArray;
			if (provDevs != null)
				this.ProvisionedDevices = provDevs.OfType<PlistString> ().Select (x => x.Value).ToArray ();
			
			var ttl = rootDict ["TimeToLive"] as PlistInteger;
			if (ttl != null)
				this.TimeToLive = ttl.Value;
			
			var uuid = rootDict ["UUID"] as PlistString;
			if (uuid != null)
				this.Uuid = uuid.Value;
			
			var version = rootDict ["Version"] as PlistInteger;
			if (version != null)
				this.Version = version.Value;
		}
		
		public string FileName { get; private set; }
		public IList<string> ApplicationIdentifierPrefix { get; private set; }
		public DateTime CreationDate { get; private set; }
		public IList<X509Certificate> DeveloperCertificates { get; private set; }
		public PlistDictionary Entitlements { get; private set; }
		public DateTime ExpirationDate { get; private set; }
		public string Name { get; private set; }
		public IList<string> ProvisionedDevices { get; private set; }
		public int TimeToLive { get; private set; }
		public string Uuid { get; private set; }
		public int Version { get; private set; }
	}
}
