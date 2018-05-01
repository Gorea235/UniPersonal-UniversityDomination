﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Initializer : MonoBehaviour
{
    #region MonoBehaviour

    void Start()
    {
      //  SoundManager.Instance.PlayMusic(Sound.MainGameMusic);
        if (Game.MementoToRestore == null)
            Game.Instance.Init();
        else
        {
            Game.Instance.RestoreMemento(Game.MementoToRestore);
            Game.MementoToRestore = null;
        }
    }

    #endregion
}