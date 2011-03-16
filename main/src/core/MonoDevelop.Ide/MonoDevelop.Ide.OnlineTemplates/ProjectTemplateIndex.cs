// 
// ProjectTemplateIndex.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Xml.Linq;

//************************ WARNING ************************
// This file should be kept in sync with MDData.Index
// and the index format should not be changed without
// bumping the version and maintaining the old format
//*********************************************************

namespace MonoDevelop.Ide.OnlineTemplates
{
	class ProjectTemplateIndex : List<ProjectTemplateDescription>
	{
		const int FORMAT_VERSION = 1;
		
		ProjectTemplateIndex ()
		{
		}
		
		public ProjectTemplateIndex (string mdVersion)
		{
			this.MDVersion = mdVersion;
		}
		
		public string MDVersion { get; private set; }
		
		public static ProjectTemplateIndex Load (string file)
		{
			var doc = XDocument.Load (file);
			var idx = new ProjectTemplateIndex ();
			
			var root = doc.Root;
			if (root.Name != "TemplateIndex")
				throw new Exception (string.Format ("Root element was {0}, expected '{1}'", root.Name, "ProjectTemplate"));
			if ((int) root.Attribute ("format") != FORMAT_VERSION)
				throw new Exception ("Invalid format version");
			
			idx.MDVersion = (string) root.Attribute ("version");
			
			return idx;
		}
		
		public void Write (string file)
		{
			var doc = new XDocument ();
			doc.Add (new XElement ("TemplateIndex", new XAttribute ("format", FORMAT_VERSION)));
			foreach (var item in this) {
				doc.Root.Add (item.Write ());
			}
			doc.Save (file);
		}
	}
	
	class ProjectTemplateDescription
	{
		public string Name { get; set; }
		public string Summary { get; set; }
		public string Description { get; set; }
		public string Author { get; set; }
		public string Tags { get; set; }
		public string IconUrl { get; set; }
		public string ScreenshotUrl { get; set; }
		public string TemplateUrl { get; set; }
		public DateTime Modified { get; set; }
		
		public ProjectTemplateDescription Read (XElement element)
		{
			return new ProjectTemplateDescription () {
				Name = (string) element.Element ("Name"),
				Summary = (string) element.Element ("Summary"),
				Description = (string) element.Element ("Description"),
				Author = (string) element.Element ("Author"),
				Tags = (string) element.Element ("Tags"),
				IconUrl = (string) element.Element ("IconUrl"),
				TemplateUrl = (string) element.Element ("TemplateUrl"),
				ScreenshotUrl = (string) element.Element ("ScreenshotUrl"),
			};
		}
		
		public XElement Write ()
		{
			return new XElement ("ProjectTemplate",
				new XAttribute ("modified", Modified),
				new XElement ("Name", Name),
				new XElement ("Summary", Summary),
				new XElement ("Description", Description),
				new XElement ("Author", Author),
				new XElement ("Tags", Tags),
				new XElement ("IconUrl", IconUrl),
				new XElement ("TemplateUrl", TemplateUrl),
				new XElement ("ScreenshotUrl", ScreenshotUrl)
			);
		}
	}
	
	class ProjectTemplateManifest
	{
		const int FORMAT_VERSION = 1;
		
		public string Name { get; set; }
		public string Summary { get; set; }
		public string Description { get; set; }
		public string Author { get; set; }
		public string Tags { get; set; }
		public string IconFile { get; set; }
		public string ScreenshotFile { get; set; }
		public string MinimumVersion { get; set; }
		
		public static ProjectTemplateManifest Load (XDocument doc)
		{
			var root = doc.Root;
			if (root.Name != "ProjectTemplate")
				throw new Exception (string.Format ("Root element was {0}, expected '{1}'", root.Name, "ProjectTemplate"));
			if ((int) root.Attribute ("format") != FORMAT_VERSION)
				throw new Exception ("Invalid format version");
			
			return new ProjectTemplateManifest {
				Name = GetRequiredStringElement (root, "Name"),
				Summary = (string) root.Element ("Summary"),
				Description = GetRequiredStringElement (root, "Description"),
				Author = GetRequiredStringElement (root, "Author"),
				Tags = GetRequiredStringElement (root, "Tags"),
				IconFile = (string) root.Element ("IconFile"),
				ScreenshotFile = (string) root.Element ("ScreenshotFile"),
			};
		}
		
		static string GetRequiredStringElement (XElement parent, string name)
		{
			string value = (string) parent.Element (name);
			if (string.IsNullOrWhiteSpace (value))
				throw new Exception (string.Format ("The '{0}' element cannot be empty", name));
			return value;
		}
	}
}

