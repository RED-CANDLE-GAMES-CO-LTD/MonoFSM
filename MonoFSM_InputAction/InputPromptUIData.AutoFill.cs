#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGInputAction
{
    //從 prompt 這一端一鍵補齊：拿自己 action 的 binding path，去每個 family 的 config 補 entry / sprite 名稱 / icon。
    //跟 DeviceIconMapConfig 那顆按鈕是同一套邏輯（FillEntriesFor），只是範圍縮到「這一則提示」，
    //新做一個 prompt 時不用再跑去五份 config 各按一次。
    public partial class InputPromptUIData
    {
        [PropertyOrder(99)]
        [BoxGroup("Editor Preview")]
        [Button("補齊各機種的 icon 對照（只補這一則用到的 binding）", ButtonSizes.Medium)]
        private void FillAllFamilies()
        {
            if (ResolveFinder() is not PromptIconRegistry registry)
            {
                Debug.LogError(
                    "[InputPromptUIData] 找不到 PromptIconRegistry，無法知道每個機種要填哪份 config",
                    this);
                return;
            }

            var usages = CollectOwnBindingUsages();
            if (usages.Count == 0)
            {
                Debug.LogWarning(
                    "[InputPromptUIData] 這則提示的 action 沒有可用的 binding path（沒設 action 或只有 composite 本體）",
                    this);
                return;
            }

            var lines = new List<string>();
            foreach (var familyEntry in registry._entries)
            {
                if (familyEntry._config == null)
                    continue;

                var report = familyEntry._config.FillEntriesFor(familyEntry._family, usages);
                var line =
                    $"  {familyEntry._family}（{familyEntry._config.name}）：新增 {report._added}、" +
                    $"sprite 名稱 {report._filledSpriteName}、icon {report._filledIcon}";
                if (report._unmatched.Count > 0)
                    line += $"、要人工補 {report._unmatched.Count}：{string.Join(" / ", report._unmatched)}";
                lines.Add(line);
            }

            PromptIconMapEditorUtility.InvalidateCache(); //剛寫進去的資料要讓預覽/下拉選單看得到
            Debug.Log($"[InputPromptUIData] {name} 補齊結果：\n{string.Join("\n", lines)}", this);
        }

        //只取自己這則提示用到的 binding（格式沿用 utility 的 BindingUsage，好餵給 config 的補表邏輯）
        private List<PromptIconMapEditorUtility.BindingUsage> CollectOwnBindingUsages()
        {
            var result = new List<PromptIconMapEditorUtility.BindingUsage>();
            var action = _input != null && _input._inputAction != null
                ? _input._inputAction.action
                : null;
            if (action == null)
                return result;

            var seen = new HashSet<string>();
            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding.isComposite) //composite 本體沒有 path，路徑在 part 上（跟 runtime 查表一致）
                    continue;

                var path = binding.effectivePath;
                if (string.IsNullOrEmpty(path) || !seen.Add(path))
                    continue;

                result.Add(new PromptIconMapEditorUtility.BindingUsage
                {
                    _path = path,
                    _layout = PromptIconMapEditorUtility.ExtractLayout(path),
                    _actionName = action.name,
                    _promptName = name,
                });
            }

            return result;
        }
    }
}
#endif
