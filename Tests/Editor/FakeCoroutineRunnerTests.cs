using System.Collections;
using NUnit.Framework;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class FakeCoroutineRunnerTests
    {
        [Test]
        public void StartAddsRoutineAndAdvanceProgresses()
        {
            var runner = new FakeCoroutineRunner();
            var steps = 0;

            runner.Start(ThreeSteps(() => steps++));
            Assert.That(runner.RunningCount, Is.EqualTo(1));

            runner.AdvanceAll(); // step 1
            runner.AdvanceAll(); // step 2
            runner.AdvanceAll(); // step 3
            runner.AdvanceAll(); // routine completes (MoveNext returns false)

            Assert.That(steps, Is.EqualTo(3));
            Assert.That(runner.RunningCount, Is.EqualTo(0));
        }

        [Test]
        public void StopRemovesRoutineBeforeCompletion()
        {
            var runner = new FakeCoroutineRunner();
            var handle = runner.Start(ThreeSteps(() => { }));

            runner.Stop(handle);

            Assert.That(runner.RunningCount, Is.EqualTo(0));
        }

        static IEnumerator ThreeSteps(System.Action onStep)
        {
            onStep(); yield return null;
            onStep(); yield return null;
            onStep(); yield return null;
        }
    }
}
