 /* 
 * WebFormsPage.cs - Represents an ASP.NET Page in the designer
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Web;
using System.IO;
using System.Web.Compilation;
using System.Collections;
using System.Web.UI;
using System.ComponentModel.Design;
using AspNetEdit.Editor.Persistence;

namespace AspNetEdit.Editor.ComponentModel
{
	internal class WebFormPage : System.Web.UI.Page
	{
		//private HttpRequest httpRequest;
		
		public WebFormPage ()
		{
			pdc = TypeDescriptor.GetProperties (this);
			
			//fake the request for some controls which need it
			/*
			HttpRequest request = new HttpRequest (string.Empty, "file:///", string.Empty);
			System.IO.StringWriter strw = new System.IO.StringWriter ();
			HttpResponse response = new HttpResponse (strw);
			HttpContext context = new HttpContext (request, response);
			this.ProcessRequest (context);
			*/
		}

		//FIXME: enforce this...
		public override void VerifyRenderingInServerForm (Control control)
		{
		}

		#region Property browser -> page directive linkage

		private PropertyDescriptorCollection pdc = null;
		private DocumentDirective pageDirective;
		private DocumentDirective PageDirective
		{
			get {
				if (pageDirective == null) {
					DesignerHost host = this.Site.GetService (typeof (IDesignerHost)) as DesignerHost;
					if (host == null)
						throw new Exception ("Could not obtain DesignerHost service");
					pageDirective = host.RootDocument.GetFirstDirective ("Page", true);
				}

				return pageDirective;
			}	
		}

		private object GetConvertedProperty (string name)
		{
			PropertyDescriptor pd = pdc.Find (name, true);
			string currentVal = PageDirective[name];
			if (currentVal == null || currentVal == string.Empty)
				return ((DefaultValueAttribute)pd.Attributes[typeof (DefaultValueAttribute)]).Value;
			return pd.Converter.ConvertFromInvariantString (currentVal);
		}

		private void SetProperty (string name, object value)
		{
			PropertyDescriptor pd = pdc.Find (name, true);
			if (value == ((DefaultValueAttribute)pd.Attributes[typeof(DefaultValueAttribute)]).Value)
				PageDirective[name] = null;
			PageDirective[name] = pd.Converter.ConvertToInvariantString (value);
		}

		#endregion

		#region Property browser attributes for @Page attributes

		[DefaultValue (false)]
		[Category("Behaviour")]
		//TODO: Add for .NET 2.0
		//[DisplayNameAttribute("AspCompat")
		[Description ("Whether the page can be executed on a single-threaded apartment thread")]
		[Bindable(false)]
		[Browsable(true)]
		public bool AspCompat
		{
			get { return (bool) GetConvertedProperty ("AspCompat"); }
			set { SetProperty ("AspCompat", value); }
		}

		[DefaultValue(true)]
		[Category("Compilation")]
		[Description("Whether the page events are automatically wired up")]
		[Bindable(false)]
		[Browsable(true)]
		public bool AutoEventWireup
		{
			get { return (bool)GetConvertedProperty("AutoEventWireup"); }
			set { SetProperty("AutoEventWireup", value); }
		}

		[DefaultValue(true)]
		[Category("Behaviour")]
		[Description("Whether HTTP response buffering is enabled")]
		[Bindable(false)]
		[Browsable(true)]
		public new bool Buffer
		{
			get { return (bool)GetConvertedProperty("Buffer"); }
			set { SetProperty("Buffer", value); }
		}

		[DefaultValue("")]
		[Category("Compilation")]
		[ReadOnly(true)]
		[Description("The class name for the page when it is compiled")]
		[Bindable(false)]
		[Browsable(true)]
		public string ClassName
		{
			get { return (string)GetConvertedProperty("ClassName"); }
			set { SetProperty("ClassName", value); }
		}

		[DefaultValue("")]
		[Category("Behaviour")]
		[Description("The user agent which controls should target when rendering")]
		[Bindable(false)]
		[Browsable(true)]
		public new string ClientTarget
		{
			get { return (string)GetConvertedProperty("ClientTarget"); }
			set { SetProperty("ClientTarget", value); }
		}

		[DefaultValue("")]
		[ReadOnly(true)]
		[Category("Designer")]
		[Description("The codebehind file associated with the page")]
		[Bindable(false)]
		[Browsable(true)]
		public string CodeBehind
		{
			get { return (string)GetConvertedProperty("CodeBehind"); }
			set { SetProperty("CodeBehind", value); }
		}

		[DefaultValue(0)]
		[Category("Globalization")]
		[Description("The code page used for the response")]
		[Bindable(false)]
		[Browsable(true)]
		public new int CodePage
		{
			get { return (int)GetConvertedProperty("CodePage"); }
			set { SetProperty("CodePage", value); }
		}

		[DefaultValue("")]
		[Category("Compilation")]
		[Description("Command-line options used when compiling the page")]
		[Bindable(false)]
		[Browsable(true)]
		public string CompilerOptions
		{
			get { return (string)GetConvertedProperty("CompilerOptions"); }
			set { SetProperty("CompilerOptions", value); }
		}

		[DefaultValue("text/html")]
		[Category("Behaviour")]
		[Description("The MIME type of the HTTP response content")]
		[Bindable(false)]
		[Browsable(true)]
		public new string ContentType
		{
			get { return (string)GetConvertedProperty("ContentType"); }
			set { SetProperty("ContentType", value); }
		}

		[DefaultValue(null)]
		[Category("Globalization")]
		[Description("The culture setting for the page")]
		[Bindable(false)]
		[Browsable(true)]
		public new CultureInfo Culture
		{
			get { return (CultureInfo)GetConvertedProperty("Culture"); }
			set { SetProperty("Culture", value); }
		}

		[DefaultValue(false)]
		[Category("Compilation")]
		[Description("Whether the page should be compiled with debugging symbols")]
		[Bindable(false)]
		[Browsable(true)]
		public bool Debug
		{
			get { return (bool)GetConvertedProperty("Debug"); }
			set { SetProperty("Debug", value); }
		}

		[DefaultValue("")]
		[Category("Designer")]
		[Description("A description of the page")]
		[Bindable(false)]
		[Browsable(true)]
		public string Description
		{
			get { return (string)GetConvertedProperty("Description"); }
			set { SetProperty("Description", value); }
		}

		[DefaultValue("true")]
		[Category("Behaviour")]
		[Description("Whether SessionState is enabled (true), read-only (ReadOnly) or disabled (false)")]
		[Bindable(false)]
		[Browsable(true)]
		public string EnableSessionState
		{
			get { return (string)GetConvertedProperty("EnableSessionState"); }
			set { SetProperty("EnableSessionState", value); }
		}

		[DefaultValue(true)]
		[Category("Behaviour")]
		[Description("Whether view state is enabled")]
		[Bindable(false)]
		[Browsable(true)]
		public new bool EnableViewState
		{
			get { return (bool)GetConvertedProperty("EnableViewState"); }
			set { SetProperty("EnableViewState", value); }
		}

		[DefaultValue(false)]
		[Category("Behaviour")]
		[Description("Whether a machine authentication check should be run on the view state")]
		[Bindable(false)]
		[Browsable(true)]
		public bool ViewStateMac
		{
			get { return (bool)GetConvertedProperty("ViewStateMac"); }
			set { SetProperty("ViewStateMac", value); }
		}

		[DefaultValue("")]
		[Category("Behaviour")]
		[Description("The URL to redirect to in the event of an unhandled page exception")]
		[Bindable(false)]
		[Browsable(true)]
		public new string ErrorPage
		{
			get { return (string)GetConvertedProperty("ErrorPage"); }
			set { SetProperty("ErrorPage", value); }
		}

		[DefaultValue(false)]
		[Category("Compilation")]
		[Description("Whether the page should be compiled with Option Explicit for VB 2005")]
		[Bindable(false)]
		[Browsable(true)]
		public bool Explicit
		{
			get { return (bool)GetConvertedProperty("Explicit"); }
			set { SetProperty("Explicit", value); }
		}

		[DefaultValue("")]
		[Category("Compilation")]
		[Description("The code-behind class from which the page inherits")]
		[Bindable(false)]
		[Browsable(true)]
		public string Inherits
		{
			get { return (string)GetConvertedProperty("Inherits"); }
			set { SetProperty("Inherits", value); }
		}

		[DefaultValue("")]
		[ReadOnly(true)]
		[Category("Compilation")]
		[Description("The language used for compiling inline rendering and block code")]
		[Bindable(false)]
		[Browsable(true)]
		public string Language
		{
			get { return (string)GetConvertedProperty("Language"); }
			set { SetProperty("Language", value); }
		}

		[DefaultValue(0)]
		[Category("Globalization")]
		[Description("The locale identifier of the page. Defaults to the web server's locale")]
		[Bindable(false)]
		[Browsable(true)]
		public new int LCID
		{
			get { return (int)GetConvertedProperty("LCID"); }
			set { SetProperty("LCID", value); }
		}

		[DefaultValue(null)]
		[Category("Globalization")]
		[Description("The encoding of the HTTP response content")]
		[Bindable(false)]
		[Browsable(true)]
		public new Encoding ResponseEncoding
		{
			get { return (Encoding)GetConvertedProperty("ResponseEncoding"); }
			set { SetProperty("ResponseEncoding", value); }
		}

		[DefaultValue("")]
		[ReadOnly(true)]
		[Category("Compilation")]
		[Description("The optional code-behind source file to compile when the page is requested")]
		[Bindable(false)]
		[Browsable(true)]
		public string Src
		{
			get { return (string)GetConvertedProperty("Src"); }
			set { SetProperty("Src", value); }
		}

		[DefaultValue(false)]
		[Category("Behaviour")]
		[Description("Whether to maintain scroll position and focus during refreshes. IE5.5 or later only.")]
		[Bindable(false)]
		[Browsable(true)]
		public new bool SmartNavigation
		{
			get { return (bool)GetConvertedProperty("SmartNavigation"); }
			set { SetProperty("SmartNavigation", value); }
		}

		[DefaultValue(false)]
		[Category("Compilation")]
		[Description("Whether the page should be compiled with Option Strict for VB 2005")]
		[Bindable(false)]
		[Browsable(true)]
		public bool Strict
		{
			get { return (bool)GetConvertedProperty("Strict"); }
			set { SetProperty("Strict", value); }
		}

		[DefaultValue(false)]
		[Category("Behaviour")]
		[Description("Whether tracing is enabled")]
		[Bindable(false)]
		[Browsable(true)]
		public new bool Trace
		{
			get { return (bool)GetConvertedProperty("Trace"); }
			set { SetProperty("Trace", value); }
		}

		[DefaultValue(TraceMode.Default)]
		[Category("Behaviour")]
		[Description("The sorting mode for tracing message")]
		[Bindable(false)]
		[Browsable(true)]
		public TraceMode TraceMode
		{
			get { return (TraceMode)GetConvertedProperty("TraceMode"); }
			set { SetProperty("TraceMode", value); }
		}

		[DefaultValue("Disabled")]
		[Category("Behaviour")]
		[Description("How transactions are supported. Disabled,	NotSupported, Supported, Required, or RequiresNew")]
		[Bindable(false)]
		[Browsable(true)]
		public string Transaction
		{
			get { return (string)GetConvertedProperty("Transaction"); }
			set { SetProperty("Transaction", value); }
		}

		[DefaultValue(null)]
		[Category("Globalization")]
		[Description("The UI culture to use")]
		[Bindable(false)]
		[Browsable(true)]
		public new CultureInfo UICulture
		{
			get { return (CultureInfo)GetConvertedProperty("UICulture"); }
			set { SetProperty("UICulture", value); }
		}

		[DefaultValue(true)]
		[Category("Behaviour")]
		[Description("Whether to use request validation to increase security")]
		[Bindable(false)]
		[Browsable(true)]
		public bool ValidateRequest
		{
			get { return (bool)GetConvertedProperty("ValidateRequest"); }
			set { SetProperty("ValidateRequest", value); }
		}

		[DefaultValue(4)]
		[Category("Compilation")]
		[Description("The compiler warning level at which to abort compilation")]
		[Bindable(false)]
		[Browsable(true)]
		public int WarningLevel
		{
			get { return (int)GetConvertedProperty("WarningLevel"); }
			set { SetProperty("WarningLevel", value); }
		}

		#endregion
	}
}
