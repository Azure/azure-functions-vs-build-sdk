using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MakeFunctionJson
{
    internal static class ParameterInfoExtensions
    {
        private static readonly HashSet<string> _supportedAttributes = new HashSet<string>
        {
            // SDK
            // "StorageAccountAttribute",
            "BlobAttribute",
            "BlobTriggerAttribute",
            "QueueAttribute",
            "QueueTriggerAttribute",
            "TableAttribute",

            "EventHubAttribute",
            "EventHubTriggerAttribute",

            "TimerTriggerAttribute",
            "DocumentDBAttribute",
            "ApiHubTableAttribute",
            "MobileTableAttribute",
            "ServiceBusTriggerAttribute",
            "ServiceBusAttribute",
            "TwilioSmsAttribute",
            "NotificationHubAttribute"
        };

        public static bool IsWebJobsSdkParameter(this ParameterInfo parameterInfo)
        {
            return parameterInfo
                .GetCustomAttributes()
                .Any(a => _supportedAttributes.Contains(a.GetType().Name));
        }

        public static IEnumerable<JObject> ToFunctionJsonBindings(this ParameterInfo parameterInfo)
        {
            return parameterInfo
                .GetCustomAttributes()
                .Where(a => _supportedAttributes.Contains(a.GetType().Name))
                .Select(AttributeToJObject)
                .Select(o =>
                {
                    o["name"] = parameterInfo.Name;
                    return o;
                });
        }

        private static JObject AttributeToJObject(Attribute attribute)
        {
            var obj = new JObject
            {
                ["type"] = attribute.ToAttributeFriendlyName()
            };

            var direction = Direction.@out;
            if (obj["type"].ToString().IndexOf("Trigger") > 0)
            {
                direction = Direction.@in;
            }

            foreach (var property in attribute
                                    .GetType()
                                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(p => p.CanRead && p.PropertyType != typeof(System.Object)))
            {
                var propertyValue = property.GetValue(attribute);

                if (propertyValue == null || (propertyValue is int && (int)propertyValue == 0))
                {
                    continue;
                }

                var propertyType = property.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = propertyType.GetGenericArguments().First();
                }

                if (propertyType == typeof(FileAccess))
                {
                    Direction convert(FileAccess value)
                    {
                        if (value == FileAccess.Read)
                        {
                            return Direction.@in;
                        }
                        else if (value == FileAccess.Write)
                        {
                            return Direction.@out;
                        }
                        else
                        {
                            return Direction.inout;
                        }
                    }
                    direction = convert((FileAccess)propertyValue);
                    continue;
                }

                var propertyName = NormalizePropertyName(attribute.GetType().Name, property);
                obj[propertyName] = JToken.FromObject(propertyValue);
            }

            obj["direction"] = direction.ToString();
            return obj;
        }

        private static string NormalizePropertyName(string attrName, PropertyInfo property)
        {
            var propertyName = property.Name;

            if ((attrName == "BlobAttribute") || (attrName == "BlobTriggerAttribute"))
            {
                if (propertyName == "BlobPath")
                {
                    return "path";
                }
            }
            else if (attrName == "MobileTableAttribute")
            {
                if (propertyName == "MobileAppUriSetting")
                {
                    return "connection";
                }
                else if (propertyName == "ApiKeySetting")
                {
                    return "apiKey";
                }
            }
            else if (attrName == "NotificationHubAttribute")
            {
                if (propertyName == "ConnectionStringSetting")
                {
                    return "connection";
                }
            }
            else if (attrName == "ServiceBusAttribute")
            {
                if (propertyName == "QueueOrTopicName")
                {
                    return "queue";
                }
            }
            else if (attrName == "TwilioSmsAttribute")
            {
                if (propertyName == "AccountSidSetting")
                {
                    return "accountSid";
                }
                else if (propertyName == "AuthTokenSetting")
                {
                    return "authToken";
                }
            }

            return Char.ToLowerInvariant(propertyName.First()) + propertyName.Substring(1);
        }
    }
}
