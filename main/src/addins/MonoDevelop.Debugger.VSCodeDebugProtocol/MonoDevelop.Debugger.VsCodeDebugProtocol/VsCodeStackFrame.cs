using System;
using System.Linq;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

using Mono.Debugging.Client;

using VsStackFrame = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame;
using VsFormat = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrameFormat;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	public class VsCodeStackFrame : Mono.Debugging.Client.StackFrame
	{
		public static VsFormat GetStackFrameFormat (EvaluationOptions evalOptions)
		{
			return new VsFormat {
				Parameters = evalOptions.StackFrameFormat.ParameterTypes || evalOptions.StackFrameFormat.ParameterNames || evalOptions.StackFrameFormat.ParameterValues,
				ParameterTypes = evalOptions.StackFrameFormat.ParameterTypes,
				ParameterNames = evalOptions.StackFrameFormat.ParameterNames,
				ParameterValues = evalOptions.StackFrameFormat.ParameterValues,
				Line = evalOptions.StackFrameFormat.Line,
				Module = evalOptions.StackFrameFormat.Module
			};
		}

		static string GetLanguage (string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			if (path.EndsWith (".cs", StringComparison.OrdinalIgnoreCase))
				return "C#";
			if (path.EndsWith (".fs", StringComparison.OrdinalIgnoreCase))
				return "F#";
			if (path.EndsWith (".vb", StringComparison.OrdinalIgnoreCase))
				return "VB";
			return null;
		}

		static SourceLocation GetSourceLocation (VsStackFrame frame)
		{
			return new SourceLocation (frame.Name, frame.Source?.Path, frame.Line, frame.Column, frame.EndLine ?? -1, frame.EndColumn ?? -1, GetHashBytes (frame.Source));
		}

		VsFormat format;
		readonly int threadId;
		readonly int frameIndex;
		internal readonly int frameId;
		string fullStackframeText;

		public VsCodeStackFrame (VsFormat format, int threadId, int frameIndex, VsStackFrame frame)
			: base (0, GetSourceLocation (frame), GetLanguage (frame.Source?.Path))
		{
			this.format = format;
			this.threadId = threadId;
			this.frameIndex = frameIndex;
			this.fullStackframeText = frame.Name;
			this.frameId = frame.Id;
		}

		static byte ToXDigit (char c)
		{
			if (c >= 'A' && c <= 'F')
				return (byte) ((c - 'A') + 10);

			if (c >= 'a' && c <= 'f')
				return (byte) ((c - 'a') + 10);

			if (c >= '0' && c <= '9')
				return (byte) (c - '0');

			throw new ArgumentException ();
		}

		public static byte[] HexToByteArray (string hex)
		{
			if (hex.Length % 2 == 1)
				return null;

			try {
				var bytes = new byte[hex.Length / 2];
				for (int i = 0, j = 0; i < bytes.Length; i++, j += 2) {
					var x1 = ToXDigit (hex[j]);
					var x2 = ToXDigit (hex[j + 1]);

					bytes[i] = (byte) ((x1 << 4) | x2);
				}

				return bytes;
			} catch {
				return null;
			}
		}

		static byte[] GetHashBytes (Source source)
		{
			if (source == null)
				return null;

			foreach (var checksum in source.Checksums) {
				switch (checksum.Algorithm) {
				case ChecksumAlgorithm.SHA256:
				case ChecksumAlgorithm.SHA1:
				case ChecksumAlgorithm.MD5:
					var hash = HexToByteArray (checksum.ChecksumValue);
					if (hash != null)
						return hash;
					break;
				}
			}

			return null;
		}

		public override string FullStackframeText {
			get {
				//If StackFrameFormat changed since last fetch, refeatch
				var currentFormat = GetStackFrameFormat (DebuggerSession.EvaluationOptions);
				if (currentFormat.Hex != format.Hex ||
					currentFormat.Line != format.Line ||
					currentFormat.Module != format.Module ||
					currentFormat.Parameters != format.Parameters ||
					currentFormat.ParameterNames != format.ParameterNames ||
					currentFormat.ParameterTypes != format.ParameterTypes ||
					currentFormat.ParameterValues != format.ParameterValues) {
					format = currentFormat;
					var body = ((VSCodeDebuggerSession)DebuggerSession).protocolClient.SendRequestSync (new StackTraceRequest (threadId) { StartFrame = frameIndex, Levels = 1, Format = currentFormat });
					fullStackframeText = body.StackFrames [0].Name;
				}
				return fullStackframeText;
			}
		}
	}
}