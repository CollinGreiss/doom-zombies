using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagedDoom.src.Doom.Event {

    class EventManager {

        private static readonly Dictionary<Type, List<Action<IGameEvent>>> listeners = new();

        public static void Subscribe<T>( Action<IGameEvent> callback ) where T : IGameEvent {

            var type = typeof(T);
            if ( !listeners.ContainsKey( type ) ) listeners[type] = new List<Action<IGameEvent>>();
            listeners[type].Add( callback );

        }

        public static void Raise( IGameEvent gameEvent ) {

            var type = gameEvent.GetType();
            if ( !listeners.ContainsKey( type ) ) return;
            foreach ( var listener in listeners[type] ) listener.Invoke( gameEvent );

        }

    }


}
