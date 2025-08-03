using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.IO;
using userspace_backend.Model;
using userspace_backend.Hardware;
using DATA = userspace_backend.Data;

namespace userspace_backend
{
    public class NotificationEventArgs : EventArgs
    {
        public string MessageKey { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public object[] FormatArgs { get; set; } = new object[0];
    }
    
    public class ModalEventArgs : EventArgs
    {
        public string ModalType { get; set; } = string.Empty;
        public object[] Parameters { get; set; } = new object[0];
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public static class NotificationManager
    {
        public static event EventHandler<NotificationEventArgs>? NotificationRequested;
        public static event EventHandler<NotificationEventArgs>? QueuedNotificationRequested;
        public static event EventHandler<ModalEventArgs>? QueuedModalRequested;

        public static void TriggerNotification(string messageKey, NotificationType type)
        {
            TriggerNotification(messageKey, type, new object[0]);
        }

        public static void TriggerNotification(string messageKey, NotificationType type, params object[] formatArgs)
        {
            NotificationRequested?.Invoke(null, new NotificationEventArgs
            {
                MessageKey = messageKey,
                Type = type,
                FormatArgs = formatArgs
            });
        }

        public static void QueueNotification(string messageKey, NotificationType type)
        {
            QueueNotification(messageKey, type, new object[0]);
        }

        public static void QueueNotification(string messageKey, NotificationType type, params object[] formatArgs)
        {
            QueuedNotificationRequested?.Invoke(null, new NotificationEventArgs
            {
                MessageKey = messageKey,
                Type = type,
                FormatArgs = formatArgs
            });
        }
        
        public static void QueueModal(string modalType, params object[] parameters)
        {
            QueuedModalRequested?.Invoke(null, new ModalEventArgs
            {
                ModalType = modalType,
                Parameters = parameters
            });
        }
    }

    public class BackEnd
    {
        private readonly IDeviceInfoProvider? deviceInfoProvider;

        public BackEnd(IBackEndLoader backEndLoader) : this(backEndLoader, null)
        {
        }

        public BackEnd(IBackEndLoader backEndLoader, IDeviceInfoProvider? deviceInfoProvider)
        {
            BackEndLoader = backEndLoader;
            this.deviceInfoProvider = deviceInfoProvider;
            Devices = new DevicesModel(deviceInfoProvider);
            Profiles = new ProfilesModel([]);
            Settings = new DATA.Settings();
            Hardware = new HardwareManager(Devices);
        }

        public DevicesModel Devices { get; set; }

        public MappingsModel Mappings { get; set; } = null!;

        public ProfilesModel Profiles { get; set; }

        public DATA.Settings Settings { get; set; }

        public HardwareManager Hardware { get; set; }

        public DeviceModel? UnconfiguredActiveDevice { get; set; }

        protected IBackEndLoader BackEndLoader { get; set; }

        public IEnumerable<string> GetAvailableDeviceNames()
        {
            // Use stored names from system devices (already resolved during RefreshSystemDevices)
            return Devices.SystemDevices.Select(d => d.name).Where(name => !string.IsNullOrEmpty(name));
        }

        public (string deviceName, string hardwareId) GetDeviceDisplayInfo(string hid)
        {
            if (string.IsNullOrEmpty(hid))
            {
                return ("Unknown Device", string.Empty);
            }

            // Use stored product string for faster lookup
            string deviceName = Devices.GetProductStringFromHID(hid);
            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = Hardware.ResolveDeviceNameFromHID(hid);
            }
            
            return (deviceName, hid);
        }

        public void Load()
        {
            IEnumerable<DATA.Device> devicesData = BackEndLoader.LoadDevices(); ;
            LoadDevicesFromData(devicesData);

            IEnumerable<DATA.Profile> profilesData = BackEndLoader.LoadProfiles(); ;
            LoadProfilesFromData(profilesData);

            DATA.MappingSet mappingData = BackEndLoader.LoadMappings();
            Mappings = new MappingsModel(mappingData, Devices.DeviceGroups, Profiles);

            Settings = BackEndLoader.LoadSettings() ?? new DATA.Settings();
        }

        protected void LoadDevicesFromData(IEnumerable<DATA.Device> devicesData)
        {
            foreach(var deviceData in devicesData)
            {
                Devices.TryAddDevice(deviceData);
            }
        }

        protected void LoadProfilesFromData(IEnumerable<DATA.Profile> profileData)
        {
            foreach (var profile in profileData)
            {
                Profiles.TryAddProfile(profile);
            }
        }

        public void ValidateDevicesAfterUIReady()
        {
            ValidateDevicesAvailability();
            
            var activeDevice = Hardware.ActiveDevice;
            
            if (activeDevice != null)
            {
                bool isConfigured = Devices.Devices.Contains(activeDevice);
                
                if (!isConfigured)
                {
                    UnconfiguredActiveDevice = activeDevice;
                    NotificationManager.QueueModal("UnconfiguredDevice", activeDevice.Name.CurrentValidatedValue);
                }
            }
        }

        protected void ValidateDevicesAvailability()
        {
            Devices.RefreshSystemDevices();
            
            var systemDeviceHWIDs = Devices.SystemDevices
                .Select(d => d.id)
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            foreach (var storedDevice in Devices.DevicesEnumerable)
            {
                var storedHWID = storedDevice.HardwareID.ModelValue;
                
                if (!string.IsNullOrEmpty(storedHWID) && !systemDeviceHWIDs.Contains(storedHWID))
                {
                    string deviceName = Devices.GetExactDeviceNameFromHID(storedHWID);
                    NotificationManager.QueueNotification("DeviceNoLongerAvailable", NotificationType.Warning, deviceName);
                }
            }
            
            if (Devices.SystemDevices.Any())
            {
                var firstMouse = Devices.SystemDevices.FirstOrDefault();
                if (firstMouse != null && !string.IsNullOrEmpty(firstMouse.id))
                {
                    Hardware.UpdateCurrentInputDevice(IntPtr.Zero, firstMouse.id, firstMouse.name ?? "Mouse");
                }
            }
        }

        public bool Apply()
        {
            try
            {
                bool success = WriteToDriver();
                if (success)
                {
                    WriteSettingsToDisk();
                }
                return success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void ApplySettingsOnly()
        {
            WriteSettingsToDisk();
        }

        protected void WriteSettingsToDisk()
        {
            BackEndLoader.WriteSettingsToDisk(
                Devices.DevicesEnumerable,
                Mappings,
                Profiles.Profiles);
            
            BackEndLoader.WriteSettings(Settings);
        }

        protected bool WriteToDriver()
        {
            MappingModel mappingToApply = Mappings.GetMappingToSetActive();
            
            // Validate mappings before applying
            if (!ValidateMappingBeforeApplying(mappingToApply))
            {
                return false;
            }
            
            DriverConfig config = MapToDriverConfig(mappingToApply);
            try
            {
                config.Activate();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        protected DriverConfig MapToDriverConfig(MappingModel mappingModel)
        {
            IEnumerable<DeviceSettings> configDevices = MapToDriverDevices(mappingModel);
            IEnumerable<Profile> configProfiles = MapToDriverProfiles(mappingModel);

            DriverConfig config = DriverConfig.GetDefault();
            config.profiles = configProfiles.ToList();
            config.devices = configDevices.ToList();
            config.accels = configProfiles.Select(p => new ManagedAccel(p)).ToList();
            return config;
        }

        protected IEnumerable<DeviceSettings> MapToDriverDevices(MappingModel mapping)
        {
            return mapping.IndividualMappings.SelectMany(
                dg => MapToDriverDevices(dg.DeviceGroup, dg.Profile.Name.ModelValue));
        }

        protected IEnumerable<Profile> MapToDriverProfiles(MappingModel mapping)
        {
            IEnumerable<ProfileModel> ProfilesToMap = mapping.IndividualMappings.Select(m => m.Profile).Distinct();
            return ProfilesToMap.Select(p => p.CurrentValidatedDriverProfile);
        }

        protected IEnumerable<DeviceSettings> MapToDriverDevices(DeviceGroupModel dg, string profileName)
        {
            IEnumerable<DeviceModel> deviceModels = Devices.Devices.Where(d => d.DeviceGroup.Equals(dg));
            return deviceModels.Select(dm => MapToDriverDevice(dm, profileName));
        }

        protected DeviceSettings MapToDriverDevice(DeviceModel deviceModel, string profileName)
        {
            return new DeviceSettings()
            {
                id = deviceModel.HardwareID.ModelValue,
                name = deviceModel.Name.ModelValue,
                profile = profileName,
                config = new DeviceConfig()
                {
                    disable = deviceModel.Ignore.ModelValue,
                    dpi = deviceModel.DPI.ModelValue,
                    pollingRate = deviceModel.PollRate.ModelValue,
                    pollTimeLock = false,
                    setExtraInfo = false,
                    maximumTime = 200,
                    minimumTime = 0.1,
                }
            };
        }

        protected bool ValidateMappingBeforeApplying(MappingModel mapping)
        {
            bool hasErrors = false;
            var systemDevices = MultiHandleDevice.GetList();
            var systemDeviceIds = systemDevices.Select(d => d.id.ToUpperInvariant()).ToHashSet();

            foreach (var individualMapping in mapping.IndividualMappings)
            {
                if (!Profiles.TryGetProfile(individualMapping.Profile.Name.ModelValue, out _))
                {
                    NotificationManager.QueueNotification("ProfileNotFound", NotificationType.Error, individualMapping.Profile.Name.ModelValue);
                    hasErrors = true;
                    continue;
                }

                var devicesInGroup = Devices.Devices.Where(d => d.DeviceGroup.Equals(individualMapping.DeviceGroup));
                
                foreach (var device in devicesInGroup)
                {
                    if (device.Ignore.ModelValue) continue;
                    
                    // Check if the device hardware ID exists in the system
                    string deviceHwId = device.HardwareID.ModelValue.ToUpperInvariant();
                    if (!string.IsNullOrEmpty(deviceHwId) && !systemDeviceIds.Contains(deviceHwId))
                    {
                        NotificationManager.QueueNotification("DeviceNotConnected", NotificationType.Error, device.Name.ModelValue, deviceHwId);
                        hasErrors = true;
                    }
                }
            }

            return !hasErrors;
        }
    }
}
