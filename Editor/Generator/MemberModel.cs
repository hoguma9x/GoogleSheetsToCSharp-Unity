using Gsheets.Editor.DownLoader;

namespace Gsheets.Editor.Generator
{
    public class GenericMemberModel : MemberModel
    {
        public string Generic1 { get; set; }
        public GenericMemberModel(HeaderType typeData) : base(typeData)
        {
            Generic1 = typeData.genericType.Name;
        }
    }
    
    public class MemberModel
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public HeaderType HeaderType { get; set; }
        public bool IsExternal { get; set; }

        public MemberModel(string name, string type)
        {
            Type = type;
            Name = name;
        }
        
        public MemberModel(HeaderType typeData)
        {
            HeaderType = typeData;
            Type = typeData.typeString;
            Name = typeData.memberName;
        }
    }
}