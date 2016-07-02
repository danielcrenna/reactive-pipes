using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests.Fakes
{
    public class ManifoldHierarchicalEventHandler : IConsume<BaseEvent>, IConsume<InheritedEvent>, IConsume<IEvent>
    {
        public int HandledInterface { get; set; }
        public int HandledBase { get; set; }
        public int HandledInherited { get; set; }

        public Task<bool> HandleAsync(BaseEvent @event)
        {
            HandledBase++;
            return Task.FromResult(true);
        }

        public Task<bool> HandleAsync(InheritedEvent @event)
        {
            HandledInherited++;
            return Task.FromResult(true);
        }

        public Task<bool> HandleAsync(IEvent @event)
        {
            HandledInterface++;
            return Task.FromResult(true);
        }
    }
}