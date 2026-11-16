using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FootballRunner.SaveSystem
{
    /// <summary>One child on this phone: a name and the player they last chose.</summary>
    [Serializable]
    public class ChildProfile
    {
        public string id;

        /// <summary>Typed by a parent; empty means the menu shows "Dijete 1" and so on.</summary>
        public string name = "";

        /// <summary>Copied from the child's save when switching away, for the list in the parents corner.</summary>
        public string characterId = "rukajja";
    }

    [Serializable]
    public class ProfileIndex
    {
        public string active = ProfileStore.FirstId;
        public List<ChildProfile> profiles = new List<ChildProfile>();
        public int nextNumber = 2;
    }

    /// <summary>
    /// Several children on one phone, each with their own save file and progress.
    /// The first child keeps the original file (footballrunner.save.json), so progress
    /// from before profiles simply becomes the first profile: nothing is copied or
    /// rewritten. The list lives in profiles.json; switching, adding and removing
    /// children happen only in the parents corner, behind its sum gate, so a child
    /// cannot dodge the daily time limit by playing as a brother or sister.
    /// </summary>
    public static class ProfileStore
    {
        public const string FirstId = "p1";
        public const int MaxProfiles = 4;
        private const string IndexFileName = "profiles.json";

        private static ProfileIndex _index;

        /// <summary>Raised after the active child changed and their save was loaded.</summary>
        public static event Action Switched;

        public static string IndexPath => Path.Combine(Application.persistentDataPath, IndexFileName);

        public static ProfileIndex Index
        {
            get
            {
                if (_index == null)
                {
                    LoadIndex();
                }

                return _index;
            }
        }

        public static string ActiveId => Index.active;

        public static ChildProfile Active => Find(ActiveId);

        public static IReadOnlyList<ChildProfile> Profiles => Index.profiles;

        /// <summary>The save file name for a child: the original one for the first child.</summary>
        public static string SaveFileName(string id)
        {
            return id == FirstId || string.IsNullOrEmpty(id) ? "footballrunner.save.json" : $"footballrunner.save.{id}.json";
        }

        public static ChildProfile Find(string id)
        {
            foreach (ChildProfile profile in Index.profiles)
            {
                if (profile.id == id)
                {
                    return profile;
                }
            }

            return null;
        }

        /// <summary>1 for the first child in the list, and so on; 0 if unknown.</summary>
        public static int Number(string id)
        {
            return Index.profiles.FindIndex(p => p.id == id) + 1;
        }

        /// <summary>Adds a child (not switched to yet). Null when the phone already has the most children allowed.</summary>
        public static ChildProfile Add(string name)
        {
            if (Index.profiles.Count >= MaxProfiles)
            {
                return null;
            }

            // Never hand out an id whose save file is still on the phone: the new child would get that progress.
            while (File.Exists(Path.Combine(Application.persistentDataPath, SaveFileName("p" + Index.nextNumber))))
            {
                Index.nextNumber++;
            }

            var profile = new ChildProfile { id = "p" + Index.nextNumber, name = Clean(name) };
            Index.nextNumber++;
            Index.profiles.Add(profile);
            SaveIndex();
            return profile;
        }

        public static bool Rename(string id, string name)
        {
            ChildProfile profile = Find(id);

            if (profile == null)
            {
                return false;
            }

            profile.name = Clean(name);
            SaveIndex();
            return true;
        }

        /// <summary>
        /// Removes a child and deletes their save files. The last child cannot be removed;
        /// removing the child who is playing switches to the first one left.
        /// </summary>
        public static bool Remove(string id)
        {
            ChildProfile profile = Find(id);

            if (profile == null || Index.profiles.Count <= 1)
            {
                return false;
            }

            if (id == ActiveId)
            {
                string other = Index.profiles.Find(p => p.id != id).id;
                Switch(other);
            }

            Index.profiles.Remove(profile);
            SaveIndex();

            string main = Path.Combine(Application.persistentDataPath, SaveFileName(id));
            foreach (string path in new[] { main, main + ".bak", main + ".tmp" })
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ProfileStore] Could not delete {path}. {e.Message}");
                }
            }

            return true;
        }

        /// <summary>
        /// Saves the current child, makes another one active and loads their save. A child
        /// who has never played starts with the current child's languages and sound
        /// settings, so a brother or sister does not have to set them up again.
        /// </summary>
        public static bool Switch(string id)
        {
            ChildProfile target = Find(id);

            if (target == null)
            {
                return false;
            }

            if (id == ActiveId)
            {
                return true;
            }

            SaveData previous = SaveManager.Data;
            ChildProfile current = Active;

            if (current != null)
            {
                current.characterId = previous.selectedCharacterId;
            }

            SaveManager.Save();
            bool firstTime = !File.Exists(Path.Combine(Application.persistentDataPath, SaveFileName(id)));

            Index.active = id;
            SaveIndex();
            SaveManager.Reload();

            if (firstTime)
            {
                SaveData data = SaveManager.Data;
                data.learningLanguage = previous.learningLanguage;
                data.homeLanguage = previous.homeLanguage;
                data.soundEnabled = previous.soundEnabled;
                data.vibrationEnabled = previous.vibrationEnabled;
                data.showTransliteration = previous.showTransliteration;
                data.dailyLimitMinutes = previous.dailyLimitMinutes;
                SaveManager.Save();
            }

            Switched?.Invoke();
            return true;
        }

        /// <summary>Forgets the cached list so it is read from disk again (tests).</summary>
        public static void ReloadIndex()
        {
            _index = null;
        }

        private static void LoadIndex()
        {
            ProfileIndex loaded = null;

            try
            {
                if (File.Exists(IndexPath))
                {
                    loaded = JsonUtility.FromJson<ProfileIndex>(File.ReadAllText(IndexPath));
                }
            }
            catch (Exception e)
            {
                // A broken list must never hide the children's progress: the first save is still there.
                Debug.LogWarning($"[ProfileStore] Could not read the profile list, starting from the first child. {e.Message}");
            }

            if (loaded == null || loaded.profiles == null || loaded.profiles.Count == 0)
            {
                // First start with profiles (or a lost list): the existing save is the first child,
                // and any other child's save still on the phone comes back (without the name).
                loaded = new ProfileIndex();
                loaded.profiles.Add(new ChildProfile { id = FirstId });
                RecoverChildren(loaded);
                _index = loaded;
                SaveIndex();
                return;
            }

            if (loaded.profiles.Find(p => p.id == loaded.active) == null)
            {
                loaded.active = loaded.profiles[0].id;
            }

            _index = loaded;
        }

        /// <summary>Adds the children whose save files exist (footballrunner.save.p3.json and so on).</summary>
        private static void RecoverChildren(ProfileIndex index)
        {
            const string prefix = "footballrunner.save.p";
            const string suffix = ".json";

            try
            {
                var numbers = new List<int>();

                foreach (string path in Directory.GetFiles(Application.persistentDataPath, prefix + "*" + suffix))
                {
                    string file = Path.GetFileName(path);
                    string middle = file.Substring(prefix.Length, file.Length - prefix.Length - suffix.Length);

                    if (int.TryParse(middle, out int number) && number > 1)
                    {
                        numbers.Add(number);
                    }
                }

                numbers.Sort();

                foreach (int number in numbers)
                {
                    if (index.profiles.Count < MaxProfiles)
                    {
                        index.profiles.Add(new ChildProfile { id = "p" + number });
                    }

                    index.nextNumber = Math.Max(index.nextNumber, number + 1);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProfileStore] Could not look for other children's saves. {e.Message}");
            }
        }

        private static void SaveIndex()
        {
            try
            {
                // Written next to the list and swapped in, so the list is never missing (as in SaveManager).
                string temp = IndexPath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(_index, true));

                if (File.Exists(IndexPath))
                {
                    File.Replace(temp, IndexPath, null);
                }
                else
                {
                    File.Move(temp, IndexPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProfileStore] Could not write the profile list. {e.Message}");
            }
        }

        private static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "";
            }

            string trimmed = name.Trim();
            return trimmed.Length > 16 ? trimmed.Substring(0, 16) : trimmed;
        }
    }
}
