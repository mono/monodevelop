// MemberCompletionData.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Xml;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.CSharpBinding
{
	public class MemberCompletionData : IMemberCompletionData, IOverloadedCompletionData
	{
		OutputFlags flags;
		bool hideExtensionParameter = true;
		static CSharpAmbience ambience = new CSharpAmbience ();
		bool descriptionCreated = false;
		
		string description, completionString;
		string displayText;
		
		Dictionary<string, ICompletionData> overloads;
		
		public string Description {
			get {
				CheckDescription ();
				return description;
			}
		}
		
		public string CompletionText {
			get { return completionString; }
			set { completionString = value; }
		}
		
		public IDomVisitable Member {
			get;
			set;
		}
		
		public string DisplayText {
			get {
				if (displayText == null) {
					displayText = ambience.GetString (Member, flags | OutputFlags.HideGenericParameterNames);
				}

				return displayText; 
			}
		}
		
		public string Icon {
			get {
				if (Member is IMember)
					return ((IMember)Member).StockIcon;
				if (Member is IParameter)
					return ((IParameter)Member).StockIcon;
				if (Member is LocalVariable)
					return ((LocalVariable)Member).StockIcon;
				return "md-literal"; 
			}
		}
		
		public DisplayFlags DisplayFlags { get; set; }
		
		public bool HideExtensionParameter {
			get {
				return hideExtensionParameter;
			}
			set {
				hideExtensionParameter = value;
			}
		}
		
		public MemberCompletionData (IDomVisitable member, OutputFlags flags) 
		{
			this.Member = member;
			this.flags = flags;
			this.completionString = ambience.GetString (Member, flags ^ OutputFlags.IncludeGenerics);
			DisplayFlags = DisplayFlags.DescriptionHasMarkup;
			IMember m = Member as IMember;
			if (m != null && m.IsObsolete)
				DisplayFlags |= DisplayFlags.Obsolete;
		}
		
		void CheckDescription ()
		{
			if (descriptionCreated)
				return;
			
			StringBuilder sb = new StringBuilder ();
			
			descriptionCreated = true;
			if (Member is IMethod && ((IMethod)Member).WasExtended)
				sb.Append (GettextCatalog.GetString ("(Extension) "));
			sb.Append (ambience.GetString (Member,
				OutputFlags.ClassBrowserEntries | OutputFlags.UseFullName | OutputFlags.IncludeParameterName | OutputFlags.IncludeMarkup
				| (HideExtensionParameter ? OutputFlags.HideExtensionsParameter : OutputFlags.None)));
			
			if (Member is IMember) {
				if ((Member as IMember).IsObsolete) {
					sb.AppendLine ();
					sb.Append (GettextCatalog.GetString ("[Obsolete]"));
				}
				XmlNode node = (Member as IMember).GetMonodocDocumentation ();
				if (node != null) {
					node = node.SelectSingleNode ("summary");
					if (node != null) {
						sb.AppendLine ();
						sb.Append (GetDocumentation (node.InnerXml));
					}
				}
			}
			description = sb.ToString ();
		}
		
		static string GetCref (string cref)
		{
			if (cref == null)
				return "";
			
			if (cref.Length < 2)
				return cref;
			
			if (cref.Substring(1, 1) == ":")
				return cref.Substring (2, cref.Length - 2);
			
			return cref;
		}
		public static string GetDocumentation (string doc)
		{
			System.IO.StringReader reader = new System.IO.StringReader("<docroot>" + doc + "</docroot>");
			XmlTextReader xml   = new XmlTextReader(reader);
			StringBuilder ret   = new StringBuilder(70);
			int lastLinePos = -1;
			
			try {
				xml.Read();
				do {
					if (xml.NodeType == XmlNodeType.Element) {
						switch (xml.Name.ToLower()) {
						case "remarks":
							ret.Append("Remarks:\n");
							break;
						// skip <example>-nodes
						case "example":
							xml.Skip();
							xml.Skip();
							break;
						case "exception":
							ret.Append ("Exception: " + GetCref (xml["cref"]) + ":\n");
							break;
						case "returns":
							ret.Append ("Returns: ");
							break;
						case "see":
							ret.Append (GetCref (xml["cref"]) + xml["langword"]);
							break;
						case "seealso":
							ret.Append ("See also: " + GetCref (xml["cref"]) + xml["langword"]);
							break;
						case "paramref":
							ret.Append (xml["name"]);
							break;
						case "param":
							ret.Append (xml["name"].Trim() + ": ");
							break;
						case "value":
							ret.Append ("Value: ");
							break;
						case "para":
							continue; // Keep new line flag
						}
						lastLinePos = -1;
					} else if (xml.NodeType == XmlNodeType.EndElement) {
						string elname = xml.Name.ToLower();
						if (elname == "para" || elname == "param") {
							if (lastLinePos == -1)
								lastLinePos = ret.Length;
							ret.Append("<span size=\"2000\">\n\n</span>");
						}
					} else if (xml.NodeType == XmlNodeType.Text) {
						string txt = xml.Value.Replace ("\r","").Replace ("\n"," ");
						if (lastLinePos != -1)
							txt = txt.TrimStart (' ');
						
						// Remove duplcate spaces.
						int len;
						do {
							len = txt.Length;
							txt = txt.Replace ("  ", " ");
						} while (len != txt.Length);
						
						txt = GLib.Markup.EscapeText (txt);
						ret.Append(txt);
						lastLinePos = -1;
					}
				} while (xml.Read ());
				if (lastLinePos != -1)
					ret.Remove (lastLinePos, ret.Length - lastLinePos);
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				return doc;
			}
			return ret.ToString ();
		}

		#region IOverloadedCompletionData implementation 
		
		class OverloadSorter : IComparer<ICompletionData>
		{
			OutputFlags flags = OutputFlags.ClassBrowserEntries | OutputFlags.IncludeParameterName;
			
			public int Compare (ICompletionData x, ICompletionData y)
			{
				IDomVisitable mx = ((MemberCompletionData)x).Member;
				IDomVisitable my = ((MemberCompletionData)y).Member;
				int result;
				
				if (mx is IType && my is IType) {
					result = ((((IType)mx).TypeParameters.Count).CompareTo (((IType)my).TypeParameters.Count));
					if (result != 0)
						return result;
				}
				
				if (mx is IMethod && my is IMethod) {
					IMethod mmx = (IMethod) mx;//, mmy = (IMethod) my;
					result = (mmx.TypeParameters.Count).CompareTo (mmx.TypeParameters.Count);
					if (result != 0)
						return result;
					result = (mmx.Parameters.Count).CompareTo (mmx.Parameters.Count);
					if (result != 0)
						return result;
				}
				
				string sx = ambience.GetString (mx, flags);
				string sy = ambience.GetString (my, flags);
				result = sx.Length.CompareTo (sy.Length);
				return result == 0? string.Compare (sx, sy) : result;
			}
		}
		
		public IEnumerable<ICompletionData> GetOverloadedData ()
		{
			if (overloads == null)
				return new ICompletionData[] { this };
			
			List<ICompletionData> sorted = new List<ICompletionData> (overloads.Values);
			sorted.Add (this);
			sorted.Sort (new OverloadSorter ());
			return sorted;
		}
		
		public bool IsOverloaded {
			get { return overloads != null && overloads.Count > 0; }
		}
		
		public void AddOverload (MemberCompletionData overload)
		{
			if (overloads == null)
				overloads = new Dictionary<string, ICompletionData> ();
			if (overload.Member is IMember && Member is IMember) {
				string MemberId = (overload.Member as IMember).HelpUrl;
				if (MemberId != (this.Member as IMember).HelpUrl && !overloads.ContainsKey (MemberId)) {
					if (((IMember)overload.Member).IsPartial)
						return;
					overloads[MemberId] = overload;
					
					//if any of the overloads is obsolete, we should not mark the item obsolete
					if (!(overload.Member as IMember).IsObsolete)
						DisplayFlags &= ~DisplayFlags.Obsolete;
					
					//make sure that if there are generic overloads, we show a generic signature
					if (overload.Member is IType && Member is IType && ((IType)Member).TypeParameters.Count == 0 && ((IType)overload.Member).TypeParameters.Count > 0) {
						displayText = overload.DisplayText;
					}
					if (overload.Member is IMethod && Member is IMethod && ((IMethod)Member).TypeParameters.Count == 0 && ((IMethod)overload.Member).TypeParameters.Count > 0) {
						displayText = overload.DisplayText;
					}
				}
			}
		}
		
		#endregion
	}
}
