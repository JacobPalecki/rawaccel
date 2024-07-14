﻿using System;

namespace userspace_backend.Data
{
    public class Device
    {
        public string Name { get; set; }

        public string HWID { get; set; }

        public int DPI { get; set; }

        public int PollingRate { get; set; }

        public string MappingGroup { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Device device &&
                   Name == device.Name &&
                   HWID == device.HWID &&
                   DPI == device.DPI &&
                   PollingRate == device.PollingRate &&
                   MappingGroup == device.MappingGroup;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, HWID, DPI, PollingRate, MappingGroup);
        }
    }
}