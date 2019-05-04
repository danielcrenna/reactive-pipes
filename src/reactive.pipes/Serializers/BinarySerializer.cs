// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace reactive.pipes.Serializers
{
	public class BinarySerializer : ISerializer
	{
		private readonly BinaryFormatter _formatter;

		public BinarySerializer() => _formatter = new BinaryFormatter();

		public Stream SerializeToStream<T>(T message)
		{
			var ms = new MemoryStream();
			_formatter.Serialize(ms, message);
			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		}

		public T DeserializeFromStream<T>(Stream stream)
		{
			return (T) _formatter.Deserialize(stream);
		}
	}
}