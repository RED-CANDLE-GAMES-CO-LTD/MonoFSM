using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 「讀這個屬性會不會炸掉 Editor」的地雷圖。
    ///
    /// 為什麼需要：Unity component 上有些 public property 的 getter 呼叫下去會在 **native 層**
    /// abort 或把 stack 爆掉（Editor.log 只留 mono stack dump），managed `try/catch` 攔不到，
    /// 整個 Editor 直接閃退。所以「掃全部屬性」這件事不可能靠接 exception 變安全。
    ///
    /// 做法是麵包屑：讀之前把 `型別.屬性名` **同步** 寫進 Library 下的檔案，讀完清掉。
    /// 真的炸了，那一行會留在檔案裡活過 crash —— 下次進來就知道元凶是誰，自動列入永久黑名單，
    /// 以後跳過。工具自己把地雷掃出來，不用人去猜。
    /// </summary>
    internal static class ProbeMineField
    {
        private static readonly string Dir = Path.Combine("Library", "MonoFSM");
        private static readonly string BreadcrumbPath = Path.Combine(Dir, "probe_breadcrumb.txt");
        private static readonly string BlacklistPath = Path.Combine(Dir, "probe_blacklist.txt");

        private static HashSet<string> _blacklist;

        /// <summary>上一次執行留下的麵包屑（= 那次讀到一半閃退的屬性）。回收後就從檔案移除。</summary>
        private static string TakeStaleBreadcrumb()
        {
            try
            {
                if (!File.Exists(BreadcrumbPath)) return null;
                var stale = File.ReadAllText(BreadcrumbPath).Trim();
                File.Delete(BreadcrumbPath);
                return string.IsNullOrEmpty(stale) ? null : stale;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 每次要開始一輪 dump 前叫一次：把上次閃退殘留的屬性收進黑名單。
        /// 回傳給呼叫端印出來的說明（沒殘留就回 null）。
        /// </summary>
        public static string HarvestCrashReport()
        {
            var stale = TakeStaleBreadcrumb();
            if (stale == null) return null;
            Blacklist().Add(stale);
            Save();
            return $"# 上次 dump 在讀 {stale} 時閃退了 —— 已永久列入黑名單，以後跳過";
        }

        private static HashSet<string> Blacklist()
        {
            if (_blacklist != null) return _blacklist;
            _blacklist = new HashSet<string>();
            try
            {
                if (File.Exists(BlacklistPath))
                    foreach (var line in File.ReadAllLines(BlacklistPath))
                        if (!string.IsNullOrWhiteSpace(line))
                            _blacklist.Add(line.Trim());
            }
            catch (Exception)
            {
                // 讀不到就當空的，不值得為它中斷 dump
            }

            return _blacklist;
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllLines(BlacklistPath, Blacklist().OrderBy(s => s));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProbeMineField] 黑名單寫入失敗：{e.Message}");
            }
        }

        private static string Key(PropertyInfo p) => $"{p.DeclaringType?.FullName}.{p.Name}";

        /// <summary>已知會炸的屬性，或本來就不該碰的（Obsolete）。</summary>
        public static bool IsMine(PropertyInfo p) =>
            p.GetCustomAttribute<ObsoleteAttribute>() != null || Blacklist().Contains(Key(p));

        /// <summary>
        /// 在麵包屑保護下讀一個屬性。native crash 的話那一行會留在檔案裡。
        /// </summary>
        public static object ReadGuarded(PropertyInfo p, object target)
        {
            var key = Key(p);
            try
            {
                Directory.CreateDirectory(Dir);
                // 必須同步落地才擋得住 crash：FileStream + Flush(true)
                using (var fs = new FileStream(BreadcrumbPath, FileMode.Create, FileAccess.Write))
                using (var w = new StreamWriter(fs))
                {
                    w.Write(key);
                    w.Flush();
                    fs.Flush(true);
                }
            }
            catch (Exception)
            {
                // 麵包屑寫不進去就沒有保護，但還是照讀 —— 這是 debug 工具不是關鍵路徑
            }

            try
            {
                return p.GetValue(target);
            }
            catch (Exception e)
            {
                return $"<throw {e.GetType().Name}>";
            }
            finally
            {
                try
                {
                    if (File.Exists(BreadcrumbPath)) File.Delete(BreadcrumbPath);
                }
                catch (Exception)
                {
                    // 清不掉最多下次誤報一次，可接受
                }
            }
        }
    }
}
