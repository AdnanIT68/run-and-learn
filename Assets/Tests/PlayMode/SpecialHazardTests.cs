using System.Collections;
using FootballRunner.Adventure;
using FootballRunner.Gameplay;
using FootballRunner.Obstacles;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>Flocks, the rolling football, world props, trampolines, mud and the extra backdrop bands.</summary>
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

        [UnityTest]
        public IEnumerator EveryWorldHasItsOwnGroundHazard()
        {
            yield return null;

            for (int world = 1; world <= 8; world++)
            {
                GameObject prop = SpecialHazards.CreateProp(GroundAhead(40f), "World" + world);
                SpriteRenderer picture = prop.GetComponentInChildren<SpriteRenderer>();
                Assert.IsNotNull(picture.sprite, "World" + world + " has no hazard of its own.");
                StringAssert.EndsWith("World" + world, picture.sprite.name);
                Assert.AreEqual(1, prop.GetComponent<Obstacle>().Damage);
                Assert.Less(prop.GetComponent<BoxCollider2D>().size.y, 1f, "A world hazard must stay jumpable.");
                Object.Destroy(prop);
            }
        }

        [UnityTest]
        public IEnumerator TrampolineThrowsTheRunnerHighAndNeverHurts()
        {
            PlayerHealth health = PlayerController.Instance.GetComponent<PlayerHealth>();
            int before = health.CurrentHealth;
            Collider2D body = PlayerController.Instance.GetComponent<Collider2D>();
            float ground = body.bounds.min.y;
            GameObject trampoline = SpecialHazards.CreateTrampoline(GroundAhead(2.5f));

            float highest = ground;
            float deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline &&
                   PlayerController.Instance.transform.position.x < trampoline.transform.position.x + 8f)
            {
                highest = Mathf.Max(highest, body.bounds.min.y);
                yield return null;
            }

            Assert.IsTrue(trampoline.GetComponent<Trampoline>().Used, "The runner never touched the trampoline.");
            Assert.Greater(highest - ground, 3f, "The trampoline bounce was not higher than a jump.");
            Assert.AreEqual(before, health.CurrentHealth, "A trampoline must never cost a heart.");
            Object.Destroy(trampoline);
        }

        [UnityTest]
        public IEnumerator MudSlowsTheRunnerForAMomentButNeverHurts()
        {
            PlayerHealth health = PlayerController.Instance.GetComponent<PlayerHealth>();
            int before = health.CurrentHealth;
            GameObject mud = SpecialHazards.CreateMud(GroundAhead(2.5f), "World1");

            float slowest = 1f;
            float deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline &&
                   PlayerController.Instance.transform.position.x < mud.transform.position.x + 3f)
            {
                slowest = Mathf.Min(slowest, PlayerController.Instance.SpeedFactor);
                yield return null;
            }

            Assert.IsTrue(mud.GetComponent<MudPuddle>().Splashed, "The runner never splashed into the mud.");
            Assert.AreEqual(MudPuddle.SlowFactor, slowest, 0.05f, "The mud did not slow the runner down.");
            Assert.AreEqual(before, health.CurrentHealth, "Mud must never cost a heart.");

            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(1f, PlayerController.Instance.SpeedFactor, 0.001f, "Speed did not come back after the mud.");
            Object.Destroy(mud);
        }

        [UnityTest]
        public IEnumerator WorldBackdropHasSkyAndForegroundBands()
        {
            yield return null;
            WorldScenery scenery = WorldScenery.Instance;
            Assert.IsNotNull(scenery);
            Assume.That(scenery.WorldName, Is.Not.Null, "This run uses the old backdrop.");

            Assert.AreEqual(4, scenery.LayerCount, "Expected far, mid, sky and foreground layers.");
            Assert.Greater(scenery.ForegroundCount, 0, "Nothing stands in front of the track.");
        }
    }
}
