// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace reactive.pipes
{
	public interface IObservableWithOutcomes<T> : IObservable<T>, IObserver<T>
	{
		bool Handled { get; }
		ICollection<ObservableOutcome> Outcomes { get; }
		void Clear();
	}
}