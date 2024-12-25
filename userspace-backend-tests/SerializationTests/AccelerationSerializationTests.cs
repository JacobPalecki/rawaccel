﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.IO;
using userspace_backend.IO.Serialization;
using static userspace_backend.Data.Profiles.Accel.LookupTableAccel;

namespace userspace_backend_tests.SerializationTests
{
    [TestClass]
    public class AccelerationSerializationTests
    {
        protected class AccelerationOnlyObject
        {
            [JsonConverter(typeof(AccelerationJsonConverter))]
            public Acceleration Acceleration { get; set; }
        }

        [TestMethod]
        public void DeserializeNoAccel()
        {
            string textToDeserialize = """
                {
                    "Acceleration": {
                        "Type": "None"
                    }
                }
                """;

            var deserializedText = JsonSerializer.Deserialize<AccelerationOnlyObject>(textToDeserialize);
            Assert.AreEqual(Acceleration.AccelerationDefinitionType.None, deserializedText.Acceleration.Type);
        }

        [TestMethod]
        public void DeserializeFormulaClassicAccel()
        {
            string textToDeserialize = """
                {
                    "Acceleration": {
                        "Type": "Formula/Classic",
                        "Gain": true,
                        "Acceleration": 0.001,
                        "Exponent": 2
                    }
                }
                """;

            var deserializedText = JsonSerializer.Deserialize<AccelerationOnlyObject>(textToDeserialize);
            Assert.AreEqual(Acceleration.AccelerationDefinitionType.Formula, deserializedText.Acceleration.Type);
            var actualClassicAccel = deserializedText.Acceleration as ClassicAccel;
            Assert.IsNotNull(actualClassicAccel);
            Assert.AreEqual(0.001, actualClassicAccel.Acceleration);
            Assert.AreEqual(2, actualClassicAccel.Exponent);
        }

        [TestMethod]
        public void DeserializeFormulaLinearAccel()
        {
            string textToDeserialize = """
                {
                    "Acceleration": {
                        "Type": "Formula/Linear",
                        "Gain": true,
                        "Acceleration": 0.001
                    }
                }
                """;

            var deserializedText = JsonSerializer.Deserialize<AccelerationOnlyObject>(textToDeserialize);
            Assert.AreEqual(Acceleration.AccelerationDefinitionType.Formula, deserializedText.Acceleration.Type);
            var actualLinearAccel = deserializedText.Acceleration as LinearAccel;
            Assert.IsNotNull(actualLinearAccel);
            Assert.AreEqual(0.001, actualLinearAccel.Acceleration);
        }

        [TestMethod]
        public void DeserializeLookupTableVelocity()
        {
            string textToDeserialize = """
                {
                    "Acceleration": {
                        "Type": "LookupTable",
                        "ApplyAs": 0,
                        "Data": [
                            1.505035,
                            0.85549892,
                            4.375,
                            3.30972978,
                            13.51,
                            15.17478447,
                            140,
                            354.7026875
                        ]
                    }
                }
                """;

            double[] expectedData = [
                1.505035,
                0.85549892,
                4.375,
                3.30972978,
                13.51,
                15.17478447,
                140,
                354.7026875,
            ];

            var deserializedText = JsonSerializer.Deserialize<AccelerationOnlyObject>(textToDeserialize);
            Assert.AreEqual(Acceleration.AccelerationDefinitionType.LookupTable, deserializedText.Acceleration.Type);
            var actualLookupTableAccel = deserializedText.Acceleration as LookupTableAccel;
            Assert.IsNotNull(actualLookupTableAccel);
            Assert.AreEqual(LookupTableType.Velocity, actualLookupTableAccel.ApplyAs);
            CollectionAssert.AreEqual(expectedData, actualLookupTableAccel.Data, StructuralComparisons.StructuralComparer);
        }

        [TestMethod]
        public void SerializeLookupTableVelocity()
        {
            LookupTableAccel toDeserialize = new LookupTableAccel()
            {
                Data = [
                    1.505035,
                    0.85549892,
                    4.375,
                    3.30972978,
                    13.51,
                    15.17478447,
                    140,
                    354.7026875],
                ApplyAs = LookupTableType.Velocity,

            };

            string expectedText = """
                {
                  "Type": "LookupTable",
                  "ApplyAs": "Velocity",
                  "Data": [
                    1.505035,
                    0.85549892,
                    4.375,
                    3.30972978,
                    13.51,
                    15.17478447,
                    140,
                    354.7026875
                  ]
                }
                """;

            var serializedText = JsonSerializer.Serialize(toDeserialize, ProfileReaderWriter.JsonOptions);

            Assert.AreEqual(expectedText, serializedText);
        }
    }
}
