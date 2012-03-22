//
// RecentItem.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml;

namespace MonoDevelop.Ide.Desktop
{
	/// <summary>
	/// Implementation of RecentItem according to "Recent File Storage Specification v0.2" from 
	/// the freedesktop.org.
	/// </summary>
    class RecentItem : IComparable
	{
        string       uri;
        string       mimeType;
        DateTime     timestamp;
        string       privateData;
		List<string> groups = new List<string> ();

		public string Uri {
			get {
				return uri; 
			}			
			set {
				uri = value; 
			}
		}
		
		public string MimeType {
			get {
				return mimeType; 
			}
			set {
				mimeType = value; 
			}
		}
		
		public DateTime Timestamp {
			get {
				return timestamp;
			}
			set {
				timestamp = value;
			}
		}
		
		public string Private {
			get {
				return privateData;
			}
			set {
				privateData = value;
			}
		}
		
		public ReadOnlyCollection<string> Groups {
			get {
				return groups.AsReadOnly ();
			}
		}
		
		public bool IsFile {
			get {
				return this.uri != null && this.uri.StartsWith ("file://");
			}
		}
		
		public string LocalPath {
			get {
				if (uri.StartsWith ("file://"))
					return uri.Substring ("file://".Length);
				return this.uri;
			}
		}
		
		RecentItem ()
		{
		}
		
		public RecentItem (string uri, string mimetype) : this (uri, mimetype, null)
		{
		}

		public RecentItem (string uri, string mimetype, string group)
		{
			Debug.Assert (uri != null);
			this.uri       = uri;
			this.mimeType  = mimetype;
			NewTimeStamp ();
			
			if (!String.IsNullOrEmpty (group)) {
				this.groups.Add (group);
			}
		}
		
		void AddGroup (string group)
		{
			if (String.IsNullOrEmpty (group))
				return;
			
			if (!this.groups.Contains (group))
				this.groups.Add (group);
		}

		public void AddGroups (IEnumerable<string> groups)
		{
			if (groups == null)
				return;
			foreach (string group in groups)
				AddGroup (group);
		}
		
		public void RemoveGroup (string group)
		{
			if (this.groups.Contains (group)) 
				this.groups.Remove (group);
		}
		
		public void NewTimeStamp ()
		{
			this.timestamp = DateTime.UtcNow;
		}
		
		public bool IsInGroup (string group)
		{
			return this.groups.Contains (group);
		}
		
		public override bool Equals (object obj)
		{
			RecentItem item = obj as RecentItem;
			if (item == null)
				return false;
			return uri == item.uri;
		}
		
		public override int GetHashCode ()
		{
			return uri.GetHashCode ();
		}
		
		
		
		public int CompareTo (object o)
		{
			RecentItem item = o as RecentItem;
			if (item == null)
				throw new ArgumentException ("Can only compare items of " + typeof (RecentItem) + " item was: " + o);
			return item.Timestamp.CompareTo (this.Timestamp);
		}
		
		public override string ToString ()
		{
			return LocalPath;
		}

#region Unix Time functions
		static readonly DateTime UnixStartTime = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		static int ToUnixTime (DateTime time)
		{
			return (int)(time - UnixStartTime).TotalSeconds;
		}
		
		static DateTime ToDateTime (int unixTime)
		{
			return UnixStartTime.AddSeconds (unixTime);
		}
#endregion

#region I/O
		public const string Node          = "RecentItem";
		public const string UriNode       = "URI";
		public const string MimeTypeNode  = "Mime-Type";
		public const string TimestampNode = "Timestamp";
		public const string PrivateNode   = "Private";
		public const string GroupsNode    = "Groups";
		public const string GroupNode     = "Group";
		
		public static RecentItem Read (XmlReader reader)
		{
			Debug.Assert (reader.LocalName == Node);
			RecentItem result = new RecentItem ();
			bool readGroup = false;
			while (reader.Read ()) {
				switch (reader.NodeType) {
				case XmlNodeType.EndElement:
					if (reader.LocalName == GroupsNode) {
						readGroup = false;
						break;
					}
					if (reader.LocalName == Node)
						return result;
					throw new XmlException ("Found unknown end element:" + reader.LocalName);
				case XmlNodeType.Element:
					if (readGroup) {
						result.AddGroup (reader.ReadElementString ());
						break;
					}
					switch (reader.LocalName) {
					case "Uri":
					case UriNode:
						result.uri = reader.ReadElementString ();
						break;
					case "MimeType":
					case MimeTypeNode:
						result.mimeType = reader.ReadElementString ();
						break;
					case TimestampNode:
						result.timestamp = ToDateTime (Int32.Parse (reader.ReadElementString ()));
						break;
					case PrivateNode:
						result.privateData = reader.ReadElementString ();
						break;
					case GroupsNode:
						readGroup = true;
						break;
					default:
						throw new XmlException ("Found unknown start element:" + reader.LocalName);
					}
					break;
				}
			}
			return null;
		}
		
		public void Write (XmlWriter writer)
		{
			writer.WriteStartElement (Node);
			writer.WriteElementString (UriNode, this.uri);
			writer.WriteElementString (MimeTypeNode, this.mimeType);
			writer.WriteElementString (TimestampNode, ToUnixTime (this.timestamp).ToString ());
			if (!String.IsNullOrEmpty (this.privateData))
				writer.WriteElementString (PrivateNode, this.privateData);
			
			if (this.Groups.Count > 0) {
				writer.WriteStartElement (GroupsNode);
				foreach (string group in this.groups)
					writer.WriteElementString (GroupNode, group);
				writer.WriteEndElement (); // GroupsNode
			}
			writer.WriteEndElement (); // Node
		}
#endregion
    }
}