using UnityEngine;

namespace MonoFSM.Editor
{
    public class WrappedEvent
        {
            public Event e;

            public bool isRepaint => e.type == EventType.Repaint;
            public bool isLayout => e.type == EventType.Layout;
            public bool isUsed => e.type == EventType.Used;
            public bool isMouseLeaveWindow => e.type == EventType.MouseLeaveWindow;
            public bool isMouseEnterWindow => e.type == EventType.MouseEnterWindow;
            public bool isContextClick => e.type == EventType.ContextClick;

            public bool isKeyDown => e.type == EventType.KeyDown;
            public bool isKeyUp => e.type == EventType.KeyUp;
            public KeyCode keyCode => e.keyCode;
            public char characted => e.character;

            public bool isExecuteCommand => e.type == EventType.ExecuteCommand;
            public string commandName => e.commandName;

            public bool isMouse => e.isMouse;
            public bool isMouseDown => e.type == EventType.MouseDown;
            public bool isMouseUp => e.type == EventType.MouseUp;
            public bool isMouseDrag => e.type == EventType.MouseDrag;
            public bool isMouseMove => e.type == EventType.MouseMove;
            public bool isScroll => e.type == EventType.ScrollWheel;
            public int mouseButton => e.button;
            public int clickCount => e.clickCount;
            public Vector2 mousePosition => e.mousePosition;
            public Vector2 mousePosition_screenSpace => GUIUtility.GUIToScreenPoint(e.mousePosition);
            public Vector2 mouseDelta => e.delta;

            public bool isDragUpdate => e.type == EventType.DragUpdated;
            public bool isDragPerform => e.type == EventType.DragPerform;
            public bool isDragExit => e.type == EventType.DragExited;

            public EventModifiers modifiers => e.modifiers;
            public bool holdingAnyModifierKey => modifiers != EventModifiers.None;

            public bool holdingAlt => e.alt;
            public bool holdingShift => e.shift;
            public bool holdingCtrl => e.control;
            public bool holdingCmd => e.command;
            public bool holdingCmdOrCtrl => e.command || e.control;

            public bool holdingAltOnly => e.modifiers == EventModifiers.Alt;        // in some sessions FunctionKey is always pressed?
            public bool holdingShiftOnly => e.modifiers == EventModifiers.Shift;        // in some sessions FunctionKey is always pressed?
            public bool holdingCtrlOnly => e.modifiers == EventModifiers.Control;
            public bool holdingCmdOnly => e.modifiers == EventModifiers.Command;
            public bool holdingCmdOrCtrlOnly => (e.modifiers == EventModifiers.Command || e.modifiers == EventModifiers.Control);

            public EventType type => e.type;

            public void Use() => e?.Use();


            public WrappedEvent(Event e) => this.e = e;

            public override string ToString() => e.ToString();

        }
}