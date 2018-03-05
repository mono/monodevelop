//
// BinaryResultsStore.cs
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
using System.Xml.Serialization;
using System.IO;
using ICSharpCode.NRefactory.Utils;

namespace MonoDevelop.UnitTesting
{
	/// <summary>
	/// ResultsStore implementation that uses binary serializer
	/// </summary>
	public class BinaryResultsStore : AbstractResultsStore
	{
		static BinaryResultsStoreSerializer serializer = new BinaryResultsStoreSerializer();

		public BinaryResultsStore (string directory, string storeId)
			: base(serializer, directory, storeId)
		{
		}
	}

	/// <summary>
	/// Serializer implementation that uses ICSharpCode.NRefactory.Utils.FastSerializer
	/// as it's main method to serialize test records. The serializer is backward compatible
	/// with the old xml-based serialization and will deserialize test record from xml
	/// if the binary form is not yet present.
	/// </summary>
	public class BinaryResultsStoreSerializer : IResultsStoreSerializer
	{
		const string binaryExtension = ".test-result";
		const string xmlExtension = ".xml";

		FastSerializer fastSerializer = new FastSerializer();
		XmlSerializer xmlSerializer;

		XmlSerializer XmlSerializer {
			get {
				if (xmlSerializer == null) {
					xmlSerializer = new XmlSerializer (typeof (TestRecord));
				}

				return xmlSerializer;
			}
		}

		public void Serialize (string xmlFilePath, TestRecord testRecord)
		{
			// no need for xml serialization because next time it will be
			// deserialized from the binary format
			string binaryFilePath = GetBinaryFilePath (xmlFilePath);
			using (var stream = File.OpenWrite(binaryFilePath)) {
				fastSerializer.Serialize (stream, testRecord);
			}
		}

		public TestRecord Deserialize (string xmlFilePath)
		{
			string binaryFilePath = GetBinaryFilePath (xmlFilePath);

			// deserialize from the binary format if the file exists
			if (File.Exists(binaryFilePath)) {
				using (var stream = File.OpenRead (binaryFilePath)) {
					return (TestRecord) fastSerializer.Deserialize (stream);
				}
			}

			// deserialize from xml if the file exists
			if (File.Exists(xmlFilePath)) {
				using (var reader = new StreamReader (xmlFilePath)) {
					return (TestRecord) XmlSerializer.Deserialize (reader);
				}
			}

			return null;
		}

		string GetBinaryFilePath(string xmlFilePath)
		{
			// filename with the binary extension
			return xmlFilePath.Substring (0, xmlFilePath.Length - xmlExtension.Length) + binaryExtension;
		}
	}
}

