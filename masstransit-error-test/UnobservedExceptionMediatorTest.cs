using MassTransit;
using Test;

namespace masstransit_error_test;

public class UnobservedExceptionMediatorTest
{
    [Fact]
    public async Task Should_observe_all_exceptions()
    {
        var mediator = MassTransit.Bus.Factory.CreateMediator(x =>
        {
        });

        List<object> unhandledExceptions = new List<object>();
        List<Exception> unobservedTaskExceptions = new List<Exception>();

        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            unhandledExceptions.Add(eventArgs.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
        {
            eventArgs.SetObserved();
            unobservedTaskExceptions.Add(eventArgs.Exception);
        };

        IRequestClient<PingMessage> requestClient = mediator.CreateRequestClient<PingMessage>(TimeSpan.FromSeconds(1));

        Task<Response<PongMessage>> response = requestClient.GetResponse<PongMessage>(new PingMessage());

        await Assert.ThrowsAsync<RequestException>(async () => await response);

        GC.Collect();
        await Task.Delay(1000);
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.Empty(unhandledExceptions);
        Assert.Empty(unobservedTaskExceptions);
    }
    
    [Fact]
    public async Task Should_observe_all_exceptions_my_code()
    {
        var mediator = MassTransit.Bus.Factory.CreateMediator(x =>
        {
        });

        List<object> unhandledExceptions = new List<object>();
        List<Exception> unobservedTaskExceptions = new List<Exception>();

        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            unhandledExceptions.Add(eventArgs.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
        {
            eventArgs.SetObserved();
            unobservedTaskExceptions.Add(eventArgs.Exception);
        };

        var context = mediator.Context;
        IClientFactory myFactory = new MyClientFactory(context);
        
        IRequestClient<PingMessage> requestClient = myFactory.CreateRequestClient<PingMessage>(TimeSpan.FromSeconds(1));

        Task<Response<PongMessage>> response = requestClient.GetResponse<PongMessage>(new PingMessage());

        await Assert.ThrowsAsync<RequestException>(async () => await response);

        GC.Collect();
        await Task.Delay(1000);
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.Empty(unhandledExceptions);
        Assert.Empty(unobservedTaskExceptions);
    }
    
    public class PingMessage :
        IEquatable<PingMessage>,
        CorrelatedBy<Guid>
    {
        Guid _id = new Guid("D62C9B1C-8E31-4D54-ADD7-C624D56085A4");

        public PingMessage()
        {
        }

        public PingMessage(Guid id)
        {
            _id = id;
        }

        public Guid CorrelationId
        {
            get => _id;
            set => _id = value;
        }

        public bool Equals(PingMessage obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj._id.Equals(_id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof(PingMessage))
                return false;
            return Equals((PingMessage)obj);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
    }
    
    public class PongMessage :
        IEquatable<PongMessage>,
        CorrelatedBy<Guid>
    {
        Guid _id;

        public PongMessage()
        {
            _id = Guid.NewGuid();
        }

        public PongMessage(Guid correlationId)
        {
            _id = correlationId;
        }

        public Guid CorrelationId
        {
            get => _id;
            set => _id = value;
        }

        public bool Equals(PongMessage obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj._id.Equals(_id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof(PongMessage))
                return false;
            return Equals((PongMessage)obj);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
    }
}