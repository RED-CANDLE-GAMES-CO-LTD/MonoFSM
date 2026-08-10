#if UNITY_EDITOR
using System;
using UnityEngine;

namespace CommandPalette
{
    /// <summary>
    /// 把搜尋結果包成 unity link（http://localhost:8888/webhook?...），
    /// 貼到筆記或聊天室，點擊時由 WebhookServerListener 收下並在 Unity 這邊執行。
    /// </summary>
    public static class CommandPaletteLinkHelper
    {
        //對應 MonoFSM-Pro 的 AssetLinkGenerator.localhostURL，跨 asmdef 拿不到所以這裡重寫一份
        private const string WebhookUrl = "http://localhost:8888/webhook?";

        public static string BuildMenuLink(string menuPath)
        {
            return WebhookUrl + "menu=" + Uri.EscapeDataString(menuPath);
        }

        public static string BuildAssetLink(string assetGuid)
        {
            return WebhookUrl + "asset_guid=" + assetGuid;
        }

        public static string ToMarkdown(string label, string url)
        {
            return "[" + label + "](" + url + ")";
        }

        public static void CopyToClipboard(string label, string url)
        {
            GUIUtility.systemCopyBuffer = ToMarkdown(label, url);
            Debug.Log("[CommandPalette] 已複製連結：" + GUIUtility.systemCopyBuffer);
        }
    }
}
#endif
