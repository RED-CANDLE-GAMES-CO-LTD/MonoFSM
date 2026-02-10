using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MonoFSM.Core;
using MonoFSMEditor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static MonoFSM.Editor.MonoAnimationWindow.GUIExtension;
using static MonoFSM.GUIExtensions.EventExtensions;

public class AnimationWindowNavbar
{
    bool isLastFocused = false;
    private static Dictionary<int, string> controlIdToName = new Dictionary<int, string>(); // 追踪控制項ID對應名稱
    private int searchFieldControlID = -1; // 固定的搜尋欄位ID
    public AnimationWindow window;
    public bool isSearchActive;
    private string searchText
    {
        get => _searchText;
        set
        {
            // Debug.Log("Search text changed: " + value);
            if (_searchText != value)
            {
                _searchText = value;
                // PerformSearch();
            }
        }
    }
    string _searchText = "";
    private float searchAnimationT;
    private float searchAnimationDerivative;
    private bool animatingSearch;
    private float searchAnimationDistance = 120;
    private List<AnimationClip> currentMatches = new List<AnimationClip>();
    private int currentMatchIndex = -1;
    private bool showDropdown = false;
    private Vector2 dropdownScrollPos;
    private bool isNavigatingDropdown = false;
    private const int maxDropdownItems = 8;
    private const float dropdownItemHeight = 20f;

    // private bool shouldFocusOnNextRepaint = false;
    private Rect pendingDropdownRect;
    private bool shouldDrawDropdown = false;
    private Animator currentAnimator; // 追踪當前的Animator Component
    private RuntimeAnimatorController currentController; // 追踪當前的AnimatorController

    private bool isCreateOptionSelected
    {
        get => _isCreateOptionSelected;
        set
        {
            // Debug.Log("Create option selected: " + value);
            _isCreateOptionSelected = value;
        }
    } // 追蹤是否選中了創建選項
    bool _isCreateOptionSelected = false; // 默認為未選中創建選項

    public AnimationWindowNavbar(EditorWindow window)
    {
        var animationWindow = window as AnimationWindow;
        ; //EditorWindow.GetWindow<AnimationWindow>(false);
        this.window = animationWindow;
    }

    public void HandleDropdownEventsFirst()
    {
        // 如果有dropdown顯示，在原始AnimationWindow處理事件之前先檢查dropdown事件
        if (!showDropdown)
            return;

        var dropdownHeight = Math.Min(
            currentMatches.Count * dropdownItemHeight,
            maxDropdownItems * dropdownItemHeight
        );
        var dropdownRect = new Rect(
            pendingDropdownRect.x,
            pendingDropdownRect.y,
            pendingDropdownRect.width + 50,
            dropdownHeight
        );

        // 處理鍵盤導航事件
        if (Event.current.type == EventType.KeyDown)
        {
            switch (Event.current.keyCode)
            {
                case KeyCode.Escape:
                    // showDropdown = false;
                    isNavigatingDropdown = false;
                    PerformSearch();
                    Event.current.Use();
                    return;

                case KeyCode.DownArrow:
                    // Debug.Log("Navigating down in dropdown");
                    // isNavigatingDropdown = true;
                    NavigateDown();
                    Event.current.Use();
                    return;

                case KeyCode.UpArrow:
                    // Debug.Log("Navigating up in dropdown");
                    // isNavigatingDropdown = true;
                    NavigateUp();
                    Event.current.Use();
                    return;

                case KeyCode.Return:
                    if (isCreateOptionSelected && !string.IsNullOrEmpty(searchText))
                    {
                        // 選中了創建選項
                        CreateAnimationClipAndState(searchText);
                    }
                    else if (currentMatches.Any())
                    {
                        // 有選取用選取的，沒選取則用第一個（最相關的）
                        var idx = isNavigatingDropdown ? currentMatchIndex : 0;
                        SetAnimationWindowClip(currentMatches[idx]);
                        searchText = "";
                        showDropdown = false;
                        isNavigatingDropdown = false;
                        isCreateOptionSelected = false;
                        // 讓Unity自然處理焦點變化，避免強制清除
                        // GUIUtility.keyboardControl = 0;
                    }
                    Event.current.Use();
                    return;

                case KeyCode.Tab:
                    if (currentMatches.Any())
                    {
                        searchText = currentMatches[currentMatchIndex].name;
                        // showDropdown = false;
                        isNavigatingDropdown = false;
                        Event.current.Use();
                    }
                    return;
            }
        }

        // 如果滑鼠在dropdown區域內且是點擊事件，先處理dropdown點擊
        if (
            dropdownRect.Contains(Event.current.mousePosition)
            && Event.current.type == EventType.MouseDown
        )
        {
            Debug.Log("Mouse down on dropdown area");
            // 用座標回推計算點擊的項目索引
            var relativeY = Event.current.mousePosition.y - (dropdownRect.y + 1); // dropdown內部的相對Y座標
            var clickedIndex = Mathf.FloorToInt(relativeY / dropdownItemHeight);
            Debug.Log("Clicked index: " + clickedIndex);
            // 確保索引在有效範圍內
            if (
                clickedIndex >= 0
                && clickedIndex < Math.Min(currentMatches.Count, maxDropdownItems)
            )
            {
                var clip = currentMatches[clickedIndex];
                Debug.Log(
                    $"Early selected: {clip.name} ({clickedIndex + 1}/{currentMatches.Count})"
                );
                currentMatchIndex = clickedIndex;
                SetAnimationWindowClip(currentMatches[currentMatchIndex]);

                showDropdown = false;
                isNavigatingDropdown = false;
                isCreateOptionSelected = false;
                // 不要強制清除焦點，讓搜尋文字清空後自然失去焦點
                // GUIUtility.keyboardControl = 0;
                searchText = ""; // 清除搜尋文字
            }

            Event.current.Use(); // 攔截事件
        }
        else if (Event.current.type == EventType.MouseDown)
        {
            Debug.Log("Mouse down outside dropdown area, clearing search");
            CloseSearch();
            Event.current.Use();
        }
    }

    void CloseSearch()
    {
        searchText = "";
        // Debug.Log("Search cleared");
        PerformSearch();
        showDropdown = false;
        isNavigatingDropdown = false;
        isCreateOptionSelected = false;
        // 不要立即清除鍵盤控制，讓Unity自然處理焦點
        // GUIUtility.keyboardControl = 0;
        isSearchActive = false;
    }

    public void OnGUI(Rect navbarRect)
    {
        // currentController =
        //     currentAnimator?.runtimeAnimatorController as AnimatorController;

        void UpdateState()
        {
            if (!curEvent.isLayout)
                return;

            // 檢查焦點狀態
            var isWindowFocused = window == EditorWindow.focusedWindow;
            if (!isWindowFocused && isSearchActive)
            {
                CloseSearch();
            }

            // if (!isSearchActive && isWindowFocused && GUI.GetNameOfFocusedControl() == "AnimationSearchFilter")
            //     isSearchActive = true;
            if (isSearchActive)
            {
                // Debug.Log("Search is active, focusing on search field");
                EditorGUI.FocusTextInControl("AnimationSearchFilter");
                // isSearchActive = false;
            }
        }

        void background()
        {
            var backgroundColor = Greyscale(isDarkTheme ? .235f : .8f);
            var lineColor = Greyscale(isDarkTheme ? .13f : .58f);
            EditorGUI.DrawRect(navbarRect, backgroundColor);
            // navbarRect.Draw(backgroundColor);
            EditorGUI.DrawRect(navbarRect.SetHeightFromBottom(1).MoveY(1), lineColor);
            // navbarRect.SetHeightFromBottom(1).MoveY(1).Draw(lineColor);
        }

        // float AnimatorButton(float startX)
        // {
        //     if (searchAnimationT == 1) return startX;
        //
        //     var width = 56f;
        //     var buttonRect = navbarRect.SetWidth(width).MoveX(startX);
        //     var iconName = "d_UnityEditor.Graphs.AnimatorControllerTool";
        //     var iconSize = 16;
        //     var colorNormal = Greyscale(isDarkTheme ? .75f : .2f);
        //     var colorHovered = Greyscale(isDarkTheme ? 1f : .2f);
        //     var colorPressed = Greyscale(isDarkTheme ? .75f : .5f);
        //
        //     if (IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed))
        //     {
        //         // 打開 Animator Window
        //         EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
        //     }
        //
        //     return startX + width;
        // }

        float AnimatorObjectField(float startX)
        {
            if (searchAnimationT == 1)
                return startX;

            // 更新當前的 Animator
            var selectedAnimator = Selection.activeGameObject?.GetComponentInParent<Animator>(true);
            if (selectedAnimator != currentAnimator)
            {
                currentAnimator = selectedAnimator;
                // 同時更新 AnimatorController
                currentController =
                    currentAnimator?.runtimeAnimatorController;
            }

            // var width = 120f;
            // var fieldRect = navbarRect.SetWidth(width).MoveX(startX);
            // var newAnimator = EditorGUI.ObjectField(fieldRect, currentAnimator, typeof(Animator), true) as Animator;
            // 響應式寬度
            var availableWidth = window.position.width;
            var width = availableWidth > 450 ? 160f : Math.Min(150f, availableWidth * 0.35f);

            // 使用圖標和更緊湊的設計
            var iconRect = new Rect(startX, 5, 18, 18);
            var icon = EditorGUIUtility.IconContent("d_UnityEditor.Graphs.AnimatorControllerTool");
            GUI.DrawTexture(
                iconRect,
                icon.image,
                ScaleMode.ScaleToFit,
                true,
                0,
                Greyscale(isDarkTheme ? 0.8f : 0.4f),
                0,
                0
            );

            // 繪製欄位，使用統一風格
            var objectFieldRect = new Rect(startX + 22, 4, width - 22, 20);

            // 自訂欄位背景
            var fieldBgColor = isDarkTheme
                ? new Color(0.15f, 0.15f, 0.15f, 0.5f)
                : new Color(1f, 1f, 1f, 0.3f);
            EditorGUI.DrawRect(objectFieldRect, fieldBgColor);

            //FIXME: readonly
            var newAnimator =
                EditorGUI.ObjectField(objectFieldRect, currentAnimator, typeof(Animator), true)
                as Animator;

            return startX + width;
        }

        float AnimatorControllerField(float startX)
        {
            if (searchAnimationT == 1)
                return startX;

            // 響應式寬度
            var availableWidth = window.position.width;
            var baseWidth = availableWidth > 450 ? 200f : Math.Min(180f, availableWidth * 0.4f);

            // 檢查是否需要顯示建立按鈕
            var shouldShowCreateButton = currentController == null && currentAnimator != null;
            var buttonWidth = shouldShowCreateButton ? 24f : 0f;
            var totalWidth = baseWidth + buttonWidth + (shouldShowCreateButton ? 2f : 0f); // 2f為間距

            // 繪製圖標（點擊開啟 Animator Window）
            var iconRect = new Rect(startX, 4, 20, 20);
            if (IconButton(iconRect, "d_AnimatorController Icon", 16,
                    Greyscale(isDarkTheme ? 0.8f : 0.4f),
                    Greyscale(isDarkTheme ? 1f : 0.2f),
                    Greyscale(isDarkTheme ? 0.6f : 0.5f)))
            {
                EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
            }

            // 計算ObjectField的寬度（如果有建立按鈕，需要預留空間）
            var objectFieldWidth = shouldShowCreateButton
                ? baseWidth - 22 - buttonWidth - 2f
                : baseWidth - 22;
            var objectFieldRect = new Rect(startX + 22, 4, objectFieldWidth, 20);

            // 繪製欄位背景
            var fieldBgColor = isDarkTheme
                ? new Color(0.15f, 0.15f, 0.15f, 0.5f)
                : new Color(1f, 1f, 1f, 0.3f);
            EditorGUI.DrawRect(objectFieldRect, fieldBgColor);

            // 繪製ObjectField
            var newController =
                EditorGUI.ObjectField(
                    objectFieldRect,
                    currentController,
                    typeof(RuntimeAnimatorController),
                    false
                ) as RuntimeAnimatorController;

            // 繪製建立按鈕（當controller為null但animator存在時）
            if (shouldShowCreateButton)
            {
                var buttonRect = new Rect(objectFieldRect.xMax + 2f, 4, buttonWidth, 20);

                // 調試：繪製按鈕區域（可選）
                // EditorGUI.DrawRect(buttonRect, Color.red * 0.3f); // 取消註解來查看按鈕位置

                if (IconButton(buttonRect, "d_CreateAddNew", 16,
                        Greyscale(isDarkTheme ? .75f : .2f),
                        Greyscale(isDarkTheme ? 1f : .2f),
                        Greyscale(isDarkTheme ? .75f : .5f)))
                {
                    Debug.Log("Create button clicked!"); // 加入調試信息
                    CreateAnimatorControllerForCurrentAnimator();
                }
            }

            // 處理controller變更
            if (newController != currentController)
            {
                currentController = newController;

                // 如果當前有 Animator，更新它的 controller
                // if (currentAnimator != null)
                // {
                //     currentAnimator.runtimeAnimatorController = newController;
                //     // 重新執行搜尋以更新匹配項目
                //     PerformSearch();
                // }
            }

            return startX + totalWidth;
        }

        float AnimationClipField(float startX)
        {
            if (searchAnimationT == 1)
                return startX;

            // 響應式寬度
            var availableWidth = window.position.width;
            var width = availableWidth > 600 ? 180f : Math.Min(160f, availableWidth * 0.3f);

            // 獲取當前 Animation Window 中選中的 clip
            var currentClip = window.animationClip;

            // 繪製圖標
            var iconRect = new Rect(startX, 5, 18, 18);
            var icon = EditorGUIUtility.IconContent("d_AnimationClip Icon");
            GUI.DrawTexture(
                iconRect,
                icon.image,
                ScaleMode.ScaleToFit,
                true,
                0,
                Greyscale(isDarkTheme ? 0.8f : 0.4f),
                0,
                0
            );

            // 繪製欄位
            var objectFieldRect = new Rect(startX + 22, 4, width - 22, 20);

            // 自訂欄位背景
            var fieldBgColor = isDarkTheme
                ? new Color(0.15f, 0.15f, 0.15f, 0.5f)
                : new Color(1f, 1f, 1f, 0.3f);
            EditorGUI.DrawRect(objectFieldRect, fieldBgColor);

            var newClip =
                EditorGUI.ObjectField(objectFieldRect, currentClip, typeof(AnimationClip), false)
                as AnimationClip;

            // 處理 clip 變更
            // if (newClip != currentClip)
            // {
            //     if (newClip != null)
            //     {
            //         // 設置新的 clip 到 Animation Window
            //         SetAnimationWindowClip(newClip);
            //     }
            //     else
            //     {
            //         // 清除 clip
            //         Debug.Log("Clearing Animation Window clip");
            //         window.animationClip = null;
            //         window.Repaint();
            //     }
            // }

            return startX + width;
        }

        float searchButton(float startX)
        {
            if (searchAnimationT == 1)
                return startX;

            var width = 28f;
            var buttonRect = navbarRect.SetWidth(width).MoveX(startX).SetHeight(26);

            var iconName = "Search_";
            var iconSize = 16;
            var colorNormal = Greyscale(isDarkTheme ? .75f : .2f);
            var colorHovered = Greyscale(isDarkTheme ? 1f : .2f);
            var colorPressed = Greyscale(isDarkTheme ? .75f : .5f);

            if (IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed))
            {
                isSearchActive = true;
                // shouldFocusOnNextRepaint = true;
            }

            return startX + width;
        }

        void searchOnCtrlF()
        {
            if (!curEvent.isKeyDown)
                return;
            if (!curEvent.holdingCmd && !curEvent.holdingCtrl)
                return;
            if (curEvent.keyCode != KeyCode.F)
                return;

            isSearchActive = true;
            // shouldFocusOnNextRepaint = true;
            curEvent.Use();
        }

        Rect searchField()
        {
            if (searchAnimationT == 0)
                return new Rect();

            var searchFieldRect = navbarRect
                .SetHeightFromMid(18)
                .AddWidth(-60)
                .SetWidth(Math.Min(300f, window.position.width - 120))
                .Move(5, 0);
            // .SetWidthFromRight(Math.Min(300f, window.position.width - 120)).Move(5, 0);

            // 使用固定的控制項ID來確保焦點穩定性
            // if (searchFieldControlID == -1)
            // {
            //     searchFieldControlID = GUIUtility.GetControlID("AnimationSearchFilter".GetHashCode(), FocusType.Keyboard);
            //     Debug.Log($"Assigned fixed searchField controlID: {searchFieldControlID}");
            // }

            GUI.SetNextControlName("AnimationSearchFilter");
            var newSearchText = GUI.TextField(
                searchFieldRect,
                searchText,
                "ToolbarSearchTextField"
            );

            // 記錄和保護我們的TextField焦點
            var currentControlID = GUIUtility.keyboardControl;
            var currentFocusedName = GUI.GetNameOfFocusedControl();

            if (currentFocusedName == "AnimationSearchFilter" && currentControlID != 0)
            {
                controlIdToName[currentControlID] = "AnimationSearchFilter";
                searchFieldControlID = currentControlID; // 更新我們記錄的ID
            }

            if (newSearchText != searchText)
            {
                // Debug.Log("searchText: " + searchText + " -> " + newSearchText);
                searchText = newSearchText;
                isNavigatingDropdown = false;
                PerformSearch();
            }

            var currentFocus = GUI.GetNameOfFocusedControl();
            var isFocused = currentFocus == "AnimationSearchFilter";

            // 簡化焦點檢查 - 只記錄真正的變化，不要過度干預
            if (Event.current.type == EventType.Layout && isLastFocused != isFocused)
            {
                var keyboardControlId = GUIUtility.keyboardControl;
                var controlInfo = GetControlInfo(keyboardControlId);

                // Debug.Log($"Focus change: {isLastFocused} -> {isFocused} (control: '{currentFocus}', keyboardControlId: {keyboardControlId}, controlInfo: {controlInfo})");
                isLastFocused = isFocused;
            }
            // 顯示dropdown如果有匹配項目，或者有搜尋文字（用於顯示創建選項），或者正在導航
            showDropdown =
                (currentMatches.Any() || isFocused || isNavigatingDropdown) && isSearchActive;
            // 處理鍵盤事件
            // KeyNavigate();

            // 準備繪製下拉選單（在最後繪製）
            if (showDropdown)
            {
                // shouldDrawDropdown = true;
                pendingDropdownRect = searchFieldRect;
            }

            return searchFieldRect;
        }

        void DrawDropdownAtTop(Rect searchFieldRect)
        {
            // 使用負數depth確保dropdown在最上層
            var originalDepth = GUI.depth;
            GUI.depth = -1000;

            // 計算dropdown高度，包含可能的"Create New Clip..."項目
            var hasCreateOption = ShouldShowCreateOption();
            var actualItemHeight = 22f; // 更緊湊的高度

            // 計算實際要顯示的現有項目數量（最多maxDropdownItems個）
            var visibleMatchesCount = Math.Min(currentMatches.Count, maxDropdownItems);
            var dropdownHeight = visibleMatchesCount * actualItemHeight;

            // 如果有搜尋文字，總是加上創建選項的高度
            if (hasCreateOption)
            {
                if (currentMatches.Any())
                {
                    // 如果有現有項目，加上分隔線和創建選項
                    dropdownHeight += actualItemHeight + 1; // +1 for separator
                }
                else
                {
                    // 如果沒有現有項目，只有創建選項
                    dropdownHeight = actualItemHeight;
                }
            }

            var dropdownRect = new Rect(
                searchFieldRect.x,
                searchFieldRect.yMax + 1,
                searchFieldRect.width + 50,
                dropdownHeight + 4 // 增加一點內邊距
            );

            // 繪製陰影 (更輕的陰影)
            var shadowRect = dropdownRect.Move(1, 1);
            EditorGUI.DrawRect(shadowRect, new Color(0, 0, 0, 0.15f));

            // 使用Unity風格的背景和邊框 - 修復淺色主題
            var backgroundColor = isDarkTheme
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.8f, 0.8f, 0.8f);
            var borderColor = isDarkTheme
                ? new Color(0.13f, 0.13f, 0.13f)
                : new Color(0.6f, 0.6f, 0.6f);

            // 先繪製邊框
            EditorGUI.DrawRect(dropdownRect, borderColor);
            // 再繪製內部背景（留出1px邊框）
            var innerRect = new Rect(
                dropdownRect.x + 1,
                dropdownRect.y + 1,
                dropdownRect.width - 2,
                dropdownRect.height - 2
            );
            EditorGUI.DrawRect(innerRect, backgroundColor);

            // 繪製選項
            var itemRect = new Rect(
                dropdownRect.x + 1,
                dropdownRect.y + 2,
                dropdownRect.width - 2,
                actualItemHeight
            );

            // 如果有匹配項目，繪製它們
            if (currentMatches.Any())
            {
                for (var i = 0; i < Math.Min(currentMatches.Count, maxDropdownItems); i++)
                {
                    var clip = currentMatches[i];
                    var isSelected = i == currentMatchIndex && !isCreateOptionSelected;
                    var isHovered =
                        itemRect.Contains(Event.current.mousePosition) && !isNavigatingDropdown;

                    // Unity風格的選中和懸停效果
                    if (isSelected)
                    {
                        var selectedColor = new Color(0.24f, 0.49f, 0.89f); // Unity藍色，深淺主題都一樣
                        EditorGUI.DrawRect(itemRect, selectedColor);
                    }
                    else if (isHovered)
                    {
                        // var hoverColor = isDarkTheme ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.9f, 0.9f, 0.9f);
                        var hoverColor = new Color(0.24f, 0.49f, 0.89f); // Unity藍色，深淺主題都一樣;
                        EditorGUI.DrawRect(itemRect, hoverColor);
                    }

                    // 繪製clip名稱，使用Unity風格
                    var labelStyle = new GUIStyle(EditorStyles.label);
                    labelStyle.normal.textColor =
                        isHovered ? Color.white
                        : isDarkTheme ? new Color(0.85f, 0.85f, 0.85f)
                        : new Color(0f, 0f, 0f);
                    labelStyle.fontSize = 12;
                    labelStyle.padding.left = 8;

                    GUI.Label(itemRect, clip.name, labelStyle);
                    itemRect.y += actualItemHeight;
                }

                // 如果需要顯示創建選項，在現有項目下方加入分隔線和創建選項
                if (hasCreateOption)
                {
                    // 繪製分隔線
                    var separatorRect = new Rect(
                        dropdownRect.x + 1,
                        itemRect.y,
                        dropdownRect.width - 2,
                        1
                    );
                    // separatorRect.Draw(borderColor);
                    EditorGUI.DrawRect(separatorRect, borderColor);
                    itemRect.y += 1;

                    // 繪製"Create New Clip..."選項
                    DrawCreateNewClipOption(itemRect, searchText);
                }
            }
            // 如果沒有匹配項目但需要顯示創建選項，只顯示創建選項
            else if (hasCreateOption)
            {
                DrawCreateNewClipOption(itemRect, searchText);
            }

            // 恢復原始depth
            GUI.depth = originalDepth;
        }

        void closeSearchButton(Rect searchFieldRect)
        {
            if (searchAnimationT == 0)
                return;

            // 位置在 searchField 的右邊
            var buttonRect = new Rect(searchFieldRect.xMax + 2, navbarRect.y, 26, 26);
            var iconName = "CrossIcon";
            var iconSize = 12;
            var colorNormal = Greyscale(isDarkTheme ? .7f : .4f);
            var colorHovered = Greyscale(isDarkTheme ? 1f : .2f);
            var colorPressed = Greyscale(isDarkTheme ? .6f : .5f);

            if (IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed))
            {
                CloseSearch();
            }
        }
        void searchAnimation()
        {
            if (!curEvent.isLayout)
                return;

            var lerpSpeed = 8f;

            if (isSearchActive)
                // VUtils.MathUtil.SmoothDamp(ref searchAnimationT, 1, lerpSpeed, ref searchAnimationDerivative, editorDeltaTime);
                searchAnimationT = 1;
            else
                // VUtils.MathUtil.SmoothDamp(ref searchAnimationT, 0, lerpSpeed, ref searchAnimationDerivative, editorDeltaTime);
                searchAnimationT = 0;
            //TODO: animation

            if (isSearchActive && searchAnimationT > .99f)
                searchAnimationT = 1;

            if (!isSearchActive && searchAnimationT < .01f)
                searchAnimationT = 0;

            animatingSearch = searchAnimationT != 0 && searchAnimationT != 1;
        }

        void buttonsGroup()
        {
            SetGUIColor(Greyscale(1, (1 - searchAnimationT).Pow(2)));

            GUI.BeginGroup(
                window.position.SetPos(0, 0).MoveX(-searchAnimationDistance * searchAnimationT)
            );

            // selectorButton();
            var x = 5f; // 起始位置
            // x = animatorButton(x);
            x = searchButton(x);
            var separatorX = x;
            var separatorRect = new Rect(separatorX, 4, 1, 18);
            var separatorColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            EditorGUI.DrawRect(separatorRect, separatorColor);

            x = AnimatorObjectField(x + 12);
            x = AnimatorControllerField(x + 12);
            x = AnimationClipField(x + 12);

            searchOnCtrlF();

            GUI.EndGroup();
            ResetGUIColor();
        }

        void searchButtonAndFieldSection()
        {
            SetGUIColor(Greyscale(1, searchAnimationT.Pow(2)));
            GUI.BeginGroup(
                window.position.SetPos(0, 0).MoveX(searchAnimationDistance * (1 - searchAnimationT))
            );

            var searchFieldRect = searchField();
            closeSearchButton(searchFieldRect);
            closeSearchOnEsc();

            GUI.EndGroup();
            ResetGUIColor();
        }

        UpdateState();
        background();

        searchAnimation();
        buttonsGroup();
        searchButtonAndFieldSection();

        // 在最後繪製dropdown，確保它在所有內容之上
        if (showDropdown)
        {
            // Debug.Log("Drawing dropdown at: " + pendingDropdownRect);
            DrawDropdownAtTop(pendingDropdownRect);
        }

        void closeSearchOnEsc()
        {
            if (!isSearchActive)
                return;
            if (curEvent.keyCode != KeyCode.Escape)
                return;
            CloseSearch();
        }

        if (animatingSearch)
            window.Repaint();
    }

    private bool ShouldShowCreateOption()
    {
        if (string.IsNullOrEmpty(searchText))
            return false;

        // 檢查是否有完全匹配的動畫名稱
        var hasExactMatch = currentMatches.Any(clip =>
            string.Equals(clip.name, searchText, StringComparison.OrdinalIgnoreCase)
        );

        return !hasExactMatch;
    }

    private void DrawCreateNewClipOption(Rect itemRect, string clipName)
    {
        var isHovered = itemRect.Contains(Event.current.mousePosition) && !isNavigatingDropdown;
        var isSelected = isCreateOptionSelected;
        // Debug.Log("Drawing create option for: " + clipName);
        // 選中和懸停效果
        if (isSelected)
        {
            var selectedColor = new Color(0.24f, 0.49f, 0.89f); // Unity藍色
            EditorGUI.DrawRect(itemRect, selectedColor);
            // itemRect.Draw(selectedColor);
            // Debug.Log($"Selected create option: {clipName}");
        }
        else if (isHovered)
        {
            var hoverColor = isDarkTheme
                ? new Color(0.1f, 0.1f, 0.1f)
                : new Color(0.9f, 0.9f, 0.9f);
            EditorGUI.DrawRect(itemRect, hoverColor);
            // itemRect.Draw(hoverColor);
            // Debug.Log($"Hovered create option: {clipName}");
        }

        // 處理點擊
        // if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
        // {
        //     CreateAnimationClipAndState(clipName);
        //     Event.current.Use();
        // }

        // 處理滑鼠懸停（更新選中狀態）
        // if (isHovered && !isNavigatingDropdown)
        // {
        //     isCreateOptionSelected = true;
        //     currentMatchIndex = -1; // 重置項目索引
        // }

        // 繪製文字，使用特殊樣式表示這是創建選項
        var labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor =
            isHovered ? Color.white
            : isDarkTheme ? new Color(0.7f, 0.7f, 0.7f)
            : new Color(0.1f, 0.1f, 0.1f);
        labelStyle.fontSize = 11;
        labelStyle.padding.left = 8;
        labelStyle.fontStyle = FontStyle.Italic;

        GUI.Label(itemRect, $"Create New Clip '{clipName}'...", labelStyle);
    }

    private void PerformSearch()
    {
        try
        {
            // if (string.IsNullOrEmpty(searchText))
            // {
            //     // ClearSearch();
            //     return;
            // }

            // 嘗試找到匹配的Animation Clip
            currentMatches = FindMatchingAnimationClips(searchText);
            currentMatchIndex = -1;

            // if (currentMatches.Any())
            // {
            //     // 不自動設置clip，讓用戶從下拉選單選擇
            //     Debug.Log($"Found {currentMatches.Count} matches for '{searchText}'");
            // }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during animation search: {e.Message}");
        }
    }

    private void NavigateDown()
    {
        var hasCreateOption = ShouldShowCreateOption();
        var totalItems = currentMatches.Count + (hasCreateOption ? 1 : 0);

        if (totalItems == 0)
            return;

        // 確保進入導航模式
        if (!isNavigatingDropdown)
        {
            isNavigatingDropdown = true;
            // 第一次導航：選擇第一個項目
            if (currentMatches.Any())
            {
                currentMatchIndex = 0;
                isCreateOptionSelected = false;
            }
            else if (hasCreateOption)
            {
                isCreateOptionSelected = true;
            }
            return;
        }

        // 已在導航模式中
        if (isCreateOptionSelected)
        {
            // 從創建選項往下：循環到第一個現有項目（如果有的話）
            if (currentMatches.Any())
            {
                isCreateOptionSelected = false;
                currentMatchIndex = 0;
            }
            // 沒有現有項目則保持在創建選項
        }
        else if (currentMatches.Any())
        {
            // 在現有項目中：往下一個，或移到創建選項，或循環到第一個
            if (currentMatchIndex < currentMatches.Count - 1)
            {
                currentMatchIndex++;
            }
            else if (hasCreateOption)
            {
                isCreateOptionSelected = true;
            }
            else
            {
                currentMatchIndex = 0; // 循環到第一個
            }
        }
    }

    private void NavigateUp()
    {
        var hasCreateOption = ShouldShowCreateOption();
        var totalItems = currentMatches.Count + (hasCreateOption ? 1 : 0);

        if (totalItems == 0)
            return;

        // 確保進入導航模式
        if (!isNavigatingDropdown)
        {
            isNavigatingDropdown = true;
            // 第一次導航：選擇最後一個項目
            if (hasCreateOption)
            {
                isCreateOptionSelected = true;
            }
            else if (currentMatches.Any())
            {
                currentMatchIndex = currentMatches.Count - 1;
                isCreateOptionSelected = false;
            }
            return;
        }

        // 已在導航模式中
        if (isCreateOptionSelected)
        {
            // 從創建選項往上：移到最後一個現有項目（如果有的話）
            if (currentMatches.Any())
            {
                isCreateOptionSelected = false;
                currentMatchIndex = currentMatches.Count - 1;
            }
            // 沒有現有項目則保持在創建選項
        }
        else if (currentMatches.Any())
        {
            // 在現有項目中：往上一個，或移到創建選項，或循環到最後一個
            if (currentMatchIndex > 0)
            {
                currentMatchIndex--;
            }
            else if (hasCreateOption)
            {
                isCreateOptionSelected = true;
            }
            else
            {
                currentMatchIndex = currentMatches.Count - 1; // 循環到最後一個
            }
        }
    }

    private List<AnimationClip> FindMatchingAnimationClips(string searchTerm)
    {
        var clips = new List<AnimationClip>();

        try
        {
            // 嘗試從當前選擇的GameObject獲取Animator
            if (Selection.activeGameObject != null)
            {
                var animator = Selection.activeGameObject.GetComponentInParent<Animator>(true);
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    var allClips = animator.runtimeAnimatorController.animationClips;
                    if (string.IsNullOrEmpty(searchTerm))
                    {
                        // 如果沒有搜尋詞，返回所有clips
                        clips.AddRange(allClips.Where(clip => clip != null));
                    }
                    else
                    {
                        // 根據搜尋詞過濾clips
                        clips.AddRange(
                            allClips.Where(clip =>
                                clip != null
                                && clip.name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase)
                                    >= 0
                            )
                        );
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error finding animation clips: {e.Message}");
        }

        // Debug.Log("clips count" + clips.Count);

        // 按字母排序
        // return clips.OrderBy(c => c.name).ToList();

        // 按搜尋詞相似度排序（完全匹配 > 開頭匹配 > 包含位置靠前）
        if (string.IsNullOrEmpty(searchTerm))
            return clips.OrderBy(c => c.name).ToList();

        var term = searchTerm.ToLowerInvariant();
        return clips.OrderBy(c =>
        {
            var name = c.name.ToLowerInvariant();
            if (name == term) return 0; // 完全匹配
            if (name.StartsWith(term)) return 1; // 開頭匹配
            var idx = name.IndexOf(term, StringComparison.Ordinal);
            if (idx >= 0) return 2 + idx; // 包含，位置越前越優先
            return 1000; // fallback
        }).ThenBy(c => c.name).ToList();
    }

    private void SetAnimationWindowClip(AnimationClip clip)
    {
        try
        {
            // 使用reflection來設置Animation Window的clip
            var animationWindow = window;

            // 確保選擇了正確的GameObject
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning(
                    "No GameObject selected. Please select a GameObject with an Animator component."
                );
                return;
            }

            var animator = Selection.activeGameObject.GetComponentInParent<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning("Selected GameObject doesn't have an Animator component.");
                return;
            }

            // 設置Animation Window的clip
            // var animationWindowType = typeof(Editor).Assembly.GetType("UnityEditor.AnimationWindow");
            // var clipProperty = animationWindow.animationClip;// animationWindowType.GetProperty("animationClip");

            Debug.Log($"Setting Animation Window clip to: {clip.name}");
            animationWindow.animationClip = clip;
            // 重新繪製窗口
            animationWindow.previewing = true;
            animationWindow.Repaint();
            CloseSearch();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error setting animation clip: {e.Message}");
        }
    }

    private void CreateAnimationClipAndState(string stateName)
    {
        try
        {
            // 檢查是否有選中的GameObject
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog(
                    "錯誤",
                    "請先選擇一個有Animator組件的GameObject",
                    "確定"
                );
                return;
            }

            var animator = Selection.activeGameObject.GetComponentInParent<Animator>();
            if (animator == null)
            {
                EditorUtility.DisplayDialog("錯誤", "選中的GameObject沒有Animator組件", "確定");
                return;
            }

            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null)
            {
                EditorUtility.DisplayDialog(
                    "錯誤",
                    "Animator沒有Controller或Controller不是AnimatorController類型",
                    "確定"
                );
                return;
            }

            // 使用Reflection調用MonoFSM的AnimatorAssetUtility（因為可能不在同一個Assembly）
            var animatorUtilityType = Type.GetType(
                "MonoFSM.AnimatorUtility.AnimatorAssetUtility, MonoFSM.Core"
            );
            if (animatorUtilityType != null)
            {
                var addStateMethod = animatorUtilityType.GetMethod(
                    "AddStateAndCreateClipToLayerIndex",
                    BindingFlags.Public | BindingFlags.Static
                );

                if (addStateMethod != null)
                {
                    addStateMethod.Invoke(null, new object[] { controller, 0, stateName });
                }
                else
                {
                    // 回退到手動建立
                    CreateStateAndClipManually(controller, stateName);
                }
            }
            else
            {
                // 如果找不到AnimatorAssetUtility，手動建立
                CreateStateAndClipManually(controller, stateName);
            }

            // 標記資產為已修改
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 重新執行搜尋，現在應該能找到新建立的clip
            PerformSearch();

            Debug.Log($"Successfully created animation clip and state: {stateName}");

            // 如果找到了新建立的clip，自動選擇它
            if (currentMatches.Any())
            {
                currentMatchIndex = 0;
                SetAnimationWindowClip(currentMatches[currentMatchIndex]);

                // 清除搜尋並關閉dropdown
                searchText = "";
                showDropdown = false;
                isNavigatingDropdown = false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating animation clip and state: {e.Message}");
            EditorUtility.DisplayDialog("錯誤", $"建立動畫失敗：{e.Message}", "確定");
        }
    }

    private void CreateStateAndClipManually(AnimatorController controller, string stateName)
    {
        // 獲取基礎層的狀態機
        var stateMachine = controller.layers[0].stateMachine;

        // 建立新的狀態
        var newState = stateMachine.AddState(stateName);

        // 建立新的AnimationClip
        var clip = new AnimationClip();
        clip.name = stateName;

        // 獲取controller的路徑並在同一資料夾建立clip
        var controllerPath = AssetDatabase.GetAssetPath(controller);
        var controllerDir = Path.GetDirectoryName(controllerPath);
        var clipPath = Path.Combine(controllerDir, $"{stateName}.anim");

        // 確保檔名唯一
        clipPath = AssetDatabase.GenerateUniqueAssetPath(clipPath);

        // 建立clip資產
        AssetDatabase.CreateAsset(clip, clipPath);

        // 設置狀態的motion為這個clip
        newState.motion = clip;

        Debug.Log($"Manually created state '{stateName}' and clip at '{clipPath}'");
    }

    private void CreateAnimatorControllerForCurrentAnimator()
    {
        Debug.Log("CreateAnimatorControllerForCurrentAnimator called!"); // 調試信息

        if (currentAnimator == null)
        {
            Debug.LogWarning("No animator selected");
            return;
        }

        Debug.Log($"Creating controller for animator: {currentAnimator.name}"); // 調試信息

        try
        {
#if UNITY_EDITOR
            // 統一使用通用方法，它現在會自動處理Prefab Stage
            var newController =
                AnimatorControllerUtility.CreateAnimatorControllerForAnimatorGeneric(
                    currentAnimator
                );

            if (newController != null)
            {
                // 更新本地狀態
                currentController = newController;

                Debug.Log(
                    $"✓ Successfully created and assigned AnimatorController: {newController.name}"
                );

                // 強制刷新UI
                window.Repaint();

                // 重新執行搜尋以更新匹配項目
                PerformSearch();
            }
            else
            {
                Debug.LogError("Failed to create AnimatorController: returned null");
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create AnimatorController: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    private string GetControlInfo(int controlId)
    {
        if (controlId == 0)
            return "None (0)";

        // 檢查我們的追蹤字典
        if (controlIdToName.ContainsKey(controlId))
        {
            return $"ID:{controlId} ({controlIdToName[controlId]})";
        }

        // 檢查當前焦點控制項
        var currentFocusedControl = GUI.GetNameOfFocusedControl();
        if (!string.IsNullOrEmpty(currentFocusedControl))
        {
            return $"ID:{controlId} (Focused: {currentFocusedControl})";
        }

        // 嘗試猜測是否是AnimationWindow的內建控制項
        var possibleSources = new string[]
        {
            "AnimationWindow timeline",
            "AnimationWindow property",
            "AnimationWindow playback control",
            "AnimationWindow other UI",
        };

        // 基於ID範圍的簡單猜測
        if (controlId < 100)
            return $"ID:{controlId} (Possibly: {possibleSources[0]})";
        else if (controlId < 200)
            return $"ID:{controlId} (Possibly: {possibleSources[1]})";
        else if (controlId < 300)
            return $"ID:{controlId} (Possibly: {possibleSources[2]})";
        else
            return $"ID:{controlId} (Possibly: {possibleSources[3]})";
    }
}
