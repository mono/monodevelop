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

namespace LeakTest
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

			visitor = new Visitor (this, scenario.GetTrackedTypes (options.ExtraTypes));
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
				TaskCompletionSource<Heapshot> source;
				lock (profilerProcessor.processingHeapshots) {
					source = profilerProcessor.processingHeapshots.Dequeue ();
				}

				source.SetResult (new Heapshot (currentHeapshot));
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
		readonly Queue<TaskCompletionSource<Heapshot>> processingHeapshots = new Queue<TaskCompletionSource<Heapshot>> ();

		public Task RemainingHeapshotsTask => Task.WhenAll (processingHeapshots.Select (x => x.Task));

		TaskCompletionSource<Heapshot> QueueHeapshot ()
		{
			var tcs = new TaskCompletionSource<Heapshot> ();

			lock (processingHeapshots) {
				processingHeapshots.Enqueue (tcs);
			}

			TriggerHeapshot ();

			return tcs;

			void TriggerHeapshot ()
			{
				if (client == null) {
					client = new TcpClient ();
					client.Connect (IPAddress.Loopback, TcpPort);
					writer = new StreamWriter (client.GetStream ());
				}
				writer.Write ("heapshot\n");
				writer.Flush ();
			}
		}

		public Task<Heapshot> TakeHeapshot ()
		{
			var tcs = QueueHeapshot ();

			return tcs.Task;
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
	}
}
