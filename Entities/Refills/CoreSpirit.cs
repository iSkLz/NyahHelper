using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;

namespace NyahHelper.Entities.Refills
{
    public static class CoreSpiritHook
    {
        public static bool Immune = false;
        public static CoreSpirit.SpiritMode Mode;

        // TODO: Unhook
        public static void Hook()
        {
            // On touching ice and lava blocks
            On.Celeste.IceBlock.OnPlayer += IceBlock_OnPlayer;
            On.Celeste.FireBarrier.OnPlayer += FireBarrier_OnPlayer;

            // On touch fireballs and their ice counterpart
            On.Celeste.FireBall.KillPlayer += FireBall_KillPlayer;

            // On death
            On.Celeste.Player.Die += Player_Die;

            Logger.Log("Nyah Helper", "Core Spirit hooks executed");
        }

        private static void FireBall_KillPlayer(On.Celeste.FireBall.orig_KillPlayer orig, FireBall self, Player player)
        {
            // If not immune or the core mode doesn't match the core spirit mode, die
            if (!Immune || (int)Mode != ((int)player.SceneAs<Level>().CoreMode - 1))
                orig(self, player);
        }

        private static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            // Reset on death
            Immune = false;
            return orig(self, direction, evenIfInvincible, registerDeathInStats);
        }

        public static void FireBarrier_OnPlayer(On.Celeste.FireBarrier.orig_OnPlayer orig, FireBarrier self, Player player)
        {
            // If not immune, or the mode isn't set to hot, die
            if (!Immune || Mode == CoreSpirit.SpiritMode.Cold) orig(self, player);
        }

        public static void IceBlock_OnPlayer(On.Celeste.IceBlock.orig_OnPlayer orig, IceBlock self, Player player)
        {
            // If not immune, or the mode isn't set to cold, die
            if (!Immune || Mode == CoreSpirit.SpiritMode.Hot) orig(self, player);
        }
    }

    [CustomEntity("nyahhelper/corespirit")]
    public class CoreSpirit : Refill
    {
        public enum SpiritMode
        {
            Hot, Cold
        }

        public SpiritMode Mode;
        public float Duration;
        public bool Synchronized;

        public PlayerCollider PlayerCollider;
        public CoreModeListener CoreListener;

        public Sprite ColdSprite;
        public Sprite HotSprite;

        #region Reflection Fields
        static readonly FieldInfo _sprite = typeof(Refill).GetField("sprite", Constants.PrivateInstance);
        static readonly FieldInfo _outline = typeof(Refill).GetField("outline", Constants.PrivateInstance);
        static readonly FieldInfo _respawnTimer = typeof(Refill).GetField("respawnTimer", Constants.PrivateInstance);

        public Sprite Sprite
        {
            get
            {
                return (Sprite)_sprite.GetValue(this);
            }
            set
            {
                _sprite.SetValue(this, value);
            }
        }

        public Image Outline
        {
            get
            {
                return (Image)_outline.GetValue(this);
            }
            set
            {
                _outline.SetValue(this, value);
            }
        }

        public float RespawnTimer
        {
            get
            {
                return (float)_respawnTimer.GetValue(this);
            }
            set
            {
                _respawnTimer.SetValue(this, value);
            }
        }
        #endregion

        #region Reflection Methods
        static readonly MethodInfo _refillRoutine = typeof(Refill).GetMethod("RefillRoutine", Constants.PrivateInstance);

        public IEnumerator RefillRoutine(params object[] args)
        {
            return (IEnumerator)_refillRoutine.Invoke(this, args);
        }
        #endregion

        public CoreSpirit(EntityData data, Vector2 offset) : base(data.Position + offset, false, data.Bool("oneUse"))
        {
            Duration = data.Float("duration", 0f);

            Sprite.RemoveSelf();

            // Replace the outline
            Outline.RemoveSelf();
            Add(Outline = new Image(GFX.Game["objects/nyahhelper/corespirit/outline"]));
            Outline.CenterOrigin();
            Outline.Visible = false;

            // Get sprites
            Add(HotSprite = new Sprite(GFX.Game, "objects/nyahhelper/corespirit/hot/idle"));
            HotSprite.AddLoop("idle", "", 0.1f);
            HotSprite.Play("idle");
            HotSprite.CenterOrigin();
            Add(ColdSprite = new Sprite(GFX.Game, "objects/nyahhelper/corespirit/cold/idle"));
            ColdSprite.AddLoop("idle", "", 0.1f);
            ColdSprite.Play("idle");
            ColdSprite.CenterOrigin();

            // Initialize sprites
            switch (Mode = data.Enum("mode", SpiritMode.Hot))
            {
                case SpiritMode.Hot:
                    HotSprite.Visible = true;
                    ColdSprite.Visible = false;
                    Sprite = HotSprite;
                    break;
                case SpiritMode.Cold:
                    HotSprite.Visible = false;
                    ColdSprite.Visible = true;
                    Sprite = ColdSprite;
                    break;
            }

            // Add a core mode synchronizer
            if (Synchronized = data.Bool("syncWithCoreMode"))
            {
                Add(CoreListener = new CoreModeListener(OnCoreMode));
            }

            // Hook the PlayerCollider event
            PlayerCollider = Get<PlayerCollider>();
            var origAction = PlayerCollider.OnCollide;
            PlayerCollider.OnCollide = (Player player) =>
            {
                // TODO: Add an outline to the player
                CoreSpiritHook.Immune = true;
                CoreSpiritHook.Mode = Mode;

                // Revoke after the set duration, if it's not set to infinite
                if (Duration > 0)
                    Add(new Coroutine(RevokeInvincibilityRoutine()));

                Audio.Play("event:/game/general/diamond_touch", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                Collidable = false;
                Add(new Coroutine(RefillRoutine(player)));
                RespawnTimer = 2.5f;

                // Use the original refill routine with the current active sprite
                Sprite = Mode == SpiritMode.Hot ? HotSprite : ColdSprite;
                origAction(player);
            };
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            // Re-initialize if synchronized
            if (Synchronized) OnCoreMode(SceneAs<Level>().Session.CoreMode);
        }

        public override void Update()
        {
            base.Update();
        }

        public void OnCoreMode(Session.CoreModes mode)
        {
            switch (mode)
            {
                case Session.CoreModes.Hot:
                    Mode = SpiritMode.Hot;
                    if (RespawnTimer <= 0)
                    {
                        HotSprite.Visible = true;
                        ColdSprite.Visible = false;
                    }
                    Sprite = HotSprite;
                    break;
                case Session.CoreModes.Cold:
                    Mode = SpiritMode.Cold;
                    if (RespawnTimer <= 0)
                    {
                        HotSprite.Visible = false;
                        ColdSprite.Visible = true;
                    }
                    Sprite = ColdSprite;
                    break;
            }
        }

        public IEnumerator RevokeInvincibilityRoutine()
        {
            yield return Duration;
            CoreSpiritHook.Immune = false;
        }
    }
}
