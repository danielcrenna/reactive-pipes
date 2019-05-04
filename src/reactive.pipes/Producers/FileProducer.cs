// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using reactive.pipes.Extensions;
using reactive.pipes.Serializers;

namespace reactive.pipes.Producers
{
	/// <summary>
	///     A file producer that publishes all files in a folder with a given pattern, and any new additions to that folder
	///     matching the pattern
	///     <remarks>
	///         By default, the producer will look for binary serialized files with an extension of .dat in the same folder as
	///         the assembly is executing
	///     </remarks>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FileProducer<T> : BackgroundProducer<T>
	{
		private readonly ISerializer _serializer;

		public FileProducer() : this(new BinarySerializer(), GetExecutingDirectory(), "*.dat")
		{
		}

		public FileProducer(string baseDirectory, string filter, bool includeSubdirectories = true) : this(
			new BinarySerializer(), baseDirectory, filter, includeSubdirectories)
		{
		}

		public FileProducer(string baseDirectory, bool includeSubdirectories = true) : this(new BinarySerializer(),
			baseDirectory, "*.dat", includeSubdirectories)
		{
		}

		public FileProducer(ISerializer serializer, string baseDirectory, string filter,
			bool includeSubdirectories = true)
		{
			_serializer = serializer;
			BaseDirectory = baseDirectory;
			Background.Produce(
				new Func<IEnumerable<T>>(() => YieldFromFiles(BaseDirectory, filter, includeSubdirectories))
					.AsContinuousObservable()
					.Buffer(TimeSpan.FromMilliseconds(1000)));
		}

		public string BaseDirectory { get; }

		private static string GetExecutingDirectory()
		{
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		}

		private IEnumerable<T> YieldFromFiles(string baseDirectory, string filter, bool includeSubdirectories)
		{
			var files = Directory.GetFiles(baseDirectory, filter,
				includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
			return FromFiles(files);
		}

		private IEnumerable<T> FromFiles(params string[] files)
		{
			return files.Select(FromFile);
		}

		private T FromFile(string file)
		{
			var stream = OpenExclusive(file, FileAccess.Read, 100, TimeSpan.FromSeconds(1));
			if (stream == null)
				throw new InvalidOperationException($"'{file}' was not producible because a lock was not acquired");
			T @event;
			using (stream) @event = _serializer.DeserializeFromStream<T>(stream);
			if (File.Exists(file)) File.Delete(file);
			return @event;
		}

		private static FileStream OpenExclusive(string filename, FileAccess fileAccess, int attempts, TimeSpan interval)
		{
			var tries = 0;
			while (true)
				try
				{
					return File.Open(filename, FileMode.Open, fileAccess, FileShare.None);
				}
				catch (IOException exception)
				{
					if (!IsFileLocked(exception)) throw;
					if (++tries > attempts) return null;
					Thread.Sleep(interval); // Boo...
				}
		}

		// http://stackoverflow.com/questions/1304/how-to-check-for-file-lock-in-c
		private static bool IsFileLocked(Exception exception)
		{
			var errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
			return errorCode == 32 || errorCode == 33;
		}
	}
}