using System;
using System.Collections.Generic;
using System.Linq;

namespace reactive.pipes.Producers
{
    public class RetryPolicy
    {
	    RetryDecision _default = RetryDecision.Requeue;
        private readonly IDictionary<int, RetryDecision> _rules;

        public RetryPolicy()
        {
            _rules = new Dictionary<int, RetryDecision>();
			RequeueInterval = a => TimeSpan.FromSeconds(5 + Math.Pow(a, 4));
		}

	    public Func<int, TimeSpan> RequeueInterval { get; set; }

	    public void Default(RetryDecision action)
	    {
			_default = action;
	    }

		public void After(int tries, RetryDecision action)
        {
            _rules.Add(tries, action);
        }

	    public void Clear()
	    {
		    _rules.Clear();
	    }

        public RetryDecision DecideOn<T>(T @event, int attempts)
        {
			foreach (var threshold in _rules.Keys.OrderBy(k => k).Where(threshold => attempts >= threshold))
            {
                return _rules[threshold];
            }
	        return _default;
        }
    }
}