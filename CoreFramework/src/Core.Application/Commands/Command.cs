using MediatR;

namespace Core.Application.Commands
{
    public interface ICommand : IRequest
    {
    }

    public interface ICommand<out TResponse> : IRequest<TResponse>
    where TResponse : ICommandResult
    {
    }
}
