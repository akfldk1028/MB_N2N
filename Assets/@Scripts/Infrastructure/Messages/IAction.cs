using System;

namespace MB.Infrastructure.Messages
{
    public enum ActionId
    {
        System_Update,
        System_LateUpdate,
        System_FixedUpdate,
        UI_OpenView,
        UI_CloseView,
        Gameplay_StartSession,
        Gameplay_EndSession,
        Network_ClientConnected,
        Network_ClientDisconnected,
        Input_PrimaryAction,
        Input_SecondaryAction
    }

    public interface IActionPayload { }

    public readonly struct NoPayload : IActionPayload
    {
        public static readonly NoPayload Instance = new NoPayload();
    }

    public readonly struct ActionMessage
    {
        public ActionId Id { get; }
        public IActionPayload Payload { get; }

        ActionMessage(ActionId id, IActionPayload payload)
        {
            Id = id;
            Payload = payload ?? NoPayload.Instance;
        }

        public static ActionMessage From(ActionId id) =>
            new ActionMessage(id, NoPayload.Instance);

        public static ActionMessage From(ActionId id, IActionPayload payload) =>
            new ActionMessage(id, payload ?? NoPayload.Instance);

        public bool TryGetPayload<TPayload>(out TPayload payload) where TPayload : IActionPayload
        {
            if (Payload is TPayload typed)
            {
                payload = typed;
                return true;
            }

            payload = default;
            return false;
        }
    }

    public interface IAction
    {
        ActionId Id { get; }
        void Execute(ActionMessage message);
    }
}
