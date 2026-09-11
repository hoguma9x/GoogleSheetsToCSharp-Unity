using System;

namespace Gsheets.Editor.Generator
{
    public interface ICustomTypeGeneratorable
    {
        Type GetType();
        
    }
}