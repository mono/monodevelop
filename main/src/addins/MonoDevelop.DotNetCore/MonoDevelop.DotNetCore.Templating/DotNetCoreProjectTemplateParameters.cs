//
// DotNetCoreTemplate.cs
//
// Author:
//       iantoal <iantoal@microsoft.com>
//
// Copyright (c) 2020 Microsoft
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.DotNetCore.Templating
{
	class AuthenticationParameter
	{
		static readonly string [] supportedParameters = new string [] { "None", "Individual" };

		public static readonly string ParameterName = "auth";

		public string Name { get; }

		public string Description => Name switch
		{
			"None" => GettextCatalog.GetString ("No Authentication"),
			"Individual" => GettextCatalog.GetString ("Individual Authentication (in\u2013app)"),
			_ => throw new ArgumentException ("Invalid auth parameter"),
		};

		public string Information => Name switch
		{
			"None" => GettextCatalog.GetString ("Select this option for applications that do not require any authentication."),
			"Individual" => GettextCatalog.GetString ("Select this option to create a project that includes a local user account store."),
			_ => throw new ArgumentException ("Invalid auth parameter"),
		};

		public AuthenticationParameter (string name)
		{
			Name = name;
		}

		public static IReadOnlyList<AuthenticationParameter> CreateSupportedParameterList (IReadOnlyDictionary<string, string> parameterChoices)
		{
			var filteredList = parameterChoices.Where (choice => supportedParameters.Contains (choice.Key))
				.Select (parameter => new AuthenticationParameter (parameter.Key))
				.ToList ();

			return (filteredList.Count == 1 && filteredList.First ().Name == "None") ? new List<AuthenticationParameter> () : filteredList;
		}
	}

	class DotNetCoreProjectTemplateParameters
	{
		public static IReadOnlyList<AuthenticationParameter> GetAuthenticationParameters (string templateId)
		{
			if (IdeServices.TemplatingService.GetSolutionTemplate (templateId) is MicrosoftTemplateEngineSolutionTemplate template) {
				if (template.IsSupportedParameter (AuthenticationParameter.ParameterName)) {
					var parameterChoices = template.GetParameterChoices (AuthenticationParameter.ParameterName);
					return AuthenticationParameter.CreateSupportedParameterList (parameterChoices);
				}
			}

			return new List<AuthenticationParameter> ();
		}
	}
}
