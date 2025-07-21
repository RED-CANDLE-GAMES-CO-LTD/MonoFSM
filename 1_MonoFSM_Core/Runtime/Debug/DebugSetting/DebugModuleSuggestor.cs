using System.Collections.Generic;

// using QFSW.QC;
//FIXME: 要有Quantom Console的才能用
namespace MonoDebugSetting
{
    // public struct DebugModuleTag : IQcSuggestorTag
    // {
    // }
    //
    //
    // public sealed class DebugModuleAttribute : SuggestorTagAttribute
    // {
    //     private DebugModuleTag _tag;
    //
    //     public override IQcSuggestorTag[] GetSuggestorTags()
    //     {
    //         return new IQcSuggestorTag[] { _tag };
    //     }
    // }
    //
    // public class DebugModuleSuggestor : BasicCachedQcSuggestor<string>
    // {
    //     protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
    //     {
    //         return context.HasTag<DebugModuleTag>();
    //     }
    //
    //     protected override IQcSuggestion ItemToSuggestion(string dirName)
    //     {
    //         return new RawSuggestion(dirName, true);
    //     }
    //
    //     protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
    //     {
    //         return DebugSetting.DebugModuleNames;
    //     }
    // }
}