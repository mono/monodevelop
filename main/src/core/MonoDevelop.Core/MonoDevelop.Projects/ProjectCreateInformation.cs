// 
// ProjectCreateInformation.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//       Viktoria Dudka <viktoriad@remobjects.com>
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

using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class ProjectCreateInformation
	{
		string projectName;
		string solutionName;
		FilePath solutionPath;
		FilePath projectBasePath;

		public string ProjectName {
			get { return projectName; }
			set { projectName = value; }
		}

		public string SolutionName {
			get { return solutionName; }
			set { solutionName = value; }
		}

		public FilePath BinPath {
			get { return projectBasePath.Combine ("bin"); }
		}

		public FilePath SolutionPath {
			get { return solutionPath; }
			set { solutionPath = value; }
		}

		public FilePath ProjectBasePath {
			get { return projectBasePath; }
			set { projectBasePath = value; }
		}
		
		public SolutionFolder ParentFolder { get; set; }
		
		public ConfigurationSelector ActiveConfiguration { get; set; }

		public ProjectCreateParameters Parameters { get; set; }

		public ProjectCreateInformation ()
		{
			Parameters = new ProjectCreateParameters ();
		}

		public ProjectCreateInformation (ProjectCreateInformation projectCreateInformation)
		{
			projectName = projectCreateInformation.ProjectName;
			solutionName = projectCreateInformation.SolutionName;
			solutionPath = projectCreateInformation.SolutionPath;
			projectBasePath = projectCreateInformation.ProjectBasePath;
			ParentFolder = projectCreateInformation.ParentFolder;
			ActiveConfiguration = projectCreateInformation.ActiveConfiguration;
			Parameters = projectCreateInformation.Parameters;
		}

		public bool ShouldCreate (string createCondition)
		{
			if (string.IsNullOrWhiteSpace (createCondition))
				return true;

			createCondition = createCondition.Trim ();

			string parameter = GetNotConditionParameterName (createCondition);
			if (parameter != null) {
				return !Parameters.GetBoolean (parameter);
			}

			return Parameters.GetBoolean (createCondition);
		}

		static string GetNotConditionParameterName (string createCondition)
		{
			if (createCondition.StartsWith ("!")) {
				return createCondition.Substring (1).TrimStart ();
			}

			return null;
		}
	}
}
