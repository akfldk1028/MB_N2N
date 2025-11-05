using MB.Infrastructure.Messages;

namespace MB.Infrastructure.State
{
    public interface IState
    {
        StateId Id { get; }
        void Enter();
        void Exit();
        bool CanHandle(ActionId actionId);
        void Handle(ActionMessage message);
    }
}
