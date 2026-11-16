using System.Collections;
using FootballRunner.Adventure;
using FootballRunner.Gameplay;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using FootballRunner.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FootballRunner.Tests
{
    /// <summary>Tablets get the whole layout and almost the same view ahead; phones stay as they were.</summary>
    [Timeout(60000)]
    public class ScreenFitTests
    {
        private const float Phone = 19.5f / 9f;
        private const float Tablet43 = 4f / 3f;
        private const float Tablet1610 = 16f / 10f;

        [Test]
        public void PhonesKeepTheirCameraAndLayout()
        {
            Assert.IsFalse(ScreenFit.IsTablet(Phone));
            Assert.IsFalse(ScreenFit.IsTablet(ScreenFit.PhoneAspect));
            Assert.AreEqual(1f, ScreenFit.ZoomOut(Phone));
            Assert.AreEqual(6f, ScreenFit.RunnerOffset(5.5f, 6f, Phone));
        }

        [Test]
        public void TabletsZoomOutALittleAndKeepTheRunnerAtTheSameShare()
        {
            Assert.IsTrue(ScreenFit.IsTablet(Tablet43));
            Assert.IsTrue(ScreenFit.IsTablet(Tablet1610));
            Assert.AreEqual(ScreenFit.MaxZoomOut, ScreenFit.ZoomOut(Tablet43), 0.001f);
            Assert.AreEqual(ScreenFit.PhoneAspect / Tablet1610, ScreenFit.ZoomOut(Tablet1610), 0.001f);

            float phoneHalf = 5.5f * ScreenFit.PhoneAspect;
            float phoneShare = (phoneHalf - 6f) / (2f * phoneHalf);
            float tabletHalf = 5.5f * ScreenFit.ZoomOut(Tablet43) * Tablet43;
            float offset = ScreenFit.RunnerOffset(5.5f, 6f, Tablet43);
            Assert.AreEqual(phoneShare, (tabletHalf - offset) / (2f * tabletHalf), 0.001f,
                "The runner should sit at the same share of the screen as on a phone.");
            Assert.Greater(tabletHalf + offset, 0.85f * (phoneHalf + 6f), "The child should see almost as far ahead.");
        }

        [Test]
        public void TabletCanvasesExpandSoNothingIsCut()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            ScreenFit.FitCanvas(scaler, Tablet43);
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand, scaler.screenMatchMode);
            ScreenFit.FitCanvas(scaler, Phone);
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scaler.screenMatchMode);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator TheGameCameraFollowsTheScreenShape()
        {
            string saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveManager.Data.wordTutorialDone = true;
            SaveManager.Data.selectedPack = WordPack.DefaultPack;
            SaveManager.Data.selectedLevel = 0;
            AdventureSession.Clear();
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            try
            {
                Camera camera = Camera.main;
                CameraFollow follow = camera.GetComponent<CameraFollow>();
                follow.FitTo(ScreenFit.PhoneAspect);
                float phoneSize = camera.orthographicSize;
                float phoneOffset = follow.HorizontalOffset;

                follow.FitTo(Tablet43);
                Assert.AreEqual(phoneSize * ScreenFit.MaxZoomOut, camera.orthographicSize, 0.001f);
                Assert.AreNotEqual(phoneOffset, follow.HorizontalOffset);

                follow.FitTo(ScreenFit.PhoneAspect);
                Assert.AreEqual(phoneSize, camera.orthographicSize, 0.001f, "Back on a phone shape, nothing changes.");
                Assert.AreEqual(phoneOffset, follow.HorizontalOffset, 0.001f);
            }
            finally
            {
                JsonUtility.FromJsonOverwrite(saveBefore, SaveManager.Data);
                SaveManager.Save();
            }
        }
    }
}
