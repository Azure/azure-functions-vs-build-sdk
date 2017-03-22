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
            // These 2 attributes are not handled currently.
            // They can go either on class, method, or parameter.
            // The code flow now assumes 1:1 mapping of attributes on parameters to function.json binding.
            // "StorageAccountAttribute",
            // "ServiceBusAccountAttribute",

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

        /// <summary>
        /// A parameter is an SDK parameter if it has at lease 1 SDK attribute.
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <returns></returns>
        public static bool IsWebJobsSdkParameter(this ParameterInfo parameterInfo)
        {
            return parameterInfo
                .GetCustomAttributes()
                .Any(a => _supportedAttributes.Contains(a.GetType().Name));
        }

        /// <summary>
        /// Every parameter can be 1 to N bindings.
        /// </summary>
        /// <param name="parameterInfo">Has to be a WebJobSdkParameter <see cref="IsWebJobsSdkParameter(ParameterInfo)"/></param>
        /// <returns></returns>
        public static IEnumerable<JObject> ToFunctionJsonBindings(this ParameterInfo parameterInfo)
        {

            return parameterInfo
                .GetCustomAttributes()
                .Where(a => _supportedAttributes.Contains(a.GetType().Name)) // this has to return at least 1.
                .Select(AttributeToJObject) // Convert the Attribute into a JObject.
                .Select(obj =>
                {
                    // Add a name property on the JObject that refers to the parameter name.
                    obj["name"] = parameterInfo.Name;
                    return obj;
                });
        }

        /// <summary>
        /// For every binding (which is what the returned JObject represents) there are 3 special keys:
        ///     "name" -> that is the parameter name, not set by this function
        ///     "type" -> that is the binding type. This is derived from the Attribute.Name itself. <see cref="AttributeExtensions.ToAttributeFriendlyName(Attribute)"/>
        ///     "direction" -> default is 'out'
        ///                    if the binding is "Trigger", then it's an in
        ///                    if the binding Attribute has a FileAccess property on it, then map it to that.
        /// a side from these 3, all the others are direct serialization of all of the attribute's properties.
        /// The mapping however isn't 1:1 in terms of the naming. Therefore, <see cref="NormalizePropertyName(string, PropertyInfo)"/>
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        private static JObject AttributeToJObject(Attribute attribute)
        {
            var obj = new JObject
            {
                // the friendly name is basically the name without 'Attribute' suffix and lowerCase first Char.
                ["type"] = attribute.ToAttributeFriendlyName()
            };

            // Default value is out
            var direction = Direction.@out;
            if (obj["type"].ToString().IndexOf("Trigger") > 0)
            {
                // if binding.type is trigger, then it's 'in'
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
                    // Don't serialize null properties and int properties for some reason.
                    // the int handling logic was copied from Mike's > "Table.Take is not nullable. So 0 means ignore"
                    continue;
                }

                var propertyType = property.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // Unwrap nullable types to their underlying type.
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }

                // What about other Enums?
                if (propertyType == typeof(FileAccess))
                {
                    // FileAccess on the Attribute dictates the "direction" property.
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

                // Normalize and store the propertyname
                var propertyName = NormalizePropertyName(attribute.GetType().Name, property);
                obj[propertyName] = JToken.FromObject(propertyValue);
            }

            // Serialize the direction
            obj["direction"] = direction.ToString();
            return obj;
        }

        /// <summary>
        /// These exceptions are coming from how the script runtime is reading function.json
        /// See https://github.com/Azure/azure-webjobs-sdk-script/tree/dev/src/WebJobs.Script/Binding
        /// If there are no exceptions for a given property name on a given attribute, then return it's name with a lowerCase first character.
        /// </summary>
        /// <param name="attrName"></param>
        /// <param name="property"></param>
        /// <returns></returns>
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
                    // The attribute has a QueueOrTopicName while function.json has distinct queue and topic.
                    // I just picked queue.
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
