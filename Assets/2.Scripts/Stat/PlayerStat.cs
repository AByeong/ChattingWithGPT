using System;
using UnityEngine;

public class PlayerStat 
{
    public int Gold;

    public PlayerStat(int gold)
    {

        if(gold < 0)
        {
            throw new Exception("���� 0���� ���� �� �����!");
        }

        Gold = gold;
    }
}
