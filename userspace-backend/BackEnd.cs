using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.IO;
using userspace_backend.Model;
using DATA = userspace_backend.Data;

namespace userspace_backend
{
    public class NotificationEventArgs : EventArgs
    {
        public string MessageKey { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public object[] FormatArgs { get; set; } = new object[0];
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
    }

    public class BackEnd
    {
        public BackEnd(IBackEndLoader backEndLoader)
        {
            BackEndLoader = backEndLoader;
            Devices = new DevicesModel();
            Profiles = new ProfilesModel([]);
            Settings = new DATA.Settings();
        }

        public DevicesModel Devices { get; set; }

        public MappingsModel Mappings { get; set; } = null!;

        public ProfilesModel Profiles { get; set; }

        public DATA.Settings Settings { get; set; }

        protected IBackEndLoader BackEndLoader { get; set; }

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
                    NotificationManager.QueueNotification("DeviceNoLongerAvailable", NotificationType.Warning, storedHWID);
                }
            }
        }

        public void Apply()
        {
            try
            {
                WriteToDriver();
            }
            catch (Exception)
            {
                return;
            }

            WriteSettingsToDisk();
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

        protected void WriteToDriver()
        {
            MappingModel mappingToApply = Mappings.GetMappingToSetActive();
            DriverConfig config = MapToDriverConfig(mappingToApply);
            try
            {
                config.Activate();
            }
            catch (Exception)
            {
                // Log this once logging is added
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
    }
}
