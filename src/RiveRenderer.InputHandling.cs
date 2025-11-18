using Stride.Input;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace VL.Rive;

partial class RiveRenderer
{
    IDisposable SubscribeToInputSource(IInputSource inputSource, RenderDrawContext context)
    {
        if (inputSource is null)
            return Disposable.Empty;

        var inputManager = context.RenderContext.Services.GetService<InputManager>();
        if (inputManager is null)
            return Disposable.Empty;

        var pointerListener = NewPointerListener(inputSource);
        inputManager.AddListener(pointerListener);

        var subscription = Disposable.Create(() =>
        {
            inputManager.RemoveListener(pointerListener);
        });

        return subscription;
    }

    private IInputEventListener NewPointerListener(IInputSource inputSource)
    {
        return new AnonymousEventListener<PointerEvent>(e =>
        {
            if (e.Device.Source != inputSource)
                return;

            if (riveScene is null)
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
