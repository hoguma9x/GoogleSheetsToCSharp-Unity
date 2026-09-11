using System;
using System.Collections.Generic;
using Scriban;

namespace Gsheets.Editor.Utils
{
    public class EnumCreator
    {
        const string templateText = @"namespace {{ namespace_name }}
 {
     public enum {{ enum_name }}
     {
         {{~ for item in items ~}}
         {{ item.name }}{{ if item.has_value }} = {{ item.value }}{{ end }},
         {{~ end ~}}
     }
 }";
        private string _enumName;
        private string _namespaceName;
        private string _csPath;
        private List<(string, int)> _enums = new List<(string, int)>();

        public EnumCreator(string enumName, string csPath, string nameSpace)
        {
            this._csPath = csPath;
            this._enumName = enumName;
            this._namespaceName = nameSpace;
        }

        public void AddEnum(string enumName, int enumValue = -1)
        {
            _enums.Add((enumName, enumValue));
        }

        public void Generator()
        {
            var template = Template.Parse(templateText);
            var model = new
            {
                NamespaceName = _namespaceName,
                EnumName = _enumName,
                Items = _enums.ConvertAll(i => new
                {
                    Name = i.Item1,
                    HasValue = i.Item2 != -1,
                    Value = i.Item2
                })
            };
            
            var codes =  template.Render(model);
            var filePath = _csPath + $"{_enumName}.cs";
            System.IO.File.Delete(filePath);
            System.IO.File.WriteAllText(filePath, codes);
        }
    }
}





