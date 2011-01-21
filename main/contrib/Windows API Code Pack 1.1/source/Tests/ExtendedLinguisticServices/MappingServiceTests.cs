// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.ExtendedLinguisticServices;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
    public class MappingServiceTests
    {
        [Theory]
        [InlineData("LanguageDetection")]
        [InlineData("ScriptDetection")]
        [InlineData("TransliterationBengaliToLatin")]
        [InlineData("TransliterationCyrillicToLatin")]
        [InlineData("TransliterationDevanagariToLatin")]
        [InlineData("TransliterationHansToHant")]
        [InlineData("TransliterationHantToHans")]
        [InlineData("TransliterationMalayalamToLatin")]
        public void ConstructorWithValidServiceGuidSucceeds(string service)
        {
            Guid guid = ServiceGuidFromServiceString(service);
            MappingService s = new MappingService(guid);

            Assert.Equal<Guid>(s.Guid, guid);
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("11111111-2222-3333-4444-555555555555")]
        public void ConstructorWithInvalidServiceGuidThrowsLinguisticException(string service)
        {
            Guid guid = ServiceGuidFromServiceString(service);

            Assert.Throws<LinguisticException>(() =>
            {
                MappingService s = new MappingService(guid);
            });
        }

        [Fact]
        public void GetServicesOfNullReturnsMoreThanZeroServices()
        {
            MappingService[] ss = MappingService.GetServices(null);
            Assert.True(ss.Length >= 0);
        }

        [Fact]
        public void GetServicesOfNullReturnsKnownServices()
        {
            MappingService[] ss = MappingService.GetServices(null);

            Guid[] guids = new Guid[] {
                MappingAvailableServices.LanguageDetection, 
                MappingAvailableServices.ScriptDetection,
                MappingAvailableServices.TransliterationBengaliToLatin,
                MappingAvailableServices.TransliterationCyrillicToLatin,
                MappingAvailableServices.TransliterationDevanagariToLatin,
                MappingAvailableServices.TransliterationHansToHant,
                MappingAvailableServices.TransliterationHantToHans,
                MappingAvailableServices.TransliterationMalayalamToLatin
            };

            foreach (MappingService s in ss)
            {
                Assert.True(s.BuildVersion >= 0);
                Assert.NotEqual<string>(s.Category, String.Empty);
                Assert.NotEqual<string>(s.Category, "");
                Assert.NotNull(s.Category);
                
                Assert.NotEqual<string>(s.Copyright, String.Empty);
                Assert.NotEqual<string>(s.Copyright, "");
                Assert.NotNull(s.Copyright);

                Assert.NotEqual<string>(s.Description, String.Empty);
                Assert.NotEqual<string>(s.Description, "");
                Assert.NotNull(s.Description);

                Assert.Contains<Guid>(s.Guid, guids);

                Assert.NotEmpty(s.InputContentTypes);
                Assert.True(s.MajorVersion >= 0 && s.MinorVersion >= 0);
                Assert.NotEmpty(s.OutputContentTypes);
            }
        }

        [Theory]
        [InlineData("TransliterationCyrillicToLatin", "добро утро!", 1, 0, 10)]
        [InlineData("TransliterationCyrillicToLatin", "добро hello утро!", 1, 0, 16)] // BUG: possible bug here -- this should be 2 ranges
        public void RecognizeText(
            string service, 
            string text, 
            int expNumberOfDataRanges, 
            int expStartIndexOfFirstDataRange,
            int expEndIndexOfFirstDataRange)
        {
            MappingService s = new MappingService(ServiceGuidFromServiceString(service));
            MappingPropertyBag b = s.RecognizeText(text, null);
            MappingDataRange[] rs = b.GetResultRanges();

            Assert.Equal<int>(expNumberOfDataRanges, rs.Length);
            Assert.Equal<int>(expStartIndexOfFirstDataRange, rs[0].StartIndex);
            Assert.Equal<int>(expEndIndexOfFirstDataRange, rs[0].EndIndex);
            Assert.Equal<string>("text/plain", rs[0].ContentType); // Win7 ELS services support only "text/plain" as content type
        }


        private Guid ServiceGuidFromServiceString(string service)
        {
            Guid guid;

            switch (service)
            {
                case "LanguageDetection": guid = MappingAvailableServices.LanguageDetection; break;
                case "ScriptDetection": guid = MappingAvailableServices.ScriptDetection; break;
                case "TransliterationBengaliToLatin": guid = MappingAvailableServices.TransliterationBengaliToLatin; break;
                case "TransliterationCyrillicToLatin": guid = MappingAvailableServices.TransliterationCyrillicToLatin; break;
                case "TransliterationDevanagariToLatin": guid = MappingAvailableServices.TransliterationDevanagariToLatin; break;
                case "TransliterationHansToHant": guid = MappingAvailableServices.TransliterationHansToHant; break;
                case "TransliterationHantToHans": guid = MappingAvailableServices.TransliterationHantToHans; break;
                case "TransliterationMalayalamToLatin": guid = MappingAvailableServices.TransliterationMalayalamToLatin; break;
                default: guid = new Guid(service); break;
            }

            return guid;
        }
    }
}

