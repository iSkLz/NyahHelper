using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;

using NyahHelper.Components;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;

namespace NyahHelper.Entities.Blocks
{
    // TODO: Separate the synced move block from the customizable move block (speed and color)
    // Hooks will conflict when other move block variations are added
    // TODO 2: Similar hooks are required for custom move blocks, so maybe figure out a way to keep everything clean? Like a ModdableMoveBlock that manages hooks

    public static class SyncedMoveBlockHook
    {
        public static float SpeedPatch;
        static readonly FieldInfo _speedPatch = typeof(SyncedMoveBlockHook).GetField(nameof(SpeedPatch), Constants.PublicStatic);

        public static bool MovePatch;
        static readonly FieldInfo _movePatch = typeof(SyncedMoveBlockHook).GetField(nameof(MovePatch), Constants.PublicStatic);

        public static IDetour ControllerILHook;

        public static void Unhook()
        {
            ControllerILHook.Dispose();
            ControllerILHook = null;

            On.Celeste.MoveBlock.UpdateColors -= MoveBlock_UpdateColors;
            On.Celeste.MoveBlock.Controller -= MoveBlock_Controller;
            On.Monocle.Scene.BeforeUpdate -= Scene_BeforeUpdate;
        }

        public static void Hook()
        {
            // For replacing the active color (green -> custom)
            On.Celeste.MoveBlock.UpdateColors += MoveBlock_UpdateColors;

            // For syncing movement
            On.Monocle.Scene.BeforeUpdate += Scene_BeforeUpdate;

            // For custom speed and syncing movement
            On.Celeste.MoveBlock.Controller += MoveBlock_Controller;
            ControllerILHook = new ILHook(typeof(MoveBlock).GetNestedType("<Controller>d__45", BindingFlags.NonPublic).GetMethod("MoveNext", Constants.All), MoveBlock_ControllerIL);

            Logger.Log("Nyah Helper", "Synced Move Block hooks executed");
        }

        #region Frame Start Hook
        private static void Scene_BeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self)
        {
            orig(self);
            var list = self.Tracker.GetEntities<SyncedMoveBlock>();
            var updated = new bool[361];

            foreach (SyncedMoveBlock block in list)
            {
                if (!updated[block.Hue] && block.Controlled)
                {
                    updated[block.Hue] = true; // Only broadcast once per color
                    block.Broadcaster.BroadcastEvent("move");
                }
            }
        }
        #endregion

        #region Controller Routine IL Hook
        private static void MoveBlock_ControllerIL(ILContext il)
        {
            FieldInfo fastField = typeof(MoveBlock).GetField("fast", Constants.PrivateInstance);
            FieldInfo angleField = typeof(MoveBlock).GetField("targetAngle", Constants.PrivateInstance);
            var cursor = new ILCursor(il);

            // Static fields used here are set in the other coroutine hook

            // Replace the instruction that changes the targetSpeed to 60f (in case of fast == false) with an instruction that loads directly from the static field
            // Since the synced move block always sets fast to false, we can only change this instruction and default to 60f for vanilla blocks
            cursor.GotoNext((inst) => inst.MatchLdcR4(60f));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _speedPatch); // Load the static field instead

            // Replace the check for whether the player is in control of the block with the static field
            cursor.GotoNext((inst) => inst.MatchStfld(angleField),
                (inst) => inst.MatchLdloc(1));
            cursor.GotoNext((inst) => inst.MatchLdloc(2));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _movePatch);
            cursor.GotoNext((inst) => inst.MatchLdloc(2));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _movePatch);
        }
        #endregion

        #region Controller Routine Hook
        private static IEnumerator MoveBlock_Controller(On.Celeste.MoveBlock.orig_Controller orig, MoveBlock self)
        {
            var enumerator = orig(self);
            object cur;

            void step()
            {
                enumerator.MoveNext();
                cur = enumerator.Current;
            }

            var dir = (int)typeof(MoveBlock).GetField("direction", Constants.PrivateInstance).GetValue(self);

            // Mimic the original routine by yielding what it yields
            // Below code is based on decompiled code
            while (true)
            {
                // Cycle 1: Before triggering
                step();
                while (cur is null)
                {
                    yield return null;
                    step();
                }

                if (self is SyncedMoveBlock syncedSelf)
                {
                    // We're going to trigger, so we broadcast the event
                    self.Scene.OnEndOfFrame += () =>
                    {
                        syncedSelf.Broadcaster.BroadcastEvent("trigger");
                    };

                    // We also wait for other blocks to trigger, for perfect synchronization
                    // TODO: Find a way to trigger without desyncing with vanilla blocks
                    yield return null;
                    yield return null;

                    yield return cur; // 0.2f

                    // After the yield 0.2f line, there is a line that sets the speed
                    // The patched IL code loads the speed from the static field, so we set it to the custom speed in case of a synced block
                    SpeedPatch = syncedSelf.CustomSpeed;
                }
                else
                {
                    yield return cur; // 0.2f

                    // We do the same for the vanilla speed
                    SpeedPatch = 60f;
                }

                // Cycle 2: Moving
                if (self is SyncedMoveBlock syncedSelf2)
                {
                    MovePatch = syncedSelf2.Move || syncedSelf2.Controlled;
                    step();
                    while (cur is null)
                    {
                        yield return null;
                        MovePatch = syncedSelf2.Move || syncedSelf2.Controlled;
                        step();
                    }
                }
                else
                {
                    // A non-synced block, do the vanilla check
                    MovePatch = (dir > 1) ? self.HasPlayerClimbing() : self.HasPlayerOnTop();
                    step();
                    while (cur is null)
                    {
                        yield return null;
                        MovePatch = (dir > 1) ? self.HasPlayerClimbing() : self.HasPlayerOnTop();
                        step();
                    }
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
    [Tracked]
    public class SyncedMoveBlock : MoveBlock
    {
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

        static readonly FieldInfo _trigger = typeof(MoveBlock).GetField("triggered", Constants.PrivateInstance);

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

        public bool Trigger
        {
            get
            {
                return (bool)_trigger.GetValue(this);
            }
            set
            {
                _trigger.SetValue(this, value);
            }
        }
        #endregion

        public Color Color;
        public int Hue;
        public float CustomSpeed;

        public bool Move;

        public Broadcaster Broadcaster;

        // Decompiled condition
        public bool Controlled => ((int)Direction > 1) ? HasPlayerClimbing() : HasPlayerOnTop();

        public SyncedMoveBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, data.Enum("direction", Directions.Left), data.Bool("canSteer"), false)
        {
            // 0.73f and 0.7f are constant to give the same shade as the original color
            // Hue is capped to 360 (degrees)
            Color = Calc.HsvToColor((Hue = Math.Max(0, Math.Min(data.Int("colorHue", 122), 360))) / 360f, 0.73f, 0.7f);

            CustomSpeed = data.Float("customSpeed", 60f);

            Add(Broadcaster = new Broadcaster("nyahhelper/syncedmoveblock/" + Hue));

            // Listen to the trigger event
            Broadcaster.AddHandler((e) =>
            {
                Trigger = true;
            }, "trigger");

            Broadcaster.AddHandler((e) =>
            {
                Move = true;
            }, "move");
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
