//
// XmlResultsStore.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections;
using System.Xml.Serialization;
using System.Globalization;

namespace MonoDevelop.NUnit
{
	public class XmlResultsStore: IResultsStore
	{
		Hashtable fileCache = new Hashtable ();
		string basePath;
		string storeId;
		Hashtable cachedRootList = new Hashtable ();
		
		static XmlSerializer serializer = new XmlSerializer (typeof(TestRecord));
		
		public XmlResultsStore (string directory, string storeId)
		{
			basePath = directory;
			this.storeId = storeId;
		}
		
		public void RegisterResult (string configuration, UnitTest test, UnitTestResult result)
		{
			string aname = test.StoreRelativeName;
			
			TestRecord root = GetRootRecord (configuration, result.TestDate);
			if (root == null) {
				root = new TestRecord ();
				fileCache [GetRootFileName (configuration, result.TestDate)] = root;
			}
			root.Modified = true;
			TestRecord record = root;
			
			if (aname.Length > 0) {
				string[] path = test.StoreRelativeName.Split ('.');
				foreach (string p in path) {
					TestRecord ctr = record.Tests != null ? record.Tests [p] : null;
					if (ctr == null) {
						ctr = new TestRecord ();
						ctr.Name = p;
						if (record.Tests == null)
							record.Tests = new TestRecordCollection ();
						record.Tests.Add (ctr);
					}
					record = ctr;
				}
			}
			
			if (record.Results == null)
				record.Results = new UnitTestResultCollection ();
			record.Results.Add (result);
		}
		
		public UnitTestResult GetNextResult (string configuration, UnitTest test, DateTime date)
		{
			DateTime currentDate = date;
			TestRecord root = GetRootRecord (configuration, currentDate);
			if (root == null)
				root = GetNextRootRecord (configuration, ref currentDate);
			
			while (root != null) {
				TestRecord tr = FindRecord (root, test.StoreRelativeName);
				if (tr != null && tr.Results != null) {
					foreach (UnitTestResult res in tr.Results) {
						if (res.TestDate > date)
							return res;
					}
				}
				root = GetNextRootRecord (configuration, ref currentDate);
			}
			return null;
		}
		
		public UnitTestResult GetPreviousResult (string configuration, UnitTest test, DateTime date)
		{
			DateTime currentDate = date;
			TestRecord root = GetRootRecord (configuration, currentDate);
			if (root == null)
				root = GetPreviousRootRecord (configuration, ref currentDate);
			
			while (root != null) {
				TestRecord tr = FindRecord (root, test.StoreRelativeName);
				if (tr != null && tr.Results != null) {
					for (int n = tr.Results.Count - 1; n >= 0; n--) {
						UnitTestResult res = (UnitTestResult) tr.Results [n];
						if (res.TestDate < date)
							return res;
					}
				}
				root = GetPreviousRootRecord (configuration, ref currentDate);
			}
			return null;
		}
		
		public UnitTestResult GetLastResult (string configuration, UnitTest test, DateTime date)
		{
			return GetPreviousResult (configuration, test, date.AddTicks (1));
		}
		
		public UnitTestResult[] GetResults (string configuration, UnitTest test, DateTime startDate, DateTime endDate)
		{
			ArrayList list = new ArrayList ();
			DateTime firstDay = new DateTime (startDate.Year, startDate.Month, startDate.Day);
			
			DateTime[] dates = GetStoreDates (configuration);
			
			foreach (DateTime date in dates) {
				if (date < firstDay)
					continue;
				if (date > endDate)
					break;
				
				TestRecord root = GetRootRecord (configuration, date);
				if (root == null) continue;

				TestRecord tr = FindRecord (root, test.StoreRelativeName);
				if (tr != null && tr.Results != null) {
					foreach (UnitTestResult res in tr.Results) {
						if (res.TestDate >= startDate && res.TestDate <= endDate)
							list.Add (res);
					}
				}
			}
			
			return (UnitTestResult[]) list.ToArray (typeof(UnitTestResult));
		}
		
		public UnitTestResult[] GetResultsToDate (string configuration, UnitTest test, DateTime endDate, int count)
		{
			ArrayList list = new ArrayList ();
			DateTime[] dates = GetStoreDates (configuration);
			
			for (int n = dates.Length - 1; n >= 0 && list.Count < count; n--) {
				if (dates [n] > endDate)
					continue;
					
				TestRecord root = GetRootRecord (configuration, dates [n]);
				if (root == null) continue;

				TestRecord tr = FindRecord (root, test.StoreRelativeName);
				if (tr != null && tr.Results != null) {
					for (int m = tr.Results.Count - 1; m >= 0 && list.Count < count; m--) {
						UnitTestResult res = (UnitTestResult) tr.Results [m];
						if (res.TestDate <= endDate)
							list.Add (res);
					}
				}
			}
			
			UnitTestResult[] array = (UnitTestResult[]) list.ToArray (typeof(UnitTestResult));
			Array.Reverse (array);
			return array;
		}
		
		public void Save ()
		{
			if (!Directory.Exists (basePath))
				Directory.CreateDirectory (basePath);

			foreach (DictionaryEntry entry in fileCache) {
				TestRecord record = (TestRecord) entry.Value;
				if (!record.Modified)
					continue;

				string file = Path.Combine (basePath, (string)entry.Key);
				StreamWriter writer = new StreamWriter (file);
				try {
					serializer.Serialize (writer, record);
				} finally {
					writer.Close ();
				}
			}
			cachedRootList.Clear ();
		}
		
		TestRecord FindRecord (TestRecord root, string aname)
		{
			if (aname.Length == 0)
				return root;
			else {
				string[] path = aname.Split ('.');
				TestRecord tr = root;
				foreach (string p in path) {
					if (tr.Tests == null)
						return null;
					tr = tr.Tests [p];
					if (tr == null)
						return null;
				}
				return tr;
			}
		}
		
		TestRecord GetRootRecord (string configuration, DateTime date)
		{
			string file = GetRootFileName (configuration, date);
			TestRecord res = (TestRecord) fileCache [file];
			if (res != null)
				return res;
			
			string filePath = Path.Combine (basePath, file);
			if (!File.Exists (filePath))
				return null;

			StreamReader s = new StreamReader (filePath);
			try {
				res = (TestRecord) serializer.Deserialize (s);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return null;
			} finally {
				s.Close ();
			}
			fileCache [file] = res;
			return res;
		}
		
		TestRecord GetNextRootRecord (string configuration, ref DateTime date)
		{
			DateTime[] dates = GetStoreDates (configuration);
			foreach (DateTime d in dates) {
				if (d > date) {
					date = d;
					return GetRootRecord (configuration, d);
				}
			}
			return null;
		}
		
		TestRecord GetPreviousRootRecord (string configuration, ref DateTime date)
		{
			date = new DateTime (date.Year, date.Month, date.Day);
			DateTime[] dates = GetStoreDates (configuration);
			for (int n = dates.Length - 1; n >= 0; n--) {
				if (dates [n] < date) {
					date = dates [n];
					return GetRootRecord (configuration, dates [n]);
				}
			}
			return null;
		}
		
		string GetRootFileName (string configuration, DateTime date)
		{
			return storeId + "-" + configuration + "-" + date.ToString ("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".xml";
		}
		
		DateTime ParseFileNameDate (string configuration, string fileName)
		{
			fileName = Path.GetFileNameWithoutExtension (fileName);
			fileName = fileName.Substring (storeId.Length + configuration.Length + 2);
			return DateTime.ParseExact (fileName, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
		
		DateTime[] GetStoreDates (string configuration)
		{
			if (!Directory.Exists (basePath))
				return new DateTime [0];
			
			DateTime[] res = (DateTime[]) cachedRootList [configuration];
			if (res != null)
				return res;

			ArrayList dates = new ArrayList ();
			foreach (string file in Directory.GetFiles (basePath, storeId + "-" + configuration + "-*")) {
				try {
					DateTime t = ParseFileNameDate (configuration, Path.GetFileName (file));
					dates.Add (t);
				} catch { }
			}
			res = (DateTime[]) dates.ToArray (typeof(DateTime));
			cachedRootList [configuration] = res;
			return res;
		}
	}
	
	public class TestRecord
	{
		string name;
		UnitTestResultCollection results;
		TestRecordCollection tests;
		internal bool Modified;
		
		[XmlAttribute]
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public UnitTestResultCollection Results {
			get { return results; }
			set { results = value; }
		}
		
		public TestRecordCollection Tests {
			get { return tests; }
			set { tests = value; }
		}
	}
	
	public class TestRecordCollection: CollectionBase
	{
		public new TestRecord this [int n] {
			get { return (TestRecord) ((IList)this) [n]; }
		}
		
		public new TestRecord this [string name] {
			get {
				for (int n=0; n<List.Count; n++)
					if (((TestRecord)List [n]).Name == name)
						return (TestRecord) List [n];
				return null;
			}
		}
		
		public void Add (TestRecord test)
		{
			((IList)this).Add (test);
		}
	}
	
	public class UnitTestResultCollection: CollectionBase
	{
		public new UnitTestResult this [int n] {
			get { return (UnitTestResult) ((IList)this) [n]; }
		}
		
		public void Add (UnitTestResult test)
		{
			((IList)this).Add (test);
		}
	}	
}

