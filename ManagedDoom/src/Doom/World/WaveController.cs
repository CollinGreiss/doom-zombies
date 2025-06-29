﻿using ManagedDoom.src.Doom.Event;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ManagedDoom {

    public sealed class WaveController {

        private static WaveController _instance;

        public static WaveController Instance {

            get {

                if ( _instance == null ) _instance = new WaveController();
                return _instance;

            }

        }

        private int wave = 0;

        private int monsterSpawnCount = 0;
        private int monstersSpawned = 0;
        private List<Mobj> spawnedMobs = new List<Mobj>();

        private int currentMonstersPerWave = 2;
        private int monstersPerWaveIncrease = 3;

        private int specialMonstersWaveInterval = 5;

        private float currentMonsterHealthMultiplyer = 0.5f;
        private float monsterHealthMultiplyerIncrease = 0.5f;

        private bool Started = false;
        private int waveStartTime;
        private int waveDelay = GameConst.TicRate * 5;


        private MobjType[] monsterTypes = {

            MobjType.Zombie,
            MobjType.Dog,

        };

        private int currencyPerHit = 1;
        private int currencyPerKill = 5;

        private World world;
        private List<MapThing> spawnPoints;


        // Powerups

        private int instaKillTime = GameConst.TicRate * 10;
        private int instaKillStartTime;

        private int doublePointsTime = GameConst.TicRate * 10;
        private int doublePointsStartTime;

        private int nukePoints = 25;

        public void Start( World world) {

            this.world = world;

            spawnPoints = new List<MapThing>();
            foreach ( var thing in world.Map.Things ) {

                if ( thing.Type != 16030 ) continue;

                if ( thing.X == Fixed.FromInt( 2144 ) )
                    continue;
                spawnPoints.Add( thing );

            }

            if (spawnPoints.Count == 0 ) Console.WriteLine( "No spawn points found!" );

            if ( !Started ) {

                Started = true;
                instaKillStartTime = -instaKillTime;
                doublePointsStartTime = -doublePointsTime;

                EventManager.Subscribe<MobKilledEvent>( e => {

                    var ev = (MobKilledEvent)e;
                    OnMobKilled( ev.Source, ev.Target );

                } );

                EventManager.Subscribe<MobDamagedEvent>( e => {

                    var ev = (MobDamagedEvent)e;
                    onMobDamaged( ev.Source, ev.Target );

                } );


            }

        }

        private void OnMobKilled( Mobj source, Mobj target ) {

            if ( source == null || source.Player == null ) return;

            GivePlayerPoints( source.Player, currencyPerKill );

            monstersSpawned--;
            spawnedMobs.Remove( target );

            if ( monsterSpawnCount != 0 || monstersSpawned != 0 ) return;

            if ( wave % 5 != 0 ) return;

            var item = world.ThingAllocation.SpawnMobj( target.X, target.Y, Mobj.OnFloorZ, MobjType.MaxAmmo );
            item.Flags |= MobjFlags.Dropped;

        }

        private void onMobDamaged( Mobj source, Mobj target ) {

            if ( target.Player != null || target.Health <= 0 ) return;

            if (instaKillStartTime + instaKillTime > world.LevelTime ) {

                world.ThingInteraction.DamageMobj( target, source, source, target.Health );

            }

            if ( source.Player == null ) return;

            GivePlayerPoints( source.Player, currencyPerHit );

        }

        private void GivePlayerPoints( Player player, int points ) {

            player.Currency += ( doublePointsStartTime + doublePointsTime > world.LevelTime ) ? points * 2 : points;

        }

        public void ActivateMaxAmmo() {




            foreach ( Player player in world.Options.Players ) {

                for ( var i = 0; i < player.WeaponOwned.Length; i++ ) {

                    if ( !player.WeaponOwned[i] ) continue;
                    if ( DoomInfo.WeaponInfos[i].Ammo == AmmoType.NoAmmo ) continue;
                    player.Ammo[(int) DoomInfo.WeaponInfos[i].Ammo] = player.MaxAmmo[(int) DoomInfo.WeaponInfos[i].Ammo];

                }
                player.SendMessage( "Max Ammo Activated!" );

            }

        }

        public void ActivateInstaKill() {

            instaKillStartTime = world.LevelTime;
            foreach ( Player player in world.Options.Players ) player.SendMessage( "InstaKill Activated!" );

        }

        public void ActivateDoublePoints() {

            doublePointsStartTime = world.LevelTime;
            foreach ( Player player in world.Options.Players ) player.SendMessage( "Double Points Activated!" );

        }

        public void ActivateNuke() {

            foreach ( Mobj mobj in spawnedMobs ) world.ThingInteraction.DamageMobj( mobj, null, null, mobj.Health );

            monsterSpawnCount = 0;
            monstersSpawned = 0;

            foreach ( Player player in world.Options.Players ) {

                player.Currency += nukePoints;
                player.SendMessage( "Nuke Activated!" );

            }

        }

        public void Update() {

            if ( !Started ) return;

            if ( monstersSpawned <= 0 && monsterSpawnCount <= 0 ) {

                waveStartTime = world.LevelTime;
                StartWave();


            }


            if ( waveStartTime + waveDelay > world.LevelTime ) return;

            if ( monsterSpawnCount <= 0 ) return;
            SpawnMonster();

        }

        private void StartWave() {

            foreach ( Mobj mobj in spawnedMobs ) world.ThingInteraction.DamageMobj( mobj, null, null, mobj.Health );

            wave++;
            world.Options.Players[0].SendMessage( "Wave " + wave + " Starting..." );

            currentMonstersPerWave += monstersPerWaveIncrease;

            monsterSpawnCount = currentMonstersPerWave;
            currentMonsterHealthMultiplyer += monsterHealthMultiplyerIncrease;

        }

        private void SpawnMonster() {

            MobjType type = monsterTypes[0];
            if ( wave % specialMonstersWaveInterval == 0 ) type = monsterTypes[1];
            else if (wave > 10) {

                type = (new Random().Next(10) < 9) ? monsterTypes[0] : monsterTypes[1];

            }

            MapThing spawnPoint = spawnPoints[ new Random().Next( spawnPoints.Count ) ];

            if ( !CheckOpenPoint( Fixed.FromInt( 100 ), spawnPoint.X, spawnPoint.Y ) ) return;

            var mobj = world.ThingAllocation.SpawnMobj( spawnPoint.X, spawnPoint.Y, Mobj.OnFloorZ, type );
            mobj.SpawnPoint = spawnPoint;
            mobj.Health = (int) (float) currentMonsterHealthMultiplyer * mobj.Health;

            spawnedMobs.Add( mobj );
            monstersSpawned++;
            monsterSpawnCount--;

        }

        public bool Respawn(Mobj actor)
        {
            MapThing spawnPoint = spawnPoints[new Random().Next(spawnPoints.Count)];

            if (!CheckOpenPoint(Fixed.FromInt(30), spawnPoint.X, spawnPoint.Y)) return false;

            actor.X = spawnPoint.X;
            actor.Y = spawnPoint.Y;
            actor.Z = Mobj.OnFloorZ;
            return true;
        }

        private bool CheckOpenPoint( Fixed radius, Fixed x, Fixed y ) {

            var thinkers = world.Thinkers;
            foreach ( Thinker thinker in thinkers ) {

                if ( thinker is not Mobj mobj ) continue;
                if ( ( mobj.Flags & MobjFlags.Solid ) == 0 ) continue;

                if ( mobj.X >= x - radius &&
                     mobj.X <= x + radius &&
                     mobj.Y >= y - radius &&
                     mobj.Y <= y + radius ) {

                    return false;

                }

            }

            return true;

        }

    }

}
