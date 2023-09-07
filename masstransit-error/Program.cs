using MassTransit;
using MassTransit.Clients;
using MassTransit.Mediator;
using Test;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddMediator(cfg =>
{
    cfg.AddConsumer<Test.DummyRequestConsumer>();
});


/*
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<Test.DummyRequestConsumer>();
    
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
*/

var app = builder.Build();

app.UseHttpsRedirection();

TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
{
    System.Console.WriteLine("Unobserved task exception!:\n" + eventArgs.Exception);
};

app.MapPost("/error", async (HttpContext ctx, IMediator requestClientFactory) =>
{
    var guid = Guid.NewGuid().ToString();
    try
    {
        var context = requestClientFactory.Context;
        IClientFactory myFactory = new MyClientFactory(context);
        
        var requestClient = myFactory.CreateRequestClient<Test.DummyRequest>();
        
        var response = await requestClient
            .GetResponse<Test.DummyResponse>(new Test.DummyRequest("foo"));
        System.Console.WriteLine("success");
    }
    catch (RequestException e)
    {
        System.Console.WriteLine("error in mass transit consumer");
    }

    await Task.Delay(3000);
    GC.Collect(); 
    GC.WaitForPendingFinalizers();            

    return Results.Ok();
});

app.Run();

namespace Test
{
    public record DummyResponse(string Id);

    public record DummyRequest(string Id);

    public class DummyRequestConsumer : IConsumer<DummyRequest>
    {
        public async Task Consume(ConsumeContext<DummyRequest> context)
        {
            await Task.Yield();

            context.CancellationToken.ThrowIfCancellationRequested();

            throw new InvalidOperationException("This is a dummy exception");
        }
    }
}