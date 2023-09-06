using MassTransit;
using MassTransit.Mediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediator(cfg =>
{
    cfg.AddConsumer<Test.DummyRequestConsumer>();
});

var app = builder.Build();

app.UseHttpsRedirection();

TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
{
    System.Console.WriteLine("Unobserved task exception!");
};

app.MapPost("/error", async (HttpContext ctx, IScopedMediator mediator) =>
{
    var guid = Guid.NewGuid().ToString();
    try
    {
        var response = await mediator
            .CreateRequestClient<Test.DummyRequest>()
            .GetResponse<Test.DummyResponse>(new Test.DummyRequest("foo"));
        System.Console.WriteLine("success");
    }
    catch (RequestException e)
    {
        System.Console.WriteLine("error in mass transit consumer");
    }

    await Task.Delay(1000);
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