// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace reactive.pipes
{
	public enum TopicFilteredResult
	{
		/// <summary>
		/// If the topic is not satisfied, the outcome counts as a failure result. Retry logic will execute.
		/// </summary>
		Failure,

		/// <summary>
		/// If the topic is not satisfied, the outcome counts as a successful result. Retry logic will not execute.
		/// </summary>
		Success
	}
}