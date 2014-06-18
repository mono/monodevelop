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
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.AspNet.Projects;

namespace MonoDevelop.AspNet.WebForms
{
	static class WebFormsDirectiveCompletion
	{
		static void AddBoolean (CompletionDataList list, bool defaultValue)
		{
			list.Add ("true", "md-literal");
			list.Add ("false", "md-literal");
			list.DefaultCompletionString = defaultValue? "true" : "false";
		}

		static void AddEnum<T> (CompletionDataList list, T defaultValue)
		{
			foreach (string name in Enum.GetNames (typeof (T))) {
				list.Add (name, "md-literal");
			}
			list.DefaultCompletionString = defaultValue.ToString ();
		}

		//
		// NOTE: MS' documentation for directives is at http://msdn.microsoft.com/en-us/library/t8syafc7.aspx
		//
		// FIXME: gettextise this
		public static CompletionDataList GetDirectives (WebSubtype type)
		{
			CompletionDataList list = new CompletionDataList ();
			
			if (type == WebSubtype.WebForm) {
				list.Add ("Implements", null, "Declare that this page implements an interface.");
				list.Add ("Page", null, "Define properties of this page.");
				list.Add ("PreviousPageType", null, "Strongly type the page's PreviousPage property.");
				list.Add ("MasterType", null, "Strongly type the page's Master property.");
			} else if (type == WebSubtype.MasterPage) {
				list.Add ("Implements", null, "Declare that this master page implements an interface.");
				list.Add ("Master", null, "Define properties of this master page.");
				list.Add ("MasterType", null, "Strongly type the page's Master property.");
			} else if (type == WebSubtype.WebControl) {
				list.Add ("Control", null, "Define properties of this user control.");
				list.Add ("Implements", null, "Declare that this control implements an interface.");
			} else {
				return null;
			}
			
			list.Add ("Assembly", null, "Reference an assembly.");
			list.Add ("Import", null, "Import a namespace.");
			
			if (type != WebSubtype.MasterPage) {
				list.Add ("OutputCache", null, "Set output caching behaviour.");
			}
			
			list.Add ("Reference", null, "Reference a page or user control.");
			list.Add ("Register", null, "Register a user control or custom web controls.");
			
			return list.Count > 0? list : null;
		}
		

		public static CompletionDataList GetAttributeValues (AspNetAppProject project, FilePath fromFile, string directiveName, string attribute)
		{
			switch (directiveName.ToLowerInvariant ()) {
			case "page":
				return GetPageAttributeValues (project, fromFile, attribute);
			case "register":
				return GetRegisterAttributeValues (project, fromFile, attribute);
			}
			return null;
		}
		
		public static CompletionDataList GetAttributes (AspNetAppProject project, string directiveName,
			Dictionary<string, string> existingAtts)
		{
			var list = new CompletionDataList ();
			bool net20 = project == null || project.TargetFramework.ClrVersion != ClrVersion.Net_1_1;
			
			//FIXME: detect whether the page is VB
			bool vb = false;
			
			switch (directiveName.ToLowerInvariant ()) {
			case "page":
				ExclusiveAdd (list, existingAtts, page11Attributes);
				if (net20)
					ExclusiveAdd (list, existingAtts, page20Attributes);
				if (vb)
					ExclusiveAdd (list, existingAtts, pageVBAttributes);
				MutexAdd (list, existingAtts, page11MutexAttributes);
				break;
				
			case "control":
				ExclusiveAdd (list, existingAtts, userControlAttributes);
				if (vb)
					ExclusiveAdd (list, existingAtts, pageVBAttributes);
				break;
				
			case "master":
				ExclusiveAdd (list, existingAtts, userControlAttributes);
				ExclusiveAdd (list, existingAtts, masterControlAttributes);
				if (vb)
					ExclusiveAdd (list, existingAtts, pageVBAttributes);
				break;
			
			case "mastertype":
				MutexAdd (list, existingAtts, mastertypeAttributes);
				break;
				
			case "assembly":
				//the two assembly directive attributes are mutually exclusive
				MutexAdd (list, existingAtts, assemblyAttributes);
				break;
				
			case "import":
				ExclusiveAdd (list, existingAtts, importAttributes);
				break;
				
			case "reference":
				MutexAdd (list, existingAtts, referenceAttributes);
				break;
				
			case "register":
				ExclusiveAdd (list, existingAtts, registerAttributes);
				if (existingAtts.Keys.Intersect (registerAssemblyAttributes, StringComparer.OrdinalIgnoreCase).Any ()) {
					ExclusiveAdd (list, existingAtts, registerAssemblyAttributes);
				} else if (existingAtts.Keys.Intersect (registerUserControlAttributes, StringComparer.OrdinalIgnoreCase).Any ()) {
					ExclusiveAdd (list, existingAtts, registerUserControlAttributes);
				} else {
					list.AddRange (registerAssemblyAttributes);
					list.AddRange (registerUserControlAttributes);
				}
				break;
				
			case "outputcache":
				ExclusiveAdd (list, existingAtts, outputcacheAttributes);
				break;
				
			case "previouspagetype":
				MutexAdd (list, existingAtts, previousPageTypeAttributes);
				break;
				
			case "implements":
				ExclusiveAdd (list, existingAtts, implementsAttributes);
				break;
			}
			return list.Count > 0? list : null;
		}
		
		static void ExclusiveAdd (CompletionDataList list, Dictionary<string, string> existingAtts,
		                          IEnumerable<string> values)
		{
			foreach (string s in values)
				if (!existingAtts.ContainsKey (s))
					list.Add (s);
		}
		
		static void MutexAdd (CompletionDataList list, Dictionary<string, string> existingAtts,
		                      IEnumerable<string> mutexValues)
		{
			foreach (string s in mutexValues)
				if (existingAtts.ContainsKey (s))
					return;
			foreach (string s in mutexValues)
				list.Add (s);
		}
		
		static CompletionDataList GetPageAttributeValues (AspNetAppProject project, FilePath fromFile, string attribute)
		{
			var list = new CompletionDataList ();
			switch (attribute.ToLowerInvariant ()) {
			
			//
			//boolean, default to false
			//
			case "async":
			case "aspcompat":
			case "explicit": // useful for VB only. set to true in machine.config
			case "maintainscrollpositiononpostback":
			case "linepragmas": //actually not sure if this defaults true or false
			case "smartnavigation":
			case "strict": //VB ONLY 
			case "trace":
				AddBoolean (list, false);
				break;
			
			//
			//boolean, default to true
			//
			case "autoeventwireup":
			case "buffer":	
			case "enableeventvalidation":
			case "enablesessionstate":
			case "enabletheming":
			case "enableviewstate":
			case "enableviewstatemac":
			case "validaterequest": //enabled in machine.config
			case "debug":
				AddBoolean (list, true);
				break;
			
			//
			//specialised hard value list completions
			//
			case "codepage":
				list.AddRange (from e in Encoding.GetEncodings () select e.CodePage.ToString ());
				list.DefaultCompletionString = Encoding.UTF8.CodePage.ToString ();
				break;
				
			case "compilationmode":
				AddEnum (list, System.Web.UI.CompilationMode.Always);
				break;
				
			case "culture":
				list.AddRange (from c in CultureInfo.GetCultures (CultureTypes.AllCultures) select c.Name);
				list.DefaultCompletionString = CultureInfo.CurrentCulture.Name;
				break;
			
			case "lcid":
				//  locale ID, MUTUALLY EXCLUSIVE with Culture
				list.AddRange (from c in CultureInfo.GetCultures (CultureTypes.AllCultures)
				               select c.LCID.ToString ());
				list.DefaultCompletionString = CultureInfo.CurrentCulture.LCID.ToString ();
				break;
			
			case "responseencoding":
				list.AddRange (from e in Encoding.GetEncodings () select e.Name);
				list.DefaultCompletionString = Encoding.UTF8.EncodingName;
				break;
			
			case "tracemode":
				list.Add ("SortByTime");
				list.Add ("SortByCategory");
				list.DefaultCompletionString = "SortByTime";
				break;
			
			case "transaction":
				list.Add ("Disabled");
				list.Add ("NotSupported");
				list.Add ("Required");
				list.Add ("RequiresNew");
				list.DefaultCompletionString = "Disabled";
				break;
			
			case "viewstateencryptionmode":
				AddEnum (list, ViewStateEncryptionMode.Auto);
				break;
				
			case "warninglevel":
				list.AddRange (new string[] {"0", "1", "2", "3", "4"});
				list.DefaultCompletionString = "0";
				break;
				
			case "masterpagefile":
				return project != null
					? MonoDevelop.AspNet.Html.HtmlPathCompletion.GetPathCompletion (project, "*.master", fromFile,
						x => "~/" + x.ProjectVirtualPath.ToString ().Replace (System.IO.Path.PathSeparator, '/')) 
					: null;
			
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
				//  string, any available .NET language
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
			//we're not likely to suggest anything for these:
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
			default:
				return null;
			}
			
			return list.Count > 0? list : null;
		}
		
		static CompletionDataList GetRegisterAttributeValues (AspNetAppProject project, FilePath fromFile, string attribute)
		{
			switch (attribute.ToLowerInvariant ()) {
			case "src":
				return project != null
					? MonoDevelop.AspNet.Html.HtmlPathCompletion.GetPathCompletion (project, "*.ascx", fromFile,
						x => "~/" + x.ProjectVirtualPath.ToString ().Replace (System.IO.Path.PathSeparator, '/')) 
					: null;
			}
			return null;
		}	
		
		#region Attribute lists
		
		static string[] page11Attributes = new string[] {
			"Async", "AspCompat", "AsyncTimeOut","Buffer", "ClientTarget", "CodeBehind", "CompilerOptions",
			"CodeFile", "CodePage", "CompilationMode", "ContentType", "CodeFileBaseClass",
			"Debug", "Description", "EnableSessionState", "EnableTheming", "EnableViewState", "EnableViewStateMac",
			"ErrorPage", "Inherits", "Language", "LinePragmas", "MasterPageFile", "ResponseEncoding",
			"SmartNavigation", "Src", "StyleSheetTheme", "Theme", "TargetSchema", "Title", "Trace", "TraceMode",
			"Transaction", "UICulture", "ValidateRequest", "ViewStateEncryptionMode", "WarningLevel"
		};
		
		static string[] page11MutexAttributes = new string[] {
			"LCID", "Culture"
		};
		
		static string[] page20Attributes = new string[] {
			"EnableEventValidation", "MaintainScrollPositionOnPostback"
		};
		
		static string[] pageVBAttributes = new string[] {
			"Explicit", "Strict"
		};
		
		static string[] userControlAttributes = new string[] {
			"AutoEventWireup", "ClassName", "CodeBehind", "CodeFile", "CodeFileBaseClass", "CompilationMode",
			"CompilerOptions", "Debug", "Description", "EnableTheming", "EnableViewState", "Inherits", "Language",
			"LinePragmas", "Src", "TargetSchema", "WarningLevel"
		};
		
		static string[] masterControlAttributes = new string[] { "MasterPageFile" };
		
		static string[] assemblyAttributes = new string[] {
			//mutually exclusive
			"Name", //assembly name to link
			"Src" //source file name to compile and link
		};
		
		static string[] importAttributes = new string[] {
			"Namespace",
		};
		
		static string[] referenceAttributes = new string[] {
			//one of:
			"Page",
			"Control",
			"VirtualPath"
		};
		
		static string[] registerAttributes = new string[] {
			"TagPrefix"
		};
		
		static string[] registerAssemblyAttributes = new string[] {
			"Assembly",  //assembly name from bin directory
			"Namespace", //if no assembly, assumes App_Code
		};
		
		static string[] registerUserControlAttributes = new string[] {
			"Src",       //user controls only. Start with ~ for app root
			"TagName"   //user controls only
		};
		
		static string[] outputcacheAttributes = new string[] {
			"Duration", //seconds, required
			"Location", //OutputCacheLocation enum, default "Any". aspx-only
			"CacheProfile", // arbitrary string from config outputCacheSettings/outputCacheProfiles. aspx-only
			"NoStore", // bool. aspx-only
			"Shared", //bool, default true. ascx-only
			"SqlDependency", //string of tables and stuff, or "CommandNotification on aspx
			"VaryByCustom", // custom requirement. "browser" varies by user agent. Other strings should be handled in GetVaryByCustomString Globl.asax
			"VaryByHeader", // HTTP headers in a semicolon-separated list. Each combination is cached. aspx-only
			"VaryByParam", // none or *, or semicolon-separated list of GET/POST parameters. Required or VaryByControl.
			"VaryByControl", // IDs of server controls, in a semicolon-separated list.  Required or VaryByParam.
			"VaryByContentEncodings" //list of Accept-Encoding encodings
		};
		
		static string[] mastertypeAttributes = new string[] {
			//only one allowed
			"TypeName",    //name of type
			"VirtualPath"  //path to strong type
		};
		
		static string[] previousPageTypeAttributes = new string[] {
			"TypeName", "VirtualPath"
		};
		
		static string[] implementsAttributes = new string[] {
			"Interface"
		};
		
		#endregion
	}
}
