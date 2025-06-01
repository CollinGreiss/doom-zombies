using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagedDoom.src.Doom.Event {

    interface IGameEvent { }

    class MobKilledEvent : IGameEvent {

        public Mobj Source;
        public Mobj Target;

        public MobKilledEvent( Mobj source, Mobj target ) {

            Source = source;
            Target = target;

        }

    }
    public class MobDamagedEvent : IGameEvent {

        public Mobj Source;
        public Mobj Target;

        public MobDamagedEvent( Mobj source, Mobj target ) {

            Source = source;
            Target = target;

        }

    }

}
