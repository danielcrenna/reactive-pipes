// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace reactive.tests
{
	public class FileFolderFixture : IDisposable
	{
		public readonly string Folder = $"FileFolder-{Guid.NewGuid()}";

		public FileFolderFixture()
		{
			if (!Directory.Exists(Folder))
				Directory.CreateDirectory(Folder);
			foreach (var file in Directory.GetFiles(Folder, "*.*"))
				File.Delete(file);
		}

		public void Dispose()
		{
			if (Directory.Exists(Folder))
			{
				foreach (var file in Directory.GetFiles(Folder, "*.*"))
					File.Delete(file);
				Directory.Delete(Folder);
			}
		}
	}
}