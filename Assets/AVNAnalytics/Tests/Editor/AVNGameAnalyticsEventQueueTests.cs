using System;
using System.IO;
using NUnit.Framework;

namespace AVNAnalytics.Tests
{
    public sealed class AVNGameAnalyticsEventQueueTests
    {
        const string Context = "{\"d\":1}";

        string dir;
        AVNGameAnalyticsEventQueue queue;

        [SetUp]
        public void SetUp() => dir = Path.Combine(Path.GetTempPath(), "avn_queue_tests_" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            queue?.Close();
            queue = null;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        AVNGameAnalyticsEventQueue Open(int maxEvents = 1000, long maxBytes = 1 << 20)
        {
            queue?.Close();
            queue = new AVNGameAnalyticsEventQueue(dir, maxEvents, maxBytes, null);
            return queue;
        }

        static string Line(int i, string context = Context) => context + "\t{\"i\":" + i + "}";

        static string Body(params int[] ids)
        {
            string events = string.Join(",", Array.ConvertAll(ids, i => "{\"i\":" + i + "}"));
            return "{\"context\":" + Context + ",\"events\":[" + events + "]}";
        }

        void Append(int from, int to)
        {
            for (int i = from; i < to; i++) Assert.IsTrue(queue.Append(Line(i)));
        }

        string QueuePath => Path.Combine(dir, "queue.jsonl");
        string CursorPath => Path.Combine(dir, "queue.cursor");

        [Test]
        public void EventsSurviveReopen()
        {
            Open(); Append(0, 3);
            Open();
            Assert.AreEqual(3, queue.Count);
            Assert.AreEqual(Body(0, 1, 2), queue.PeekBatch(10, 1 << 20, out int count));
            Assert.AreEqual(3, count);
        }

        [Test]
        public void AckedEventsStayAckedAfterReopen()
        {
            Open(); Append(0, 5);
            queue.Advance(2);
            Open();
            Assert.AreEqual(Body(2, 3, 4), queue.PeekBatch(10, 1 << 20, out _));
        }

        [Test]
        public void AckingEverythingDeletesTheFiles()
        {
            Open(); Append(0, 2);
            queue.Advance(2);
            Assert.AreEqual(0, queue.Count);
            Assert.IsFalse(File.Exists(QueuePath));
            Assert.IsFalse(File.Exists(CursorPath));
        }

        [Test]
        public void TornLastLineIsDropped()
        {
            Open(); Append(0, 2);
            queue.Close();
            File.AppendAllText(QueuePath, Context + "\t{\"i\":");
            Open();
            Assert.AreEqual(Body(0, 1), queue.PeekBatch(10, 1 << 20, out _));
        }

        [Test]
        public void CursorFromAnotherFileIsIgnored()
        {
            Open(); Append(0, 3);
            queue.Close();
            File.WriteAllText(CursorPath, "someotherfileid 2");
            Open();
            Assert.AreEqual(3, queue.Count); // re-sending is safe, skipping is not
        }

        [Test]
        public void BatchStopsWhereContextChanges()
        {
            Open();
            queue.Append(Line(0));
            queue.Append(Line(1, "{\"d\":2}"));
            Assert.AreEqual(Body(0), queue.PeekBatch(10, 1 << 20, out int count));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void BatchRespectsCountLimit()
        {
            Open(); Append(0, 5);
            Assert.AreEqual(Body(0, 1), queue.PeekBatch(2, 1 << 20, out int count));
            Assert.AreEqual(2, count);
        }

        [Test]
        public void EventCapDropsOldest()
        {
            Open(maxEvents: 10); Append(0, 15);
            Assert.AreEqual(10, queue.Count);
            Assert.AreEqual(Body(5, 6, 7, 8, 9, 10, 11, 12, 13, 14), queue.PeekBatch(100, 1 << 20, out _));
        }

        [Test]
        public void ByteCapDropsOldest()
        {
            int lineBytes = Line(0).Length; // ASCII: chars == bytes
            Open(maxBytes: lineBytes * 4); Append(0, 10);
            Assert.LessOrEqual(queue.Count, 4);
            StringAssert.EndsWith("{\"i\":9}]}", queue.PeekBatch(100, 1 << 20, out _));
        }

        [Test]
        public void QuarantineMovesEventAside()
        {
            Open(); Append(0, 2);
            queue.Quarantine(1);
            Assert.AreEqual(1, queue.Count);
            StringAssert.Contains(Line(0), File.ReadAllText(Path.Combine(dir, "quarantine.jsonl")));
        }

        [Test]
        public void CompactionRewritesFileWithPendingOnly()
        {
            Open(); Append(0, 250);
            queue.Advance(240); // 240 dead >= 200 and >= 10 pending: compacts
            Assert.IsFalse(File.Exists(CursorPath));
            Assert.AreEqual(11, File.ReadAllLines(QueuePath).Length); // header + 10 events

            queue.Append(Line(250)); // appends to the compacted file
            Open();
            Assert.AreEqual(11, queue.Count);
            StringAssert.StartsWith("{\"context\":" + Context + ",\"events\":[{\"i\":240}", queue.PeekBatch(100, 1 << 20, out _));
        }
    }
}
