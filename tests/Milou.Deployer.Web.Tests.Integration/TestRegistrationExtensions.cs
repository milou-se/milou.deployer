using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Milou.Deployer.Web.Tests.Integration
{
    public static class TestRegistrationExtensions
    {
        public static IServiceCollection RegisterHandler<T, TRequest, TResponse>(this IServiceCollection services)
            where T : class, IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse> =>
            services.AddSingleton<IRequestHandler<TRequest, TResponse>, T>();
    }
}