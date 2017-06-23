using System;
using System.Collections.Generic;

namespace reactive.pipes
{
    public interface IObservableWithOutcomes<out T> : IObservable<T>
    {
        bool Handled { get; }
        ICollection<ObservableOutcome> Outcomes { get; }
    }
}