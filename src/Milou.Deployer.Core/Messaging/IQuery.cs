using MediatR;

namespace Milou.Deployer.Core.Messaging
{
    public interface IQuery<out T> : IRequest<T> where T : IQueryResult {}
}