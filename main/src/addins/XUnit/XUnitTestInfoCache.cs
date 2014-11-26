//
// XUnitTestInfoCache.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
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
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MonoDevelop.XUnit
{
	/// <summary>
	/// Stores data extracted by test loader on disk.
	/// </summary>
	public class XUnitTestInfoCache
	{
		CachedTestInfo cachedTestInfo = new CachedTestInfo ();

		bool modified = false;
		XUnitAssemblyTestSuite testSuite;

		public XUnitTestInfoCache (XUnitAssemblyTestSuite testSuite)
		{
			this.testSuite = testSuite;
		}

		public bool Exists {
			get {
				return testSuite.CachePath != null && File.Exists (testSuite.CachePath);
			}
		}

		public void SetTestInfo (XUnitTestInfo testInfo)
		{
			if (testInfo != null && File.Exists (testSuite.AssemblyPath)) {
				cachedTestInfo.TestInfo = testInfo;
				cachedTestInfo.LastWriteTime = File.GetLastWriteTime (testSuite.AssemblyPath);
				modified = true;
			}
		}

		public XUnitTestInfo GetTestInfo ()
		{
			if (cachedTestInfo.TestInfo != null && File.Exists (testSuite.AssemblyPath) && File.GetLastWriteTime (testSuite.AssemblyPath) == cachedTestInfo.LastWriteTime) {
				return cachedTestInfo.TestInfo;
			}
			return null;
		}

		public void ReadFromDisk ()
		{
			using (var stream = new FileStream (testSuite.CachePath, FileMode.Open, FileAccess.Read)) {
				var formatter = new BinaryFormatter ();
				cachedTestInfo = (CachedTestInfo)formatter.Deserialize (stream);
				modified = false;
			}
		}

		public void WriteToDisk ()
		{
			if (modified && cachedTestInfo.TestInfo != null) {
				using (var stream = new FileStream (testSuite.CachePath, FileMode.Create, FileAccess.Write)) {
					var formatter = new BinaryFormatter ();
					formatter.Serialize (stream, cachedTestInfo);
				}
			}
		}

		[Serializable]
		class CachedTestInfo
		{
			public DateTime LastWriteTime;
			public XUnitTestInfo TestInfo;
		}
	}
}

