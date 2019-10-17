﻿//
// PlatformTelemetryDetails.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2018 
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
namespace MonoDevelop.Ide.Desktop
{
	public enum PlatformHardDriveMediaType {
		Unknown,
		SolidState,
		Rotational
	};

	public struct ScreenDetails
	{
		public float PointWidth { get; set; }
		public float PointHeight { get; set; }
		public float PixelWidth { get; set; }
		public float PixelHeight { get; set; }
		public float BackingScaleFactor { get; set; }
	}

	public struct GraphicsDetails
	{
		public string Model { get; set; }
		public string Memory { get; set; }
	}

	public interface IPlatformTelemetryDetails
	{
		TimeSpan TimeSinceMachineStart { get; }
		TimeSpan TimeSinceLogin { get; }
		TimeSpan KernelAndUserTime { get; }
		TimeSpan KernelTime { get; }
		TimeSpan UserTime { get; }

		ScreenDetails[] Screens { get; }
		GraphicsDetails[] GPU { get; }

		string CpuArchitecture { get; }
		string Model { get; }
		int PhysicalCpuCount { get; }
		int CpuCount { get; }
		int CpuFamily { get; }
		long CpuFrequency { get; }
		ulong HardDriveTotalVolumeSize { get; }
		ulong HardDriveFreeVolumeSize { get; }
		ulong RamTotal { get; }
		PlatformHardDriveMediaType HardDriveOsMediaType { get; }

		bool TrySampleHostCpuLoad (out double value);

		TimeSpan GetEventTime (Gdk.EventKey eventKey);
	}
}
