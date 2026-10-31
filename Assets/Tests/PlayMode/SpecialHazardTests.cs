using System.Collections;
using FootballRunner.Adventure;
using FootballRunner.Obstacles;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>Flocks to pass under by staying down, and the football that rolls towards the runner.</summary>
    [Timeout(60000)]
    public class SpecialHazardTests
    {
        private string _saveBefore;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            Debug.Log("[TEST] " + TestContext.CurrentContext.Test.Name);
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveManager.Data.wordTutorialDone = true;
            Time.timeScale = 1f;
            AdventureSession.Clear();
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        private static Vector3 GroundAhead(float distance)
        {
            Collider2D body = PlayerController.Instance.GetComponent<Collider2D>();
            return new Vector3(PlayerController.Instance.transform.position.x + distance, body.bounds.min.y, 0f);
        }

        [UnityTest]
        public IEnumerator RunnerOnTheGroundPassesUnderTheFlock()
        {
            PlayerHealth health = PlayerController.Instance.GetComponent<PlayerHealth>();
            int before = health.CurrentHealth;
            GameObject flock = SpecialHazards.CreateFlyers(GroundAhead(3f), "World1");

            float x = flock.transform.position.x;
            float deadline = Time.realtimeSinceStartup + 6f;
            while (PlayerController.Instance.transform.position.x < x + 2f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.Greater(PlayerController.Instance.transform.position.x, x + 2f, "The runner never got past the flock.");
            Assert.AreEqual(before, health.CurrentHealth, "Staying on the ground under the flock still cost a heart.");
            Object.Destroy(flock);
        }

        [UnityTest]
        public IEnumerator JumpingIntoTheFlockCostsAHeart()
        {
            PlayerHealth health = PlayerController.Instance.GetComponent<PlayerHealth>();
            int before = health.CurrentHealth;
            GameObject flock = SpecialHazards.CreateFlyers(GroundAhead(2.2f), "World2");

            // A jump just before the flock, the way a child taps out of habit.
            var body = PlayerController.Instance.GetComponent<Rigidbody2D>();
            body.linearVelocity = new Vector2(body.linearVelocity.x, 14f);

            float deadline = Time.realtimeSinceStartup + 3f;
            while (health.CurrentHealth == before && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(before - 1, health.CurrentHealth, "Jumping into the flock should cost a heart.");
            Object.Destroy(flock);
        }

        [UnityTest]
        public IEnumerator EveryWorldHasItsOwnFlyerPicture()
        {
            yield return null;

            for (int world = 1; world <= 8; world++)
            {
                GameObject flock = SpecialHazards.CreateFlyers(GroundAhead(40f), "World" + world);
                SpriteRenderer picture = flock.GetComponentInChildren<SpriteRenderer>();
                Assert.IsNotNull(picture.sprite, "World" + world + " has no flyer picture.");
                StringAssert.EndsWith("World" + world, picture.sprite.name);
                Assert.IsNotNull(flock.GetComponent<Obstacle>());
                Object.Destroy(flock);
            }
        }

        [UnityTest]
        public IEnumerator BallWaitsThenRollsTowardsTheRunner()
        {
            GameObject far = SpecialHazards.CreateBall(GroundAhead(40f));
            GameObject near = SpecialHazards.CreateBall(GroundAhead(9f));
            float farX = far.transform.position.x;
            float nearX = near.transform.position.x;

            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(far.GetComponent<RollingBall>().IsRolling, "A far ball should wait for the runner.");
            Assert.AreEqual(farX, far.transform.position.x, 0.001f);
            Assert.IsTrue(near.GetComponent<RollingBall>().IsRolling, "A near ball should roll.");
            Assert.Less(near.transform.position.x, nearX - 0.5f, "The ball did not roll towards the runner.");

            Object.Destroy(far);
            Object.Destroy(near);
        }
    }
}
