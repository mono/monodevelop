//
// UpdateChannel.cs
//
// Author:
//       Tim Miller <timothy.miller@xamarin.com>
//
// Copyright (c) 2017 Microsoft
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
using System.IO;
using Newtonsoft.Json;

namespace MonoDevelop.Core.Setup
{
	public class UpdateChannel
	{
		public static readonly UpdateChannel Stable = new UpdateChannel ("Stable", "Stable", "", 0);
		public static readonly UpdateChannel Beta = new UpdateChannel ("Beta", "Beta", "", 1);
		public static readonly UpdateChannel Alpha = new UpdateChannel ("Alpha", "Alpha", "", 2);
		public static readonly UpdateChannel Test = new UpdateChannel ("Test", "Test", "", 100);
		public static readonly UpdateChannel [] DefaultLevels = { Stable, Beta, Alpha };

		public string Id { get; set; }
		public string Name { get; set; }
		public string BannerMessage { get; set; }
		public int Idx { get; set; }

		public UpdateChannel (string id, string name, string bannerMessage, int idx)
		{
			this.Id = id;
			this.Name = name;
			this.BannerMessage = bannerMessage;
			this.Idx = idx;
		}

		public UpdateChannel () { }

		public override bool Equals (System.Object obj)
		{
			if (obj == null) {
				return false;
			}

			UpdateChannel a = obj as UpdateChannel;
			if ((System.Object)a == null) {
				return false;
			}

			return a.Id == Id;
		}

		public bool Equals (UpdateChannel a)
		{
			if ((object)a == null) {
				return false;
			}
			return (a.Id == Id);
		}

		public static bool operator == (UpdateChannel a, UpdateChannel b)
		{
			if (Object.ReferenceEquals (a, null) && Object.ReferenceEquals (b, null)) {
				return true;
			}
			if (Object.ReferenceEquals (a, null) || Object.ReferenceEquals (b, null)) {
				return false;
			}
			return a.Id == b.Id;
		}

		public static bool operator != (UpdateChannel a, UpdateChannel b)
		{
			if (Object.ReferenceEquals (a, null) && Object.ReferenceEquals (b, null)) {
				return false;
			}
			if (Object.ReferenceEquals (a, null) || Object.ReferenceEquals (b, null)) {
				return true;
			}
			return a.Id != b.Id;
		}

		public static bool operator <= (UpdateChannel a, UpdateChannel b)
		{
			if (Object.ReferenceEquals (a, null)) {
				return true;
			}
			if (Object.ReferenceEquals (b, null)) {
				return false;
			}
			return a.Idx <= b.Idx;
		}

		public static bool operator >= (UpdateChannel a, UpdateChannel b)
		{
			if (Object.ReferenceEquals (a, null))
				return false;
			if (Object.ReferenceEquals (b, null))
				return true;
			return a.Idx >= b.Idx;
		}


		public override int GetHashCode ()
		{
			return Idx;
		}

		public override string ToString ()
		{
			return Id;
		}

		public int ToInt32 ()
		{
			return Idx;
		}

		public UpdateLevel ToUpdateLevel() 
		{
			if (this.Id == Stable) {
				return UpdateLevel.Stable;
			}
			if (this.Id == Beta) {
				return UpdateLevel.Beta;
			} 
			// If Alpha or "dynamic", return Alpha.
			return UpdateLevel.Alpha;
		}

		public static UpdateChannel FromUpdateLevel(UpdateLevel level) {
			switch (level) {
				case UpdateLevel.Stable:
					return UpdateChannel.Stable;
				case UpdateLevel.Beta:
					return UpdateChannel.Beta;
				case UpdateLevel.Alpha:
				case UpdateLevel.Test:
					return UpdateChannel.Alpha;
				default:
					return UpdateChannel.Alpha;
			}
		}

		public static implicit operator int (UpdateChannel a)
		{
			return a.ToInt32 ();
		}

		public static implicit operator string (UpdateChannel a)
		{
			return a.ToString ();
		}

		public static FilePath VersionSourceCacheFile {
			get { return UserProfile.Current.LocalConfigDir.Combine ("version-source"); }
		}

		static string UpdateChannelFromVersion (string fullVersion)
		{
			if (!File.Exists (VersionSourceCacheFile)) {
				return null;
			}

			using (var file = File.OpenText (VersionSourceCacheFile)) {
				var serializer = new JsonSerializer ();
				var versionCache = (Dictionary<string, string>)serializer.Deserialize (file, typeof (Dictionary<string, string>));

				if (versionCache == null) {
					return null;
				}

				versionCache.TryGetValue (fullVersion, out var channelName);
				return channelName;
		    }
		}

		public static string ChannelNameForVersion ()
		{
			// If the VersionLabel doesn't contain "Preview" then it came from the stable channel or an installer
			if (!BuildInfo.VersionLabel.Contains ("Preview")) {
				return UpdateChannel.Stable;
			}

			// The DownloadService stores what channel each version came from.
			var channel = UpdateChannelFromVersion (BuildInfo.FullVersion);

			// If the DownloadService doesn't know what channel, then it was an unofficial build installed by hand
			// (or built from source) - so we mark that as Test
			return channel ?? "Test";
		}
	}
}
