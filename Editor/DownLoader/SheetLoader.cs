using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gsheets.IO;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Gsheets.Editor.DownLoader
{
    internal class SheetLoader
    {
        public static async Task<SheetRawData> Load(string sheetID, GSheetInfo sheetinfo)
        {
            // 1. 요청할 URL 생성
            //string targetUrl =  $"https://docs.google.com/spreadsheets/d/{sheetID}/export?format=csv&sheet={sheetName}";
            string targetUrl = $"https://docs.google.com/spreadsheets/d/{sheetID}/export?format=csv&gid={sheetinfo.Gid}";
            // 2. UnityWebRequest를 사용한 비동기 요청
            using (UnityWebRequest webRequest = UnityWebRequest.Get(targetUrl))
            {
                await webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"GoogleSheet Load Error: {webRequest.error}");
                    return new SheetRawData(sheetinfo, "");
                }
                else
                {
                    string csvData = webRequest.downloadHandler.text;
                    return new SheetRawData(sheetinfo, csvData);
                }
            }
        }
        
        /// <summary>
        /// 구글 스프레드시트안에 포함된 모든 시트를 가져옵니다.
        /// </summary>
        /// <param name="sheetID"></param>
        /// <returns></returns>
        internal static async Task<List<GSheetInfo>> GetSheetNames(string sheetID)
        {
            List<GSheetInfo> result = new();
            string accessUrl = $"https://docs.google.com/spreadsheets/d/{sheetID}/edit";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(accessUrl))
            {
                webRequest.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                await webRequest.SendWebRequest();
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed GetSheetNames : {webRequest.error}");
                    return result;
                }
                string htmlContent = webRequest.downloadHandler.text;
                for (int i = 0; i < 100; i++)
                {
                    int startIdx = htmlContent.LastIndexOf($"\"[{i},0,\\\"", StringComparison.Ordinal);
                    if(startIdx < 0)
                        break;
                    var block = htmlContent.Substring(startIdx, 100);
                    // 1. 첫 번째 값 (24006901) 추출을 위한 패턴
                    string patternId = @",0,\\""([^""]+)";
                    // 2. 두 번째 값 (ExampleSturct) 추출을 위한 패턴
                    string patternName = @"\[\[0,0,\\""([^""]+)";
                    // 매칭 실행
                    Match matchId = Regex.Match(block, patternId);
                    Match matchName = Regex.Match(block, patternName);
                    result.Add(new GSheetInfo(matchName.Groups[1].Value.Replace("\\", ""),
                        matchId.Groups[1].Value.Replace("\\", "")));
                }
            }
            return result;
        }
    }
}