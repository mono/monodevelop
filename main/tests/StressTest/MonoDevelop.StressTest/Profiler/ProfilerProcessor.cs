using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mono.Profiler.Log;
using QuickGraph;

namespace MonoDevelop.StressTest
{
	public class ProfilerProcessor
	{
		public ProfilerOptions Options { get; }
		private Thread processingThread;
		private Visitor visitor;
		private LogProcessor processor;
		private CancellationTokenSource cts = new CancellationTokenSource ();

		public ProfilerProcessor (ITestScenario scenario, ProfilerOptions options)
		{
			Options = options;

			visitor = new Visitor (this, scenario.GetTrackedTypes ());
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
					processor = new LogProcessor (logStream, null, visitor);
					processor.Process (cts.Token);
				}
			} catch (OperationCanceledException) { } catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		class Visitor : Mono.Profiler.Log.LogEventVisitor
		{
			ProfilerProcessor profilerProcessor;
			NativeHeapshot currentHeapshot;
			Dictionary<long, TypeInfo> typeInfos = new Dictionary<long, TypeInfo> ();
			Dictionary<long, long> vtableToClassInfo = new Dictionary<long, long> ();
			readonly HashSet<string> trackedTypeNames;

			public Visitor (ProfilerProcessor profilerProcessor, HashSet<string> trackedTypeNames)
			{
				this.profilerProcessor = profilerProcessor;
				this.trackedTypeNames = trackedTypeNames;
			}

			public override void Visit (ClassLoadEvent ev)
			{
				typeInfos[ev.ClassPointer] = new TypeInfo (ev.ClassPointer, ev.Name);
			}

			public override void Visit (VTableLoadEvent ev)
			{
				vtableToClassInfo[ev.VTablePointer] = ev.ClassPointer;
			}

			public override void Visit (HeapBeginEvent ev)
			{
				currentHeapshot = new NativeHeapshot (trackedTypeNames);
			}

			readonly Dictionary<long, HeapRootRegisterEvent> rootsEvents = new Dictionary<long, HeapRootRegisterEvent> ();
			readonly List<long> rootsEventsBinary = new List<long> ();

			public override void Visit (HeapRootsEvent ev)
			{
				for (int i = 0; i < ev.Roots.Count; ++i) {
					var root = ev.Roots[i];
					ProcessNewRoot (root.ObjectPointer, root.SlotPointer);
				}
			}

			public override void Visit (HeapRootRegisterEvent ev)
			{
				var index = rootsEventsBinary.BinarySearch (ev.RootPointer);
				if (index < 0) {//negative index means it's not there
					index = ~index;
					if (index - 1 >= 0) {
						var oneBefore = rootsEvents[rootsEventsBinary[index - 1]];
						if (oneBefore.RootPointer + oneBefore.RootSize > ev.RootPointer) {
							Console.WriteLine ("2 HeapRootRegisterEvents overlap:");
							Console.WriteLine (ev);
							Console.WriteLine (oneBefore);
						}
					}
					if (index < rootsEventsBinary.Count) {
						var oneAfter = rootsEvents[rootsEventsBinary[index]];
						if (oneAfter.RootPointer < ev.RootPointer + ev.RootSize) {
							Console.WriteLine ("2 HeapRootRegisterEvents overlap:");
							Console.WriteLine (ev);
							Console.WriteLine (oneAfter);
						}
					}
					rootsEventsBinary.Insert (index, ev.RootPointer);
					rootsEvents.Add (ev.RootPointer, ev);
				} else {
					Console.WriteLine ("2 HeapRootRegisterEvent at same address:");
					Console.WriteLine (ev);
					Console.WriteLine (rootsEvents[ev.RootPointer]);
					rootsEvents[ev.RootPointer] = ev;
				}
			}

			public override void Visit (HeapRootUnregisterEvent ev)
			{
				if (rootsEvents.Remove (ev.RootPointer)) {
					var index = rootsEventsBinary.BinarySearch (ev.RootPointer);
					rootsEventsBinary.RemoveAt (index);
				} else {
					Console.WriteLine ("HeapRootUnregisterEvent attempted at address that was not Registred:");
					Console.WriteLine (ev);
				}
			}

			public override void Visit (HeapObjectEvent ev)
			{
				var classInfoId = vtableToClassInfo[ev.VTablePointer];
				var typeInfo = typeInfos[classInfoId];

				currentHeapshot.AddObject (typeInfo, ev);
			}

			public override void Visit (HeapEndEvent ev)
			{
				profilerProcessor.completionSource.SetResult (new Heapshot (currentHeapshot));
			}

			void ProcessNewRoot (long objAddr, long rootAddr)
			{
				var index = rootsEventsBinary.BinarySearch (rootAddr);
				if (index < 0) {
					index = ~index;
					if (index == 0) {
						Console.WriteLine ($"This should not happen. Root is before any HeapRootsEvent {rootAddr}.");
						return;
					}
					var rootReg = rootsEvents[rootsEventsBinary[index - 1]];
					if (rootReg.RootPointer < rootAddr && rootReg.RootPointer + rootReg.RootSize >= rootAddr) {
						currentHeapshot.RegisterRoot (objAddr, rootReg);
					} else {
						Console.WriteLine ($"This should not happen. Closest root is too small({rootAddr}):");
						Console.WriteLine (rootReg);
					}
				} else {
					//We got exact match
					currentHeapshot.RegisterRoot (objAddr, rootsEvents[rootAddr]);
				}
			}

		}

		int TcpPort => processor.StreamHeader.Port;
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
				case ProfilerOptions.ProfilerType.HeapOnly:
					return $"--profile=log:nodefaults,heapshot=ondemand,output=\"{Options.MlpdOutputPath}\"";
				case ProfilerOptions.ProfilerType.All:
					return $"--profile=log:nodefaults,heapshot-on-shutdown,heapshot=ondemand,gcalloc,gcmove,gcroot,counter,maxframes={Options.MaxFrames},output=\"{Options.MlpdOutputPath}\"";
				case ProfilerOptions.ProfilerType.Custom:
					return Options.CustomProfilerArguments;
				default:
					throw new NotImplementedException (Options.Type.ToString ());
			}
		}

		Heapshot lastHeapshot;
		public async Task<Heapshot> TakeHeapshotAndMakeReport ()
		{
			var newHeapshot = await TakeHeapshot ();

			if (Options.PrintReportTypes.HasFlag (ProfilerOptions.PrintReport.ObjectsTotal)) {
				Console.WriteLine ($"Total objects per type({newHeapshot.Types.Count}):");
				foreach (var heapTypeInfo in newHeapshot.Types.Values.OrderByDescending (p => p.Objects.Count)) {
					var name = heapTypeInfo.TypeInfo.Name;
					if (ShouldReportItem(name))
						Console.WriteLine ($"{name}:{heapTypeInfo.Objects.Count}");
				}
			}

			if (Options.PrintReportTypes.HasFlag (ProfilerOptions.PrintReport.ObjectsDiff)) {
				Heapshot oldHeapshot = lastHeapshot;
				lastHeapshot = newHeapshot;

				if (oldHeapshot == null) {
					Console.WriteLine ("No objects diff report on 1st Heapshot.");
					return newHeapshot;
				}

				var diffCounter = new List<Tuple<string, int>> ();
				foreach (var kvp in newHeapshot.Types)//ClassInfos is not Heapshot specific, all heapshot has same
				{
					var typeId = kvp.Key;
					var name = kvp.Value.TypeInfo.Name;

					int oldCount = oldHeapshot.GetObjectCount (typeId);
					int newCount = newHeapshot.GetObjectCount (typeId);

					if (newCount - oldCount != 0)
						diffCounter.Add (Tuple.Create (name, newCount - oldCount));
				}

				Console.WriteLine ($"Heapshot diff has {diffCounter.Count} entries:");
				foreach (var diff in diffCounter.OrderByDescending (d => d.Item2)) {
					var name = diff.Item1;
					if (ShouldReportItem (name)) {
						Console.WriteLine ($"{name}:{diff.Item2}");
					}
				}
			}

			return newHeapshot;

			bool ShouldReportItem (string name) => Options.PrintReportObjectNames.Count == 0 || Options.PrintReportObjectNames.Contains (name);
		}
	}
}
