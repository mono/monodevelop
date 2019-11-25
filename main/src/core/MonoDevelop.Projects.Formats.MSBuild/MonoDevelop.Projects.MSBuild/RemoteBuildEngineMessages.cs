//
// RemoteBuildEngineMessages.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects.MSBuild
{
	[MessageDataTypeAttribute]
	class InitializeRequest: BinaryMessage
	{
		[MessageDataProperty]
		public int IdeProcessId { get; set; }

		[MessageDataProperty]
		public string CultureName { get; set; }

		[MessageDataProperty]
		public string BinDir { get; set; }

		[MessageDataProperty]
		public Dictionary<string, string> GlobalProperties { get; set; }
	}

	[MessageDataTypeAttribute]
	class LoadProjectRequest: BinaryMessage<LoadProjectResponse>
	{
		[MessageDataProperty]
		public string ProjectFile { get; set; }
	}

	[MessageDataTypeAttribute]
	class LoadProjectResponse: BinaryMessage
	{
		[MessageDataProperty]
		public int ProjectId { get; set; }
	}

	[MessageDataTypeAttribute]
	class UnloadProjectRequest: BinaryMessage
	{
		[MessageDataProperty]
		public int ProjectId { get; set; }
	}

	[MessageDataTypeAttribute]
	class CancelTaskRequest: BinaryMessage
	{
		[MessageDataProperty]
		public int TaskId { get; set; }
	}

	[MessageDataTypeAttribute]
	class SetGlobalPropertiesRequest: BinaryMessage
	{
		[MessageDataProperty]
		public Dictionary<string, string> Properties { get; set; }
	}

	[MessageDataTypeAttribute]
	class PingRequest: BinaryMessage
	{
		[MessageDataProperty]
		public int TaskId = 1;
	}

	[MessageDataTypeAttribute]
	class DisposeRequest: BinaryMessage
	{
	}

	[MessageDataTypeAttribute]
	class RefreshProjectRequest: BinaryMessage
	{
		[MessageDataProperty]
		public int ProjectId { get; set; }
	}

	[MessageDataTypeAttribute]
	class RefreshWithContentRequest: BinaryMessage
	{
		[MessageDataProperty]
		public int ProjectId { get; set; }

		[MessageDataProperty]
		public string Content { get; set; }
	}

	[MessageDataTypeAttribute]
	class RunProjectRequest: BinaryMessage<RunProjectResponse>
	{
		[MessageDataProperty]
		public int ProjectId { get; set; }

		[MessageDataProperty]
		public string Content { get; set; }

		[MessageDataProperty]
		public ProjectConfigurationInfo [] Configurations { get; set; }

		[MessageDataProperty]
		public int LogWriterId { get; set; }

		[MessageDataProperty]
		public MSBuildEvent EnabledLogEvents { get; set; }

		[MessageDataProperty]
		public MSBuildVerbosity Verbosity { get; set; }

		[MessageDataProperty]
		public string [] RunTargets { get; set; }

		[MessageDataProperty]
		public string [] EvaluateItems { get; set; }

		[MessageDataProperty]
		public string [] EvaluateProperties { get; set; }

		[MessageDataProperty]
		public Dictionary<string, string> GlobalProperties { get; set; }

		[MessageDataProperty]
		public int TaskId { get; set; }

		[MessageDataProperty]
		public string BinLogFilePath { get; set; }
	}

	[MessageDataTypeAttribute]
	class RunProjectResponse : BinaryMessage
	{
		[MessageDataProperty]
		public MSBuildResult Result { get; set; }
	}

	[MessageDataTypeAttribute]
	class LogMessage : BinaryMessage
	{
		[MessageDataProperty]
		public int LoggerId { get; set; }

		[MessageDataProperty]
		public string LogText { get; set; }

		[MessageDataProperty]
		public LogEvent[] Events { get; set; }
	}

	[MessageDataType]
	class LogEvent
	{
		[MessageDataProperty]
		public MSBuildEvent Event { get; set; }

		[MessageDataProperty]
		public string Message { get; set; }
	}

	public enum MSBuildVerbosity
	{
		Quiet,
		Minimal,
		Normal,
		Detailed,
		Diagnostic
	}

	[MessageDataTypeAttribute]
	class ProjectConfigurationInfo
	{
		[MessageDataProperty]
		public string ProjectFile { get; set; }

		[MessageDataProperty]
		public string ProjectGuid { get; set; }

		[MessageDataProperty]
		public string Configuration { get; set; }

		[MessageDataProperty]
		public string Platform { get; set; }

		[MessageDataProperty]
		public bool Enabled { get; set; }
	}

	[MessageDataType]
	class LoggerInfo
	{
		[MessageDataProperty]
		public string Id { get; set; }

		[MessageDataProperty]
		public bool ConsoleLog { get; set; }

		[MessageDataProperty]
		public MSBuildEvent EventsFilter { get; set; }
	}

	[MessageDataType]
	class BeginBuildRequest : BinaryMessage
	{
		[MessageDataProperty]
		public string BinLogFilePath { get; set; }

		[MessageDataProperty]
		public int LogWriterId { get; set; }

		[MessageDataProperty]
		public MSBuildEvent EnabledLogEvents { get; set; }

		[MessageDataProperty]
		public MSBuildVerbosity Verbosity { get; set; }

		[MessageDataProperty]
		public ProjectConfigurationInfo [] Configurations { get; set; }
	}

	[MessageDataType]
	class EndBuildRequest : BinaryMessage
	{
	}
}

