// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
    public class PropertyKeyTests
    {
        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000", 5)]
        public void ConstructorWithGuid(string formatIdString, int propertyId)
        {
            Guid formatId = new Guid(formatIdString);
            PropertyKey pk = new PropertyKey(formatId, propertyId);

            Assert.Equal<Guid>(formatId, pk.FormatId);
            Assert.Equal<int>(propertyId, pk.PropertyId);
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000", 5)]
        public void ConstructorWithString(string formatId, int propertyId)
        {
            PropertyKey pk = new PropertyKey(formatId, propertyId);

            Assert.Equal<Guid>(new Guid(formatId), pk.FormatId);
            Assert.Equal<int>(propertyId, pk.PropertyId);
        }

        [Fact]
        public void ToStringReturnsExpectedString()
        {
            Guid guid = new Guid("00000000-1111-2222-3333-000000000000");
            int property = 1234;

            PropertyKey key = new PropertyKey(guid, property);

            Assert.Equal<string>(
                "{" + guid.ToString() + "}, " + property.ToString(),
                key.ToString());
        }
    }
}
