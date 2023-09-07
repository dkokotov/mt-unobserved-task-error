using MassTransit;
using MassTransit.Clients;
using MassTransit.Configuration;
using MassTransit.Internals;
using MassTransit.Util;

namespace Test;

public class MyResponseHandlerConfigurator<TResponse> :
    IHandlerConfigurator<TResponse>
    where TResponse : class
{
    readonly TaskCompletionSource<ConsumeContext<TResponse>> _completed;
    readonly MessageHandler<TResponse> _handler;
    readonly IBuildPipeConfigurator<ConsumeContext<TResponse>> _pipeConfigurator;
    readonly Task _requestTask;
    readonly TaskScheduler _taskScheduler;

    public MyResponseHandlerConfigurator(TaskScheduler taskScheduler, MessageHandler<TResponse> handler, Task requestTask)
    {
        _taskScheduler = taskScheduler;
        _handler = handler;
        _requestTask = requestTask;

        _pipeConfigurator = new PipeConfigurator<ConsumeContext<TResponse>>();
        _completed = TaskUtil.GetTask<ConsumeContext<TResponse>>();
        _completed.Task.IgnoreUnobservedExceptions();
    }

    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TResponse>> specification)
    {
        _pipeConfigurator.AddPipeSpecification(specification);
    }

    public ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer)
    {
        return new EmptyConnectHandle();
    }

    public HandlerConnectHandle<TResponse> Connect(IRequestPipeConnector connector, Guid requestId)
    {
        MessageHandler<TResponse> messageHandler = _handler != null ? (MessageHandler<TResponse>)AsyncMessageHandler : MessageHandler;

        var connectHandle = connector.ConnectRequestHandler(requestId, messageHandler, _pipeConfigurator);

        return new ResponseHandlerConnectHandle<TResponse>(connectHandle, _completed, _requestTask);
    }

    async Task AsyncMessageHandler(ConsumeContext<TResponse> context)
    {
        try
        {
            await Task.Factory.StartNew(() => _handler(context), context.CancellationToken, TaskCreationOptions.None, _taskScheduler)
                .Unwrap()
                .ConfigureAwait(false);

            _completed.TrySetResult(context);
        }
        catch (Exception ex)
        {
            _completed.TrySetException(ex);
        }
    }

    Task MessageHandler(ConsumeContext<TResponse> context)
    {
        try
        {
            _completed.TrySetResult(context);
        }
        catch (Exception ex)
        {
            _completed.TrySetException(ex);
        }

        return Task.CompletedTask;
    }
}
