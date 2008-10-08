// 
// Directive.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.UI;

using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.AspNet.Parser
{
	static class DirectiveCompletion
	{
		public static ICompletionDataProvider GetAttributeValues (string tag, string attribute, ClrVersion clrVersion)
		{
			switch (tag.ToLower ()) {
			case "page":
				return GetPageAttributeValues (attribute, clrVersion);
			}
			return null;
		}
		
		public static ICompletionDataProvider GetAttributes (string tag, ClrVersion clrVersion)
		{
			switch (tag.ToLower ()) {
			case "page":
				return GetPageAttributes (clrVersion);
			}
			return null;
		}
		
		static ICompletionDataProvider GetPageAttributes (ClrVersion clrVersion)
		{
			List<CompletionData> list = new List<CompletionData> ();
			foreach (string s in new string[] {
				"Async",
				"AspCompat",
				//"Explicit",
				"MaintainScrollPositionOnPostback"
				})
				list.Add (new CompletionData (s));
			
			return new SimpleCompletionDataProvider (list.ToArray (), null);
		}
		
		static ICompletionDataProvider GetPageAttributeValues (string attribute, ClrVersion clrVersion)
		{
			switch (attribute.ToLower ()) {
			
			//
			//boolean, default to false
			//
			case "Async":
			case "AspCompat":
			case "Explicit": // useful for VB only. set to true in machine.config
			case "MaintainScrollPositionOnPostback":
			case "LinePragmas": //actually not sure if this defaults true or false
			case "SmartNavigation":
			case "Strict": //VB ONLY 
			case "Trace":
				return GetBooleanProvider (false);
			
			//
			//boolean, default to true
			//
			case "AutoEventWireup":
			case "Buffer":	
			case "EnableEventValidation":
			case "EnableSessionState":
			case "EnableTheming":
			case "EnableViewState":
			case "EnableViewStateMac":
			case "ValidateRequest": //enabled in machine.config
			case "Debug":
				return GetBooleanProvider (true);
			
			//
			//specialised hard value list completions
			//
			case "CodePage":
				return GetProvider (Encoding.UTF8.CodePage,
					from EncodingInfo e in Encoding.GetEncodings () select e.CodePage);
				
			case "CompilationMode":
				return GetEnumProvider<System.Web.UI.CompilationMode> (System.Web.UI.CompilationMode.Always);
				
			case "Culture":
				return GetProvider (CultureInfo.CurrentCulture.Name,
					from CultureInfo c in CultureInfo.GetCultures (CultureTypes.AllCultures) select c.Name);
			
			case "LCID":
				//  locale ID, MUTUALLY EXCLUSIVE with Culture
				return GetProvider (CultureInfo.CurrentCulture.LCID,
					from CultureInfo c in CultureInfo.GetCultures (CultureTypes.AllCultures) select c.LCID);
			
			case "ResponseEncoding":
				return GetProvider (Encoding.UTF8.EncodingName,
					from EncodingInfo e in Encoding.GetEncodings () select e.Name);
			
			case "TraceMode":
				return GetProvider ("SortByTime", new string[] { "SortByTime", "SortByCategory" });
			
			case "Transaction":
				return GetProvider ("Disabled",
				                          new string[] { "Disabled", "NotSupported", "Required", "RequiresNew" });
			
			case "ViewStateEncryptionMode":
				return GetEnumProvider<ViewStateEncryptionMode> (ViewStateEncryptionMode.Auto);
				
			case "WarningLevel":
				return GetProvider ("0", new string[] { "1", "2", "3", "4" });	
			
			//
			//we can probably complete these using info from the project, but not yet
			//
			/*
			case "CodeFile":
				//source file to compile for codebehind on server	
			case "ContentType":
				//string, HTTP MIME content-type
			case "CodeFileBaseClass":
				// known base class for the partial classes, so code generator knows not 
				//to redefine fields to ignore members in partial class	
			case "ErrorPage":
				// string, URL
			case "Inherits":
				//  IType : Page. defaults to namespace from ClassName 
			case "Language":
				//  string, any available .NET langauge	
			case "MasterPageFile":
				//  master page path
			case "Src":
				//  string, extra source code for page
			case "StyleSheetTheme":
				//  theme ID, can be overridden by controls
			case "Theme":
				//  theme identifier, overrides controls' themes
			case "UICulture":
				//  string, valid UI culture	
			*/
			
			//
			//we're not likely to be able to complete these:
			//
			/*
			case "AsyncTimeOut":
				// int in seconds, default 45
			case "ClassName":
				//string, .NET name, default namespace ASP
			case "ClientTarget":
				//string, user agent
			case "CodeBehind":
				//valid but IGNORE. VS-only
			case "CompilerOptions":
				//string, list of compiler switches
			case "Description":
				// string, pointless
			case "TargetSchema":
				//  schema to validate page content. IGNORED, so rather pointless
			case "Title":
				//  string for <title>
			*/	
			}
			
			return null;
		}
		
		
		
		static ICompletionDataProvider GetBooleanProvider (bool defaultValue)
		{
			CodeCompletionDataProvider provider = null;//new CodeCompletionDataProvider ();
//			provider.Add (new CodeCompletionData ("true", "md-literal"));
//			provider.Add (new CodeCompletionData ("false", "md-literal"));
			provider.DefaultCompletionString = defaultValue? "true" : "false";
			return provider;
		}
		
		static ICompletionDataProvider GetEnumProvider<T> (T defaultValue)
		{
			CodeCompletionDataProvider provider = null;//new CodeCompletionDataProvider ();
			foreach (string name in Enum.GetNames (typeof (T))) {
//				provider.Add (new CodeCompletionData (name, "md-literal"));
			}
			provider.DefaultCompletionString = defaultValue.ToString ();
			return provider;
		}
		
		static ICompletionDataProvider GetProvider (string defaultValue, IEnumerable<string> vals)
		{
			CodeCompletionDataProvider provider = null;//new CodeCompletionDataProvider ();
			foreach (string v in vals) {
//				provider.Add (new CodeCompletionData (v, "md-literal"));
			}
			provider.DefaultCompletionString = defaultValue;
			return provider;
		}
		
		static ICompletionDataProvider GetProvider (int defaultValue, IEnumerable<int> vals)
		{
			return GetProvider (defaultValue.ToString (), from s in vals select s.ToString ());
		}
		/*
		
		
		Async = false, bool
AsyncTimeOut = 45, int
AspCompat = false, bool 
AutoEventWireup = true, bool
Buffer = true, bool
ClassName = string, .NET name, default namespace ASP
ClientTarget = string, user agent
CodeBehind = valid but IGNORE. VS-only
CodeFile = file to compile for codebehind on server
CodeFileBaseClass = known base class for the partial classes, so code generator knows not to redefine fields to ignore members in partial class
CodePage = int, Encoding
CompilationMode = enum, Always
CompilerOptions = string, list of compiler switches
ContentType = string, HTTP MIME content-type
Culture = CultureInfo
Debug = bool, true
Description = string, pointless
EnableEventValidation = bool, true
EnableSessionState = bool, true
EnableTheming = bool, true
EnableViewState = bool, true
EnableViewStateMac = bool, true
ErrorPage = string, URL
Explicit = bool, false // useful for VB only. set to true in machine.config
Inherits = IType : Page. defaults to namespace from ClassName 
Language = string, any .NET langauge
LCID = locale ID, MUTUALLY EXCLUSIVE with Culture
LinePragmas = bool, unknown
MaintainScrollPositionOnPostback = bool, false
MasterPageFile = master page path
ResponseEncoding = string, Encoding
SmartNavigation = bool, false 
Src = string, extra source code for page
Strict = bool, false  //VB ONLY 
StyleSheetTheme == theme ID, can be overridden by controls
TargetSchema == schema to validate page content. IGNORED, so rather pointless
Theme = theme identifier, overrides controls' themes
Title = string for <title>
Trace = bool, false
TraceMode = string: SortByTime, SortByCategory
Transaction = string: Disabled, NotSupported, Supported, Required, RequiresNew. Default disabled
UICulture = string, valid UI culture 
ValidateRequest = bool, true //enabled in machine.config
ViewStateEncryptionMode = ViewStateEncryptionMode:: Auto, Always, or Never.  Default auto.
WarningLevel 0,1,2,3,4

*/
	}
}
