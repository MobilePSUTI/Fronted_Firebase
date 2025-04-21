using UnityEngine;

public static class GameSession
{
    public static int SessionCoins { get; private set; } = 0;
    public static float SessionTime { get; private set; } = 0f;

    public static int TotalCoins
    {
        get => PlayerPrefs.GetInt("TotalCoins", 0);
        private set
        {
            PlayerPrefs.SetInt("TotalCoins", value);
            PlayerPrefs.Save();
        }
    }

    public static void AddCoins(int amount)
    {
        SessionCoins += amount;
        TotalCoins += amount;
    }

    public static void SetTime(float time)
    {
        SessionTime = time;
    }

    public static void ResetSession()
    {
        SessionCoins = 0;
        SessionTime = 0f;
    }
}