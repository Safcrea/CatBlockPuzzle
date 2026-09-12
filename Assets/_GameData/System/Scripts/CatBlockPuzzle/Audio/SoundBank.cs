using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatBlockPuzzle
{
    [CreateAssetMenu(fileName = "SoundBank", menuName = "Cat Block Puzzle/Audio/Sound Bank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Serializable]
        public sealed class SoundEntry
        {
            [SerializeField] private string id;
            [SerializeField] private AudioClip clip;
            [SerializeField, Range(0f, 1f)] private float volume = 1f;

            public string Id => id;
            public AudioClip Clip => clip;
            public float Volume => volume;
        }

        [SerializeField] private List<SoundEntry> entries = new List<SoundEntry>();
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [Header("Music")]
        [SerializeField] private AudioClip musicTrack;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.45f;

        private Dictionary<string, SoundEntry> lookup;

        public IReadOnlyList<SoundEntry> Entries => entries;
        public float MasterVolume => masterVolume;
        public AudioClip MusicTrack => musicTrack;
        public float MusicVolume => musicVolume;

        public bool TryGet(string id, out SoundEntry entry)
        {
            entry = null;
            if (lookup == null)
            {
                RebuildLookup();
            }

            return !string.IsNullOrWhiteSpace(id) && lookup.TryGetValue(id, out entry);
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            lookup = new Dictionary<string, SoundEntry>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.Count; i++)
            {
                SoundEntry entry = entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Id))
                {
                    lookup[entry.Id.Trim()] = entry;
                }
            }
        }
    }
}
