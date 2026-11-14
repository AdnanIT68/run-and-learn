using FootballRunner.Learning;
using NUnit.Framework;
using UnityEngine;

namespace FootballRunner.Tests
{
    /// <summary>
    /// Every word still has a picture and a voice after the import settings were
    /// tightened (60 % Vorbis, mono, compressed in memory; pictures capped at 512 px).
    /// </summary>
    public class WordAssetLoadTests
    {
        private static readonly string[] Languages = { "bs", "en", "de" };

        [Test]
        public void EveryWordHasAPictureAndAVoiceInEveryLanguage()
        {
            int checkedWords = 0;

            foreach (string packName in WordPack.PackOrder)
            {
                WordPack pack = WordPack.Load(packName);
                Assert.IsNotNull(pack, packName);

                foreach (WordEntry word in pack.Words)
                {
                    Sprite picture = pack.Picture(word.PictureId);
                    Assert.IsNotNull(picture, $"{packName}/{word.id} picture");
                    Assert.LessOrEqual(picture.texture.width, 512, $"{packName}/{word.id} picture size");

                    foreach (string language in Languages)
                    {
                        AudioClip voice = pack.Voice(language, word.id);
                        Assert.IsNotNull(voice, $"{packName}/{word.id} voice {language}");
                        Assert.Greater(voice.length, 0.1f, $"{packName}/{word.id} voice {language} length");
                    }

                    checkedWords++;
                }
            }

            Assert.Greater(checkedWords, 70, "All eight worlds were checked.");
        }

        [Test]
        public void VoicesAreMonoAndStayCompressedInMemory()
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>("Words");
            Assert.Greater(clips.Length, 500);

            foreach (AudioClip clip in clips)
            {
                Assert.AreEqual(1, clip.channels, clip.name);
                Assert.AreEqual(AudioClipLoadType.CompressedInMemory, clip.loadType, clip.name);
                Assert.IsTrue(clip.LoadAudioData() || clip.loadState == AudioDataLoadState.Loaded, clip.name);
            }
        }
    }
}
