﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnhollowerBaseLib.Attributes;

namespace Nebula.Map
{
    public class SabotageData
    {
        public SystemTypes Room { get; private set; }
        public Vector3 Position { get; private set; }
        public bool IsLeadingSabotage { get; private set; }
        public bool IsUrgent { get; private set; }

        public SabotageData(SystemTypes Room,Vector3 Position,bool IsLeadingSabotage,bool IsUrgent)
        {
            this.Room = Room;
            this.Position = Position;
            this.IsLeadingSabotage = IsLeadingSabotage;
            this.IsUrgent = IsUrgent;
        }
    }

    public class WiringData
    {
        HashSet<int>[] WiringCandidate;

        public WiringData()
        {
            WiringCandidate = new HashSet<int>[3] { new HashSet<int>(), new HashSet<int>(), new HashSet<int>() };            
        }
    }

    public class MapData
    {
        //Skeld=0,MIRA=1,Polus=2,AirShip=4

        public ShipStatus Assets;
        public int MapId { get; }

        public bool IsModMap { get; }

        public static Dictionary<int, MapData> MapDatabase = new Dictionary<int, MapData>();


        public Dictionary<SystemTypes, SabotageData> SabotageMap;

        //マップ内の代表点
        public HashSet<Vector2> MapPositions;

        //部屋の関連性
        public Dictionary<SystemTypes, HashSet<SystemTypes>> RoomsRelation;
        //ドアを持つ部屋
        public HashSet<SystemTypes> DoorRooms;

        //ドアサボタージュがサボタージュの発生を阻止するかどうか
        public bool DoorHackingCanBlockSabotage;
        //ドアサボタージュの有効時間
        public float DoorHackingDuration;

        //マップの端から端までの距離
        public float MapScale;

        //スポーン位置候補
        public List<SpawnCandidate> SpawnCandidates;
        public bool SpawnOriginalPositionAtFirst;

        //スポーン位置選択がもとから発生するかどうか
        public bool HasDefaultPrespawnMinigame;

        

        public static void Load()
        {
            new Database.SkeldData();
            new Database.MIRAData();
            new Database.PolusData();
            new Database.AirshipData();
            new MapData(5);
        }

        public static Map.MapData GetCurrentMapData()
        {
            if (MapDatabase.ContainsKey(PlayerControl.GameOptions.MapId))
            {
                return MapDatabase[PlayerControl.GameOptions.MapId];
            }
            else
            {
                return MapDatabase[5];
            }
        }

        public bool isOnTheShip(Vector2 pos)
        {
            Vector2 vector;
            float magnitude;

            foreach (Vector2 p in MapPositions)
            {
                vector= p - pos;
                magnitude = vector.magnitude;
                if (magnitude < 12.0f)
                {
                    if (!PhysicsHelpers.AnyNonTriggersBetween(pos, vector.normalized, magnitude, Constants.ShipAndAllObjectsMask))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public int isOnTheShip_Debug(Vector2 pos)
        {
            int num = 0;

            Vector2 vector;
            float magnitude;

            foreach (Vector2 p in MapPositions)
            {
                vector = p - pos;
                magnitude = vector.magnitude;

                if (magnitude < 12.0f)
                {
                    if (!PhysicsHelpers.AnyNonTriggersBetween(pos, vector.normalized, magnitude, Constants.ShipAndAllObjectsMask))
                    {
                        num++;
                    }
                }
            }
            return num;
        }

        public int OutputMap(Vector2 pos,Vector2 size,string fileName)
        {
            int x1, y1, x2, y2;
            x1 = (int)(pos.x * 10);
            y1 = (int)(pos.y * 10);
            x2 = x1 + (int)(size.x * 10);
            y2 = y1 + (int)(size.y * 10);
            int temp;
            if (x1 > x2)
            {
                temp = x1;
                x1 = x2;
                x2 = temp;
            }
            if (y1 > y2)
            {
                temp = y1;
                y1 = y2;
                y2 = temp;
            }

            Color color = new Color(40/255f, 40/255f, 40/255f);
            var texture = new Texture2D(x2 - x1, y2 - y1, TextureFormat.RGB24, false);

            int num;
            int r = 0;
            for (int y = y1; y < y2; y++) {
                for (int x = x1; x < x2; x++) {
                    num = isOnTheShip_Debug(new Vector2(((float)x) / 10f, ((float)y) / 10f));
                    //if (num > 20) num = 20;
                    texture.SetPixel(x-x1, y-y1, (num == 0) ? color : new Color((num > 1 ? 100 : 0)/255f, (150 + (num * 5))/255f, 0));
                    if(num>0)r++;
                }
            }

            texture.Apply();

            byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(Helpers.CreateReadabeTexture(texture));
            //保存
            File.WriteAllBytes(fileName + ".png", bytes);

            return r;
        }

        public MapData(int mapId)
        {
            MapId = mapId;
            MapDatabase[mapId] = this;

            IsModMap = mapId >= 5;

            SabotageMap = new Dictionary<SystemTypes, SabotageData>();
            RoomsRelation = new Dictionary<SystemTypes, HashSet<SystemTypes>>();
            DoorRooms = new HashSet<SystemTypes>();
            MapPositions = new HashSet<Vector2>();

            SpawnCandidates = new List<SpawnCandidate>();
            SpawnOriginalPositionAtFirst = false;

            DoorHackingCanBlockSabotage = false;

            HasDefaultPrespawnMinigame = false;

            MapScale = 1f;
            DoorHackingDuration = 10f;
        }

        public void LoadAssets(AmongUsClient __instance)
        {
            if (IsModMap) return;

            AssetReference assetReference = __instance.ShipPrefabs.ToArray()[MapId];
            AsyncOperationHandle<GameObject> asset = assetReference.LoadAsset<GameObject>();
            asset.WaitForCompletion();
            Assets = assetReference.Asset.Cast<GameObject>().GetComponent<ShipStatus>();
        }

        
        public bool PlayInitialPrespawnMinigame
        {
            get
            {
                if (HasDefaultPrespawnMinigame) return true;

                return (SpawnCandidates.Count >= 3 && !SpawnOriginalPositionAtFirst && CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.multipleSpawnPoints.getBool());
            }
        }
    }
}
