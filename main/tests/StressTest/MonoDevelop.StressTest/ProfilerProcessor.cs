using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mono.Profiler.Log;

namespace MonoDevelop.StressTest
{
	public class ProfilerProcessor
	{
		public StressTestOptions.ProfilerOptions Options { get; }
		private Thread processingThread;
		private Visitor visitor;
		private LogProcessor processor;
		private CancellationTokenSource cts = new CancellationTokenSource ();

		public ProfilerProcessor (StressTestOptions.ProfilerOptions options)
		{
			this.Options = options;
			visitor = new Visitor (this);
			processingThread = new Thread (new ThreadStart (ProcessFile));
			processingThread.Start ();
		}

		public void Stop ()
		{
			cts.Cancel ();
		}

		class NeverEndingLogStream : LogStream
		{
			readonly CancellationToken token;

			public NeverEndingLogStream (Stream baseStream, CancellationToken token) : base (baseStream)
			{
				this.token = token;
			}

			public override bool EndOfStream => false;

			byte[] _byteBuffer = new byte[1];
			public override int ReadByte ()
			{
				while (BaseStream.Length - BaseStream.Position < 1) {
					Thread.Sleep (100);
					token.ThrowIfCancellationRequested ();
				}
				// The base method on Stream is extremely inefficient in that it
				// allocates a 1-byte array for every call. Simply use a private
				// buffer instead.
				return Read (_byteBuffer, 0, sizeof (byte)) == 0 ? -1 : _byteBuffer[0];
			}

			public override int Read (byte[] buffer, int offset, int count)
			{
				while (BaseStream.Length - BaseStream.Position < count) {
					Thread.Sleep (100);
					token.ThrowIfCancellationRequested ();
				}
				return BaseStream.Read (buffer, offset, count);
			}
		}

		private void ProcessFile ()
		{
			try {
				//Give runtime 10 seconds to create .mlpd
				for (int i = 0; i < 100; i++) {
					if (File.Exists (Options.MlpdOutputPath))
						break;
					Thread.Sleep (100);
				}
				using (var fs = new FileStream (Options.MlpdOutputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (var logStream = new NeverEndingLogStream (fs, cts.Token)) {
					processor = new LogProcessor (logStream, new Visitor (this), null);
					processor.Process (cts.Token);
				}
			} catch (OperationCanceledException) { } catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		public class Heapshot
		{
			public Dictionary<long, int> ObjectsPerClassCounter = new Dictionary<long, int> ();
			public Dictionary<long, ClassLoadEvent> ClassInfos;
		}

		class Visitor : Mono.Profiler.Log.LogEventVisitor
		{
			ProfilerProcessor profilerProcessor;
			Heapshot currentHeapshot;
			Dictionary<long, ClassLoadEvent> classInfos = new Dictionary<long, ClassLoadEvent> ();
			Dictionary<long, long> vtableToClassInfo = new Dictionary<long, long> ();

			public Visitor (ProfilerProcessor profilerProcessor)
			{
				this.profilerProcessor = profilerProcessor;
			}

			public override void Visit (ClassLoadEvent ev)
			{
				classInfos[ev.ClassPointer] = ev;
			}

			public override void Visit (VTableLoadEvent ev)
			{
				vtableToClassInfo[ev.VTablePointer] = ev.ClassPointer;
			}

			public override void Visit (HeapBeginEvent ev)
			{
				currentHeapshot = new Heapshot ();
			}

			public override void Visit (HeapObjectEvent ev)
			{
				if (ev.ObjectSize == 0)//This means it's just reporting references
					return;
				var classInfoId = vtableToClassInfo[ev.VTablePointer];
				if (currentHeapshot.ObjectsPerClassCounter.ContainsKey (classInfoId))
					currentHeapshot.ObjectsPerClassCounter[classInfoId]++;
				else
					currentHeapshot.ObjectsPerClassCounter[classInfoId] = 0;
			}

			public override void Visit (HeapEndEvent ev)
			{
				currentHeapshot.ClassInfos = classInfos;
				profilerProcessor.completionSource.SetResult (currentHeapshot);
			}
		}

		int TcpPort {
			get {
				return processor.StreamHeader.Port;
			}
		}
		TcpClient client;
		StreamWriter writer;
		TaskCompletionSource<Heapshot> completionSource;

		public async Task<Heapshot> TakeHeapshot ()
		{
			if (completionSource != null) {
				throw new InvalidOperationException ("Heapshot taking in progress");
			}
			completionSource = new TaskCompletionSource<Heapshot> ();
			if (client == null) {
				client = new TcpClient ();
				await client.ConnectAsync (IPAddress.Loopback, TcpPort);
				writer = new StreamWriter (client.GetStream ());
			}
			await writer.WriteAsync ("heapshot\n");
			await writer.FlushAsync ();
			var result = await completionSource.Task;
			completionSource = null;
			return result;
		}

		public string GetMonoArguments ()
		{
			switch (Options.Type) {
				case StressTestOptions.ProfilerOptions.ProfilerType.HeapOnly:
					return $"--profile=log:heapshot=ondemand,noalloc,nocalls,maxframes=0,output=\"{Options.MlpdOutputPath}\"";
				case StressTestOptions.ProfilerOptions.ProfilerType.All:
					return $"--profile=log:heapshot=ondemand,alloc,nocalls,maxframes={Options.MaxFrames},output=\"{Options.MlpdOutputPath}\"";
				case StressTestOptions.ProfilerOptions.ProfilerType.Custom:
					return Options.CustomProfilerArguments;
				default:
					throw new NotImplementedException (Options.Type.ToString ());
			}
		}
		List<Heapshot> heapshots = new List<Heapshot> ();
		public async Task TakeHeapshotAndMakeReport ()
		{
			var newHeapshot = await TakeHeapshot ();

			if (Options.PrintReportTypes.HasFlag (StressTestOptions.ProfilerOptions.PrintReport.ObjectsTotal)) {
				Console.WriteLine ($"Total objects per type({newHeapshot.ObjectsPerClassCounter.Count}):");
				foreach (var typeWithCount in newHeapshot.ObjectsPerClassCounter.Where (p => p.Value > 0).OrderByDescending (p => p.Value)) {
					Console.WriteLine ($"{newHeapshot.ClassInfos[typeWithCount.Key].Name}:{typeWithCount.Value}");
				}
			}

			if (Options.PrintReportTypes.HasFlag (StressTestOptions.ProfilerOptions.PrintReport.ObjectsDiff)) {
				heapshots.Add (newHeapshot);
				if (heapshots.Count == 1) {
					Console.WriteLine ("No objects diff report on 1st Heapshot.");
					return;
				}
				var oldHeapshot = heapshots[heapshots.Count - 2];
				var diffCounter = new List<Tuple<long, int>> ();
				foreach (var classInfoId in newHeapshot.ClassInfos.Keys)//ClassInfos is not Heapshot specific, all heapshot has same
				{
					if (!oldHeapshot.ObjectsPerClassCounter.TryGetValue (classInfoId, out int oldCount))
						oldCount = 0;
					if (!newHeapshot.ObjectsPerClassCounter.TryGetValue (classInfoId, out int newCount))
						newCount = 0;
					if (newCount - oldCount != 0)
						diffCounter.Add (Tuple.Create (classInfoId, newCount - oldCount));
				}
				Console.WriteLine ($"Heapshot diff has {diffCounter.Count} entries:");
				foreach (var diff in diffCounter.OrderByDescending (d => d.Item2)) {
					Console.WriteLine ($"{newHeapshot.ClassInfos[diff.Item1].Name}:{diff.Item2}");
				}
			}
		}
	}
}
