//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) Microsoft Corp.
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
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Text;

namespace MonoDevelop.Core.Assemblies
{
	class FrameworkInfo
	{
		public TargetFrameworkMoniker Id { get; set; }
		public string Name { get; set; }
		public string IncludeFramework { get; set; }
		public string TargetFrameworkDirectory { get; set; }
		public List<AssemblyInfo> Assemblies { get; set; }
		public List<SupportedFramework> SupportedFrameworks { get; set; }

		public static FrameworkInfo Load (TargetFrameworkMoniker moniker, FilePath frameworkListFile)
		{
			var info = new FrameworkInfo { Id = moniker };

			//for non-cached files, this file is in the RedistList subdir of the assembly dir
			info.TargetFrameworkDirectory = frameworkListFile.ParentDirectory.ParentDirectory;

			using (var reader = XmlReader.Create (frameworkListFile)) {
				if (!reader.ReadToDescendant ("FileList"))
					throw new Exception ("Missing FileList element");

				if (reader.MoveToAttribute ("Name") && reader.ReadAttributeValue ())
					info.Name = reader.ReadContentAsString ();

				if (reader.MoveToAttribute ("IncludeFramework") && reader.ReadAttributeValue ()) {
					string include = reader.ReadContentAsString ();
					if (!string.IsNullOrEmpty (include))
						info.IncludeFramework = include;
				}

				//this is a Mono-specific extension
				if (reader.MoveToAttribute ("TargetFrameworkDirectory") && reader.ReadAttributeValue ()) {
					string targetDir = reader.ReadContentAsString ();
					if (!string.IsNullOrEmpty (targetDir)) {
						targetDir = targetDir.Replace ('\\', Path.DirectorySeparatorChar);
						info.TargetFrameworkDirectory = frameworkListFile.ParentDirectory.Combine (targetDir).FullPath;
					}
				}

				info.Assemblies = new List<AssemblyInfo> ();
				info.SupportedFrameworks = new List<SupportedFramework> ();

				while (reader.Read ()) {
					if (reader.IsStartElement ()) {
						switch (reader.LocalName) {
						case "File":
							info.Assemblies.Add (ReadFileElement (reader));
							break;
						case "SupportedFramework":
							info.SupportedFrameworks.Add (SupportedFramework.LoadFromAttributes (reader));
							break;
						}
					}
				}
			}
			return info;
		}

		static AssemblyInfo ReadFileElement (XmlReader reader)
		{
			var ainfo = new AssemblyInfo ();
			if (reader.MoveToAttribute ("AssemblyName") && reader.ReadAttributeValue ())
				ainfo.Name = reader.ReadContentAsString ();
			if (string.IsNullOrEmpty (ainfo.Name))
				throw new Exception ("Missing AssemblyName attribute");
			if (reader.MoveToAttribute ("Version") && reader.ReadAttributeValue ())
				ainfo.Version = reader.ReadContentAsString ();
			if (reader.MoveToAttribute ("PublicKeyToken") && reader.ReadAttributeValue ())
				ainfo.PublicKeyToken = reader.ReadContentAsString ();
			if (reader.MoveToAttribute ("Culture") && reader.ReadAttributeValue ())
				ainfo.Culture = reader.ReadContentAsString ();
			if (reader.MoveToAttribute ("ProcessorArchitecture") && reader.ReadAttributeValue ())
				ainfo.ProcessorArchitecture = (ProcessorArchitecture)
					Enum.Parse (typeof (ProcessorArchitecture), reader.ReadContentAsString (), true);
			if (reader.MoveToAttribute ("InGac") && reader.ReadAttributeValue ())
				ainfo.InGac = reader.ReadContentAsBoolean ();
			return ainfo;
		}

		public void Save (string path)
		{
			using (var writer = XmlWriter.Create (path, new XmlWriterSettings { Encoding = Encoding.UTF8 })) {
				writer.WriteStartDocument ();
				writer.WriteStartElement ("FileList");
				WriteNonEmptyAttribute ("Name", Name);
				WriteNonEmptyAttribute ("IncludeFramework", IncludeFramework);
				WriteNonEmptyAttribute ("TargetFrameworkDirectory", TargetFrameworkDirectory);
				if (Assemblies.Count > 0) {
					foreach (var asm in Assemblies) {
						writer.WriteStartElement ("File");
						writer.WriteAttributeString ("AssemblyName", asm.Name);
						WriteNonEmptyAttribute ("Version", asm.Version);
						WriteNonEmptyAttribute ("PublicKeyToken", asm.PublicKeyToken);
						WriteNonEmptyAttribute ("Culture", asm.Culture);
						if (asm.ProcessorArchitecture != ProcessorArchitecture.None) {
							writer.WriteAttributeString ("ProcessorArchitecture", asm.ProcessorArchitecture.ToString ());
						}
						if (asm.InGac) {
							writer.WriteAttributeString ("InGac", "true");
						}
						writer.WriteEndElement ();
					}
				}
				if (SupportedFrameworks.Count > 0) {
					foreach (var sf in SupportedFrameworks) {
						sf.SaveAsElement (writer);
					}
				}
				writer.WriteEndDocument ();

				void WriteNonEmptyAttribute (string name, string val)
				{
					if (!string.IsNullOrEmpty (val)) {
						writer.WriteAttributeString (name, val);
					}
				}
			}
		}
	}
}
