using System.Collections;
using FootballRunner.Adventure;
using FootballRunner.Learning;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using FootballRunner.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CharacterRoster = FootballRunner.Player.CharacterRoster;

namespace FootballRunner.Tests
{
    /// <summary>Caps, glasses and a scarf, bought only with game coins, worn on the runner's head.</summary>
    [Timeout(60000)]
    public class OutfitTests
    {
        private static readonly string[] Frames = { "Idle", "Run_01", "Run_02", "Run_03", "Run_04", "Run_05", "Jump_Up", "Fall_Air", "Hit" };
        private string _saveBefore;

        [SetUp]
        public void FreshWardrobe()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveData data = SaveManager.Data;
            data.unlockedItems.RemoveAll(i => i.StartsWith("outfit_"));
            data.outfitHead = data.outfitEyes = data.outfitNeck = "";
            data.totalCoins = 0;
            data.wordTutorialDone = true;
            data.selectedPack = WordPack.DefaultPack;
            data.selectedLevel = 0;
            data.pepiWelcomed.Add("World1:" + LearningProgress.Language);
            AdventureSession.Clear();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void ItemsCostGameCoinsAndNeedEnoughOfThem()
        {
            Assert.IsFalse(Outfits.Buy("cap"), "Bought without coins.");
            SaveManager.Data.totalCoins = 35;
            Assert.IsTrue(Outfits.Buy("cap"));
            Assert.AreEqual(5, SaveManager.Data.totalCoins);
            Assert.IsTrue(Outfits.IsOwned("cap"));
            Assert.IsTrue(Outfits.IsWorn("cap"), "A bought item should be put on at once.");
            Assert.IsFalse(Outfits.Buy("cap"), "The same item was bought twice.");

            foreach (Outfits.Item item in Outfits.All)
            {
                Assert.Greater(item.Price, 0);
                Assert.IsNotNull(Outfits.Picture(item.Id), item.Id + " has no picture");
            }
        }

        [Test]
        public void OneItemPerPlaceAndItCanComeOff()
        {
            SaveManager.Data.totalCoins = 500;
            Outfits.Buy("cap");
            Outfits.Buy("crown");
            Assert.AreEqual("crown", Outfits.Worn(Outfits.Slot.Head).Id, "Only one thing fits on the head.");
            Assert.IsFalse(Outfits.IsWorn("cap"));

            Outfits.Buy("sunglasses");
            Outfits.Buy("scarf");
            Assert.AreEqual("sunglasses", Outfits.Worn(Outfits.Slot.Eyes).Id);
            Assert.AreEqual("scarf", Outfits.Worn(Outfits.Slot.Neck).Id);

            Outfits.TakeOff(Outfits.Slot.Head);
            Assert.IsNull(Outfits.Worn(Outfits.Slot.Head));
            Assert.IsTrue(Outfits.Wear("cap"));
            Assert.IsFalse(Outfits.Wear("glasses"), "An item not bought cannot be worn.");
        }

        [Test]
        public void EveryKidHasAHeadInEveryFrame()
        {
            foreach (CharacterRoster.Entry kid in CharacterRoster.All)
            {
                foreach (string frame in Frames)
                {
                    Assert.IsTrue(Outfits.TryGetHead(kid.Id + "/" + frame, out Outfits.Head head), $"{kid.Id}/{frame}");
                    Assert.Greater(head.Width, 10f, $"{kid.Id}/{frame} head is too small");
                }
            }
        }

        [Test]
        public void AHatSitsAboveTheGlassesAndTheScarfBelow()
        {
            Outfits.TryGetHead("emir/Idle", out Outfits.Head head);
            Outfits.Place(Outfits.Slot.Head, head, out Vector2 hat, out _);
            Outfits.Place(Outfits.Slot.Eyes, head, out Vector2 glasses, out _);
            Outfits.Place(Outfits.Slot.Neck, head, out Vector2 scarf, out _);
            Assert.Greater(hat.y, glasses.y);
            Assert.Greater(glasses.y, scarf.y);
        }

        [UnityTest]
        public IEnumerator TheRunnerWearsWhatWasBought()
        {
            SaveManager.Data.totalCoins = 200;
            Outfits.Buy("cap");
            Outfits.Buy("sunglasses");
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            yield return null;

            OutfitRenderer renderer = Object.FindFirstObjectByType<OutfitRenderer>();
            Assert.IsNotNull(renderer);
            SpriteRenderer cap = renderer.Part(Outfits.Slot.Head);
            SpriteRenderer glasses = renderer.Part(Outfits.Slot.Eyes);
            Assert.IsNotNull(cap, "The cap is not drawn.");
            Assert.IsNotNull(glasses, "The glasses are not drawn.");
            Assert.IsNull(renderer.Part(Outfits.Slot.Neck), "Nothing is worn at the neck.");
            Assert.Greater(cap.bounds.center.y, glasses.bounds.center.y, "The cap should sit above the glasses.");
        }

        [UnityTest]
        public IEnumerator TheWardrobeBuysPutsOnAndTakesOff()
        {
            SaveManager.Data.totalCoins = 45;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            var chooser = Object.FindFirstObjectByType<CharacterChooserUi>();
            chooser.Open();
            chooser.OpenOutfits();
            var shop = Object.FindFirstObjectByType<OutfitShopUi>();
            Assert.IsTrue(shop.IsOpen);

            Assert.IsFalse(shop.Press("crown"), "The crown costs more than the child has.");
            Assert.IsTrue(shop.Press("scarf"));
            Assert.AreEqual(5, SaveManager.Data.totalCoins);
            Assert.IsTrue(Outfits.IsWorn("scarf"));
            Assert.IsTrue(shop.Press("scarf"), "Tapping a worn item takes it off.");
            Assert.IsFalse(Outfits.IsWorn("scarf"));
            shop.Close();
        }
    }
}
