//
// FileTemplateSnippets.cs
//
// Author:
//       Vincent Dondain <vincent.dondain@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc
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

namespace MonoDevelop.Ide.Templates
{
	public static class FileTemplateSnippets
	{
		static string AssembleTemplateBody (string category, string projectType) {
			const string bodyStart = @"<?xml version=""1.0"" encoding=""UTF-8"" ?><Template><TemplateConfiguration><_Name>TemplateName</_Name>";
			const string bodyEnd = @"<LanguageName>C#</LanguageName><_Description>Description</_Description></TemplateConfiguration></Template>";
			return string.Format ("{0}{1}{2}{3}", bodyStart, category, projectType, bodyEnd);
		}

		public static string Template_Simple {
			get {
				return AssembleTemplateBody ("<_Category>iOS</_Category>", "<ProjectType>XamarinIOS</ProjectType>");
			}
		}

		// All these templates will use the same default iOS category.
		public static string Template_Multi_Project_Type {
			get {
				return AssembleTemplateBody ("<_Category>iOS</_Category>", "<ProjectType>XamarinIOS, WatchOS, TVOS</ProjectType>");
			}
		}

		// Correct but only the watchOS category will be created since there is only a WatchOS Project Type.
		public static string Template_Multi_Categories_One_Match {
			get {
				const string category =
					@"<_Category projectType=""XamarinIOS"">iOS</_Category>
					<_Category projectType=""WatchOS"">watchOS</_Category>
					<_Category projectType=""TVOS"">tvOS</_Category>";

				return AssembleTemplateBody (category, "<ProjectType>WatchOS</ProjectType>");
			}
		}

		// Correct, all categories will find the appropriate Project Type.
		public static string Template_Multi_Categories_Full_Match {
			get {
				const string category =
					@"<_Category projectType=""XamarinIOS"">iOS</_Category>
					<_Category projectType=""WatchOS"">watchOS</_Category>
					<_Category projectType=""TVOS"">tvOS</_Category>";

				return AssembleTemplateBody (category, "<ProjectType>XamarinIOS, WatchOS, TVOS</ProjectType>");
			}
		}

		// Correct but we input multiple categories without specifying a project type
		// so we'll take the first one as default. Ignored categories should show a warning in the logs.
		public static string Template_Multi_Categories_Default_First {
			get {
				const string category =
					@"<_Category>iOS</_Category>
					<_Category>watchOS</_Category>
					<_Category>tvOS</_Category>";

				return AssembleTemplateBody (category, "<ProjectType>XamarinIOS</ProjectType>");
			}
		}

		// Correct but the project types don't match so we'll take the first category.
		// Ignored categories should show a warning in the logs.
		public static string Template_Multi_Categories_Default_First_2 {
			get {
				const string category =
					@"<_Category>iOS</_Category>
					<_Category projectType=""WatchOS"">watchOS</_Category>
					<_Category>tvOS</_Category>";

				return AssembleTemplateBody (category, "<ProjectType>XamarinIOS</ProjectType>");
			}
		}
	}
}

