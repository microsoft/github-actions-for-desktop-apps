// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using NUnit.Framework;
using MyWPFApp;

namespace MyWPFApp.Tests
{
    public class Tests
    {
        [Test]
        public void GetDisplayName()
        {
            var displayName = ThisAppInfo.GetDisplayName();
            Assert.AreEqual("Not packaged", displayName);
        }

        [Test]
        public void GetAppInstallerUri()
        {
            var appInstallerUri = ThisAppInfo.GetAppInstallerUri();

            Assert.AreEqual("Not packaged", appInstallerUri);
        }
    }
}
