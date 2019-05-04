// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Running;

namespace reactive.pipes.benchmarks
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			BenchmarkRunner.Run<ConsumerBenchmarks>();
		}
	}
}