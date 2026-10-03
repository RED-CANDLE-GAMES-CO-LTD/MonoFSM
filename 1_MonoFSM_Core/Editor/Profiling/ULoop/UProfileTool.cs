using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using io.github.hatayama.UnityCliLoop.ToolContracts;

namespace MonoFSM.Editor.Profiling
{
    /// <summary>
    /// `uloop uprofile` 的參數。CLI flag 是屬性名的 kebab-case（MinMs → --min-ms）。
    /// agent 不直接打 uloop，走 .claude/scripts/uprofile wrapper（它負責 usage、相對路徑、tool 沒註冊時自動 sync）。
    /// </summary>
    public class UProfileSchema : UnityCliLoopToolSchema
    {
        [Description("status | start | stop | top | spikes | frame | gc")]
        public string Command { get; set; } = "status";

        [Description("Frame range: A-B / A / -N (last N). frame 子指令用它當 frame index")]
        public string Frames { get; set; } = "";

        [Description("main / render / thread 名稱子字串")]
        public string Thread { get; set; } = "main";

        [Description("top 的排序：self | total | gc | calls")]
        public string Sort { get; set; } = "self";

        [Description("列幾筆；0 = 子指令預設（top/gc 30，spikes 10）")]
        public int Limit { get; set; } = 0;

        [Description("spikes 門檻 ms")]
        public float Threshold { get; set; } = 33f;

        [Description("frame 子指令的樹深度")]
        public int Depth { get; set; } = 4;

        [Description("frame 子指令剪掉 total 低於這個 ms 的節點")]
        public float MinMs { get; set; } = 0.1f;

        [Description("先 LoadProfile 這個 .data（絕對路徑或相對專案根目錄）")]
        public string File { get; set; } = "";

        [Description("stop 時存檔路徑；空 = Library/uprofile/<時間>.data")]
        public string Save { get; set; } = "";

        [Description("start 前先清 buffer")]
        public bool Clear { get; set; } = false;

        [Description("start 時暫時把 Profiler target 切成 Editor（stop 切回）")]
        public bool Editor { get; set; } = false;

        [Description("start 時開 deep profile（會 domain reload；stop 時關掉）")]
        public bool Deep { get; set; } = false;

        [Description("輸出字數上限")]
        public int MaxChars { get; set; } = 4000;
    }

    /// <summary>uprofile 回傳：Text 就是要印給 agent 看的全部內容。</summary>
    public class UProfileResponse : UnityCliLoopToolResponse
    {
        public string Text { get; set; }

        public UProfileResponse()
        {
        }
    }

    /// <summary>
    /// uloop custom tool：把 Unity Profiler buffer 壓成精簡文字。實際邏輯在 UProfileReader（MonoFSM.Core.Editor），
    /// 這支只做 schema 轉接，好讓沒裝 uloop 的專案也能編過（本 asmdef 有 MONOFSM_ULOOP define constraint）。
    /// </summary>
    [UnityCliLoopTool]
    public class UProfileTool : UnityCliLoopTool<UProfileSchema, UProfileResponse>
    {
        public override string ToolName => "uprofile";

        protected override Task<UProfileResponse> ExecuteAsync(UProfileSchema p, CancellationToken ct)
        {
            var args = new UProfileArgs
            {
                Command = p.Command,
                Frames = p.Frames,
                Thread = p.Thread,
                Sort = p.Sort,
                Limit = p.Limit,
                Threshold = p.Threshold,
                Depth = p.Depth,
                MinMs = p.MinMs,
                File = p.File,
                Save = p.Save,
                Clear = p.Clear,
                Editor = p.Editor,
                Deep = p.Deep,
                MaxChars = p.MaxChars
            };
            var text = UProfileReader.Run(args, out bool ok);
            return Task.FromResult(new UProfileResponse { Text = text, Success = ok });
        }
    }
}
