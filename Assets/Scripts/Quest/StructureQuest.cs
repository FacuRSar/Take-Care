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

        public string Name;
        [TextArea(2, 5)] public string description; // Añadido para mejor visualización en el inspector
        public int id;

        [Header("To Collect")]
        public bool differentItems;
        public List<itemsToPick> itemsToPickData; // Corregido typo de "Date" a "Data"

        [System.Serializable]
        public struct itemsToPick
        {
            public string name;
            public int quantity;
            public int itemID;
        }

        [Header("ToGo or Delivery")]
        public bool deliveryEnabled;
        public Transform roomOrDestiny;

        [Header("Points (Rewards)")]
        public questEmotionType EmotionID;
        public int addPoints;
        public bool otherEmotionAdd;
        public addOtherEmotion[] addPointsEmotions;

        [System.Serializable]
        public struct addOtherEmotion
        {
            public questEmotionType emotionID;
            public int addPoint;
        }

        [Header("Points (Penalties)")]
        public questEmotionType EmotionID_;
        public int removePoints;
        public bool otherEmotionRemove;
        public removeOtherEmotion[] removePointsEmotions;

        [System.Serializable]
        public struct removeOtherEmotion
        {
            public questEmotionType emotionID_;
            public int removePoint;
        }
    }

    public QuestGeneric[] quests;
}