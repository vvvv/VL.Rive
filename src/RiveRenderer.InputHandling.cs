using Stride.Input;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.IO.Notifications;
using VL.Rive.Interop;

namespace VL.Rive;

partial class RiveRenderer
{
    /// <summary>
    /// Forwards a mouse or touch notification to the scene. <paramref name="getPosition"/> must return the position in the pixel space of the renderer's target. Returns true if the notification was handled.
    /// </summary>
    public bool SendNotification(INotification notification, Func<NotificationWithPosition, Vector2> getPosition)
    {
        if (riveScene is null || riveScene.IsClosed)
            return false;

        switch (notification)
        {
            case MouseNotification mouse:
                {
                    var p = ToArtboard(getPosition(mouse));
                    const int mouseId = 0;
                    switch (mouse)
                    {
                        case MouseDownNotification:
                            riveScene.PointerDown(p.X, p.Y, mouseId);
                            return true;
                        case MouseUpNotification:
                            riveScene.PointerUp(p.X, p.Y, mouseId);
                            return true;
                        case MouseMoveNotification:
                            riveScene.PointerMove(p.X, p.Y, 0f, mouseId);
                            return true;
                    }
                    return false;
                }
            case KeyPressNotification press:
                // Control characters (Backspace, Enter, Ctrl+letter) are handled as keys
                return !char.IsControl(press.KeyChar) && riveScene.TextInput(press.KeyChar.ToString());
            case KeyCodeNotification keyCode:
                {
                    var isDown = keyCode is KeyDownNotification;
                    if (!isDown && keyCode is not KeyUpNotification)
                        return false;
                    var modifiers = KeyModifiers.none;
                    if (keyCode.ShiftKey) modifiers |= KeyModifiers.shift;
                    if (keyCode.CtrlKey) modifiers |= KeyModifiers.ctrl;
                    if (keyCode.AltKey) modifiers |= KeyModifiers.alt;
                    return RiveKeyMapping.TryMap(keyCode.KeyData & VL.Lib.IO.Keys.KeyCode, out var key)
                        && riveScene.KeyInput(key, modifiers, isDown, isRepeat: false);
                }
            case TouchNotification touch:
                {
                    var p = ToArtboard(getPosition(touch));
                    switch (touch.Kind)
                    {
                        case TouchNotificationKind.TouchDown:
                            riveScene.PointerDown(p.X, p.Y, touch.Id);
                            return true;
                        case TouchNotificationKind.TouchMove:
                            riveScene.PointerMove(p.X, p.Y, 0f, touch.Id);
                            return true;
                        case TouchNotificationKind.TouchUp:
                            riveScene.PointerUp(p.X, p.Y, touch.Id);
                            return true;
                        default:
                            riveScene.PointerExit(p.X, p.Y, touch.Id);
                            return true;
                    }
                }
        }
        return false;
    }

    Vector2 ToArtboard(Vector2 position) => alignmentMat.InvertOrIdentity() * position;
    IDisposable SubscribeToInputSource(IInputSource? inputSource, RenderDrawContext context)
    {
        if (inputSource is null)
            return Disposable.Empty;

        var inputManager = context.RenderContext.Services.GetService<InputManager>();
        if (inputManager is null)
            return Disposable.Empty;

        var listeners = new IInputEventListener[]
        {
            NewPointerListener(inputSource),
            NewKeyListener(inputSource, inputManager),
            NewTextInputListener(inputSource),
        };
        foreach (var listener in listeners)
            inputManager.AddListener(listener);

        return Disposable.Create(() =>
        {
            foreach (var listener in listeners)
                inputManager.RemoveListener(listener);
        });
    }

    private IInputEventListener NewKeyListener(IInputSource inputSource, InputManager inputManager)
    {
        return new AnonymousEventListener<KeyEvent>(e =>
        {
            if (e.Device.Source != inputSource)
                return;

            if (riveScene is null || riveScene.IsClosed)
                return;

            if (!RiveKeyMapping.TryMap(e.Key, out var key))
                return;

            var modifiers = KeyModifiers.none;
            if (inputManager.IsKeyDown(Keys.LeftShift) || inputManager.IsKeyDown(Keys.RightShift))
                modifiers |= KeyModifiers.shift;
            if (inputManager.IsKeyDown(Keys.LeftCtrl) || inputManager.IsKeyDown(Keys.RightCtrl))
                modifiers |= KeyModifiers.ctrl;
            if (inputManager.IsKeyDown(Keys.LeftAlt) || inputManager.IsKeyDown(Keys.RightAlt))
                modifiers |= KeyModifiers.alt;
            if (inputManager.IsKeyDown(Keys.LeftWin) || inputManager.IsKeyDown(Keys.RightWin))
                modifiers |= KeyModifiers.meta;

            riveScene.KeyInput(key, modifiers, e.IsDown, isRepeat: e.RepeatCount > 0);
        });
    }

    private IInputEventListener NewTextInputListener(IInputSource inputSource)
    {
        return new AnonymousEventListener<TextInputEvent>(e =>
        {
            if (e.Device.Source != inputSource)
                return;

            if (riveScene is null || riveScene.IsClosed)
                return;

            // Text only: Backspace, Enter, Tab and Ctrl+letter arrive as control characters and are handled as keys
            if (e.Type != TextInputEventType.Input || string.IsNullOrEmpty(e.Text) || e.Text.All(char.IsControl))
                return;

            riveScene.TextInput(e.Text);
        });
    }

    private IInputEventListener NewPointerListener(IInputSource inputSource)
    {
        return new AnonymousEventListener<PointerEvent>(e =>
        {
            if (e.Device.Source != inputSource)
                return;

            if (riveScene is null || riveScene.IsClosed)
                return;

            var inverseViewTrasform = alignmentMat.InvertOrIdentity();
            var position = inverseViewTrasform * new Vector2(e.AbsolutePosition.X, e.AbsolutePosition.Y);

            switch (e.EventType)
            {
                case PointerEventType.Pressed:
                    riveScene.PointerDown(position.X, position.Y, e.PointerId);
                    break;
                case PointerEventType.Moved:
                    riveScene.PointerMove(position.X, position.Y, 0f, e.PointerId);
                    break;
                case PointerEventType.Released:
                    riveScene.PointerUp(position.X, position.Y, e.PointerId);
                    break;
                case PointerEventType.Canceled:
                    riveScene.PointerExit(position.X, position.Y, e.PointerId);
                    break;
                default:
                    break;
            }
        });
    }

    sealed class AnonymousEventListener<T> : IInputEventListener<T>
        where T : InputEvent
    {
        readonly Action<T> ProcessEventAction;

        public AnonymousEventListener(Action<T> processEventAction)
        {
            ProcessEventAction = processEventAction;
        }

        public void ProcessEvent(T inputEvent)
        {
            ProcessEventAction(inputEvent);
        }
    }
}
