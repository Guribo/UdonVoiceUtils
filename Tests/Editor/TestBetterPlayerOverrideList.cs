using Guribo.UdonBetterAudio.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Guribo.UdonBetterAudio.Tests.Editor
{
    [TestFixture]
    public class TestBetterPlayerOverrideList
    {
        [Test]
        public void Get_EntryInvalidAtIndex()
        {
            var go = new GameObject();
            var betterPlayerAudioOverrideList = go.AddComponent<BetterPlayerAudioOverrideList>();
            var betterPlayerAudioOverride1 =go.AddComponent<BetterPlayerAudioOverride>();
            var betterPlayerAudioOverride2 =go.AddComponent<BetterPlayerAudioOverride>();
            
            betterPlayerAudioOverrideList.BetterPlayerAudioOverrides = new[]
            {
                betterPlayerAudioOverride1,
                null,
                betterPlayerAudioOverride2
            };

            Assert.AreEqual(betterPlayerAudioOverride2, betterPlayerAudioOverrideList.Get(1));
        }
    }
}