using NUnit.Framework;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class FakeLoggerTests
    {
        [Test]
        public void CapturesMessagesByLevel()
        {
            var logger = new FakeLogger();

            logger.Info("hello");
            logger.Warning("careful");
            logger.Error("boom");

            Assert.That(logger.Infos,    Is.EqualTo(new[] { "hello" }));
            Assert.That(logger.Warnings, Is.EqualTo(new[] { "careful" }));
            Assert.That(logger.Errors,   Is.EqualTo(new[] { "boom" }));
        }

        [Test]
        public void ClearResetsAllLists()
        {
            var logger = new FakeLogger();
            logger.Info("x"); logger.Warning("y"); logger.Error("z");

            logger.Clear();

            Assert.That(logger.Infos,    Is.Empty);
            Assert.That(logger.Warnings, Is.Empty);
            Assert.That(logger.Errors,   Is.Empty);
        }
    }
}
