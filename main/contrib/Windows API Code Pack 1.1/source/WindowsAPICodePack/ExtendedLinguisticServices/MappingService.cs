// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.WindowsAPICodePack.ExtendedLinguisticServices.Resources;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{

    /// <summary>
    /// Represents an ELS service.
    /// A detailed overview of the Extended Linguistic Services platform is available at:
    /// http://msdn.microsoft.com/en-us/library/dd317839(VS.85).aspx
    /// </summary>
    public class MappingService
    {
        private Win32Service _win32Service;
        private IntPtr _service;

        /// <summary>
        /// Constructs a new <see cref="MappingService">MappingService</see> object by instanciating an ELS service
        /// by its guid. For Windows 7, the only supported GUIDs are provided as
        /// readonly members of the <see cref="MappingAvailableServices">MappingAvailableServices</see> class.
        ///
        /// If the service
        /// with the specified guid doesn't exist, a <see cref="LinguisticException">LinguisticException</see> is thrown.
        /// </summary>
        /// <param name="serviceIdentifier">The guid of the service to instantiate.</param>        
        public MappingService(Guid serviceIdentifier)
        {
            ThrowIfNotWin7();

            IntPtr servicePointer;
            UInt32 serviceCount = 0;
            UInt32 hresult = 0;

            // First, check to see if we already have the service in the cache:
            servicePointer = ServiceCache.Instance.GetCachedService(ref serviceIdentifier);
            if (servicePointer != IntPtr.Zero)
            {
                _service = servicePointer;
                _win32Service = InteropTools.Unpack<Win32Service>(_service);
            }
            else // pService is IntPtr.Zero in this case.
            {
                // If we don't, we must find it via MappingGetServices:
                IntPtr guidPtr = IntPtr.Zero;
                try
                {
                    guidPtr = Marshal.AllocHGlobal(InteropTools.SizeOfGuid);
                    Win32EnumOptions enumOptions = new Win32EnumOptions();
                    enumOptions._size = InteropTools.SizeOfWin32EnumOptions;
                    Marshal.StructureToPtr(serviceIdentifier, guidPtr, false);
                    enumOptions._pGuid = guidPtr;
                    hresult = Win32NativeMethods.MappingGetServices(ref enumOptions, ref servicePointer, ref serviceCount);
                    if (hresult != 0)
                    {
                        throw new LinguisticException(hresult);
                    }
                    if (servicePointer == IntPtr.Zero)
                    {
                        throw new InvalidOperationException();
                    }
                    if (serviceCount != 1)
                    {
                        hresult = Win32NativeMethods.MappingFreeServices(servicePointer);
                        if (hresult == 0)
                        {
                            throw new InvalidOperationException();
                        }
                        else
                        {
                            throw new LinguisticException(hresult);
                        }
                    }
                    IntPtr[] services = new IntPtr[1];
                    ServiceCache.Instance.RegisterServices(ref servicePointer, services);
                    _service = services[0];
                    _win32Service = InteropTools.Unpack<Win32Service>(_service);
                }
                finally
                {
                    if (servicePointer != IntPtr.Zero)
                    {
                        // Ignore the result if an exception is being thrown.
                        Win32NativeMethods.MappingFreeServicesVoid(servicePointer);
                    }
                    if (guidPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(guidPtr);
                    }
                }
            }
        }

        private MappingService(IntPtr pService)
        {
            _service = pService;
            _win32Service = InteropTools.Unpack<Win32Service>(_service);
        }

        /// <summary>
        /// Retrieves a list of available ELS platform-supported services, along with associated
        /// information, according to application-specified criteria.
        /// </summary>
        /// <param name="options">Optional. A <see cref="MappingEnumOptions">MappingEnumOptions</see> object containing criteria to use during
        /// enumeration of services. The application specifies null for this parameter to retrieve all
        /// installed services.</param>
        /// <returns>An array of <see cref="MappingService">MappingService</see> objects matching the criteria supplied in the options
        /// parameter.</returns>
        public static MappingService[] GetServices(MappingEnumOptions options)
        {
            ThrowIfNotWin7();

            IntPtr servicePointer = IntPtr.Zero;
            UInt32 serviceCount = 0;
            UInt32 hresult = 0;
            IntPtr guidPointer = IntPtr.Zero;
            try
            {
                if (options != null)
                {
                    Win32EnumOptions enumOptions = options._win32EnumOption;
                    Nullable<Guid> pGuid = options._guid;
                    if (pGuid != null)
                    {
                        Guid guid = (Guid)pGuid;
                        guidPointer = Marshal.AllocHGlobal(InteropTools.SizeOfGuid);
                        Marshal.StructureToPtr(guid, guidPointer, false);
                        enumOptions._pGuid = guidPointer;
                    }
                    hresult = Win32NativeMethods.MappingGetServices(ref enumOptions, ref servicePointer, ref serviceCount);
                }
                else
                {
                    hresult = Win32NativeMethods.MappingGetServices(IntPtr.Zero, ref servicePointer, ref serviceCount);
                }

                if (hresult != 0)
                {
                    throw new LinguisticException(hresult);
                }
                if ((servicePointer == IntPtr.Zero) != (serviceCount == 0))
                {
                    throw new InvalidOperationException();
                }

                IntPtr[] services = new IntPtr[serviceCount];
                ServiceCache.Instance.RegisterServices(ref servicePointer, services);
                MappingService[] result = new MappingService[serviceCount];
                for (int i = 0; i < serviceCount; ++i)
                {
                    result[i] = new MappingService(services[i]);
                }
                return result;
            }
            finally
            {
                if (servicePointer != IntPtr.Zero)
                {
                    // Ignore the result if an exception is being thrown.
                    Win32NativeMethods.MappingFreeServicesVoid(servicePointer);
                }
                if (guidPointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(guidPointer);
                }
            }
        }

        /// <summary>
        /// Calls an ELS service to recognize text. For example, the Microsoft Language Detection service
        /// will attempt to recognize the language in which the input text is written.
        /// </summary>
        /// <param name="text">The text to recognize. The text must be UTF-16, but some services have additional
        /// requirements for the input format. This parameter cannot be set to null.</param>
        /// <param name="options">Optional. A <see cref="MappingOptions">MappingOptions</see> object containing options that affect the result and
        /// behavior of text recognition. The application does not have to specify values for all object members.
        /// This parameter can be set to null to use the default mapping options.</param>
        /// <returns>A <see cref="MappingPropertyBag">MappingPropertyBag</see> object in which the service has stored its results. The structure is filled
        /// with information produced by the service during text recognition.</returns>
        public MappingPropertyBag RecognizeText(string text, MappingOptions options)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            return RecognizeText(text, text.Length, 0, options);
        }

        /// <summary>
        /// Calls an ELS service to recognize text. For example, the Microsoft Language Detection service
        /// will attempt to recognize the language in which the input text is written.
        /// </summary>
        /// <param name="text">The text to recognize. The text must be UTF-16, but some services have additional
        /// requirements for the input format. This parameter cannot be set to null.</param>
        /// <param name="length">Length, in characters, of the text specified in text.</param>
        /// <param name="index">Index inside the specified text to be used by the service. This value should be
        /// between 0 and length-1. If the application wants to process the entire text, it should set this
        /// parameter to 0.</param>
        /// <param name="options">Optional. A <see cref="MappingOptions">MappingOptions</see> object containing options that affect the result and
        /// behavior of text recognition. The application does not have to specify values for all object members.
        /// This parameter can be set to null to use the default mapping options.</param>
        /// <returns>A <see cref="MappingPropertyBag">MappingPropertyBag</see> object in which the service has stored its results. The structure is filled
        /// with information produced by the service during text recognition.</returns>
        public MappingPropertyBag RecognizeText(string text, int length, int index, MappingOptions options)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            if (length > text.Length || length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            UInt32 hResult = LinguisticException.Fail;
            MappingPropertyBag bag = new MappingPropertyBag(options, text);
            try
            {
                hResult = Win32NativeMethods.MappingRecognizeText(
                    _service, bag._text.AddrOfPinnedObject(), (uint)length, (uint)index,
                    bag._options, ref bag._win32PropertyBag);
                if (hResult != 0)
                {                    
                    throw new LinguisticException(hResult);
                }
                return bag;
            }
            catch
            {
                bag.Dispose();
                throw;
            }

        }

        private void RunRecognizeText(object threadContext)
        {
            MappingRecognizeAsyncResult asyncResult = (MappingRecognizeAsyncResult)threadContext;
            MappingResultState resultState = new MappingResultState();
            MappingPropertyBag bag = null;
            try
            {
                bag = this.RecognizeText(asyncResult.Text, asyncResult.Length, asyncResult.Index, asyncResult.Options);
            }
            catch (LinguisticException linguisticException)
            {
                resultState = linguisticException.ResultState;
            }
            asyncResult.SetResult(bag, resultState);
            // Don't catch any exceptions.
            try
            {
                asyncResult.AsyncCallback(asyncResult);
            }
            finally
            {
                Thread.MemoryBarrier();
                ((ManualResetEvent)asyncResult.AsyncWaitHandle).Set();
            }
        }

        /// <summary>
        /// Calls an ELS service to recognize text. For example, the Microsoft Language Detection service
        /// will attempt to recognize the language in which the input text is written. The execution will be
        /// handled in a thread from the managed thread pool, and the asynchronous wait handle of the returned
        /// <see cref="MappingRecognizeAsyncResult">MappingRecognizeAsyncResult</see> object will be notified when the operation completes. The results of
        /// the call will be stored inside the <see cref="MappingRecognizeAsyncResult">MappingRecognizeAsyncResult</see> object.
        /// </summary>
        /// <param name="text">The text to recognize. The text must be UTF-16, but some services have additional
        /// requirements for the input format. This parameter cannot be set to null.</param>
        /// <param name="options">Optional. A <see cref="MappingOptions">MappingOptions</see> object containing options that affect the result and
        /// behavior of text recognition. The application does not have to specify values for all object members.
        /// This parameter can be set to null to use the default mapping options.</param>
        /// <param name="asyncCallback">An application callback delegate to receive callbacks with the results from
        /// the recognize operation. Cannot be set to null.</param>
        /// <param name="callerData">Optional. Private application object passed to the callback function
        /// by a service after text recognition is complete. The application must set this parameter to null to
        /// indicate no private application data.</param>
        /// <returns>A <see cref="MappingRecognizeAsyncResult">MappingRecognizeAsyncResult</see> object describing the asynchronous operation.</returns>
        public MappingRecognizeAsyncResult BeginRecognizeText(string text, MappingOptions options, AsyncCallback asyncCallback, object callerData)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            return BeginRecognizeText(text, text.Length, 0, options, asyncCallback, callerData);
        }

        /// <summary>
        /// Calls an ELS service to recognize text. For example, the Microsoft Language Detection service
        /// will attempt to recognize the language in which the input text is written. The execution will be
        /// handled in a thread from the managed thread pool, and the asynchronous wait handle of the returned
        /// <see cref="MappingRecognizeAsyncResult">MappingRecognizeAsyncResult</see> object will be notified when the operation completes. The results of
        /// the call will be stored inside the <see cref="MappingRecognizeAsyncResult">MappingRecognizeAsyncResult</see> object.
        /// </summary>
        /// <param name="text">The text to recognize. The text must be UTF-16, but some services have additional
        /// requirements for the input format. This parameter cannot be set to null.</param>
        /// <param name="length">Length, in characters, of the text specified in text.</param>
        /// <param name="index">Index inside the specified text to be used by the service. This value should be
        /// between 0 and length-1. If the application wants to process the entire text, it should set this
        /// parameter to 0.</param>
        /// <param name="options">Optional. A <see cref="MappingOptions">MappingOptions</see> object containing options that affect the result and
        /// behavior of text recognition. The application does not have to specify values for all object members.
        /// This parameter can be set to null to use the default mapping options.</param>
        /// <param name="asyncCallback">An application callback delegate to receive callbacks with the results from
        /// the recognize operation. Cannot be set to null.</param>
        /// <param name="callerData">Optional. Private application object passed to the callback function
        /// by a service after text recognition is complete. The application must set this parameter to null to
        /// indicate no private application data.</param>
        /// <returns>A <see cref="MappingRecognizeAsyncResult">MappingRecognizeAsyncResult</see> object describing the asynchronous operation.</returns>
        public MappingRecognizeAsyncResult BeginRecognizeText(string text, int length, int index, MappingOptions options, AsyncCallback asyncCallback, object callerData)
        {
            if (asyncCallback == null)
            {
                throw new ArgumentNullException("asyncCallback");
            }
            MappingRecognizeAsyncResult result = new MappingRecognizeAsyncResult(callerData, asyncCallback, text, length, index, options);
            try
            {
                ThreadPool.QueueUserWorkItem(this.RunRecognizeText, result);
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Waits for the asynchronous operation to complete.
        /// </summary>
        /// <param name="asyncResult">The <see cref="MappingRecognizeAsyncResult">MappingRecognizeAsyncResult</see> object associated with the operation.</param>        
        public static void EndRecognizeText(MappingRecognizeAsyncResult asyncResult)
        {
            if (asyncResult != null && !asyncResult.IsCompleted)
            {
                asyncResult.AsyncWaitHandle.WaitOne();
            }
        }

        /// <summary>
        /// Causes an ELS service to perform an action after text recognition has occurred. For example,
        /// a phone dialer service first must recognize phone numbers and then can perform the "action"
        /// of dialing a number.
        /// </summary>
        /// <param name="bag">A <see cref="MappingPropertyBag">MappingPropertyBag</see> object containing the results of a previous call to
        /// MappingService.MappingRecognizeText. This parameter cannot be set to null.</param>
        /// <param name="rangeIndex">A starting index inside the text recognition results for a recognized
        /// text range. This value should be between 0 and the range count.</param>
        /// <param name="actionId">The identifier of the action to perform.
        /// This parameter cannot be set to null.</param>
        public static void DoAction(MappingPropertyBag bag, int rangeIndex, string actionId)
        {
            if (bag == null) { throw new ArgumentNullException("bag"); }

            if (rangeIndex < 0)
            {
                throw new LinguisticException(LinguisticException.InvalidArgs);
            }
            UInt32 hResult = Win32NativeMethods.MappingDoAction(ref bag._win32PropertyBag, (uint)rangeIndex, actionId);
            if (hResult != 0)
            {
                throw new LinguisticException(hResult);
            }
        }

        private void RunDoAction(object threadContext)
        {
            MappingActionAsyncResult asyncResult = (MappingActionAsyncResult)threadContext;
            MappingResultState resultState = new MappingResultState();
            try
            {
                DoAction(asyncResult.PropertyBag, asyncResult.RangeIndex, asyncResult.ActionId);
            }
            catch (LinguisticException linguisticException)
            {
                resultState = linguisticException.ResultState;
            }
            asyncResult.SetResult(asyncResult.PropertyBag, resultState);

            // Don't catch any exceptions.
            try
            {
                asyncResult.AsyncCallback(asyncResult);
            }
            finally
            {
                Thread.MemoryBarrier();
                ((ManualResetEvent)asyncResult.AsyncWaitHandle).Set();
            }
        }

        /// <summary>
        /// Causes an ELS service to perform an action after text recognition has occurred. For example,
        /// a phone dialer service first must recognize phone numbers and then can perform the "action"
        /// of dialing a number.
        /// </summary>
        /// <param name="bag">A <see cref="MappingPropertyBag">MappingPropertyBag</see> object containing the results of a previous call to
        /// MappingService.MappingRecognizeText. This parameter cannot be set to null.</param>
        /// <param name="rangeIndex">A starting index inside the text recognition results for a recognized
        /// text range. This value should be between 0 and the range count.</param>
        /// <param name="actionId">The identifier of the action to perform.
        /// This parameter cannot be set to null.</param>
        /// <param name="asyncCallback">An application callback delegate to receive callbacks with the results from
        /// the action operation. Cannot be set to null.</param>
        /// <param name="callerData">Optional. Private application object passed to the callback function
        /// by a service after the action operation is complete. The application must set this parameter to null
        /// to indicate no private application data.</param>
        /// <returns>A <see cref="MappingActionAsyncResult">MappingActionAsyncResult</see> object describing the asynchronous operation.</returns>
        public MappingActionAsyncResult BeginDoAction(MappingPropertyBag bag, int rangeIndex, string actionId, AsyncCallback asyncCallback, object callerData)
        {
            MappingActionAsyncResult result = new MappingActionAsyncResult(callerData, asyncCallback, bag, rangeIndex, actionId);
            try
            {
                ThreadPool.QueueUserWorkItem(this.RunDoAction, result);
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Waits for the asynchronous operation to complete.
        /// </summary>
        /// <param name="asyncResult">The MappingActionAsyncResult object associated with the operation.</param>
        public static void EndDoAction(MappingActionAsyncResult asyncResult)
        {
            if (asyncResult != null && !asyncResult.IsCompleted)
            {
                asyncResult.AsyncWaitHandle.WaitOne();
            }
        }

        /// <summary>
        /// Copyright information about the service.
        /// </summary>
        public string Copyright
        {
            get
            {
                return _win32Service._copyright;
            }
        }

        /// <summary>
        /// Major version number that is used to track changes to the service.
        /// </summary>
        public int MajorVersion
        {
            get
            {
                return _win32Service._majorVersion;
            }
        }

        /// <summary>
        /// Minor version number that is used to track changes to the service.
        /// </summary>
        public int MinorVersion
        {
            get
            {
                return _win32Service._minorVersion;
            }
        }

        /// <summary>
        /// Build version that is used to track changes to the service.
        /// </summary>
        public int BuildVersion
        {
            get
            {
                return _win32Service._buildVersion;
            }
        }

        /// <summary>
        /// Step version that is used to track changes to the service.
        /// </summary>
        public int StepVersion
        {
            get
            {
                return _win32Service._stepVersion;
            }
        }

        /// <summary>
        /// Optional. A collection of input content types, following the format of the MIME content types, that
        /// identify the format that the service interprets when the application passes data. Examples of
        /// content types are "text/plain", "text/html" and "text/css".
        /// </summary>
        public IEnumerable<string> InputContentTypes
        {
            get
            {
                return InteropTools.UnpackStringArray(_win32Service._inputContentTypes, _win32Service._inputContentTypesCount);
            }
        }

        /// <summary>
        /// Optional. A collection of output content types, following the format of the MIME content types, that
        /// identify the format in which the service retrieves data.
        /// </summary>
        public IEnumerable<string> OutputContentTypes
        {
            get
            {
                return InteropTools.UnpackStringArray(_win32Service._outputContentTypes, _win32Service._outputContentTypesCount);
            }
        }

        /// <summary>
        /// A collection of the input languages, following the IETF naming convention, that the service accepts. This
        /// member is set to null if the service can work with any input language.
        /// </summary>
        public IEnumerable<string> InputLanguages
        {
            get
            {
                return InteropTools.UnpackStringArray(_win32Service._inputLanguages, _win32Service._inputLanguagesCount);
            }
        }

        /// <summary>
        /// A collection of output languages, following the IETF naming convention, in which the service can retrieve
        /// results. This member is set to null if the service can retrieve results in any language, or if the service
        /// ignores the output language.
        /// </summary>
        public IEnumerable<string> OutputLanguages
        {
            get
            {
                return InteropTools.UnpackStringArray(_win32Service._outputLanguages, _win32Service._outputLanguagesCount);
            }
        }

        /// <summary>
        /// A collection of input scripts, with Unicode standard script names, that are supported by the service.
        /// This member is set to null if the service can work with any scripts, or if the service ignores the
        /// input scripts.
        /// </summary>
        public IEnumerable<string> InputScripts
        {
            get
            {
                return InteropTools.UnpackStringArray(_win32Service._inputScripts, _win32Service._inputScriptsCount);
            }
        }

        /// <summary>
        /// A collection of output scripts supported by the service. This member is set to null if the service can work with
        /// any scripts, or the service ignores the output scripts.
        /// </summary>
        public IEnumerable<string> OutputScripts
        {
            get
            {
                return InteropTools.UnpackStringArray(_win32Service._outputScripts, _win32Service._outputScriptsCount);
            }
        }

        /// <summary>
        /// Globally unique identifier (guid) for the service.
        /// </summary>
        public Guid Guid
        {
            get
            {
                return _win32Service._guid;
            }
        }

        /// <summary>
        /// The service category for the service, for example, "Transliteration".
        /// </summary>
        public string Category
        {
            get
            {
                return _win32Service._category;
            }
        }

        /// <summary>
        /// The service description. This text can be localized.
        /// </summary>
        public string Description
        {
            get
            {
                return _win32Service._description;
            }
        }

        /// <summary>
        /// Flag indicating the language mapping between input language and output language that is supported
        /// by the service. Possible values are shown in the following table.
        /// 0 - The input and output languages are not paired and the service can receive data in any of the
        /// input languages and render data in any of the output languages.
        /// 1 - The arrays of the input and output languages for the service are paired. In other words, given
        /// a particular input language, the service retrieves results in the paired language defined in the
        /// output language array. Use of the language pairing can be useful, for example, in bilingual
        /// dictionary scenarios.
        /// </summary>
        public bool IsOneToOneLanguageMapping
        {
            get
            {
                return (_win32Service._serviceTypes & ServiceTypes.IsOneToOneLanguageMapping)
                    == ServiceTypes.IsOneToOneLanguageMapping;
            }
        }

        /// <summary>
        /// Indicates whether this feature is supported on the current platform.
        /// </summary>
        public static bool IsPlatformSupported
        {
            get
            {
                // We need Windows 7 onwards ...
                return RunningOnWin7;
            }
        }

        /// <summary>
        /// Determines if the application is running on Windows 7
        /// </summary>
        internal static bool RunningOnWin7
        {
            get
            {
                return (Environment.OSVersion.Version.Major > 6) ||
                    (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1);
            }
        }

        /// <summary>
        /// Throws PlatformNotSupportedException if the application is not running on Windows 7
        /// </summary>
        internal static void ThrowIfNotWin7()
        {
            if (!RunningOnWin7)
            {
                throw new PlatformNotSupportedException(LocalizedMessages.SupportedWindows7);
            }
        }

    }

}
