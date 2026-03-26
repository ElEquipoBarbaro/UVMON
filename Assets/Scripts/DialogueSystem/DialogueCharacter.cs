using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Character", menuName = "ScriptableObjects/DialogueCharacters")]
    public class DialogueCharacter : ScriptableObject
    {
        [Header("Character Info")]
        [SerializeField] private string characterName;
        [SerializeField] private Sprite profilePhoto;

        public string Name => characterName;
        public Sprite ProfilePhoto => profilePhoto;
    }