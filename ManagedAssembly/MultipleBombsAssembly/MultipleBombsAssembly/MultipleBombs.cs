using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class MultipleBombs : KMService
    {
        private Rect windowRect;

        public void Awake()
        {
            windowRect = new Rect(0, 0, 300, 200);
        }

        public void Update()
        {

        }

        public void OnGUI()
        {
            windowRect = GUI.Window(1337, windowRect, windowFunction, "Multiple Bombs Debug");
        }

        private void windowFunction(int id)
        {
            if (id == 1337)
            {
                GUILayout.BeginVertical();
                if (GUILayout.Button("Generate new bomb"))
                {
                    GameplayState gameplayState = FindObjectOfType<GameplayState>();
                    BombGenerator bombGenerator = FindObjectOfType<BombGenerator>();
                    if (gameplayState != null)
                    {
                        if (bombGenerator != null)
                        {
                            Debug.Log("[MultipleBombs]Generating new bomb");
                            bombGenerator.CreateBomb(gameplayState.Mission.GeneratorSetting, gameplayState.Room.BombSpawnPosition, (new System.Random()).Next(), Assets.Scripts.Missions.BombTypeEnum.Default);
                            Debug.Log("[MultipleBombs]Bomb generated");
                        }
                    }
                }
                GUILayout.EndVertical();
            }
        }
    }
}
