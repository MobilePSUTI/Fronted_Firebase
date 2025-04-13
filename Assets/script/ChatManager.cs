using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
[System.Serializable]
public class Chat
{
    public string chatName; // Название чата
    public bool isRead; // Прочитан ли чат
    public string lastMessage; // Последнее сообщение
    public ChatType chatType; // Тип чата (бот, личный, групповой, избранный)
}

public enum ChatType
{
    Bot,
    Personal,
    Group,
    Favorite
}

public class ChatManager : MonoBehaviour
{
    public List<Chat> allChats = new List<Chat>();
    public GameObject chatItemPrefab;
    public Transform chatContainer;
    public Button botButton, personalButton, groupButton, favoriteButton;
    public Button searchButton;
    public InputField searchInputField;

    void Start()
    {
        // Инициализация кнопок
        botButton.onClick.AddListener(() => FilterChats(ChatType.Bot));
        personalButton.onClick.AddListener(() => FilterChats(ChatType.Personal));
        groupButton.onClick.AddListener(() => FilterChats(ChatType.Group));
        favoriteButton.onClick.AddListener(() => FilterChats(ChatType.Favorite));

        searchButton.onClick.AddListener(() => SearchChats(searchInputField.text));

        // Инициализация данных
        allChats = new List<Chat>
        {
        new Chat { chatName = "Бот", isRead = true, lastMessage = "Привет!", chatType = ChatType.Bot },
        new Chat { chatName = "Личный чат", isRead = false, lastMessage = "Как дела?", chatType = ChatType.Personal }
        };

        // Отображение чатов
        if (allChats != null && allChats.Count > 0)
        {
            DisplayChats(allChats);
        }
        else
        {
            Debug.LogError("allChats is null or empty.");
        }
    }

    public void FilterChats(ChatType chatType)
    {
        var filteredChats = allChats.Where(chat => chat.chatType == chatType).ToList();
        DisplayChats(filteredChats);
    }

    public void SearchChats(string searchText)
    {
        var searchedChats = allChats.Where(chat => chat.chatName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
        DisplayChats(searchedChats);
    }

    public void DisplayChats(List<Chat> chats)
    {
        if (chatItemPrefab == null || chatContainer == null)
        {
            Debug.LogError("chatItemPrefab or chatContainer is not assigned.");
            return;
        }

        Debug.Log("Displaying chats..."); // Отладочное сообщение

        // Очистите контейнер
        foreach (Transform child in chatContainer)
        {
            Destroy(child.gameObject);
        }

        // Создайте элементы чатов
        foreach (var chat in chats)
        {
            GameObject chatItem = Instantiate(chatItemPrefab, chatContainer);
            if (chatItem == null)
            {
                Debug.LogError("Failed to instantiate chat item.");
                continue;
            }

            Debug.Log("Chat item created: " + chat.chatName); // Отладочное сообщение

            // Название чата
            var chatNameText = chatItem.transform.Find("ChatName")?.GetComponent<Text>();
            if (chatNameText != null)
            {
                chatNameText.text = chat.chatName;
            }
            else
            {
                Debug.LogError("ChatName not found in chat item prefab.");
            }

            // Последнее сообщение
            var lastMessageText = chatItem.transform.Find("LastMessage")?.GetComponent<Text>();
            if (lastMessageText != null)
            {
                lastMessageText.text = chat.lastMessage;
            }
            else
            {
                Debug.LogError("LastMessage not found in chat item prefab.");
            }

            // Индикатор прочитанного/непрочитанного
            var readIndicator = chatItem.transform.Find("ReadIndicator")?.gameObject;
            if (readIndicator != null)
            {
                readIndicator.SetActive(!chat.isRead);
            }
            else
            {
                Debug.LogError("ReadIndicator not found in chat item prefab.");
            }
        }
    }
}
