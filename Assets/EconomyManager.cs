using UnityEngine;
using System;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [SerializeField] private int startingGold = 100;
    private int currentGold;

    // Event so your UI can automatically update whenever gold changes
    public static event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        currentGold = startingGold;
        OnGoldChanged?.Invoke(currentGold);
    }

    public int GetCurrentGold() => currentGold;

    public void AddGold(int amount)
    {
        currentGold += amount;
        OnGoldChanged?.Invoke(currentGold);
    }

    public bool CanAfford(int amount)
    {
        return currentGold >= amount;
    }

    public bool SpendGold(int amount)
    {
        if (CanAfford(amount))
        {
            currentGold -= amount;
            OnGoldChanged?.Invoke(currentGold);
            return true; // Purchase successful
        }

        Debug.LogWarning("Not enough gold!");
        return false; // Purchase failed
    }
}