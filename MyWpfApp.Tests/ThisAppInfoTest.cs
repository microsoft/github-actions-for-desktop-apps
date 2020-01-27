using NUnit.Framework;
using MyWPFApp;

namespace MyWPFApp.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            var thisAppInfo = new ThisAppInfo();
        }

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
