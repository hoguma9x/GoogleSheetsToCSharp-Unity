using System;
using System.Collections.Generic;
using System.Linq;
using Gsheets.Editor.Generator;
using UnityEditor;
using UnityEngine;

namespace Gsheets.Parsing
{
    [InitializeOnLoad]
    public static class ParserValidationChecker
    {
        static ParserValidationChecker()
        {
            string innerType = typeof(Format_String).Namespace;
            bool hasCheck(Type type)
            {
                if (typeof(IGSheetParser).IsAssignableFrom(type) || type.Namespace.Contains(innerType))
                    return false;
                return true;
            }
            HashSet<string> errorTypes = new();
            var interfaces = TypeFinder.GetAssignableFroms<IParserFormatter>();
            var attributes = TypeFinder.GetAssignableFromAttributes<ParserTriggerAttribute>();
            
            
            foreach (var type in interfaces)
            {
                if(!hasCheck(type))
                    continue;
                if (type.GetCustomAttributes(typeof(ParserTriggerAttribute), false).Length == 0)
                    errorTypes.Add(type.Name);
            }
            foreach (var atrr in attributes)
            {
                if(!hasCheck(atrr))
                    continue;
                if (!typeof(IParserFormatter).IsAssignableFrom(atrr))
                    errorTypes.Add(atrr.Name);
            }
            foreach (var error in errorTypes)
                Debug.LogError($"[GsheetError:Type:{error}] Both ParserTriggerAttribute and IParserFormatter " +
                               $"support custom formats only for types that have been explicitly declared.");
        }
    }
}