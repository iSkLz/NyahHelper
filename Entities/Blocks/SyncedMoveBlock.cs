using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using MonoMod.Utils;
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

    public static class SyncedMoveBlockHook
    {
        public static readonly MethodInfo Controller = typeof(MoveBlock).GetMethod("Controller", Constants.PrivateInstance).GetStateMachineTarget();
        public static readonly Type ControllerClass = Controller.DeclaringType;
        public static readonly FieldInfo ControllerThis = ControllerClass.GetFields().First((fd) => fd.Name.Contains("this"));

        public static IDetour ControllerHook;

        public static void Unhook()
        {
            ControllerHook.Dispose();
            ControllerHook = null;

            On.Celeste.MoveBlock.UpdateColors -= MoveBlock_UpdateColors;
            On.Monocle.Scene.BeforeUpdate -= Scene_BeforeUpdate;
        }

        public static void Hook()
        {
            // For replacing the active color (green -> custom)
            On.Celeste.MoveBlock.UpdateColors += MoveBlock_UpdateColors;

            // For syncing movement
            On.Monocle.Scene.BeforeUpdate += Scene_BeforeUpdate;

            // For custom speed and syncing movement
            ControllerHook = new ILHook(Controller, MoveBlock_Controller);

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
        private static void MoveBlock_Controller(ILContext il)
        {
            FieldInfo fastField = typeof(MoveBlock).GetField("fast", Constants.PrivateInstance);
            FieldInfo angleField = typeof(MoveBlock).GetField("targetAngle", Constants.PrivateInstance);
            var cursor = new ILCursor(il);

            void emitThis()
            {
                cursor.Emit(OpCodes.Ldfld, ControllerThis);
            }

            //List<string> offsets = new List<string>();

            // Inject trigger broadcasting code before the routine yields 0.2f
            cursor.GotoNext(MoveType.After, (inst) => inst.MatchLdarg(0),
                (inst) => inst.MatchLdcI4(2),
                (inst) => inst.OpCode == OpCodes.Stfld);
            //offsets.Add("Before yielding 0.2f, " + cursor.Index);
            emitThis();
            //offsets.Add("After emitting this field, " + cursor.Index);
            cursor.EmitDelegate<Action<MoveBlock>>((block) => {
                if (block is SyncedMoveBlock synced)
                {
                    // We're going to trigger, so we broadcast the event
                    synced.Scene.OnEndOfFrame += () =>
                    {
                        synced.Broadcaster.BroadcastEvent("trigger");
                    };
                }
            });
            //offsets.Add("After emitting broadcast delegate, " + cursor.Index);

            // Customize the speed
            cursor.GotoNext(MoveType.After, (inst) => inst.MatchLdcR4(60f));
            //offsets.Add("After pushing 60f onto the stack, " + cursor.Index);
            emitThis();
            //offsets.Add("After emitting this field, " + cursor.Index);
            cursor.EmitDelegate<Func<MoveBlock, float, float>>((block, value) => {
                if (block is SyncedMoveBlock synced) return synced.CustomSpeed;
                else return value;
            });
            //offsets.Add("After emitting custom speed delegate, " + cursor.Index);

            // A function that replaces the check for whether the player is in control of the block
            // In other words, it syncs movement
            Func<MoveBlock, bool, bool> moveCheck = (block, value) =>
            {
                if (block is SyncedMoveBlock synced)
                {
                    synced.Move = false;
                    return value || synced.Move;
                }
                else return value;
            };

            // Inject the function in the check locations
            cursor.GotoNext((inst) => inst.MatchStfld(angleField),
                (inst) => inst.MatchLdloc(1));
            //offsets.Add("After storing targetAngle and loading local variable #1, " + cursor.Index);

            // Match the two instructions that load local variable #2, which stores the check
            cursor.GotoNext(MoveType.After, (inst) => inst.MatchLdloc(2));
            //offsets.Add("After loading local variable #2, which contains the check for moving, " + cursor.Index);
            emitThis();
            //offsets.Add("After emitting this field, " + cursor.Index);
            cursor.EmitDelegate(moveCheck);
            //offsets.Add("After emitting custom check delegate, " + cursor.Index);

            cursor.GotoNext(MoveType.After, (inst) => inst.MatchLdloc(2));
            //offsets.Add("After loading local variable #2, which contains the check for moving, " + cursor.Index);
            emitThis();
            //offsets.Add("After emitting this field, " + cursor.Index);
            cursor.EmitDelegate(moveCheck);
            //offsets.Add("After emitting custom check delegate, " + cursor.Index);
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
