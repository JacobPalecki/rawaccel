using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace userspace_backend.Data
{
    public class Device
    {
        public string Name { get; set; }

        public string HWID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ProductString { get; set; } = string.Empty;

        public int DPI { get; set; }

        public int PollingRate { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Ignore { get; set; }

        public string DeviceGroup { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Device device &&
                   Name == device.Name &&
                   HWID == device.HWID &&
                   ProductString == device.ProductString &&
                   DPI == device.DPI &&
                   PollingRate == device.PollingRate &&
                   Ignore == device.Ignore &&
                   DeviceGroup == device.DeviceGroup;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, HWID, ProductString, DPI, PollingRate, DeviceGroup);
        }
    }
}
