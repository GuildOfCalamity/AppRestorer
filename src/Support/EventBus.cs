using System;

namespace AppRestorer
{
    /// <summary>
    /// Defines our <see cref="ObjectEventBusArgs"/>-based event bus class.
    /// The <see cref="EventBus"/> provides a way for other pages to push 
    /// notifications back to the <see cref="MainWindow"/>.
    /// </summary>
    public class EventBus : IDisposable
    {
        bool disposed = false;
        Dictionary<string, EventHandler<ObjectEventBusArgs>> eventHandlers = new Dictionary<string, EventHandler<ObjectEventBusArgs>>();

        /// <summary>
        /// Adds an event to the handler pool.
        /// </summary>
        public void Subscribe(string eventName, EventHandler<ObjectEventBusArgs> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null)
            {
                Extensions.WriteToLog($"EventBus: \"{nameof(eventName)}\" or \"{nameof(handler)}\" is null.");
                return;
            }

            if (!eventHandlers.ContainsKey(eventName))
            {   // Create the key.
                eventHandlers[eventName] = null;
            }

            // Add the new handler for the key.
            eventHandlers[eventName] += handler;
        }

        /// <summary>
        /// Removes an event from the handler pool.
        /// </summary>
        public void Unsubscribe(string eventName, EventHandler<ObjectEventBusArgs> handler)
        {
            if (!string.IsNullOrEmpty(eventName) && eventHandlers.ContainsKey(eventName) && handler != null)
            {   // Remove the existing handler.
                eventHandlers[eventName] -= handler;
            }
        }

        /// <summary>
        /// Calls <see cref="Unsubscribe(string, EventHandler{ObjectEventBusArgs})"/> and then <see cref="Subscribe(string, EventHandler{ObjectEventBusArgs})"/>.
        /// </summary>
        public void Resubscribe(string eventName, EventHandler<ObjectEventBusArgs> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null)
            {
                Extensions.WriteToLog($"EventBus: \"{nameof(eventName)}\" or \"{nameof(handler)}\" is null.");
                return;
            }
            Unsubscribe(eventName, handler);
            Subscribe(eventName, handler);
        }

        /// <summary>
        /// Causes an event to be invoked through the handler pool.
        /// </summary>
        public void Publish(string eventName, object payload)
        {
            if (!string.IsNullOrEmpty(eventName) && eventHandlers.ContainsKey(eventName))
            {
                EventHandler<ObjectEventBusArgs> handlers = eventHandlers[eventName];
                handlers?.Invoke(this, new ObjectEventBusArgs(payload));
            }
            else
            {
                Extensions.WriteToLog($"EventBus: \"{nameof(eventName)}\" key \"{eventName}\" was not found. If you wish to use this you must first Subscribe it.");
            }
        }

        /// <summary>
        /// Reports if an event already resides in the handler pool.
        /// </summary>
        public bool IsSubscribed(string eventName)
        {
            return eventHandlers.ContainsKey(eventName);
        }

        #region [IDispose]
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    eventHandlers.Clear(); // Cleanup managed resources.

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer for safety.
        /// </summary>
        ~EventBus() => Dispose();
        #endregion
    }

    #region [EventArg Model]
    /// <summary>
    /// Define our event args class which uses an object value 
    /// that could be switched upon in the main UI update routine.  
    /// More complex types could be passed to encapsulate additional information.
    /// </summary>
    public class ObjectEventBusArgs : EventArgs
    {
        public object Payload { get; }
        public ObjectEventBusArgs(object payload)
        {
            Payload = payload;
        }
    }
    #endregion
}
