using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model
{
    /// <summary>
    /// Holds system devices in observable collection and refreshes list when desired.
    /// </summary>
    public interface ISystemDevicesProvider
    {
        ReadOnlyObservableCollection<ISystemDevice> SystemDevices { get; }

        void RefreshSystemDevices();
    }

    /// <summary>
    /// Application implementation of <see cref="ISystemDevicesProvider"/>
    /// </summary>
    public class SystemDevicesProvider : ISystemDevicesProvider
    {
        public SystemDevicesProvider(ISystemDevicesRetriever devicesRetriever)
        {
            DevicesRetriever = devicesRetriever;
            SystemDevicesInternal = new ObservableCollection<ISystemDevice>();
            SystemDevices = new ReadOnlyObservableCollection<ISystemDevice>(SystemDevicesInternal);
            RefreshSystemDevices();
        }

        public ReadOnlyObservableCollection<ISystemDevice> SystemDevices { get; }

        protected ObservableCollection<ISystemDevice> SystemDevicesInternal { get; }

        protected ISystemDevicesRetriever DevicesRetriever { get; }

        public void RefreshSystemDevices()
        {
            // TODO: Replace with "addrange" equivalent from ObservableCollection child class
            SystemDevicesInternal.Clear();
            IList<ISystemDevice> retrievedDevices = DevicesRetriever.GetSystemDevices();

            foreach (ISystemDevice retrievedDevice in retrievedDevices)
            {
                SystemDevicesInternal.Add(retrievedDevice);
            }
        }
    }

    /// <summary>
    /// Retrieves list of devices from operating system
    /// </summary>
    public interface ISystemDevicesRetriever
    {
        IList<ISystemDevice> GetSystemDevices();
    }

    /// <summary>
    /// Application implementation of <see cref="SystemDevicesRetriever"/>
    /// </summary>
    public sealed class SystemDevicesRetriever : ISystemDevicesRetriever
    {
        public IList<ISystemDevice> GetSystemDevices()
        {
            IList<MultiHandleDevice> rawDevices = MultiHandleDevice.GetList();
            return rawDevices.Select(d => new SystemDevice(d) as ISystemDevice).ToList();
        }
    }

    /// <summary>
    /// Interface to represent devices as they come from windows.
    /// The actual class from windows is non-trivial to construct and test.
    /// </summary>
    public interface ISystemDevice
    {
        public string Name { get; }

        public string HWID { get; }
    }

    /// <summary>
    /// Data class to wrap <see cref="MultiHandleDevice"/>
    /// </summary>
    public class SystemDevice : ISystemDevice
    {
        public SystemDevice(MultiHandleDevice multiHandleDevice)
        {
            RawDevice = multiHandleDevice;
        }

        public string Name { get => RawDevice.name; }

        public string HWID { get => RawDevice.id; }

        private MultiHandleDevice RawDevice { get; }
    }
}
