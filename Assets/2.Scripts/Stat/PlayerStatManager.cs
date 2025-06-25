using System;
using UnityEngine;

public class PlayerStatManager : MonoBehaviour
{
    public static PlayerStatManager Instance;

    public PlayerStat MyStat;

    public event Action OnDataChanged;
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        MyStat = new PlayerStat(100);
        OnDataChanged?.Invoke();

    }

    public void Purchase(int price)
    {
        MyStat.Gold -= price;

        OnDataChanged?.Invoke();
    }







}
