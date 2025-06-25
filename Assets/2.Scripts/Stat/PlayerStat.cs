using System;
using UnityEngine;

public class PlayerStat 
{
    public int Gold;

    public PlayerStat(int gold)
    {

        if(gold < 0)
        {
            throw new Exception("골드는 0보다 작을 수 없어요!");
        }

        Gold = gold;
    }
}
