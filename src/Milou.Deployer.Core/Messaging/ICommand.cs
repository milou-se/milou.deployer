using MediatR;

namespace Milou.Deployer.Core.Messaging
{
    public interface ICommand<out T> : IRequest<T> where T : ICommandResult
    {

    }
}