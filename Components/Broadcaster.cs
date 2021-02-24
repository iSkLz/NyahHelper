using System;
using System.Collections.Generic;

using Celeste;
using Monocle;

namespace NyahHelper.Components
{
    [Tracked]
    public class Broadcaster : Component
    {
        public readonly string Channel;

        private readonly List<Action<string>> handlers;

        public Broadcaster(string channel) : base(false, false)
        {
            Channel = channel;
            handlers = new List<Action<string>>();
        }

        /// <summary>
        /// Fires an event on this Broadcaster instance
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        public void FireEvent(string channel, string eventName)
        {
            if (Channel == channel)
                foreach (var handler in handlers)
                    handler(eventName);
        }

        /// <summary>
        /// Broadcasts an event to all Broadcaster instances listening to the same channel
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        public void BroadcastEvent(string eventName)
        {
            foreach (Broadcaster broadcaster in Scene.Tracker.GetComponents<Broadcaster>())
                broadcaster.FireEvent(Channel, eventName);
        }

        /// <summary>
        /// Adds an event handler
        /// </summary>
        /// <param name="handler">An event handler which is called with fired event names</param>
        /// <param name="eventName">An event only for which the handler will be invoked</param>
        public void AddHandler(Action<string> handler, string eventName = null) {
            if (eventName is null)
            {
                handlers.Add(handler);
            }
            else
            {
                handlers.Add((e) =>
                {
                    if (e == eventName) handler(e);
                });
            }
        }
    }
}
