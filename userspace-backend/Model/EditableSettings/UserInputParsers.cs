﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.AccelDefinitions;

namespace userspace_backend.Model.EditableSettings
{
    public static class UserInputParsers
    {
        public static StringParser StringParser = new StringParser();
        public static IntParser IntParser = new IntParser();
        public static DoubleParser DoubleParser = new DoubleParser();
        public static BoolParser BoolParser = new BoolParser();
        public static AccelerationDefinitionTypeParser AccelerationDefinitionTypeParser = new AccelerationDefinitionTypeParser();
        public static LookupTableTypeParser LookupTableTypeParser = new LookupTableTypeParser();
        public static LookupTableDataParser LookupTableDataParser = new LookupTableDataParser();
        public static AccelerationFormulaTypeParser AccelerationFormulaTypeParser = new AccelerationFormulaTypeParser();
    }

    public interface IUserInputParser<T>
    {
        bool TryParse(string input, out T parsedValue);
    }

    public class StringParser : IUserInputParser<string>
    {
        public bool TryParse(string input, out string parsedValue)
        {
            parsedValue = input;
            return true;
        }
    }

    public class IntParser : IUserInputParser<int>
    {
        public bool TryParse(string input, out int parsedValue)
        {
            if (int.TryParse(input, out int innerParsedValue))
            {
                parsedValue = innerParsedValue;
                return true;
            }

            parsedValue = default;
            return false;
        }
    }

    public class DoubleParser : IUserInputParser<double>
    {
        public bool TryParse(string input, out double parsedValue)
        {
            if (double.TryParse(input, out parsedValue))
            {
                return true;
            }

            parsedValue = default;
            return false;
        }
    }

    public class BoolParser : IUserInputParser<bool>
    {
        public bool TryParse(string input, out bool parsedValue)
        {
            if (bool.TryParse(input, out parsedValue))
            {
                return true;
            }

            parsedValue = default;
            return false;
        }
    }

    public class AccelerationDefinitionTypeParser : IUserInputParser<Acceleration.AccelerationDefinitionType>
    {
        public bool TryParse(string input, out Acceleration.AccelerationDefinitionType parsedValue)
        {
            if (Enum.TryParse(input, ignoreCase: true, out parsedValue))
            {
                return true;
            }

            parsedValue = default;
            return false;
        }
    }

    public class LookupTableTypeParser : IUserInputParser<LookupTableAccel.LookupTableType>
    {
        public bool TryParse(string input, out LookupTableAccel.LookupTableType parsedValue)
        {
            if (Enum.TryParse(input, ignoreCase: true, out parsedValue))
            {
                return true;
            }

            parsedValue = default;
            return false;
        }
    }

    public class LookupTableDataParser : IUserInputParser<LookupTableData>
    {
        public bool TryParse(string input, out LookupTableData parsedValue)
        {
            IEnumerable<double> ToDoubles(string[] splitInput)
            {
                foreach(string coordinateInput in splitInput)
                {
                    if (double.TryParse(coordinateInput.Trim(), out double result))
                    {
                        yield return result;
                    }
                }
            }

            try
            {
                string[] splitInput = input.Split(',');
                double[] inputCoordinates = ToDoubles(splitInput).ToArray();

                // TODO: further input point validation
                if (inputCoordinates.Length == splitInput.Length &&
                    inputCoordinates.Length % 2 == 0)
                {
                    parsedValue = new LookupTableData(inputCoordinates);
                    return true;
                }
            }
            catch
            {
            }

            parsedValue = new LookupTableData();
            return false;
        }
    }

    public class AccelerationFormulaTypeParser : IUserInputParser<FormulaAccel.AccelerationFormulaType>
    {
        public bool TryParse(string input, out FormulaAccel.AccelerationFormulaType parsedValue)
        {
            if (Enum.TryParse(input, ignoreCase: true, out parsedValue))
            {
                return true;
            }

            parsedValue = default;
            return false;
        }
    }
}
