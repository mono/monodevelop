// 
// PcFileCache.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Core.Assemblies
{
	class PcFileCache
	{
		Dictionary<string, SystemPackageInfo> infos = new Dictionary<string, SystemPackageInfo> ();
		string cacheFile;
		bool hasChanges;
		
		public PcFileCache()
		{
			cacheFile = Path.Combine (PropertyService.ConfigPath, "pkgconfig-cache.xml");
			try {
				if (File.Exists (cacheFile)) {
					using (StreamReader sr = new StreamReader (cacheFile)) {
						XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
						infos = (Dictionary<string, SystemPackageInfo>) ser.Deserialize (sr, typeof(Dictionary<string, SystemPackageInfo>));
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("pc file cache could not be loaded.", ex);
			}
		}
		
		public SystemPackageInfo GetPackageInfo (string file)
		{
			lock (infos) {
				SystemPackageInfo info;
				if (infos.TryGetValue (Path.GetFullPath (file), out info)) {
					try {
						if (info.LastWriteTime == File.GetLastWriteTime (file))
							return info;
					} catch (Exception ex) {
						LoggingService.LogError ("", ex);
					}
				}
				return null;
			}
		}
		
		public void StorePackageInfo (string file, SystemPackageInfo info)
		{
			lock (infos) {
				if (!info.IsValidPackage)
					info = new SystemPackageInfo (); // Create a default empty instance
				info.LastWriteTime = File.GetLastWriteTime (file);
				infos [Path.GetFullPath (file)] = info;
				hasChanges = true;
			}
		}
		
		public void Save ()
		{
			lock (infos) {
				if (!hasChanges)
					return;
				using (StreamWriter sw = new StreamWriter (cacheFile)) {
					XmlTextWriter tw = new XmlTextWriter (sw);
					tw.Formatting = Formatting.Indented;
					XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
					ser.Serialize (tw, infos);
					hasChanges = false;
				}
			}
		}
		
		public object SyncRoot {
			get { return infos; }
		}
	}
}
