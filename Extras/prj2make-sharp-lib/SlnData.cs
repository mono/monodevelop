//
// SlnData.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects;

using System.Collections.Generic;

namespace MonoDevelop.Prj2Make
{
	class SlnData
	{
		string guid;
		Dictionary<CombineConfiguration, string> configStrings;
		List<string> globalExtra; // unused GlobalSections
		Dictionary<string, List<string>> sectionExtras;
		List<string> extra; //used by solution folders..
		List<string> unknownProjects;

		public string Guid {
			get { return guid; }
			set { guid = value; }
		}

		public Dictionary<CombineConfiguration, string> ConfigStrings {
			get {
				if (configStrings == null)
					configStrings = new Dictionary<CombineConfiguration, string> ();
				return configStrings;
			}
		}

		public List<string> GlobalExtra {
			get { return globalExtra; }
			set { globalExtra = value; }
		}

		public List<string> Extra {
			get { return extra; }
			set { extra = value; }
		}
		
		public List<string> UnknownProjects {
			get {
				if (unknownProjects == null)
					unknownProjects = new List<string> ();
				return unknownProjects;
			}
		}

		//Extra lines per section which need to be preserved
		//eg. lines in ProjectConfigurationPlatforms for projects
		//that we couldn't load
		public Dictionary<string, List<string>> SectionExtras {
			get {
				if (sectionExtras == null)
					sectionExtras = new Dictionary<string, List<string>> ();
				return sectionExtras;
			}
		}

	}
}
