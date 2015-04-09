//
// MSBuildImport.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

using System.Xml;


namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildImport: MSBuildObject
	{
		string evaluatedProjectPath;
		XmlElement fakeImport;
		string originalCondition;

		public MSBuildImport (XmlElement elem): base (elem)
		{
		}

		public string Project {
			get { return Element.GetAttribute ("Project"); }
			set { Element.SetAttribute ("Project", value); }
		}

		public string EvaluatedProject {
			get { return evaluatedProjectPath ?? Project; }
		}

		internal void SetEvalResult (string evaluatedProjectPath)
		{
			this.evaluatedProjectPath = evaluatedProjectPath;
		}

		internal override void OnEvaluationStarting ()
		{
			base.OnEvaluationStarting ();

			var newTarget = MSBuildProjectService.GetImportRedirect (Project);
			if (newTarget != null) {
				originalCondition = Condition;

				// If an import redirect exists, add a fake import to the project which will be used only
				// if the original import doesn't exist.

				// Modify the original import by adding a condition, so that this import will be used only
				// if the targets file exists.

				string cond = "Exists('" + Project + "')";
				if (!string.IsNullOrEmpty (originalCondition))
					cond = "( " + originalCondition + " ) AND " + cond;
				Element.SetAttribute ("Condition", cond);

				// Now add the fake import, with a condition so that it will be used only if the original
				// import does not exist.

				fakeImport = Element.OwnerDocument.CreateElement ("Import", MSBuildProject.Schema);

				cond = "!Exists('" + Project + "')";
				if (!string.IsNullOrEmpty (originalCondition))
					cond = "( " + originalCondition + " ) AND " + cond;

				fakeImport.SetAttribute ("Project", MSBuildProjectService.ToMSBuildPath (null, newTarget));
				fakeImport.SetAttribute ("Condition", cond);
				Element.ParentNode.InsertAfter (fakeImport, Element);
				return;
			}
		}

		internal override void OnEvaluationFinished ()
		{
			base.OnEvaluationFinished ();
			if (fakeImport != null) {

				// Remove the fake import and restore the original import condition

				fakeImport.ParentNode.RemoveChild (fakeImport);
				fakeImport = null;

				if (!string.IsNullOrEmpty (originalCondition))
					Element.SetAttribute ("Condition", originalCondition);
				else
					Element.RemoveAttribute ("Condition");
			}
		}
	}

}
