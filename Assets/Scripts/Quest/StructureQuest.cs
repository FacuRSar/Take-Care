using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(fileName = "Quest", menuName = "Game/Quest", order = 1)]
public class StructureQuest : ScriptableObject
{
    [System.Serializable]
    public struct QuestGeneric
    {
        [SerializeField] public questEmotionType State;
        [SerializeField] public questType QuestType;

        public string Name;
        public string description;
        public int id;


        [Header("ToCollect")]
        public bool differentItems;
        public List<itemsToPick> date;

        [System.Serializable]
        public struct itemsToPick
        {
            public string name;
            public int quantity;
            public int itemID;
        }

        [Header("Points")]
        public string EmotionID;
        public int addpoint;
        public addOtherEmotion[] AddPointsEmotions;

        [System.Serializable]
        public struct addOtherEmotion
        {
            public string EmotionID;
            public int addPoint;
        }

        public string EmotionID_;
        public int removePoint;
        public reduceOtherEmotion[] removePointsEmotions;

        [System.Serializable]
        public struct reduceOtherEmotion
        {
            public string EmotionID_;
            public int removePoint;
        }

        public string getDescription()
        {
            if (description != null)
                return description;
            else
                return "No description";
        }
        public void setDescription(string newDescription)
        {
            description = newDescription;
        }
    }
    public QuestGeneric[] quests;

    void OnEnable()
    {
        Quest quest = FindFirstObjectByType<Quest>();
        quest.Initialize(quests);
    }
}