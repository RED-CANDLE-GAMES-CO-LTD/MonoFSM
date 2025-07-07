// Decompiled with JetBrains decompiler
// Type: UnityEditor.WindowLayout
// Assembly: UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: EEF2B0F7-6063-43DA-ABD3-99A4DF22654F
// Assembly location: /Applications/Unity/Hub/Editor/6000.2.0b5/Unity.app/Contents/Managed/UnityEngine/UnityEditor.CoreModule.dll
// XML documentation location: /Applications/Unity/Hub/Editor/6000.2.0b5/Unity.app/Contents/Managed/UnityEngine/UnityEditor.CoreModule.xml

using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Scripting;

#nullable disable
namespace UnityEditor;

[InitializeOnLoad]
internal static class WindowLayout
{
private const string tabsLayoutKey = "tabs";
private const string verticalLayoutKey = "vertical";
private const string horizontalLayoutKey = "horizontal";
private const string k_TopViewClassName = "top_view";
private const string k_CenterViewClassName = "center_view";
private const string k_BottomViewClassName = "bottom_view";
internal const string kMaximizeRestoreFile = "CurrentMaximizeLayout.dwlt";
private const string kDefaultLayoutName = "Default.wlt";

internal static string layoutResourcesPath
{
get => Path.Combine(EditorApplication.applicationContentsPath, "Resources/Layouts");
}

internal static string layoutsPreferencesPath
{
get => FileUtil.CombinePaths(InternalEditorUtility.unityPreferencesFolder, "Layouts");
}

internal static string layoutsModePreferencesPath
{
get => FileUtil.CombinePaths(WindowLayout.layoutsPreferencesPath, ModeService.currentId);
}

internal static string layoutsDefaultModePreferencesPath
{
get => FileUtil.CombinePaths(WindowLayout.layoutsPreferencesPath, "default");
}

internal static string layoutsCurrentModePreferencesPath
{
get => FileUtil.CombinePaths(WindowLayout.layoutsPreferencesPath, "current");
}

internal static string layoutsProjectPath => FileUtil.CombinePaths("UserSettings", "Layouts");

internal static string ProjectLayoutPath
{
get => WindowLayout.GetProjectLayoutPerMode(ModeService.currentId);
}

internal static string currentLayoutName
{
get => WindowLayout.GetLayoutFileName(ModeService.currentId, Application.unityVersionVer);
}

[RequiredByNativeCode]
[UsedImplicitly]
public static void LoadDefaultWindowPreferences()
{
WindowLayout.LoadCurrentModeLayout((bool) (UnityEngine.Object) WindowLayout.FindMainWindow());
ModeService.InitializeCurrentMode();
}

public static void LoadCurrentModeLayout(bool keepMainWindow)
{
WindowLayout.InitializeLayoutPreferencesFolder();
IDictionary dynamicLayout = ModeService.GetDynamicLayout();
if (dynamicLayout == null)
WindowLayout.LoadLastUsedLayoutForCurrentMode(keepMainWindow);
else if (File.Exists(WindowLayout.ProjectLayoutPath) && Convert.ToBoolean(dynamicLayout[(object) "restore_saved_layout"]) || !WindowLayout.LoadModeDynamicLayout(keepMainWindow, dynamicLayout))
WindowLayout.LoadLastUsedLayoutForCurrentMode(keepMainWindow);
}

private static bool LoadModeDynamicLayout(bool keepMainWindow, IDictionary layoutData)
{
WindowLayout.LayoutViewInfo viewInfo1 = new WindowLayout.LayoutViewInfo((object) "top_view", 36f, true);
WindowLayout.LayoutViewInfo viewInfo2 = new WindowLayout.LayoutViewInfo((object) "bottom_view", 20f, true);
WindowLayout.LayoutViewInfo viewInfo3 = new WindowLayout.LayoutViewInfo((object) "center_view", 0.0f, true);
System.Type[] array = TypeCache.GetTypesDerivedFrom<EditorWindow>().ToArray<System.Type>();
if (!WindowLayout.GetLayoutViewInfo(layoutData, array, ref viewInfo3))
return false;
WindowLayout.GetLayoutViewInfo(layoutData, array, ref viewInfo1);
WindowLayout.GetLayoutViewInfo(layoutData, array, ref viewInfo2);
ContainerWindow mainWindow = WindowLayout.FindMainWindow();
if (keepMainWindow && (UnityEngine.Object) mainWindow == (UnityEngine.Object) null)
{
Debug.LogWarning((object) ("No main window to restore layout from while loading dynamic layout for mode " + ModeService.currentId));
return false;
}
string windowId = "MainView_" + ModeService.currentId;
WindowLayout.InitContainerWindow(ref mainWindow, windowId, layoutData);
WindowLayout.GenerateLayout(mainWindow, ShowMode.MainWindow, array, viewInfo3, viewInfo1, viewInfo2, layoutData);
mainWindow.m_DontSaveToLayout = !Convert.ToBoolean(layoutData[(object) "restore_saved_layout"]);
return true;
}

private static View LoadLayoutView<T>(
System.Type[] availableEditorWindowTypes,
WindowLayout.LayoutViewInfo viewInfo,
float width,
float height)
where T : View
{
if (!viewInfo.used)
return (View) null;
View view = (View) null;
if (viewInfo.isContainer)
{
bool flag1 = viewInfo.extendedData.Contains((object) "tabs") && Convert.ToBoolean(viewInfo.extendedData[(object) "tabs"]);
bool flag2 = viewInfo.extendedData.Contains((object) "vertical") || viewInfo.extendedData.Contains((object) "horizontal");
bool flag3 = viewInfo.extendedData.Contains((object) "vertical") && Convert.ToBoolean(viewInfo.extendedData[(object) "vertical"]);
if (flag1 & flag2)
Debug.LogWarning((object) (ModeService.currentId + " defines both tabs and splitter (horizontal or vertical) layouts.\n You can only define one to true (i.e. tabs = true) in the editor mode file."));
if (flag2)
{
SplitView instance = ScriptableObject.CreateInstance<SplitView>();
instance.vertical = flag3;
view = (View) instance;
}
else if (flag1)
view = (View) ScriptableObject.CreateInstance<DockArea>();
view.position = new Rect(0.0f, 0.0f, width, height);
if (!(viewInfo.extendedData[(object) "children"] is IList list))
throw new LayoutException("Invalid split view data");
int key = 0;
foreach (object viewData in (IEnumerable) list)
{
WindowLayout.LayoutViewInfo viewInfo1 = new WindowLayout.LayoutViewInfo((object) key, flag1 ? 1f : 1f / (float) list.Count, true);
if (WindowLayout.ParseViewData(availableEditorWindowTypes, viewData, ref viewInfo1))
{
float width1 = flag1 ? width : (flag3 ? width : width * viewInfo1.size);
float height1 = flag1 ? height : (flag3 ? height * viewInfo1.size : height);
DockArea dockArea;
int num;
if (flag1)
{
dockArea = view as DockArea;
num = dockArea != null ? 1 : 0;
}
else
num = 0;
if (num != 0)
{
dockArea.AddTab((EditorWindow) ScriptableObject.CreateInstance(viewInfo1.type));
}
else
{
view.AddChild(WindowLayout.LoadLayoutView<HostView>(availableEditorWindowTypes, viewInfo1, width1, height1));
view.children[key].position = new Rect(0.0f, 0.0f, width1, height1);
}
++key;
}
}
}
else if (viewInfo.type != (System.Type) null)
{
HostView instance = ScriptableObject.CreateInstance<HostView>();
instance.SetActualViewInternal(ScriptableObject.CreateInstance(viewInfo.type) as EditorWindow, true);
view = (View) instance;
}
else
view = (View) ScriptableObject.CreateInstance<T>();
return view;
}

internal static void InitContainerWindow(
ref ContainerWindow window,
string windowId,
IDictionary layoutData)
{
if ((UnityEngine.Object) window == (UnityEngine.Object) null)
{
window = ScriptableObject.CreateInstance<ContainerWindow>();
Vector2 min = new Vector2(120f, 80f);
Vector2 max = new Vector2(8192f, 8192f);
if (layoutData.Contains((object) "min_width"))
min.x = Convert.ToSingle(layoutData[(object) "min_width"]);
if (layoutData.Contains((object) "min_height"))
min.y = Convert.ToSingle(layoutData[(object) "min_height"]);
if (layoutData.Contains((object) "max_width"))
max.x = Convert.ToSingle(layoutData[(object) "max_width"]);
if (layoutData.Contains((object) "max_height"))
max.y = Convert.ToSingle(layoutData[(object) "max_height"]);
window.SetMinMaxSizes(min, max);
}
bool flag = EditorPrefs.HasKey(windowId + "h");
window.windowID = windowId;
if (!(Convert.ToBoolean(layoutData[(object) "restore_layout_dimension"]) & flag))
return;
window.LoadGeometry(true);
}

internal static ContainerWindow FindMainWindow()
{
return ((IEnumerable<ContainerWindow>) Resources.FindObjectsOfTypeAll<ContainerWindow>()).FirstOrDefault<ContainerWindow>((Func<ContainerWindow, bool>) (w => w.showMode == ShowMode.MainWindow));
}

internal static ContainerWindow ShowWindowWithDynamicLayout(
string windowId,
string layoutDataPath)
{
try
{
ContainerWindow.SetFreezeDisplay(true);
if (!File.Exists(layoutDataPath))
{
Debug.LogError((object) ("Failed to find layout data file at path " + layoutDataPath));
return (ContainerWindow) null;
}
IDictionary layoutData = SJSON.LoadString(File.ReadAllText(layoutDataPath));
System.Type[] array = TypeCache.GetTypesDerivedFrom<EditorWindow>().ToArray<System.Type>();
WindowLayout.LayoutViewInfo viewInfo1 = new WindowLayout.LayoutViewInfo((object) "top_view", 36f, false);
WindowLayout.LayoutViewInfo viewInfo2 = new WindowLayout.LayoutViewInfo((object) "bottom_view", 20f, false);
string key = "view";
if (!layoutData.Contains((object) key))
key = "center_view";
WindowLayout.LayoutViewInfo viewInfo3 = new WindowLayout.LayoutViewInfo((object) key, 0.0f, true);
if (!WindowLayout.GetLayoutViewInfo(layoutData, array, ref viewInfo3))
{
Debug.LogError((object) "Failed to load window layout; no view defined");
return (ContainerWindow) null;
}
WindowLayout.GetLayoutViewInfo(layoutData, array, ref viewInfo1);
WindowLayout.GetLayoutViewInfo(layoutData, array, ref viewInfo2);
ContainerWindow window = ((IEnumerable<ContainerWindow>) Resources.FindObjectsOfTypeAll<ContainerWindow>()).FirstOrDefault<ContainerWindow>((Func<ContainerWindow, bool>) (w => w.windowID == windowId));
WindowLayout.InitContainerWindow(ref window, windowId, layoutData);
WindowLayout.GenerateLayout(window, ShowMode.Utility, array, viewInfo3, viewInfo1, viewInfo2, layoutData);
window.m_DontSaveToLayout = !Convert.ToBoolean(layoutData[(object) "restore_saved_layout"]);
return window;
}
finally
{
ContainerWindow.SetFreezeDisplay(false);
}
}

private static void GenerateLayout(
ContainerWindow window,
ShowMode showMode,
System.Type[] availableEditorWindowTypes,
WindowLayout.LayoutViewInfo center,
WindowLayout.LayoutViewInfo top,
WindowLayout.LayoutViewInfo bottom,
IDictionary layoutData)
{
try
{
ContainerWindow.SetFreezeDisplay(true);
Rect position = window.position;
float width = position.width;
position = window.position;
float height1 = position.height;
View child1 = WindowLayout.LoadLayoutView<DockArea>(availableEditorWindowTypes, center, width, height1);
View child2 = WindowLayout.LoadLayoutView<Toolbar>(availableEditorWindowTypes, top, width, height1);
View child3 = WindowLayout.LoadLayoutView<AppStatusBar>(availableEditorWindowTypes, bottom, width, height1);
MainView instance = ScriptableObject.CreateInstance<MainView>();
instance.useTopView = top.used;
instance.useBottomView = bottom.used;
instance.topViewHeight = top.size;
instance.bottomViewHeight = bottom.size;
if ((bool) (UnityEngine.Object) child2)
{
child2.position = new Rect(0.0f, 0.0f, width, top.size);
instance.AddChild(child2);
}
float height2 = height1 - bottom.size - top.size;
child1.position = new Rect(0.0f, top.size, width, height2);
instance.AddChild(child1);
if ((bool) (UnityEngine.Object) child3)
{
child3.position = new Rect(0.0f, height1 - bottom.size, width, bottom.size);
instance.AddChild(child3);
}
if ((bool) (UnityEngine.Object) window.rootView)
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) window.rootView, true);
window.rootView = (View) instance;
window.rootView.position = new Rect(0.0f, 0.0f, width, height1);
window.Show(showMode, true, true, true);
window.DisplayAllViews();
}
finally
{
ContainerWindow.SetFreezeDisplay(false);
}
}

private static bool GetLayoutViewInfo(
IDictionary layoutData,
System.Type[] availableEditorWindowTypes,
ref WindowLayout.LayoutViewInfo viewInfo)
{
if (!layoutData.Contains(viewInfo.key))
return false;
object viewData = layoutData[viewInfo.key];
return WindowLayout.ParseViewData(availableEditorWindowTypes, viewData, ref viewInfo);
}

private static bool ParseViewData(
System.Type[] availableEditorWindowTypes,
object viewData,
ref WindowLayout.LayoutViewInfo viewInfo)
{
switch (viewData)
{
case string _:
viewInfo.className = Convert.ToString(viewData);
viewInfo.used = !string.IsNullOrEmpty(viewInfo.className);
if (!viewInfo.used)
return true;
break;
case IDictionary dictionary:
if (dictionary.Contains((object) "children") || dictionary.Contains((object) "vertical") || dictionary.Contains((object) "horizontal") || dictionary.Contains((object) "tabs"))
{
viewInfo.isContainer = true;
viewInfo.className = string.Empty;
}
else
{
viewInfo.isContainer = false;
viewInfo.className = Convert.ToString(dictionary[(object) "class_name"]);
}
if (dictionary.Contains((object) "size"))
viewInfo.defaultSize = Convert.ToSingle(dictionary[(object) "size"]);
viewInfo.extendedData = dictionary;
viewInfo.used = true;
break;
default:
viewInfo.className = string.Empty;
viewInfo.type = (System.Type) null;
viewInfo.used = false;
return true;
}
if (string.IsNullOrEmpty(viewInfo.className))
return true;
foreach (System.Type editorWindowType in availableEditorWindowTypes)
{
if (!(editorWindowType.Name != viewInfo.className))
{
viewInfo.type = editorWindowType;
break;
}
}
if (!(viewInfo.type == (System.Type) null))
return true;
Debug.LogWarning((object) $"Invalid layout view {viewInfo.key} with type {viewInfo.className} for mode {ModeService.currentId}");
return false;
}

internal static string GetLayoutFileName(string mode, int version) => $"{mode}-{version}.dwlt";

private static IEnumerable<string> GetCurrentModeLayouts()
{
object layouts = ModeService.GetModeDataSection(ModeService.currentIndex, "layouts");
if (layouts is IList<object> modeLayoutPaths)
{
foreach (string str in modeLayoutPaths.Cast<string>())
{
string layoutPath = str;
if (File.Exists(layoutPath))
{
yield return layoutPath;
layoutPath = (string) null;
}
}
}
}

private static void LoadLastUsedLayoutForCurrentMode(bool keepMainWindow)
{
foreach (string path in WindowLayout.GetLastLayout())
{
if (WindowLayout.LoadWindowLayout_Internal(path, path != WindowLayout.ProjectLayoutPath, false, keepMainWindow, false))
return;
}
foreach (string currentModeLayout in WindowLayout.GetCurrentModeLayouts())
{
if (WindowLayout.LoadWindowLayout_Internal(currentModeLayout, currentModeLayout != WindowLayout.ProjectLayoutPath, false, keepMainWindow, false))
return;
}
if (!string.IsNullOrEmpty(ModeService.GetDefaultModeLayout()) && WindowLayout.LoadWindowLayout_Internal(ModeService.GetDefaultModeLayout(), true, false, keepMainWindow, false) || WindowLayout.LoadWindowLayout_Internal(WindowLayout.GetDefaultLayoutPath(), true, false, keepMainWindow, false))
return;
int num = 0;
if (!Application.isTestRun && Application.isHumanControllingUs)
num = EditorUtility.DisplayDialogComplex("Missing Default Layout", "No valid user created or default window layout found. Please revert factory settings to restore the default layouts.", "Quit", "Revert Factory Settings", "");
else
WindowLayout.ResetUserLayouts();
switch (num)
{
case 0:
EditorApplication.Exit(0);
break;
case 1:
WindowLayout.ResetFactorySettings();
break;
}
}

[RequiredByNativeCode]
[UsedImplicitly]
public static void SaveDefaultWindowPreferences()
{
if (!InternalEditorUtility.isHumanControllingUs)
return;
WindowLayout.SaveCurrentLayoutPerMode(ModeService.currentId);
}

internal static void SaveCurrentLayoutPerMode(string modeId)
{
WindowLayout.SaveWindowLayout(FileUtil.CombinePaths(System.IO.Directory.GetCurrentDirectory(), WindowLayout.GetProjectLayoutPerMode(modeId)));
WindowLayout.SaveWindowLayout(Path.Combine(WindowLayout.layoutsCurrentModePreferencesPath, WindowLayout.GetLayoutFileName(modeId, Application.unityVersionVer)));
}

public static IEnumerable<string> GetLastLayout(string directory, string mode, int version)
{
string currentModeAndVersionLayout = WindowLayout.GetLayoutFileName(mode, version);
string layoutSearchPattern = mode + "-*.*wlt";
string preferred = Path.Combine(directory, currentModeAndVersionLayout);
if (File.Exists(preferred))
yield return preferred;
if (System.IO.Directory.Exists(directory))
{
IOrderedEnumerable<string> paths = ((IEnumerable<string>) System.IO.Directory.GetFiles(directory, layoutSearchPattern)).Where<string>((Func<string, bool>) (p => string.Compare(p, preferred, StringComparison.OrdinalIgnoreCase) != 0)).OrderByDescending<string, string>((Func<string, string>) (p => p), (IComparer<string>) StringComparer.OrdinalIgnoreCase);
foreach (string path in (IEnumerable<string>) paths)
yield return path;
paths = (IOrderedEnumerable<string>) null;
}
}

internal static IEnumerable<string> GetLastLayout()
{
string mode = ModeService.currentId;
int version = Application.unityVersionVer;
foreach (string layout in WindowLayout.GetLastLayout(WindowLayout.layoutsProjectPath, mode, version))
yield return layout;
foreach (string layout in WindowLayout.GetLastLayout(WindowLayout.layoutsCurrentModePreferencesPath, mode, version))
yield return layout;
}

internal static string GetCurrentLayoutPath()
{
string currentLayoutPath = WindowLayout.ProjectLayoutPath;
if (!File.Exists(WindowLayout.ProjectLayoutPath))
currentLayoutPath = WindowLayout.GetDefaultLayoutPath();
return currentLayoutPath;
}

internal static string GetDefaultLayoutPath()
{
return Path.Combine(WindowLayout.layoutsModePreferencesPath, "Default.wlt");
}

internal static string GetProjectLayoutPerMode(string modeId)
{
return FileUtil.CombinePaths(WindowLayout.layoutsProjectPath, WindowLayout.GetLayoutFileName(modeId, Application.unityVersionVer));
}

private static void InitializeLayoutPreferencesFolder()
{
string defaultLayoutPath = WindowLayout.GetDefaultLayoutPath();
if (!System.IO.Directory.Exists(WindowLayout.layoutsPreferencesPath))
System.IO.Directory.CreateDirectory(WindowLayout.layoutsPreferencesPath);
if (!System.IO.Directory.Exists(WindowLayout.layoutsModePreferencesPath))
{
Console.WriteLine($"[LAYOUT] {WindowLayout.layoutsModePreferencesPath} does not exist. Copying base layouts.");
if (WindowLayout.layoutsDefaultModePreferencesPath == WindowLayout.layoutsModePreferencesPath)
{
FileUtil.CopyFileOrDirectory(WindowLayout.layoutResourcesPath, WindowLayout.layoutsDefaultModePreferencesPath);
foreach (string file in System.IO.Directory.GetFiles(WindowLayout.layoutsPreferencesPath, "*.wlt"))
{
string str = Path.Combine(WindowLayout.layoutsDefaultModePreferencesPath, Path.GetFileName(file));
if (!File.Exists(str))
FileUtil.CopyFileIfExists(file, str, false);
}
}
else
System.IO.Directory.CreateDirectory(WindowLayout.layoutsModePreferencesPath);
}
if (!File.Exists(defaultLayoutPath))
{
string str = ModeService.GetDefaultModeLayout();
if (!File.Exists(str))
str = Path.Combine(WindowLayout.layoutResourcesPath, "Default.wlt");
Console.WriteLine($"[LAYOUT] Copying {str} to {defaultLayoutPath}");
FileUtil.CopyFileOrDirectory(str, defaultLayoutPath);
}
Debug.Assert(File.Exists(defaultLayoutPath));
}

static WindowLayout()
{
EditorApplication.CallDelayed(new EditorApplication.CallbackFunction(WindowLayout.UpdateWindowLayoutMenu));
}

internal static void UpdateWindowLayoutMenu()
{
if (!ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true))
return;
WindowLayout.ReloadWindowLayoutMenu();
}

internal static void ReloadWindowLayoutMenu()
{
Menu.RemoveMenuItem("Window/Layouts");
if (!ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true))
return;
int layoutMenuItemPriority = -20;
string[] strArray = new string[0];
if (System.IO.Directory.Exists(WindowLayout.layoutsModePreferencesPath))
{
strArray = ((IEnumerable<string>) System.IO.Directory.GetFiles(WindowLayout.layoutsModePreferencesPath)).Where<string>((Func<string, bool>) (path => path.EndsWith(".wlt"))).ToArray<string>();
foreach (string str in strArray)
{
string layoutPath = str;
Menu.AddMenuItem("Window/Layouts/" + Path.GetFileNameWithoutExtension(layoutPath), "", false, layoutMenuItemPriority++, (Action) (() => WindowLayout.TryLoadWindowLayout(layoutPath, false, true, true, true)), (Func<bool>) null);
}
layoutMenuItemPriority += 500;
}
WindowLayout.AddLegacyLayoutMenuItems(ref layoutMenuItemPriority);
if (ModeService.GetModeDataSection(ModeService.currentIndex, "layouts") is IList<object> modeDataSection)
{
foreach (string str in modeDataSection.Cast<string>())
{
string layoutPath = str;
if (File.Exists(layoutPath))
{
string withoutExtension = Path.GetFileNameWithoutExtension(layoutPath);
Menu.AddMenuItem("Window/Layouts/" + withoutExtension, "", Toolbar.lastLoadedLayoutName == withoutExtension, layoutMenuItemPriority++, (Action) (() => WindowLayout.TryLoadWindowLayout(layoutPath, false)), (Func<bool>) null);
}
}
}
layoutMenuItemPriority += 500;
Menu.AddMenuItem("Window/Layouts/Save Layout...", "", false, layoutMenuItemPriority++, new Action(WindowLayout.SaveGUI), (Func<bool>) null);
Menu.AddMenuItem("Window/Layouts/Save Layout to File...", "", false, layoutMenuItemPriority++, new Action(WindowLayout.SaveToFile), (Func<bool>) null);
Menu.AddMenuItem("Window/Layouts/Load Layout from File...", "", false, layoutMenuItemPriority++, new Action(WindowLayout.LoadFromFile), (Func<bool>) null);
Menu.AddMenuItem("Window/Layouts/Delete Layout/", "", false, layoutMenuItemPriority++, (Action) null, (Func<bool>) null);
foreach (string str in strArray)
{
string layoutPath = str;
Menu.AddMenuItem("Window/Layouts/Delete Layout/" + Path.GetFileNameWithoutExtension(layoutPath), "", false, layoutMenuItemPriority++, (Action) (() => WindowLayout.DeleteWindowLayout(layoutPath)), (Func<bool>) null);
}
Menu.AddMenuItem("Window/Layouts/Reset All Layouts", "", false, layoutMenuItemPriority++, (Action) (() => WindowLayout.ResetAllLayouts(false)), (Func<bool>) null);
}

private static void AddLegacyLayoutMenuItems(ref int layoutMenuItemPriority)
{
if (File.Exists("Library/CurrentLayout-default.dwlt"))
Menu.AddMenuItem("Window/Layouts/Other Versions/Default (2020)", "", false, layoutMenuItemPriority++, (Action) (() => WindowLayout.TryLoadWindowLayout("Library/CurrentLayout-default.dwlt", false, true, false, true)), (Func<bool>) null);
if (!System.IO.Directory.Exists(WindowLayout.layoutsProjectPath))
return;
string str = FileUtil.CombinePaths(WindowLayout.layoutsProjectPath, "CurrentMaximizeLayout.dwlt");
HashSet<string> stringSet;
using (CollectionPool<HashSet<string>, string>.Get(out stringSet))
{
stringSet.Add(WindowLayout.GetCurrentLayoutPath());
stringSet.Add(str);
foreach (string file in System.IO.Directory.GetFiles(WindowLayout.layoutsProjectPath, "*.dwlt"))
{
string layoutPath = file;
if (!stringSet.Contains(layoutPath))
{
string withoutExtension = Path.GetFileNameWithoutExtension(layoutPath);
string[] strArray = Path.GetFileName(withoutExtension).Split('-');
string name = "Window/Layouts/Other Versions/" + withoutExtension;
if (strArray.Length == 2)
name = $"Window/Layouts/Other Versions/{ObjectNames.NicifyVariableName(strArray[0])} ({strArray[1]})";
Menu.AddMenuItem(name, "", false, layoutMenuItemPriority++, (Action) (() => WindowLayout.TryLoadWindowLayout(layoutPath, false, true, false, true)), (Func<bool>) null);
}
}
}
}

internal static EditorWindow FindEditorWindowOfType(System.Type type)
{
UnityEngine.Object[] objectsOfTypeAll = Resources.FindObjectsOfTypeAll(type);
return objectsOfTypeAll.Length != 0 ? objectsOfTypeAll[0] as EditorWindow : (EditorWindow) null;
}

internal static void CheckWindowConsistency()
{
foreach (EditorWindow editorWindow in Resources.FindObjectsOfTypeAll(typeof (EditorWindow)))
{
if ((UnityEngine.Object) editorWindow.m_Parent == (UnityEngine.Object) null)
Debug.LogErrorFormat("Invalid editor window of type: {0}, title: {1}", (object) editorWindow.GetType(), (object) editorWindow.titleContent.text);
}
}

internal static EditorWindow TryGetLastFocusedWindowInSameDock()
{
System.Type type = (System.Type) null;
string windowTypeInSameDock = WindowFocusState.instance.m_LastWindowTypeInSameDock;
if (windowTypeInSameDock != "")
type = System.Type.GetType(windowTypeInSameDock);
PlayModeView mainPlayModeView = PlayModeView.GetMainPlayModeView();
if (type != (System.Type) null && (bool) (UnityEngine.Object) mainPlayModeView && (UnityEngine.Object) mainPlayModeView != (UnityEngine.Object) null && mainPlayModeView.m_Parent is DockArea)
{
object[] objectsOfTypeAll = (object[]) Resources.FindObjectsOfTypeAll(type);
DockArea parent = mainPlayModeView.m_Parent as DockArea;
for (int index = 0; index < objectsOfTypeAll.Length; ++index)
{
EditorWindow windowInSameDock = objectsOfTypeAll[index] as EditorWindow;
if ((bool) (UnityEngine.Object) windowInSameDock && (UnityEngine.Object) windowInSameDock.m_Parent == (UnityEngine.Object) parent)
return windowInSameDock;
}
}
return (EditorWindow) null;
}

internal static void SaveCurrentFocusedWindowInSameDock(EditorWindow windowToBeFocused)
{
if (!((UnityEngine.Object) windowToBeFocused.m_Parent != (UnityEngine.Object) null) || !(windowToBeFocused.m_Parent is DockArea))
return;
EditorWindow actualView = (windowToBeFocused.m_Parent as DockArea).actualView;
if ((bool) (UnityEngine.Object) actualView)
WindowFocusState.instance.m_LastWindowTypeInSameDock = actualView.GetType().ToString();
}

internal static void FindFirstGameViewAndSetToMaximizeOnPlay()
{
GameView editorWindowOfType = (GameView) WindowLayout.FindEditorWindowOfType(typeof (GameView));
if (!(bool) (UnityEngine.Object) editorWindowOfType)
return;
editorWindowOfType.enterPlayModeBehavior = PlayModeView.EnterPlayModeBehavior.PlayMaximized;
}

internal static void FindFirstGameViewAndSetToPlayFocused()
{
GameView editorWindowOfType = (GameView) WindowLayout.FindEditorWindowOfType(typeof (GameView));
if (!(bool) (UnityEngine.Object) editorWindowOfType)
return;
editorWindowOfType.enterPlayModeBehavior = PlayModeView.EnterPlayModeBehavior.PlayFocused;
}

internal static EditorWindow TryFocusAppropriateWindow(bool enteringPlaymode)
{
PlayModeView playModeViewToFocus = PlayModeView.GetCorrectPlayModeViewToFocus();
bool flag = (bool) (UnityEngine.Object) playModeViewToFocus && playModeViewToFocus.enterPlayModeBehavior != PlayModeView.EnterPlayModeBehavior.PlayUnfocused;
if (enteringPlaymode)
{
if (flag)
{
WindowLayout.SaveCurrentFocusedWindowInSameDock((EditorWindow) playModeViewToFocus);
playModeViewToFocus.Focus();
}
return (EditorWindow) playModeViewToFocus;
}
if (!flag)
return EditorWindow.focusedWindow;
EditorWindow windowInSameDock = WindowLayout.TryGetLastFocusedWindowInSameDock();
if ((bool) (UnityEngine.Object) windowInSameDock)
windowInSameDock.ShowTab();
return windowInSameDock;
}

internal static EditorWindow GetMaximizedWindow()
{
UnityEngine.Object[] objectsOfTypeAll = Resources.FindObjectsOfTypeAll(typeof (MaximizedHostView));
if (objectsOfTypeAll.Length != 0)
{
MaximizedHostView maximizedHostView = objectsOfTypeAll[0] as MaximizedHostView;
if ((bool) (UnityEngine.Object) maximizedHostView.actualView)
return maximizedHostView.actualView;
}
return (EditorWindow) null;
}

internal static EditorWindow ShowAppropriateViewOnEnterExitPlaymode(bool entering)
{
if (WindowFocusState.instance.m_CurrentlyInPlayMode == entering)
return (EditorWindow) null;
WindowFocusState.instance.m_CurrentlyInPlayMode = entering;
EditorWindow maximizedWindow = WindowLayout.GetMaximizedWindow();
if (entering)
{
if (!PlayModeView.openWindowOnEnteringPlayMode && PlayModeView.GetCorrectPlayModeViewToFocus() == null)
return (EditorWindow) null;
WindowFocusState.instance.m_WasMaximizedBeforePlay = (UnityEngine.Object) maximizedWindow != (UnityEngine.Object) null;
if ((UnityEngine.Object) maximizedWindow != (UnityEngine.Object) null)
return maximizedWindow;
}
else if (WindowFocusState.instance.m_WasMaximizedBeforePlay)
return maximizedWindow;
if ((bool) (UnityEngine.Object) maximizedWindow)
WindowLayout.Unmaximize(maximizedWindow);
if (entering)
{
PlayModeView playModeViewToFocus = PlayModeView.GetCorrectPlayModeViewToFocus();
if ((UnityEngine.Object) playModeViewToFocus != (UnityEngine.Object) null && playModeViewToFocus.enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayUnfocused)
{
playModeViewToFocus.m_Parent.OnLostFocus();
return (EditorWindow) playModeViewToFocus;
}
}
EditorWindow editorWindow = WindowLayout.TryFocusAppropriateWindow(entering);
if ((bool) (UnityEngine.Object) editorWindow || !entering || !PlayModeView.openWindowOnEnteringPlayMode)
return editorWindow;
EditorWindow editorWindowOfType = WindowLayout.FindEditorWindowOfType(typeof (SceneView));
if ((bool) (UnityEngine.Object) editorWindowOfType && editorWindowOfType.m_Parent is DockArea)
{
DockArea parent = editorWindowOfType.m_Parent as DockArea;
if ((bool) (UnityEngine.Object) parent)
{
WindowFocusState.instance.m_LastWindowTypeInSameDock = editorWindowOfType.GetType().ToString();
GameView instance = ScriptableObject.CreateInstance<GameView>();
parent.AddTab((EditorWindow) instance);
return (EditorWindow) instance;
}
}
GameView instance1 = ScriptableObject.CreateInstance<GameView>();
instance1.Show(true);
instance1.Focus();
return (EditorWindow) instance1;
}

internal static bool IsMaximized(EditorWindow window) => window.m_Parent is MaximizedHostView;

public static void Unmaximize(EditorWindow win)
{
HostView parent1 = win.m_Parent;
if ((UnityEngine.Object) parent1 == (UnityEngine.Object) null)
{
Debug.LogError((object) "Host view was not found");
WindowLayout.ResetAllLayouts();
}
else
{
UnityEngine.Object[] objectArray = InternalEditorUtility.LoadSerializedFileAndForget(Path.Combine(WindowLayout.layoutsProjectPath, "CurrentMaximizeLayout.dwlt"));
if (objectArray.Length < 2)
{
Debug.LogError((object) "Maximized serialized file backup not found.");
WindowLayout.ResetAllLayouts();
}
else
{
SplitView child = objectArray[0] as SplitView;
EditorWindow pane = objectArray[1] as EditorWindow;
if ((UnityEngine.Object) child == (UnityEngine.Object) null)
{
Debug.LogError((object) "Maximization failed because the root split view was not found.");
WindowLayout.ResetAllLayouts();
}
else
{
ContainerWindow window = win.m_Parent.window;
if ((UnityEngine.Object) window == (UnityEngine.Object) null)
{
Debug.LogError((object) "Maximization failed because the root split view has no container window.");
WindowLayout.ResetAllLayouts();
}
else
{
try
{
ContainerWindow.SetFreezeDisplay(true);
int idx1 = (bool) (UnityEngine.Object) parent1.parent ? parent1.parent.IndexOfChild((View) parent1) : throw new LayoutException("No parent view");
Rect position = parent1.position;
View parent2 = parent1.parent;
parent2.RemoveChild(idx1);
parent2.AddChild((View) child, idx1);
child.position = position;
DockArea parent3 = pane.m_Parent as DockArea;
int idx2 = parent3.m_Panes.IndexOf(pane);
parent1.actualView = (EditorWindow) null;
win.m_Parent = (HostView) null;
parent3.AddTab(idx2, win);
parent3.RemoveTab(pane);
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) pane);
foreach (UnityEngine.Object @object in objectArray)
{
EditorWindow editorWindow = @object as EditorWindow;
if ((UnityEngine.Object) editorWindow != (UnityEngine.Object) null)
editorWindow.MakeParentsSettingsMatchMe();
}
parent2.Initialize(parent2.window);
parent2.position = parent2.position;
child.Reflow();
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) parent1);
win.Focus();
GameView gameView = win as GameView;
if ((UnityEngine.Object) gameView != (UnityEngine.Object) null)
gameView.m_Parent.EnableVSync(gameView.vSyncEnabled);
window.DisplayAllViews();
win.m_Parent.MakeVistaDWMHappyDance();
}
catch (Exception ex)
{
Debug.Log((object) ("Maximization failed: " + ex?.ToString()));
WindowLayout.ResetAllLayouts();
}
try
{
if (Application.platform != RuntimePlatform.OSXEditor || !SystemInfo.operatingSystem.Contains("10.7") || !SystemInfo.graphicsDeviceVendor.Contains("ATI"))
return;
foreach (GUIView guiView in Resources.FindObjectsOfTypeAll(typeof (GUIView)))
guiView.Repaint();
}
finally
{
ContainerWindow.SetFreezeDisplay(false);
}
}
}
}
}
}

internal static void MaximizeGestureHandler()
{
if (UnityEngine.Event.current.type != EditorGUIUtility.magnifyGestureEventType || GUIUtility.hotControl != 0)
return;
EditorWindow mouseOverWindow = EditorWindow.mouseOverWindow;
if ((UnityEngine.Object) mouseOverWindow == (UnityEngine.Object) null)
return;
ShortcutArguments args = new ShortcutArguments()
{
stage = ShortcutStage.End
} with
{
context = !WindowLayout.IsMaximized(mouseOverWindow) ? ((double) UnityEngine.Event.current.delta.x > 0.05000000074505806 ? (object) mouseOverWindow : (object) (EditorWindow) null) : ((double) UnityEngine.Event.current.delta.x < -0.05000000074505806 ? (object) mouseOverWindow : (object) (EditorWindow) null)
};
if (args.context == null)
return;
WindowLayout.MaximizeKeyHandler(args);
}

[FormerlyPrefKeyAs("Window/Maximize View", "# ")]
[Shortcut("Window/Maximize View", KeyCode.Space, ShortcutModifiers.Shift)]
internal static void MaximizeKeyHandler(ShortcutArguments args)
{
if (args.context is PreviewWindow)
return;
EditorWindow mouseOverWindow = EditorWindow.mouseOverWindow;
if (!((UnityEngine.Object) mouseOverWindow != (UnityEngine.Object) null))
return;
if (WindowLayout.IsMaximized(mouseOverWindow))
WindowLayout.Unmaximize(mouseOverWindow);
else
WindowLayout.Maximize(mouseOverWindow);
}

private static View FindRootSplitView(EditorWindow win)
{
View parent = win.m_Parent.parent;
View rootSplitView = parent;
for (; parent is SplitView; parent = parent.parent)
rootSplitView = parent;
return rootSplitView;
}

public static void AddSplitViewAndChildrenRecurse(View splitview, ArrayList list)
{
list.Add((object) splitview);
DockArea dockArea = splitview as DockArea;
if ((UnityEngine.Object) dockArea != (UnityEngine.Object) null)
{
list.AddRange((ICollection) dockArea.m_Panes);
list.Add((object) dockArea.actualView);
}
foreach (View child in splitview.children)
WindowLayout.AddSplitViewAndChildrenRecurse(child, list);
}

public static void SaveSplitViewAndChildren(View splitview, EditorWindow win, string path)
{
ArrayList list = new ArrayList();
WindowLayout.AddSplitViewAndChildrenRecurse(splitview, list);
list.Remove((object) splitview);
list.Remove((object) win);
list.Insert(0, (object) splitview);
list.Insert(1, (object) win);
if (!WindowLayout.EnsureDirectoryCreated(path))
return;
InternalEditorUtility.SaveToSerializedFileAndForget(list.ToArray(typeof (UnityEngine.Object)) as UnityEngine.Object[], path, true);
}

private static bool EnsureDirectoryCreated(string path)
{
string directoryName = Path.GetDirectoryName(path);
if (string.IsNullOrEmpty(directoryName))
return false;
if (!System.IO.Directory.Exists(directoryName))
System.IO.Directory.CreateDirectory(directoryName);
return true;
}

public static void Maximize(EditorWindow win)
{
if (!WindowLayout.MaximizePrepare(win))
return;
WindowLayout.MaximizePresent(win);
}

public static bool MaximizePrepare(EditorWindow win)
{
View rootSplitView = WindowLayout.FindRootSplitView(win);
if ((UnityEngine.Object) rootSplitView == (UnityEngine.Object) null)
return false;
View parent1 = rootSplitView.parent;
DockArea parent2 = win.m_Parent as DockArea;
if ((UnityEngine.Object) parent2 == (UnityEngine.Object) null || (UnityEngine.Object) parent1 == (UnityEngine.Object) null || (UnityEngine.Object) (rootSplitView.parent as MainView) == (UnityEngine.Object) null)
return false;
ContainerWindow window = win.m_Parent.window;
if ((UnityEngine.Object) window == (UnityEngine.Object) null)
return false;
int index = parent2.m_Panes.IndexOf(win);
if (index == -1 || !window.CanCloseAllExcept(win))
return false;
parent2.selected = index;
WindowLayout.SaveSplitViewAndChildren(rootSplitView, win, Path.Combine(WindowLayout.layoutsProjectPath, "CurrentMaximizeLayout.dwlt"));
parent2.actualView = (EditorWindow) null;
parent2.m_Panes[index] = (EditorWindow) null;
MaximizedHostView instance = ScriptableObject.CreateInstance<MaximizedHostView>();
int idx = parent1.IndexOfChild(rootSplitView);
Rect position = rootSplitView.position;
parent1.RemoveChild(rootSplitView);
parent1.AddChild((View) instance, idx);
instance.actualView = win;
instance.position = position;
GameView gameView = win as GameView;
if ((UnityEngine.Object) gameView != (UnityEngine.Object) null)
instance.EnableVSync(gameView.vSyncEnabled);
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) rootSplitView, true);
return true;
}

public static void MaximizePresent(EditorWindow win)
{
ContainerWindow.SetFreezeDisplay(true);
win.Focus();
WindowLayout.CheckWindowConsistency();
win.m_Parent.window.DisplayAllViews();
win.m_Parent.MakeVistaDWMHappyDance();
ContainerWindow.SetFreezeDisplay(false);
win.OnMaximized();
}

private static void DeleteWindowLayout(string path)
{
string withoutExtension = Path.GetFileNameWithoutExtension(path);
if (!EditorUtility.DisplayDialog("Delete Layout", $"Delete window layout '{withoutExtension}'?", "Delete", "Cancel"))
return;
WindowLayout.DeleteWindowLayoutImpl(withoutExtension, path);
}

[UsedImplicitly]
internal static void DeleteNamedWindowLayoutNoDialog(string name)
{
string path = Path.Combine(WindowLayout.layoutsModePreferencesPath, name + ".wlt");
WindowLayout.DeleteWindowLayoutImpl(name, path);
}

private static void DeleteWindowLayoutImpl(string name, string path)
{
if (Toolbar.lastLoadedLayoutName == name)
Toolbar.lastLoadedLayoutName = (string) null;
File.Delete(path);
WindowLayout.ReloadWindowLayoutMenu();
EditorUtility.Internal_UpdateAllMenus();
ShortcutIntegration.instance.RebuildShortcuts();
}

public static bool TryLoadWindowLayout(string path, bool newProjectLayoutWasCreated)
{
WindowLayout.LoadWindowLayoutFlags windowLayoutFlags = WindowLayout.GetLoadWindowLayoutFlags(newProjectLayoutWasCreated, true, false, true);
return WindowLayout.TryLoadWindowLayout(path, windowLayoutFlags);
}

public static bool TryLoadWindowLayout(
string path,
bool newProjectLayoutWasCreated,
bool setLastLoadedLayoutName,
bool keepMainWindow,
bool logErrorsToConsole)
{
WindowLayout.LoadWindowLayoutFlags windowLayoutFlags = WindowLayout.GetLoadWindowLayoutFlags(newProjectLayoutWasCreated, setLastLoadedLayoutName, keepMainWindow, logErrorsToConsole);
return WindowLayout.TryLoadWindowLayout(path, windowLayoutFlags);
}

public static bool TryLoadWindowLayout(string path, WindowLayout.LoadWindowLayoutFlags flags)
{
if (WindowLayout.LoadWindowLayout_Internal(path, flags))
return true;
WindowLayout.LoadCurrentModeLayout((bool) (UnityEngine.Object) WindowLayout.FindMainWindow());
return false;
}

[Obsolete("Do not use this method. Use TryLoadWindowLayout instead.")]
public static bool LoadWindowLayout(
string path,
bool newProjectLayoutWasCreated,
bool setLastLoadedLayoutName,
bool keepMainWindow,
bool logErrorsToConsole)
{
return WindowLayout.TryLoadWindowLayout(path, newProjectLayoutWasCreated, setLastLoadedLayoutName, keepMainWindow, logErrorsToConsole);
}

[Obsolete("Do not use this method. Use TryLoadWindowLayout instead.")]
public static bool LoadWindowLayout(string path, WindowLayout.LoadWindowLayoutFlags flags)
{
return WindowLayout.TryLoadWindowLayout(path, flags);
}

private static bool LoadWindowLayout_Internal(
string path,
bool newProjectLayoutWasCreated,
bool setLastLoadedLayoutName,
bool keepMainWindow,
bool logErrorsToConsole)
{
WindowLayout.LoadWindowLayoutFlags windowLayoutFlags = WindowLayout.GetLoadWindowLayoutFlags(newProjectLayoutWasCreated, setLastLoadedLayoutName, keepMainWindow, logErrorsToConsole);
return WindowLayout.LoadWindowLayout_Internal(path, windowLayoutFlags);
}

private static WindowLayout.LoadWindowLayoutFlags GetLoadWindowLayoutFlags(
bool newProjectLayoutWasCreated,
bool setLastLoadedLayoutName,
bool keepMainWindow,
bool logErrorsToConsole)
{
WindowLayout.LoadWindowLayoutFlags windowLayoutFlags = WindowLayout.LoadWindowLayoutFlags.SaveLayoutPreferences;
if (newProjectLayoutWasCreated)
windowLayoutFlags |= WindowLayout.LoadWindowLayoutFlags.NewProjectCreated;
if (setLastLoadedLayoutName)
windowLayoutFlags |= WindowLayout.LoadWindowLayoutFlags.SetLastLoadedLayoutName;
if (keepMainWindow)
windowLayoutFlags |= WindowLayout.LoadWindowLayoutFlags.KeepMainWindow;
if (logErrorsToConsole)
windowLayoutFlags |= WindowLayout.LoadWindowLayoutFlags.LogsErrorToConsole;
return windowLayoutFlags;
}

private static bool LoadWindowLayout_Internal(
string path,
WindowLayout.LoadWindowLayoutFlags flags)
{
Console.WriteLine($"[LAYOUT] About to load {path}, keepMainWindow={flags.HasFlag((Enum) WindowLayout.LoadWindowLayoutFlags.KeepMainWindow)}");
if (!Application.isTestRun && !ContainerWindow.CanCloseAll(flags.HasFlag((Enum) WindowLayout.LoadWindowLayoutFlags.KeepMainWindow)))
return false;
ContainerWindow mainWindow1 = ContainerWindow.mainWindow;
bool flag = mainWindow1 != null && mainWindow1.maximized;
ContainerWindow mainWindow2 = ContainerWindow.mainWindow;
Rect rect = mainWindow2 != null ? mainWindow2.position : new Rect();
try
{
ContainerWindow.SetFreezeDisplay(true);
WindowLayout.CloseWindows(flags.HasFlag((Enum) WindowLayout.LoadWindowLayoutFlags.KeepMainWindow));
ContainerWindow containerWindow1 = (ContainerWindow) null;
ContainerWindow containerWindow2 = (ContainerWindow) null;
foreach (ContainerWindow containerWindow3 in Resources.FindObjectsOfTypeAll(typeof (ContainerWindow)))
{
if ((UnityEngine.Object) containerWindow2 == (UnityEngine.Object) null && containerWindow3.showMode == ShowMode.MainWindow)
containerWindow2 = containerWindow3;
else
containerWindow3.Close();
}
UnityEngine.Object[] objectArray = InternalEditorUtility.LoadSerializedFileAndForget(path);
if (objectArray == null || objectArray.Length == 0)
throw new LayoutException("No windows found in layout.");
List<UnityEngine.Object> objectList = new List<UnityEngine.Object>();
for (int index = 0; index < objectArray.Length; ++index)
{
UnityEngine.Object @object = objectArray[index];
int num;
switch (@object)
{
case EditorWindow editorWindow:
if (!(bool) (UnityEngine.Object) editorWindow || !(bool) (UnityEngine.Object) editorWindow.m_Parent || !(bool) (UnityEngine.Object) editorWindow.m_Parent.window)
{
Console.WriteLine("[LAYOUT] Removed un-parented EditorWindow while reading window layout" + $" window #{index}, type={@object.GetType()} instanceID={@object.GetInstanceID()}");
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) editorWindow, true);
continue;
}
goto label_23;
case ContainerWindow containerWindow4:
num = (UnityEngine.Object) containerWindow4.rootView == (UnityEngine.Object) null ? 1 : 0;
break;
default:
num = 0;
break;
}
if (num != 0)
{
containerWindow4.Close();
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) containerWindow4, true);
continue;
}
if (@object is DockArea dockArea && dockArea.m_Panes.Count == 0)
{
dockArea.Close((object) null);
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) dockArea, true);
continue;
}
if (@object is HostView hostView && (UnityEngine.Object) hostView.actualView == (UnityEngine.Object) null)
{
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) hostView, true);
continue;
}
label_23:
objectList.Add(@object);
}
for (int index = 0; index < objectList.Count; ++index)
{
ContainerWindow containerWindow5 = objectList[index] as ContainerWindow;
if ((UnityEngine.Object) containerWindow5 != (UnityEngine.Object) null && containerWindow5.showMode == ShowMode.MainWindow)
{
if ((UnityEngine.Object) containerWindow2 == (UnityEngine.Object) null)
{
containerWindow2 = containerWindow5;
}
else
{
containerWindow2.rootView = containerWindow5.rootView;
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) containerWindow5, true);
containerWindow5 = containerWindow2;
objectList[index] = (UnityEngine.Object) null;
}
if ((double) rect.width != 0.0)
{
containerWindow1 = containerWindow5;
containerWindow1.SetFreeze(true);
containerWindow1.position = rect;
break;
}
break;
}
}
for (int index = 0; index < objectList.Count; ++index)
{
UnityEngine.Object @object = objectList[index];
if ((bool) @object && flags.HasFlag((Enum) WindowLayout.LoadWindowLayoutFlags.NewProjectCreated))
@object.GetType().GetMethod("OnNewProjectLayoutWasCreated", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke((object) @object, (object[]) null);
}
if ((bool) (UnityEngine.Object) containerWindow1)
{
containerWindow1.SetFreeze(true);
containerWindow1.position = rect;
}
if ((bool) (UnityEngine.Object) containerWindow2)
{
if ((UnityEngine.Object) containerWindow2.rootView == (UnityEngine.Object) null || !(bool) (UnityEngine.Object) containerWindow2.rootView)
{
containerWindow2.Close();
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) containerWindow2, true);
throw new LayoutException("Error while reading window layout: no root view on main window.");
}
containerWindow2.SetFreeze(true);
containerWindow2.Show(containerWindow2.showMode, true, true, true);
if ((bool) (UnityEngine.Object) containerWindow1 && containerWindow2.maximized != flag)
containerWindow2.ToggleMaximize();
containerWindow2.SetFreeze(false);
if (flags.HasFlag((Enum) WindowLayout.LoadWindowLayoutFlags.KeepMainWindow))
containerWindow2.m_DontSaveToLayout = false;
}
else if (!flags.HasFlag((Enum) WindowLayout.LoadWindowLayoutFlags.NoMainWindowSupport))
throw new LayoutException("Error while reading window layout: no main window found");
for (int index = 0; index < objectList.Count; ++index)
{
if (!(objectList[index] == (UnityEngine.Object) null))
{
EditorWindow editorWindow = objectList[index] as EditorWindow;
if ((bool) (UnityEngine.Object) editorWindow)
editorWindow.minSize = editorWindow.minSize;
ContainerWindow containerWindow6 = objectList[index] as ContainerWindow;
if ((bool) (UnityEngine.Object) containerWindow6 && (UnityEngine.Object) containerWindow6 != (UnityEngine.Object) containerWindow2)
{
containerWindow6.Show(containerWindow6.showMode, false, true, true);
if (flags.HasFlag((Enum) WindowLayout.LoadWindowLayoutFlags.NoMainWindowSupport))
containerWindow6.m_DontSaveToLayout = (UnityEngine.Object) containerWindow2 != (UnityEngine.Object) null;
}
}
}
PlayModeView maximizedWindow = WindowLayout.GetMaximizedWindow() as PlayModeView;
if ((UnityEngine.Object) maximizedWindow != (UnityEngine.Object) null && maximizedWindow.enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayMaximized)
WindowLayout.Unmaximize((EditorWindow) maximizedWindow);
}
catch (Exception ex)
{
string message = $"Failed to load window layout \"{path}\": {ex}";
if (flags.HasFlag((Enum) WindowLayout.LoadWindowLayoutFlags.LogsErrorToConsole))
Debug.LogError((object) message);
else
Console.WriteLine("[LAYOUT] " + message);
return false;
}
finally
{
ContainerWindow.SetFreezeDisplay(false);
Toolbar.lastLoadedLayoutName = !flags.HasFlag((Enum) WindowLayout.LoadWindowLayoutFlags.SetLastLoadedLayoutName) || !(Path.GetExtension(path) == ".wlt") ? (string) null : Path.GetFileNameWithoutExtension(path);
}
if (flags.HasFlag((Enum) WindowLayout.LoadWindowLayoutFlags.SaveLayoutPreferences))
WindowLayout.SaveDefaultWindowPreferences();
return true;
}

internal static void LoadDefaultLayout()
{
WindowLayout.InitializeLayoutPreferencesFolder();
FileUtil.DeleteFileOrDirectory(WindowLayout.ProjectLayoutPath);
if (WindowLayout.EnsureDirectoryCreated(WindowLayout.ProjectLayoutPath))
{
Console.WriteLine($"[LAYOUT] LoadDefaultLayout: Copying Project Current Layout: {WindowLayout.ProjectLayoutPath} from {WindowLayout.GetDefaultLayoutPath()}");
FileUtil.CopyFileOrDirectory(WindowLayout.GetDefaultLayoutPath(), WindowLayout.ProjectLayoutPath);
}
Debug.Assert(File.Exists(WindowLayout.ProjectLayoutPath));
WindowLayout.LoadWindowLayout_Internal(WindowLayout.ProjectLayoutPath, true, true, false, false);
}

public static void CloseWindows() => WindowLayout.CloseWindows(false);

private static void CloseWindows(bool keepMainWindow)
{
try
{
TooltipView.ForceClose();
}
catch (Exception ex)
{
}
ContainerWindow containerWindow1 = (ContainerWindow) null;
foreach (ContainerWindow containerWindow2 in Resources.FindObjectsOfTypeAll(typeof (ContainerWindow)))
{
try
{
if (containerWindow2.showMode != ShowMode.MainWindow || !keepMainWindow || (UnityEngine.Object) containerWindow1 != (UnityEngine.Object) null)
{
containerWindow2.Close();
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) containerWindow2, true);
}
else
{
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) containerWindow2.rootView, true);
containerWindow2.rootView = (View) null;
containerWindow1 = containerWindow2;
}
}
catch (Exception ex)
{
}
}
UnityEngine.Object[] objectsOfTypeAll1 = Resources.FindObjectsOfTypeAll(typeof (EditorWindow));
if (objectsOfTypeAll1.Length != 0)
{
string str = "";
foreach (EditorWindow editorWindow in objectsOfTypeAll1)
{
str += $"{editorWindow.GetType().Name} {editorWindow.name} {editorWindow.titleContent.text} [{editorWindow.GetInstanceID()}]\r\n";
UnityEngine.Object.DestroyImmediate((UnityEngine.Object) editorWindow, true);
}
}
UnityEngine.Object[] objectsOfTypeAll2 = Resources.FindObjectsOfTypeAll(typeof (View));
if (objectsOfTypeAll2.Length == 0)
return;
foreach (UnityEngine.Object @object in objectsOfTypeAll2)
UnityEngine.Object.DestroyImmediate(@object, true);
}

internal static void SaveWindowLayout(string path) => WindowLayout.SaveWindowLayout(path, true);

public static void SaveWindowLayout(string path, bool reportErrors)
{
if (!WindowLayout.EnsureDirectoryCreated(path))
return;
Console.WriteLine("[LAYOUT] About to save layout " + path);
TooltipView.ForceClose();
UnityEngine.Object[] objectsOfTypeAll1 = Resources.FindObjectsOfTypeAll(typeof (EditorWindow));
UnityEngine.Object[] objectsOfTypeAll2 = Resources.FindObjectsOfTypeAll(typeof (ContainerWindow));
UnityEngine.Object[] objectsOfTypeAll3 = Resources.FindObjectsOfTypeAll(typeof (View));
List<UnityEngine.Object> source = new List<UnityEngine.Object>();
List<ScriptableObject> scriptableObjectList = new List<ScriptableObject>();
foreach (ContainerWindow containerWindow in objectsOfTypeAll2)
{
if (!(bool) (UnityEngine.Object) containerWindow || containerWindow.m_DontSaveToLayout)
scriptableObjectList.Add((ScriptableObject) containerWindow);
else
source.Add((UnityEngine.Object) containerWindow);
}
foreach (View view in objectsOfTypeAll3)
{
if (!(bool) (UnityEngine.Object) view || !(bool) (UnityEngine.Object) view.window || scriptableObjectList.Contains((ScriptableObject) view.window))
scriptableObjectList.Add((ScriptableObject) view);
else
source.Add((UnityEngine.Object) view);
}
foreach (EditorWindow editorWindow in objectsOfTypeAll1)
{
if (!(bool) (UnityEngine.Object) editorWindow || !(bool) (UnityEngine.Object) editorWindow.m_Parent || scriptableObjectList.Contains((ScriptableObject) editorWindow.m_Parent))
{
if (reportErrors && (bool) (UnityEngine.Object) editorWindow)
Debug.LogWarning((object) $"Cannot save invalid window {editorWindow.titleContent.text} {editorWindow} to layout.");
}
else
source.Add((UnityEngine.Object) editorWindow);
}
if (!source.Any<UnityEngine.Object>())
return;
InternalEditorUtility.SaveToSerializedFileAndForget(source.Where<UnityEngine.Object>((Func<UnityEngine.Object, bool>) (o => (bool) o)).ToArray<UnityEngine.Object>(), path, true);
}

internal static void SaveGUI() => UnityEditor.SaveWindowLayout.ShowWindow();

public static void LoadFromFile()
{
string path = EditorUtility.OpenFilePanelWithFilters("Load layout from disk...", "", new string[2]
{
"Layout",
"wlt"
});
if (string.IsNullOrEmpty(path))
return;
WindowLayout.TryLoadWindowLayout(path, false);
}

public static void SaveToFile()
{
string path = EditorUtility.SaveFilePanel("Save layout to disk...", "", "layout", "wlt");
if (string.IsNullOrEmpty(path))
return;
WindowLayout.SaveWindowLayout(path);
EditorUtility.RevealInFinder(path);
}

private static void ResetUserLayouts()
{
foreach (string file in System.IO.Directory.GetFiles(WindowLayout.layoutResourcesPath, "*.wlt"))
{
string dst = FileUtil.CombinePaths(WindowLayout.layoutsDefaultModePreferencesPath, Path.GetFileName(file));
FileUtil.CopyFileIfExists(file, dst, true);
}
if (System.IO.Directory.Exists(WindowLayout.layoutsProjectPath))
System.IO.Directory.Delete(WindowLayout.layoutsProjectPath, true);
if (!System.IO.Directory.Exists(WindowLayout.layoutsCurrentModePreferencesPath))
return;
System.IO.Directory.Delete(WindowLayout.layoutsCurrentModePreferencesPath, true);
}

public static void ResetAllLayouts(bool quitOnCancel = true)
{
if (!Application.isTestRun && !EditorUtility.DisplayDialog("Revert All Window Layouts", "Unity is about to delete all window layouts and restore them to the default settings.", "Continue", quitOnCancel ? "Quit" : "Cancel"))
{
if (!quitOnCancel)
return;
EditorApplication.Exit(0);
}
else
{
if (!ContainerWindow.CanCloseAll(false))
return;
WindowLayout.ResetFactorySettings();
}
}

public static void ResetFactorySettings()
{
WindowLayout.ResetUserLayouts();
ModeService.ChangeModeById("default");
WindowLayout.LoadCurrentModeLayout(false);
WindowLayout.ReloadWindowLayoutMenu();
EditorUtility.Internal_UpdateAllMenus();
ShortcutIntegration.instance.RebuildShortcuts();
}

private struct LayoutViewInfo(object key, float defaultSize, bool usedByDefault)
{
public object key = key;
public string className = string.Empty;
public System.Type type = (System.Type) null;
public bool used = usedByDefault;
public float defaultSize = defaultSize;
public bool isContainer = false;
public IDictionary extendedData = (IDictionary) null;

    public float size => !this.used ? 0.0f : this.defaultSize;
}

[Flags]
public enum LoadWindowLayoutFlags
{
None = 0,
NewProjectCreated = 1,
SetLastLoadedLayoutName = 2,
KeepMainWindow = 4,
LogsErrorToConsole = 8,
NoMainWindowSupport = 16, // 0x00000010
SaveLayoutPreferences = 32, // 0x00000020
}
}
