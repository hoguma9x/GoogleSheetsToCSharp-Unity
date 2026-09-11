using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gsheets.Editor.Utils
{
    public static class ScriptableCreator
    {
		/// <summary> T에 해당하는 ScriptableObject를 path에 생성 한 후 반환합니다</summary>
		public static T Create<T>(string path) where T: ScriptableObject
		{
			var result = ScriptableObject.CreateInstance<T>();
			var dir = Path.GetDirectoryName(path);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
				AssetDatabase.Refresh();
			}
			AssetDatabase.CreateAsset(result,path);
			return result;
		}

    }
}