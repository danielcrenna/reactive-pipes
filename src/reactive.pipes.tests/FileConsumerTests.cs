using System.IO;
using reactive.pipes;
using reactive.pipes.Consumers;
using reactive.pipes.Serializers;
using reactive.tests.Fakes;
using Xunit;

namespace reactive.tests
{
    public class FileConsumerTests : IClassFixture<FileFolderFixture>
    {
        private readonly FileFolderFixture _fixture;

        public FileConsumerTests(FileFolderFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Events_persist_as_json_on_disk()
        {
            PersistsAsSerialized(new JsonSerializer(), ".json");
        }

        private async void PersistsAsSerialized(ISerializer serializer, string extension)
        {
            var consumer = new FileConsumer<StringEvent>(serializer, _fixture.Folder, extension);
            var @event = new StringEvent("Test!");
            await consumer.HandleAsync(@event);

            var file = OneFileSaved(extension);
            FileContainsTheEvent(file, serializer, @event);
        }

        private static void FileContainsTheEvent<T>(string file, ISerializer serializer, T @event) where T : class
        {
            var expected = @event.ToString();

            T deserialized;
            using (var fs = File.OpenRead(file))
                deserialized = serializer.DeserializeFromStream<T>(fs);
            Assert.NotNull(deserialized);

            var actual = deserialized.ToString();
            Assert.Equal(expected, actual);
        }

        private string OneFileSaved(string extension)
        {
            var files = Directory.GetFiles(_fixture.Folder, "*" + extension);
            Assert.Single(files);
            var file = files[0];
            return file;
        }
    }
}