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
using userspace_backend.Model.EditableSettings;
using userspace_backend.Hardware;
using userspace_backend.Logging;
using DATA = userspace_backend.Data;
using BE = userspace_backend.Model;

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
        private static ILoggingService? loggingService;

        public static event EventHandler<NotificationEventArgs>? NotificationRequested;
        public static event EventHandler<NotificationEventArgs>? QueuedNotificationRequested;
        public static event EventHandler<ModalEventArgs>? QueuedModalRequested;

        public static void Initialize(ILoggingService? logger)
        {
            loggingService = logger;
        }

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
            loggingService?.LogInformation(LogSource.Toast, "Queuing notification: MessageKey={MessageKey}, Type={Type}, FormatArgs={FormatArgs}", 
                messageKey, type, formatArgs.Length > 0 ? string.Join(", ", formatArgs) : "none");

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
        private readonly ILoggingService? loggingService;

        public BackEnd(IBackEndLoader backEndLoader) : this(backEndLoader, null, null)
        {
        }

        public BackEnd(IBackEndLoader backEndLoader, IDeviceInfoProvider? deviceInfoProvider) : this(backEndLoader, deviceInfoProvider, null)
        {
        }

        public BackEnd(IBackEndLoader backEndLoader, IDeviceInfoProvider? deviceInfoProvider, ILoggingService? loggingService)
        {
            BackEndLoader = backEndLoader;
            this.deviceInfoProvider = deviceInfoProvider;
            this.loggingService = loggingService;
            
            // Initialize static logging for models
            ProfileModel.InitializeLogging(loggingService);
            EditableSettingLogging.Initialize(loggingService);
            NotificationManager.Initialize(loggingService);
            
            Devices = new DevicesModel(deviceInfoProvider);
            Profiles = new ProfilesModel([]);
            Settings = new DATA.Settings();
            Hardware = new HardwareManager(Devices);

            loggingService?.LogInformation(LogSource.Backend, "BackEnd initialized");
        }

        public DevicesModel Devices { get; set; }

        public MappingsModel Mappings { get; set; } = null!;

        public ProfilesModel Profiles { get; set; }

        public DATA.Settings Settings { get; set; }

        public HardwareManager Hardware { get; set; }

        public DeviceModel? UnconfiguredActiveDevice { get; set; }

        public ILoggingService? LoggingService => loggingService;

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
            loggingService?.LogInformation(LogSource.Backend, "Starting backend data load");

            try
            {
                IEnumerable<DATA.Device> devicesData = BackEndLoader.LoadDevices();
                LoadDevicesFromData(devicesData);
                loggingService?.LogInformation(LogSource.Backend, "Loaded {DeviceCount} devices", devicesData.Count());

                IEnumerable<DATA.Profile> profilesData = BackEndLoader.LoadProfiles();
                LoadProfilesFromData(profilesData);
                loggingService?.LogInformation(LogSource.Backend, "Loaded {ProfileCount} profiles", profilesData.Count());

                DATA.MappingSet mappingData = BackEndLoader.LoadMappings();
                Mappings = new MappingsModel(mappingData, Devices.DeviceGroups, Profiles);
                loggingService?.LogInformation(LogSource.Backend, "Loaded {MappingCount} mappings", mappingData.Mappings?.Length ?? 0);

                Settings = BackEndLoader.LoadSettings() ?? new DATA.Settings();
                loggingService?.LogInformation(LogSource.Backend, "Settings loaded successfully");

                loggingService?.LogInformation(LogSource.Backend, "Backend data load completed");
            }
            catch (Exception ex)
            {
                loggingService?.LogError(LogSource.Backend, ex, "Failed to load backend data");
                throw;
            }
        }

        protected void LoadDevicesFromData(IEnumerable<DATA.Device> devicesData)
        {
            foreach(var deviceData in devicesData)
            {
                if (Devices.TryAddDevice(deviceData))
                {
                    loggingService?.LogDebug(LogSource.Backend, "Added device: {DeviceName} ({HWID})", deviceData.Name, deviceData.HWID);
                }
                else
                {
                    loggingService?.LogWarning(LogSource.Backend, "Failed to add device: {DeviceName} ({HWID})", deviceData.Name, deviceData.HWID);
                }
            }
        }

        protected void LoadProfilesFromData(IEnumerable<DATA.Profile> profileData)
        {
            foreach (var profile in profileData)
            {
                if (Profiles.TryAddProfile(profile))
                {
                    loggingService?.LogDebug(LogSource.Backend, "Added profile: {ProfileName}", profile.Name);
                }
                else
                {
                    loggingService?.LogWarning(LogSource.Backend, "Failed to add profile: {ProfileName}", profile.Name);
                }
            }
        }

        public void ValidateDevicesAfterUIReady()
        {
            loggingService?.LogInformation(LogSource.Hardware, "Validating devices after UI ready");
            
            ValidateDevicesAvailability();
            
            var activeDevice = Hardware.ActiveDevice;
            
            if (activeDevice != null)
            {
                loggingService?.LogInformation(LogSource.Hardware, "Active device detected: {DeviceName}", activeDevice.Name.CurrentValidatedValue);
                bool isConfigured = Devices.Devices.Contains(activeDevice);
                
                if (!isConfigured)
                {
                    UnconfiguredActiveDevice = activeDevice;
                    loggingService?.LogWarning(LogSource.Hardware, "Active device is not configured: {DeviceName}", activeDevice.Name.CurrentValidatedValue);
                    NotificationManager.QueueModal("UnconfiguredDevice", activeDevice.Name.CurrentValidatedValue);
                }
                else
                {
                    loggingService?.LogInformation(LogSource.Hardware, "Active device is properly configured: {DeviceName}", activeDevice.Name.CurrentValidatedValue);
                }
            }
            else
            {
                loggingService?.LogInformation(LogSource.Hardware, "No active device detected");
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

        public void ApplyUserSettingsOnly()
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
            loggingService?.LogInformation(LogSource.Backend, "Starting WriteToDriver for mapping: {MappingName}", mappingToApply?.Name?.ModelValue ?? "Unknown");
            
            if (mappingToApply?.IndividualMappings?.Count > 0)
            {
                loggingService?.LogInformation(LogSource.Backend, "Mapping contains {MappingCount} individual mappings", mappingToApply.IndividualMappings.Count);
                foreach (var individualMapping in mappingToApply.IndividualMappings)
                {
                    loggingService?.LogInformation(LogSource.Backend, "  - DeviceGroup: {DeviceGroup}, Profile: {ProfileName}", 
                        individualMapping.DeviceGroup?.DisplayName ?? "Unknown", 
                        individualMapping.Profile?.Name?.ModelValue ?? "Unknown");
                }
            }
            else
            {
                loggingService?.LogWarning(LogSource.Backend, "Mapping has no individual mappings");
            }
            
            var validationResult = ValidateMappingAndSeparateDevices(mappingToApply);
            
            loggingService?.LogInformation(LogSource.Backend, "Validation result: {ValidCount} valid mappings, {InvalidCount} invalid mappings", 
                validationResult.ValidMappings.Count, validationResult.InvalidMappings.Count);
            
            foreach (var invalidMapping in validationResult.InvalidMappings)
            {
                loggingService?.LogWarning(LogSource.Backend, "Invalid mapping: {DeviceGroup} - {Error}", 
                    invalidMapping.mapping.DeviceGroup?.DisplayName ?? "Unknown", invalidMapping.error);
            }
            
            if (!validationResult.HasValidMappings)
            {
                loggingService?.LogError(LogSource.Backend, "No valid devices found for mapping - aborting driver write");
                NotificationManager.QueueNotification("NoValidDevicesForMapping", NotificationType.Error);
                return false;
            }
            
            DriverConfig config = MapToDriverConfig(validationResult.ValidMappings);
            loggingService?.LogInformation(LogSource.Backend, "Created driver config with {DeviceCount} devices and {ProfileCount} profiles", 
                config.devices?.Count ?? 0, config.profiles?.Count ?? 0);
            
            if (config.devices != null)
            {
                foreach (var device in config.devices)
                {
                    loggingService?.LogInformation(LogSource.Backend, "Driver device: {DeviceName} (ID: {DeviceId}, Profile: {ProfileName}, Disabled: {Disabled})", 
                        device.name, device.id, device.profile, device.config.disable);
                }
            }
            
            try
            {
                loggingService?.LogInformation(LogSource.Backend, "Attempting to activate driver config");
                config.Activate();
                loggingService?.LogInformation(LogSource.Backend, "Driver config activated successfully");
                
                // Delay success notifications by 1 second
                Task.Delay(1000).ContinueWith(_ =>
                {
                    foreach (var validMapping in validationResult.ValidMappings)
                    {
                        var devicesInGroup = Devices.Devices.Where(d => d.DeviceGroup.Equals(validMapping.DeviceGroup));
                        foreach (var device in devicesInGroup.Where(d => !d.Ignore.ModelValue))
                        {
                            loggingService?.LogInformation(LogSource.Backend, "Settings applied successfully to device: {DeviceName} ({DeviceId})", 
                                device.Name.ModelValue, device.HardwareID.ModelValue);
                            NotificationManager.QueueNotification("DeviceSettingsAppliedSuccessfully", NotificationType.Success, device.Name.ModelValue);
                        }
                    }
                });
                
                return true;
            }
            catch (Exception ex)
            {
                loggingService?.LogError(LogSource.Backend, ex, "Failed to activate driver config");
                NotificationManager.QueueNotification("DriverActivationFailed", NotificationType.Error, ex.Message);
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

        protected DriverConfig MapToDriverConfig(IEnumerable<BE.MappingGroup> validMappings)
        {
            IEnumerable<DeviceSettings> configDevices = MapToDriverDevices(validMappings);
            IEnumerable<Profile> configProfiles = MapToDriverProfiles(validMappings);

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

        protected IEnumerable<DeviceSettings> MapToDriverDevices(IEnumerable<BE.MappingGroup> validMappings)
        {
            return validMappings.SelectMany(
                dg => MapToDriverDevices(dg.DeviceGroup, dg.Profile.Name.ModelValue));
        }

        protected IEnumerable<Profile> MapToDriverProfiles(MappingModel mapping)
        {
            IEnumerable<ProfileModel> ProfilesToMap = mapping.IndividualMappings.Select(m => m.Profile).Distinct();
            return ProfilesToMap.Select(p => p.CurrentValidatedDriverProfile);
        }

        protected IEnumerable<Profile> MapToDriverProfiles(IEnumerable<BE.MappingGroup> validMappings)
        {
            IEnumerable<ProfileModel> ProfilesToMap = validMappings.Select(m => m.Profile).Distinct();
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

        protected class MappingValidationResult
        {
            public List<BE.MappingGroup> ValidMappings { get; set; } = new List<BE.MappingGroup>();
            public List<(BE.MappingGroup mapping, string error)> InvalidMappings { get; set; } = new List<(BE.MappingGroup, string)>();
            public bool HasValidMappings => ValidMappings.Count > 0;
            public bool HasInvalidMappings => InvalidMappings.Count > 0;
        }

        protected MappingValidationResult ValidateMappingAndSeparateDevices(MappingModel mapping)
        {
            loggingService?.LogInformation(LogSource.Backend, "Starting device validation for mapping: {MappingName}", 
                mapping?.Name?.ModelValue ?? "Unknown");
            
            var result = new MappingValidationResult();
            var systemDevices = MultiHandleDevice.GetList();
            var systemDeviceIds = systemDevices.Select(d => d.id.ToUpperInvariant()).ToHashSet();
            
            loggingService?.LogInformation(LogSource.Backend, "Found {SystemDeviceCount} system devices: [{SystemDevices}]", 
                systemDevices.Count(), string.Join(", ", systemDevices.Select(d => $"{d.name}({d.id})")));

            foreach (var individualMapping in mapping.IndividualMappings)
            {
                loggingService?.LogInformation(LogSource.Backend, "Validating mapping: DeviceGroup={DeviceGroup}, Profile={ProfileName}", 
                    individualMapping.DeviceGroup?.DisplayName ?? "Unknown", 
                    individualMapping.Profile?.Name?.ModelValue ?? "Unknown");
                
                if (!Profiles.TryGetProfile(individualMapping.Profile.Name.ModelValue, out _))
                {
                    loggingService?.LogError(LogSource.Backend, "Profile not found: {ProfileName}", individualMapping.Profile.Name.ModelValue);
                    NotificationManager.QueueNotification("ProfileNotFound", NotificationType.Error, individualMapping.Profile.Name.ModelValue);
                    result.InvalidMappings.Add((individualMapping, $"Profile not found: {individualMapping.Profile.Name.ModelValue}"));
                    continue;
                }

                bool mappingIsValid = true;
                var devicesInGroup = Devices.Devices.Where(d => d.DeviceGroup.Equals(individualMapping.DeviceGroup));
                
                loggingService?.LogInformation(LogSource.Backend, "Found {DeviceCount} devices in group {DeviceGroup}", 
                    devicesInGroup.Count(), individualMapping.DeviceGroup?.DisplayName ?? "Unknown");
                
                foreach (var device in devicesInGroup)
                {
                    loggingService?.LogInformation(LogSource.Backend, "Checking device: {DeviceName} (ID: {DeviceId}, Ignored: {Ignored})", 
                        device.Name.ModelValue, device.HardwareID.ModelValue, device.Ignore.ModelValue);
                    
                    if (device.Ignore.ModelValue) 
                    {
                        loggingService?.LogInformation(LogSource.Backend, "Device {DeviceName} is set to ignore - skipping", device.Name.ModelValue);
                        continue;
                    }
                    
                    string deviceHwId = device.HardwareID.ModelValue.ToUpperInvariant();
                    if (!string.IsNullOrEmpty(deviceHwId) && !systemDeviceIds.Contains(deviceHwId))
                    {
                        loggingService?.LogWarning(LogSource.Backend, "Device {DeviceName} (ID: {DeviceId}) not found in system devices", 
                            device.Name.ModelValue, deviceHwId);
                        NotificationManager.QueueNotification("DeviceNotConnected", NotificationType.Error, device.Name.ModelValue, deviceHwId);
                        result.InvalidMappings.Add((individualMapping, $"Device not connected: {device.Name.ModelValue}"));
                        mappingIsValid = false;
                        break;
                    }
                    else
                    {
                        loggingService?.LogInformation(LogSource.Backend, "Device {DeviceName} (ID: {DeviceId}) found in system - valid", 
                            device.Name.ModelValue, deviceHwId);
                    }
                }

                if (mappingIsValid)
                {
                    loggingService?.LogInformation(LogSource.Backend, "Mapping for DeviceGroup {DeviceGroup} is valid", 
                        individualMapping.DeviceGroup?.DisplayName ?? "Unknown");
                    result.ValidMappings.Add(individualMapping);
                }
                else
                {
                    loggingService?.LogWarning(LogSource.Backend, "Mapping for DeviceGroup {DeviceGroup} is invalid", 
                        individualMapping.DeviceGroup?.DisplayName ?? "Unknown");
                }
            }
                
            return result;
        }

        protected bool ValidateMappingBeforeApplying(MappingModel mapping)
        {
            var result = ValidateMappingAndSeparateDevices(mapping);
            return !result.HasInvalidMappings;
        }
    }
}
