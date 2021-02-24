using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NyahHelper.Components;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;

namespace NyahHelper.Entities.Blocks
{
    public static class SyncedMoveBlockHook
    {
        public static SyncedMoveBlock ToPatch;

        public static void Hook()
        {
            for (int i = 0; i < SyncedMoveBlock.SyncTracker.Length; i++)
            {
                SyncedMoveBlock.SyncTracker[i] = new List<SyncedMoveBlock>();
            }

            // For replacing the active color (green -> custom)
            On.Celeste.MoveBlock.UpdateColors += MoveBlock_UpdateColors;

            // For syncing movement
            On.Celeste.Solid.HasPlayerOnTop += Solid_HasPlayerOnTop;
            On.Celeste.Solid.HasPlayerClimbing += Solid_HasPlayerClimbing;

            // For custom speed
            // This is a VERY VERY dumb way of doing it but I can't really figure out any better way
            // As of writing this I am offline so I have no resources whatsoever
            On.Celeste.MoveBlock.Controller += MoveBlock_Controller;
            On.Celeste.SoundSource.Play += SoundSource_Play;

            Logger.Log("Nyah Helper", "Synced Move Block hooks executed");
        }

        #region Sync Movement Hooks
        private static bool Solid_HasPlayerClimbing(On.Celeste.Solid.orig_HasPlayerClimbing orig, Solid self)
        {
            // If the caller is a synced move block which's color is active
            if (self is SyncedMoveBlock block && SyncedMoveBlock.SyncTracker[block.Hue].Count > 0) return true;
            else return orig(self);
        }

        private static bool Solid_HasPlayerOnTop(On.Celeste.Solid.orig_HasPlayerOnTop orig, Solid self)
        {
            if (self is SyncedMoveBlock block && SyncedMoveBlock.SyncTracker[block.Hue].Count > 0) return true;
            else return orig(self);
        }
        #endregion

        #region Custom Speed Hooks
        private static SoundSource SoundSource_Play(On.Celeste.SoundSource.orig_Play orig, SoundSource self, string path, string param, float value)
        {
            // If we have a synced move block to patch
            if (!(ToPatch is null))
            {
                ToPatch.FullSpeed = ToPatch.CustomSpeed;
                // Set back to null for next calls
                ToPatch = null;
            }
            return orig(self, path, param, value);
        }

        private static IEnumerator MoveBlock_Controller(On.Celeste.MoveBlock.orig_Controller orig, MoveBlock self)
        {
            // If not a synced move block, use the original routine
            if (!(self is SyncedMoveBlock))
            {
                yield return orig(self);
            }

            // Below code is based on decompiled code

            var enumerator = orig(self);
            object cur;

            void step()
            {
                enumerator.MoveNext();
                cur = enumerator.Current;
            }

            // Mimic the original routine by yielding what it yields
            while (true)
            {
                // Cycle 1: Before triggering
                step();
                while (cur is null)
                {
                    yield return null;
                    step();
                }

                yield return cur; // 0.2f
                // Below the yield 0.2f line, there is a line that sets the speed then a line that triggers a sound effect
                // What I'm doing is temporarily setting the current move block to a static field before calling on the coroutine code
                // Then I patch the sound effect method to check for that field and edit the speed
                ToPatch = (SyncedMoveBlock)self;

                // Cycle 2: Moving
                step();
                while (cur is null)
                {
                    yield return null;
                    step();
                }

                yield return cur; // 0.2f
                step();
                yield return cur; // 2.2f

                // Cycle 3: Breaking
                step();
                while (cur is null)
                {
                    yield return null;
                    step();
                }

                yield return cur; // 0.2f
                step();
                yield return cur; // 0.6f
            }
        }
        #endregion

        #region Color Hook
        private static void MoveBlock_UpdateColors(On.Celeste.MoveBlock.orig_UpdateColors orig, MoveBlock self)
        {
            if (self is SyncedMoveBlock syncedSelf) syncedSelf.UpdateColors();
            else orig(self);
        }
        #endregion
    }

    [CustomEntity("nyahhelper/syncedmoveblock")]
    public class SyncedMoveBlock : MoveBlock
    {
        // There exist 360 different possible synced colors
        // This tracks every synced move blocks of every sync color
        public static List<SyncedMoveBlock>[] SyncTracker = new List<SyncedMoveBlock>[361];

        public enum MovementState
        {
            Idling,
            Moving,
            Breaking
        }

        #region Reflection Fields
        static readonly FieldInfo _topButton = typeof(MoveBlock).GetField("topButton", Constants.PrivateInstance);
        static readonly FieldInfo _leftButton = typeof(MoveBlock).GetField("leftButton", Constants.PrivateInstance);
        static readonly FieldInfo _rightButton = typeof(MoveBlock).GetField("rightButton", Constants.PrivateInstance);

        static readonly FieldInfo _state = typeof(MoveBlock).GetField("state", Constants.PrivateInstance);
        static readonly FieldInfo _direction = typeof(MoveBlock).GetField("direction", Constants.PrivateInstance);

        static readonly FieldInfo _fill = typeof(MoveBlock).GetField("fillColor", Constants.PrivateInstance);

        static readonly FieldInfo _fullspeed = typeof(MoveBlock).GetField("targetSpeed", Constants.PrivateInstance);

        public static readonly Color IdleColor = (Color)typeof(MoveBlock).GetField("idleBgFill",
            Constants.PrivateStatic).GetValue(null);
        public static readonly Color BreakColor = (Color)typeof(MoveBlock).GetField("breakingBgFill",
            Constants.PrivateStatic).GetValue(null);

        public List<Image> TopButton
        {
            get
            {
                return (List<Image>)(_topButton.GetValue(this));
            }
            set
            {
                _topButton.SetValue(this, value);
            }
        }

        public List<Image> LeftButton
        {
            get
            {
                return (List<Image>)(_leftButton.GetValue(this));
            }
            set
            {
                _leftButton.SetValue(this, value);
            }
        }

        public List<Image> RightButton
        {
            get
            {
                return (List<Image>)(_rightButton.GetValue(this));
            }
            set
            {
                _rightButton.SetValue(this, value);
            }
        }

        public MovementState State
        {
            get
            {
                return (MovementState)(int)_state.GetValue(this);
            }
            set
            {
                _state.SetValue(this, (int)value);
            }
        }

        public Directions Direction
        {
            get
            {
                return (Directions)_direction.GetValue(this);
            }
            set
            {
                _direction.SetValue(this, value);
            }
        }

        public Color Fill
        {
            get
            {
                return (Color)_fill.GetValue(this);
            }
            set
            {
                _fill.SetValue(this, value);
            }
        }

        public float FullSpeed
        {
            get
            {
                return (float)_fullspeed.GetValue(this);
            }
            set
            {
                _fullspeed.SetValue(this, value);
            }
        }
        #endregion

        public Color Color;
        public int Hue;
        public float CustomSpeed;

        public SyncedMoveBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, data.Enum("direction", Directions.Left), data.Bool("canSteer"), false)
        {
            // 0.73f and 0.7f are constant to give the same shade as the original color
            // Hue is capped to 360 (degrees)
            Color = Calc.HsvToColor((Hue = Math.Min(data.Int("colorHue", 122), 360)) / 360f, 0.73f, 0.7f);

            CustomSpeed = data.Float("customSpeed", 60f);
        }

        public override void Update()
        {
            base.Update();

            // Condition based on decompiled code from the Controller coroutine
            if (State == MovementState.Moving && (((int)Direction > 1) ? HasPlayerClimbing() : HasPlayerOnTop()))
            {
                if (!SyncTracker[Hue].Contains(this))
                    SyncTracker[Hue].Add(this);
            }
            else if (State != MovementState.Moving)
            {
                // TODO: Remove the comments after enough time
                // I can get away with, without having to check for every synced block of the same hue
                // If this one is the last one to update, this will be corrected in the next frame
                // If it's not, coroutines are updated after entities so the next block will correct
                // This will not affect the current block because it's not moving anyways
                // Edit: Actually this is a problem, if the previous block to update is active and the next one isn't, this will incorrectly not trigger the next one
                // Edit From Later: Such good times!
                // - SyncedMoveBlockHook.States[Hue] = false;

                if (SyncTracker[Hue].Contains(this))
                    SyncTracker[Hue].Remove(this);
            }
        }

        public void UpdateColors()
        {
            // Based on decompiled code

            Color color;
            switch (State)
            {
                case MovementState.Moving:
                    color = Color;
                    break;
                case MovementState.Breaking:
                    color = BreakColor;
                    break;
                default:
                    color = IdleColor;
                    break;
            }

            Fill = Color.Lerp(Fill, color, 10f * Engine.DeltaTime);

            foreach (Image img in TopButton)
            {
                img.Color = color;
            }
            foreach (Image img in LeftButton)
            {
                img.Color = color;
            }
            foreach (Image img in RightButton)
            {
                img.Color = color;
            }
        }
    }
}
