//
// SystemAssemblyService.cs
//
// Author:
//   Todd Berman <tberman@sevenl.net>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2004 Todd Berman
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using Mono.Addins;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Core.Assemblies
{
	public sealed class SystemAssemblyService
	{
		object frameworkWriteLock = new object ();
		Dictionary<TargetFrameworkMoniker,TargetFramework> frameworks = new Dictionary<TargetFrameworkMoniker, TargetFramework> ();
		List<TargetRuntime> runtimes;
		TargetRuntime defaultRuntime;

		public TargetRuntime CurrentRuntime { get; private set; }

		public event EventHandler DefaultRuntimeChanged;
		public event EventHandler RuntimesChanged;
		public event EventHandler FrameworksChanged;

		internal void Initialize ()
		{
			runtimes = new List<TargetRuntime> ();
			foreach (ITargetRuntimeFactory factory in AddinManager.GetExtensionObjects ("/MonoDevelop/Core/Runtimes", typeof(ITargetRuntimeFactory))) {
				foreach (TargetRuntime runtime in factory.CreateRuntimes ()) {
					runtimes.Add (runtime);
					if (runtime.IsRunning)
						DefaultRuntime = CurrentRuntime = runtime;
				}
			}

			// Don't initialize until Current and Default Runtimes are set
			foreach (TargetRuntime runtime in runtimes) {
				runtime.FrameworksInitialized += HandleRuntimeInitialized;
			}

			if (CurrentRuntime == null)
				LoggingService.LogFatalError ("Could not create runtime info for current runtime");

			CurrentRuntime.StartInitialization ();
		}

		void HandleRuntimeInitialized (object sender, EventArgs e)
		{
			var runtime = (TargetRuntime) sender;
			if (runtime.CustomFrameworks.Any ())
				UpdateFrameworks (runtime.CustomFrameworks);
		}

		//this MUST be used when mutating `frameworks` after Initialize ()
		void UpdateFrameworks (IEnumerable<TargetFramework> toAdd)
		{
			lock (frameworkWriteLock) {
				var newFxList = new Dictionary<TargetFrameworkMoniker,TargetFramework> (frameworks);
				bool changed = false;
				foreach (var fx in toAdd) {
					TargetFramework existing;
					//TODO: can we update dummies w/real frameworks if later-added runtime has definitions?
					if (!newFxList.TryGetValue (fx.Id, out existing) || existing.Assemblies.Length == 0) {
						newFxList [fx.Id] = fx;
						changed = true;
					}
				}
				if (!changed)
					return;
				BuildFrameworkRelations (newFxList);
				frameworks = newFxList;
			}
			FrameworksChanged?.Invoke (this, EventArgs.Empty);
		}

		public TargetRuntime DefaultRuntime {
			get {
				return defaultRuntime;
			}
			set {
				defaultRuntime = value;
				if (DefaultRuntimeChanged != null)
					DefaultRuntimeChanged (this, EventArgs.Empty);
			}
		}

		[Obsolete ("Assembly folders are no longer supported")]
		public DirectoryAssemblyContext UserAssemblyContext => new DirectoryAssemblyContext ();

		public IAssemblyContext DefaultAssemblyContext {
			get { return DefaultRuntime.AssemblyContext; }
		}

		public void RegisterRuntime (TargetRuntime runtime)
		{
			runtime.FrameworksInitialized += HandleRuntimeInitialized;
			runtimes.Add (runtime);
			RuntimesChanged?.Invoke (this, EventArgs.Empty);
		}

		public void UnregisterRuntime (TargetRuntime runtime)
		{
			if (runtime == CurrentRuntime)
				return;
			DefaultRuntime = CurrentRuntime;
			runtimes.Remove (runtime);
			runtime.FrameworksInitialized -= HandleRuntimeInitialized;
			RuntimesChanged?.Invoke (this, EventArgs.Empty);
		}

		internal IEnumerable<TargetFramework> GetKnownFrameworks ()
		{
			return frameworks.Values;
		}

		internal bool IsKnownFramework (TargetFrameworkMoniker moniker)
		{
			return frameworks.ContainsKey (moniker);
		}

		public IEnumerable<TargetFramework> GetTargetFrameworks ()
		{
			return frameworks.Values;
		}

		public IEnumerable<TargetRuntime> GetTargetRuntimes ()
		{
			return runtimes;
		}

		public TargetRuntime GetTargetRuntime (string id)
		{
			foreach (TargetRuntime r in runtimes) {
				if (r.Id == id)
					return r;
			}
			return null;
		}

		public IEnumerable<TargetRuntime> GetTargetRuntimes (string runtimeId)
		{
			foreach (TargetRuntime r in runtimes) {
				if (r.RuntimeId == runtimeId)
					yield return r;
			}
		}

		public TargetFramework GetTargetFramework (TargetFrameworkMoniker id)
		{
			TargetFramework fx;
			if (frameworks.TryGetValue (id, out fx))
				return fx;

			LoggingService.LogDebug ("Unknown TargetFramework '{0}' is being requested from SystemAssemblyService, ensuring runtimes initialized and trying again", id);
			foreach (var r in runtimes)
				r.EnsureInitialized ();
			if (frameworks.TryGetValue (id, out fx))
				return fx;

			
			LoggingService.LogWarning ("Unknown TargetFramework '{0}' is being requested from SystemAssemblyService, returning empty TargetFramework", id);
			UpdateFrameworks (new [] { new TargetFramework (id) });
			return frameworks [id];
		}

		public SystemPackage GetPackageFromPath (string assemblyPath)
		{
			foreach (TargetRuntime r in runtimes) {
				SystemPackage p = r.AssemblyContext.GetPackageFromPath (assemblyPath);
				if (p != null)
					return p;
			}
			return null;
		}

		public static AssemblyName ParseAssemblyName (string fullname)
		{
			var aname = new AssemblyName ();
			int i = fullname.IndexOf (',');
			if (i == -1) {
				aname.Name = fullname.Trim ();
				return aname;
			}

			var fullNameSpan = fullname.AsSpan ();

			aname.Name = fullNameSpan.Slice (0, i).Trim ().ToString ();
			i = fullname.IndexOf ("Version", i + 1, StringComparison.Ordinal);
			if (i == -1)
				return aname;
			i = fullname.IndexOf ('=', i);
			if (i == -1)
				return aname;

			int j = fullname.IndexOf (',', i);
			if (j == -1)
				fullNameSpan = fullNameSpan.Slice (i + 1);
			else
				fullNameSpan = fullNameSpan.Slice (i + 1, j - i - 1);
			aname.Version = new Version (fullNameSpan.Trim ().ToString ());
			return aname;
		}

		static readonly Dictionary<string, AssemblyName> assemblyNameCache = new Dictionary<string, AssemblyName> ();
		internal static AssemblyName GetAssemblyNameObj (string file)
		{
			AssemblyName name;

			lock (assemblyNameCache) {
				if (assemblyNameCache.TryGetValue (file, out name))
					return name;
			}

			try {
				name = AssemblyName.GetAssemblyName (file);
				lock (assemblyNameCache) {
					assemblyNameCache [file] = name;
				}
				return name;
			} catch (FileNotFoundException) {
				// GetAssemblyName is not case insensitive in mono/windows. This is a workaround
				foreach (string f in Directory.GetFiles (Path.GetDirectoryName (file), Path.GetFileName (file))) {
					if (f != file) {
						GetAssemblyNameObj (f);
						return assemblyNameCache [file];
					}
				}
				throw;
			}
		}

		public static string GetAssemblyName (string file)
		{
			return AssemblyContext.NormalizeAsmName (GetAssemblyNameObj (file).ToString ());
		}

		public static bool IsManagedAssembly(string filePath)
		{
			try
			{
				using (Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				using (BinaryReader binaryReader = new BinaryReader(fileStream))
				{
					if (fileStream.Length < 64)
					{
						return false;
					}

					// PE Header starts @ 0x3C (60). Its a 4 byte header.
					fileStream.Position = 0x3C;
					uint peHeaderPointer = binaryReader.ReadUInt32();
					if (peHeaderPointer == 0)
					{
						peHeaderPointer = 0x80;
					}

					// Ensure there is at least enough room for the following structures:
					//     24 byte PE Signature & Header
					//     28 byte Standard Fields         (24 bytes for PE32+)
					//     68 byte NT Fields               (88 bytes for PE32+)
					// >= 128 byte Data Dictionary Table
					if (peHeaderPointer > fileStream.Length - 256)
					{
						return false;
					}

					// Check the PE signature.  Should equal 'PE\0\0'.
					fileStream.Position = peHeaderPointer;
					uint peHeaderSignature = binaryReader.ReadUInt32();
					if (peHeaderSignature != 0x00004550)
					{
						return false;
					}

					// skip over the PEHeader fields
					fileStream.Position += 20;

					const ushort PE32 = 0x10b;
					const ushort PE32Plus = 0x20b;

					// Read PE magic number from Standard Fields to determine format.
					var peFormat = binaryReader.ReadUInt16();
					if (peFormat != PE32 && peFormat != PE32Plus)
					{
						return false;
					}

					// Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
					// When this is non-zero then the file contains CLI data otherwise not.
					ushort dataDictionaryStart = (ushort)(peHeaderPointer + (peFormat == PE32 ? 232 : 248));
					fileStream.Position = dataDictionaryStart;

					uint cliHeaderRva = binaryReader.ReadUInt32();
					if (cliHeaderRva == 0)
					{
						return false;
					}

					return true;
				}
			}
			catch (Exception)
			{
				return false;
			}
		}

		//warning: this may mutate `frameworks` and any newly-added TargetFrameworks in it
		static void BuildFrameworkRelations (Dictionary<TargetFrameworkMoniker, TargetFramework> frameworks)
		{
			foreach (TargetFramework fx in frameworks.Values)
				BuildFrameworkRelations (fx, frameworks);
		}

		static void BuildFrameworkRelations (TargetFramework fx, Dictionary<TargetFrameworkMoniker, TargetFramework> frameworks)
		{
			if (fx.RelationsBuilt)
				return;

			var includesFramework = fx.GetIncludesFramework ();
			if (includesFramework != null) {
				fx.IncludedFrameworks.Add (includesFramework);
				TargetFramework compatFx;
				if (frameworks.TryGetValue (includesFramework, out compatFx)) {
					BuildFrameworkRelations (compatFx, frameworks);
					fx.IncludedFrameworks.AddRange (compatFx.IncludedFrameworks);
				} else {
					// the framework is broken, can't depend on an unknown framework
					LoggingService.LogWarning ("TargetFramework '{0}' imports unknown framework '{0}'", fx.Id, includesFramework);
				}
			}

			fx.RelationsBuilt = true;
		}

		//FIXME: the fallback is broken since multiple frameworks can have the same corlib
		public TargetFrameworkMoniker GetTargetFrameworkForAssembly (TargetRuntime tr, string file)
		{
			if (!File.Exists (file))
				return TargetFrameworkMoniker.UNKNOWN;

			try {
				using (var reader = new PEReader (File.OpenRead (file))) {
					var mr = reader.GetMetadataReader ();

					foreach (var customAttributeHandle in mr.GetAssemblyDefinition ().GetCustomAttributes ()) {
						var customAttribute = mr.GetCustomAttribute (customAttributeHandle);

						var ctorHandle = customAttribute.Constructor;
						if (ctorHandle.Kind != HandleKind.MemberReference)
							continue;

						var ctor = mr.GetMemberReference ((MemberReferenceHandle)ctorHandle);
						var attrType = mr.GetTypeReference ((TypeReferenceHandle)ctor.Parent);

						var ns = mr.GetString (attrType.Namespace);
						if (ns != "System.Runtime.Versioning")
							continue;

						var typeName = mr.GetString (attrType.Name);
						if (typeName != "TargetFrameworkAttribute")
							continue;

						var provider = new StringParameterValueTypeProvider (mr, customAttribute.Value);
						var signature = ctor.DecodeMethodSignature (provider, null);
						var parameterTypes = signature.ParameterTypes;
						if (parameterTypes.Length != 1)
							continue;

						var value = parameterTypes [0];
						if (value != null && TargetFrameworkMoniker.TryParse (value, out var m)) {
							return m;
						}
						LoggingService.LogError ("Invalid TargetFrameworkAttribute in assembly {0} - {1}", file, value);
					}

					if (tr != null) {
						foreach (var assemblyReferenceHandle in mr.AssemblyReferences) {
							var assemblyReference = mr.GetAssemblyReference (assemblyReferenceHandle);

							var name = mr.GetString (assemblyReference.Name);
							if (name != "mscorlib")
								continue;

							TargetFramework compatibleFramework = null;
							// If there are several frameworks that can run the file, pick one that is installed
							foreach (TargetFramework tf in GetKnownFrameworks ()) {
								if (tf.GetCorlibVersion () == assemblyReference.Version.ToString ()) {
									compatibleFramework = tf;
									if (tr.IsInstalled (tf))
										return tf.Id;
								}
							}
							if (compatibleFramework != null)
								return compatibleFramework.Id;
							break;
						}
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error determining target framework for assembly {0}: {1}", file, ex);
			}
			return TargetFrameworkMoniker.UNKNOWN;
		}

		/// <summary>
		/// Simply get all assembly reference names from an assembly given it's file name.
		/// </summary>
		public static ImmutableArray<string> GetAssemblyReferences (string fileName)
		{
			try {
				using (var reader = new PEReader (File.OpenRead (fileName))) {
					var mr = reader.GetMetadataReader ();
					var assemblyReferences = mr.AssemblyReferences;

					var builder = ImmutableArray.CreateBuilder<string> (assemblyReferences.Count);

					foreach (var assemblyReferenceHandle in assemblyReferences) {
						var assemblyReference = mr.GetAssemblyReference (assemblyReferenceHandle);
						builder.Add (mr.GetString (assemblyReference.Name));
					}

					return builder.MoveToImmutable();
				}
			} catch {
				return ImmutableArray<string>.Empty;
			}
		}

		static Dictionary<string, bool> facadeReferenceDict = new Dictionary<string, bool> ();

		static bool RequiresFacadeAssembliesInternal (string fileName)
		{
			if (facadeReferenceDict.TryGetValue (fileName, out var result))
				return result;

			try {
				using (var reader = new PEReader (File.OpenRead (fileName))) {
					var mr = reader.GetMetadataReader ();

					foreach (var assemblyReferenceHandle in mr.AssemblyReferences) {
						var assemblyReference = mr.GetAssemblyReference (assemblyReferenceHandle);
						var name = mr.GetString (assemblyReference.Name);

						// Don't compare the version number since it may change depending on the version of .net standard
						if (name.Equals ("System.Runtime") || name.Equals ("netstandard")) {
							facadeReferenceDict [fileName] = true;
							return true;
						}
					}
				}
			} catch {
				return false;
			}

			facadeReferenceDict [fileName] = false;
			return false;
		}

		static readonly SemaphoreSlim referenceLockAsync = new SemaphoreSlim (1, 1);
		public static async System.Threading.Tasks.Task<bool> RequiresFacadeAssembliesAsync (string filename)
		{
			try {
				await referenceLockAsync.WaitAsync ().ConfigureAwait (false);
				return RequiresFacadeAssembliesInternal (filename);
			} finally {
				referenceLockAsync.Release ();
			}
		}

		public class ManifestResource
		{
			public string Name {
				get; private set;
			}

			Func<Stream> streamCallback;
			public Stream Open ()
			{
				return streamCallback ();
			}

			public ManifestResource (string name, Func<Stream> streamCallback)
			{
				this.streamCallback = streamCallback;
				Name = name;
			}
		}

		/// <summary>
		/// Simply get all assembly manifest resources from an assembly given it's file name.
		/// </summary>
		public static IEnumerable<ManifestResource> GetAssemblyManifestResources (string fileName)
		{
			using (var reader = new PEReader (File.OpenRead (fileName))) {
				var mr = reader.GetMetadataReader ();

				var headers = reader.PEHeaders;
				var resources = headers.CorHeader.ResourcesDirectory;
				var sectionData = reader.GetSectionData (resources.RelativeVirtualAddress);
				if (sectionData.Length == 0)
					return Array.Empty<ManifestResource> (); // RVA could not be found in any section

				var sectionReader = sectionData.GetReader ();
				var manifestResources = mr.ManifestResources;
				var result = new List<ManifestResource> (manifestResources.Count);

				foreach (var manifestResourceHandle in manifestResources) {
					var manifestResource = mr.GetManifestResource (manifestResourceHandle);

					// This means the type is Embedded.
					var isEmbeddedResource = manifestResource.Implementation.IsNil;
					if (!isEmbeddedResource)
						continue;

					int offset = (int)manifestResource.Offset;
					sectionReader.Offset += offset;
					try {
						int length = sectionReader.ReadInt32 ();
						if ((uint)length > sectionReader.RemainingBytes) {
							LoggingService.LogError ("Resource stream invalid length {0}", length.ToString ());
							continue;
						}

						var name = mr.GetString (manifestResource.Name);
						unsafe {
							using (var unmanagedStream = new UnmanagedMemoryStream (sectionReader.CurrentPointer, length, length, FileAccess.Read)) {
								var memoryStream = new MemoryStream (length);
								unmanagedStream.CopyTo (memoryStream);
								memoryStream.Position = 0;
								result.Add (new ManifestResource (name, () => memoryStream));
							}
						}
					} finally {
						sectionReader.Offset -= offset;
					}
				}
				return result;
			}
		}

		[Obsolete("Use Runtime.LoadAssemblyFrom")]
		public Assembly LoadAssemblyFrom (string asmPath)
		{
			return Runtime.LoadAssemblyFrom (asmPath);
		}

		sealed class StringParameterValueTypeProvider : ISignatureTypeProvider<string, object>
		{
			readonly BlobReader valueReader;

			public StringParameterValueTypeProvider (MetadataReader reader, BlobHandle value)
			{
				valueReader = reader.GetBlobReader (value);

				var prolog = valueReader.ReadUInt16 ();
				if (prolog != 1)
					throw new BadImageFormatException ("Invalid custom attribute prolog.");
			}

			public string GetPrimitiveType (PrimitiveTypeCode typeCode) => typeCode != PrimitiveTypeCode.String ? "" : valueReader.ReadSerializedString ();
			public string GetArrayType (string elementType, ArrayShape shape) => "";
			public string GetByReferenceType (string elementType) => "";
			public string GetFunctionPointerType (MethodSignature<string> signature) => "";
			public string GetGenericInstance (string genericType, ImmutableArray<string> typestrings) => "";
			public string GetGenericInstantiation (string genericType, ImmutableArray<string> typeArguments) { throw new NotImplementedException (); }
			public string GetGenericMethodParameter (int index) => "";
			public string GetGenericMethodParameter (object genericContext, int index) { throw new NotImplementedException (); }
			public string GetGenericTypeParameter (int index) => "";
			public string GetGenericTypeParameter (object genericContext, int index) { throw new NotImplementedException (); }
			public string GetModifiedType (string modifier, string unmodifiedType, bool isRequired) => "";
			public string GetPinnedType (string elementType) => "";
			public string GetPointerType (string elementType) => "";
			public string GetSZArrayType (string elementType) => "";
			public string GetTypeFromDefinition (MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => "";
			public string GetTypeFromReference (MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => "";
			public string GetTypeFromSpecification (MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => "";
		}
	}
}
