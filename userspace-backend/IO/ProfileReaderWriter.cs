﻿using System.Text.Json;
using System.Text.Json.Serialization;
using DATA = userspace_backend.Data;

namespace userspace_backend.IO
{
    public class ProfileReaderWriter : ReaderWriterBase<DATA.Profile>
    {
        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(),
            }
        };

        protected override string FileType => "Profile";

        public override DATA.Profile Deserialize(string toRead)
        {
            return JsonSerializer.Deserialize<DATA.Profile>(toRead);
        }

        public override string Serialize(DATA.Profile toWrite)
        {
            return JsonSerializer.Serialize(toWrite, JsonOptions);
        }
    }
}
