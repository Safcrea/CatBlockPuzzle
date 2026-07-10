using NUnit.Framework;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatPlacementResolverTests
    {
        [Test]
        public void Resolve_MapsEveryCatToItsCorrespondingAvailableCell()
        {
            Vector2[] centers =
            {
                new Vector2(1.08f, 1.05f),
                new Vector2(1.04f, 2.1f),
                new Vector2(2.12f, 1.02f)
            };
            Vector2Int[] offsets =
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 0)
            };
            bool[,] available = AllAvailable(4, 4);

            CatPlacementResult result = CatPlacementResolver.Resolve(centers, offsets, available, 0.45f);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Row, Is.EqualTo(1));
            Assert.That(result.Col, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_RejectsGroupWhenAnyCatMissesThreshold()
        {
            Vector2[] centers =
            {
                new Vector2(1f, 1f),
                new Vector2(1f, 2.46f)
            };
            Vector2Int[] offsets =
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1)
            };

            CatPlacementResult result = CatPlacementResolver.Resolve(centers, offsets, AllAvailable(4, 4), 0.45f);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Resolve_RejectsOccupiedOrInactiveTargetCell()
        {
            Vector2[] centers =
            {
                new Vector2(1f, 1f),
                new Vector2(1f, 2f)
            };
            Vector2Int[] offsets =
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1)
            };
            bool[,] available = AllAvailable(4, 4);
            available[1, 2] = false;

            CatPlacementResult result = CatPlacementResolver.Resolve(centers, offsets, available, 0.45f);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Resolve_SelectsLowestDistanceCandidate()
        {
            Vector2[] centers =
            {
                new Vector2(1.12f, 1.08f),
                new Vector2(1.1f, 2.06f)
            };
            Vector2Int[] offsets =
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1)
            };

            CatPlacementResult result = CatPlacementResolver.Resolve(centers, offsets, AllAvailable(4, 4), 0.75f);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Row, Is.EqualTo(1));
            Assert.That(result.Col, Is.EqualTo(1));
        }

        [Test]
        public void ProfileValidation_ClampsUnsafeEditorValues()
        {
            PortraitLayoutProfile profile = ScriptableObject.CreateInstance<PortraitLayoutProfile>();
            profile.MaximumVerticalGain = 5f;
            profile.CatSnapThreshold = -1f;
            profile.PieceSizeTransitionSeconds = 0f;

            profile.ValidateValues();

            Assert.That(profile.MaximumVerticalGain, Is.EqualTo(1.75f));
            Assert.That(profile.CatSnapThreshold, Is.EqualTo(0.1f));
            Assert.That(profile.PieceSizeTransitionSeconds, Is.EqualTo(0.05f));
            Object.DestroyImmediate(profile);
        }

        private static bool[,] AllAvailable(int rows, int cols)
        {
            bool[,] available = new bool[rows, cols];
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    available[row, col] = true;
                }
            }

            return available;
        }
    }
}
