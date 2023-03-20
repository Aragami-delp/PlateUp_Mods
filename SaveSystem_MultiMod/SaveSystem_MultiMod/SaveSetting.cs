using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace SaveSystem
{
    public class SaveSetting
    {
        [JsonProperty] public string Name { get; private set; }
        [JsonProperty] public string StringValue { get; private set; }
        public SaveSetting(string _name, string _stringValue)
        {
            Name = _name;
            if (_stringValue != null)
                StringValue = _stringValue;
            else
                StringValue = String.Empty;
        }

        public object GetValue(SettingType _type)
        {
            if (!String.IsNullOrWhiteSpace(StringValue))
            {
                switch (_type)
                {
                    case SettingType.stringValue:
                        return StringValue;
                    case SettingType.intValue:
                        {
                            if (int.TryParse(StringValue, out int value))
                            {
                                return value;
                            }
                            throw new SaveSettingParseException("Not an int value");
                        }
                    case SettingType.boolValue:
                        {
                            if (bool.TryParse(StringValue, out bool value))
                            {
                                return value;
                            }
                            throw new SaveSettingParseException("Not a bool value");
                        }
                    default:
                        throw new SaveSettingParseException("No type");
                }
            }
            // Default values
            switch (_type)
            {
                case SettingType.stringValue:
                    return StringValue;
                case SettingType.intValue:
                    return 0;
                case SettingType.boolValue:
                    return false;
                default:
                    throw new SaveSettingParseException("No type");
            }
            // NOTTODO: This method looks like a sorted mess
        }

        public void SetValue(object _value)
        {
            StringValue = _value.ToString();
        }

        public class SaveSettingParseException : Exception
        {
            public SaveSettingParseException(string message) : base(message)
            {
            }
        }

        public enum SettingType
        {
            stringValue = 0,
            intValue = 1,
            boolValue = 2,
        }
    }
}
