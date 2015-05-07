//
// TemplateWizard.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	public abstract class TemplateWizard
	{
		public abstract string Id { get; }

		public abstract WizardPage GetPage (int pageNumber);

		public virtual void ConfigureWizard ()
		{
		}

		public virtual int TotalPages {
			get { return 1; }
		}

		ProjectCreateParameters parameters;

		public ProjectCreateParameters Parameters {
			get {
				if (parameters == null) {
					parameters = new ProjectCreateParameters ();
				}
				return parameters;
			}
			set { parameters = value; }
		}

		List<string> supportedParameters;

		internal void UpdateParameters (SolutionTemplate template)
		{
			Parameters ["TemplateName"] = template.Name;
			UpdateSupportedParameters (template.SupportedParameters);
			UpdateDefaultParameters (template.DefaultParameters);
		}

		void UpdateSupportedParameters (string parameters)
		{
			if (String.IsNullOrEmpty (parameters)) {
				supportedParameters = null;
				return;
			}

			supportedParameters = new List<string> ();
			foreach (string part in parameters.Split (new [] {',', ';'}, StringSplitOptions.RemoveEmptyEntries)) {
				supportedParameters.Add (part.Trim ());
			}
		}

		public bool IsSupportedParameter (string name)
		{
			if (supportedParameters == null) {
				return true;
			}

			return supportedParameters.Contains (name);
		}

		void UpdateDefaultParameters (string parameters)
		{
			if (String.IsNullOrEmpty (parameters)) {
				return;
			}

			foreach (TemplateParameter parameter in GetValidParameters (parameters)) {
				Parameters [parameter.Name] = parameter.Value;
			}
		}

		static IEnumerable<TemplateParameter> GetValidParameters (string parameters)
		{
			return TemplateParameter.CreateParameters (parameters)
				.Where (parameter => parameter.IsValid);
		}

		public virtual void ItemsCreated (IEnumerable<IWorkspaceFileObject> items)
		{
		}
	}
}

