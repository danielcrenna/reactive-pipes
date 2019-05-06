// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
namespace reactive.pipes
{
	public enum SubscriptionKeyMode
	{
		/// <summary>
		/// The subscription is keyed off of its type. 
		/// </summary>
		Default,
		
		/// <summary>
		/// The subscription is keyed off of its type and topic, if any.
		/// </summary>
		Topical
	}
}