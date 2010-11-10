// 
// IPhoneFrameworkBackend.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide;
using Gtk;
using MonoDevelop.Core.Serialization;
namespace MonoDevelop.IPhone
{
	public class IPhoneSimulatorTarget : IEquatable<IPhoneSimulatorTarget>
	{
		public IPhoneSimulatorTarget (TargetDevice device, IPhoneSdkVersion version)
		{
			this.Device = device;
			this.Version = version;
		}
		
		//for deserializer
		private IPhoneSimulatorTarget () {}
		
		[ItemProperty]
		public TargetDevice Device { get; private set; }
		
		public IPhoneSdkVersion Version { get; private set; }
		
		//for serialization
		[ItemProperty]
		private string SdkVersion {
			get {
				return Version.ToString ();
			}	
			set {
				Version = string.IsNullOrEmpty (value)
					? IPhoneSdkVersion.UseDefault
					: IPhoneSdkVersion.Parse (value);
			}
		}
		
		public bool Supports (IPhoneSdkVersion minVersion, TargetDevice appTargetDevice)
		{
			if (appTargetDevice == TargetDevice.IPad && Device == TargetDevice.IPhone)
				return false;
			return minVersion.CompareTo (Version) <= 0;
		}
		
		public override string ToString ()
		{
			return (Device == TargetDevice.IPad? "iPad Simulator " : "iPhone Simulator ") + Version;
		}
		
		public override bool Equals (object obj)
		{
			if (ReferenceEquals (this, obj))
				return true;
			var other = obj as MonoDevelop.IPhone.IPhoneSimulatorTarget;
			return other != null && Device == other.Device && Version.Equals (other.Version);
		}

		public override int GetHashCode ()
		{
			unchecked {
				return Device.GetHashCode () ^ (Version.GetHashCode ());
			}
		}
		
		public bool Equals (IPhoneSimulatorTarget other)
		{
			return other != null && Device == other.Device && Version.Equals (other.Version);
		}
	}
}
