using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Quest", menuName = "Game/Quest", order = 1)]
public class StructureQuest : ScriptableObject
{
    [System.Serializable]
    public struct QuestGeneric
    {
        public questEmotionType State;
        public questType QuestType;

        public string SubtitlesForQuest;
        public string SubtitlesForQuestComplete;
        public string SubtitlesForQuestFail;

        public int timer;

        public string Name;
        [TextArea(4, 10)] public string description; // Añadido para mejor visualización en el inspector
        public int id;

        public string TpDollID;

        [Header("To Collect")]
        public List<itemsToPick> itemsToPickData; // Corregido typo de "Date" a "Data"

        [System.Serializable]
        public struct itemsToPick
        {
            public int quantity;
            public int itemID;
        }

        [Header("ToGo or Delivery")]

        public String roomID;

        [Header("Points (Rewards)")]
        public questEmotionType EmotionID;
        public int addPoints;


        [Header("Points (Penalties)")]
        public questEmotionType EmotionID_;
        public int removePoints;

    }

    public QuestGeneric[] quests;
}