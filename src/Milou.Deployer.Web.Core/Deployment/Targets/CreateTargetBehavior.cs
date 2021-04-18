using System.Threading;
using System.Threading.Tasks;
using Arbor.KVConfiguration.Core;
using MediatR;
using Milou.Deployer.Web.Core.Deployment.Messages;

namespace Milou.Deployer.Web.Core.Deployment.Targets
{
    public class CreateTargetBehavior : IPipelineBehavior<CreateTarget, CreateTargetResult>
    {
        private readonly IMediator _mediator;

        public CreateTargetBehavior(IMediator mediator) => _mediator = mediator;

        public async Task<CreateTargetResult> Handle(CreateTarget request,
            CancellationToken cancellationToken,
            RequestHandlerDelegate<CreateTargetResult>? next)
        {
            if (next is null)
            {
                return new CreateTargetResult(new ValidationError("Next handler is not set"));
            }
            CreateTargetResult response = await next();

            if (response.ValidationErrors.IsDefaultOrEmpty)
            {
                await _mediator.Publish(new TargetCreated(response.TargetId), cancellationToken);
            }

            return response;
        }
    }
}