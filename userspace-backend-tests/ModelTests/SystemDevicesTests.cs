using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model;

namespace userspace_backend_tests.ModelTests
{
    [TestClass]
    public class SystemDevicesTests
    {
        private class TestDevicesRetriever : ISystemDevicesRetriever
        {
            public List<ISystemDevice> Devices { get; set; }

            public IList<ISystemDevice> GetSystemDevices() => Devices;
        }

        private class TestSystemDevice : ISystemDevice
        {
            public string Name { get; set; }

            public string HWID { get; set; }
        }

        [TestMethod]
        public void SystemDevicesProvider_ProvidesDevices()
        {
            List<ISystemDevice> testSystemDevices = new List<ISystemDevice>()
            {
                new TestSystemDevice()
                {
                    Name = "test",
                    HWID = "garble",
                }
            };

            TestDevicesRetriever testDevicesRetriever = new TestDevicesRetriever()
            {
                Devices = testSystemDevices,
            };
            var services = new ServiceCollection();
            services.Add(ServiceDescriptor.Describe(
                serviceType: typeof(ISystemDevicesRetriever),
                implementationFactory: _ => testDevicesRetriever,
                lifetime: ServiceLifetime.Singleton));
            services.AddSingleton<SystemDevicesProvider>();
            var serviceProvider = services.BuildServiceProvider();

            SystemDevicesProvider testObject = serviceProvider.GetRequiredService<SystemDevicesProvider>();
            
            Assert.IsNotNull(testObject);
            Assert.AreEqual(testSystemDevices.Count, testObject.SystemDevices.Count);
            CollectionAssert.AreEquivalent(testSystemDevices, testObject.SystemDevices);
        }

        // This test may need to be excluded if built from deviceless server.
        [TestMethod]
        public void SystemDevicesRetriever_RetrievesDevices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<SystemDevicesRetriever>();
            var serviceProvider = services.BuildServiceProvider();

            SystemDevicesRetriever testObject = serviceProvider.GetRequiredService<SystemDevicesRetriever>();
            Assert.IsNotNull(testObject);

            // These devices will be different per user, but should not be null as long as the user has something giving mouse input.
            IList<ISystemDevice> retrievedDevices = testObject.GetSystemDevices();
            Assert.IsNotNull(retrievedDevices);
            Assert.IsTrue(retrievedDevices.Count > 0);
        }
    }
}
