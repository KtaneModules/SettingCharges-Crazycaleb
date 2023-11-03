using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

public class SettingChargesScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable[] Charges;
    public KMSelectable Submit;
    public KMSelectable Reset;
    public TextMesh Number;
    public GameObject[] Caps;
    public Material[] ChargeColors;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    int[] redIndex = new int[]{-1, -1, -1, -1};
    int rigNumb;
    int rng;


    
    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        rigNumb = Rnd.Range(3,7);
        rng = rigNumb;
        Number.text = rng.ToString();
        foreach (KMSelectable Charge in Charges)
        {
            Charge.OnInteract += delegate () { chargePress(Charge); return false; };
        }
        Submit.OnInteract += delegate () { submitPress(Submit); return false; };

        Reset.OnInteract += delegate () { clearPress(Reset); return false; };
        RedSpots();
    }

    void RedSpots()
    {
        for (int i = 0; i < 4; i++)
        {
            int the = Rnd.Range(0, Caps.Length);
            while (redIndex.Contains(the))
            {
                the = Rnd.Range(0, Caps.Length);
            }
            redIndex[i] = the;
            Caps[the].GetComponent<MeshRenderer>().material = ChargeColors[1];
        }
    }

    void clearPress(KMSelectable Reset)
    {
        Reset.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Reset.transform);
        for (int i = 0; i < Caps.Length; i++)
        {
            if (!redIndex.Contains(i)){
                Caps[i].GetComponent<MeshRenderer>().material = ChargeColors[0];
            }
        }
        Number.text = rigNumb.ToString();
        rng = rigNumb;
    }

    void chargePress(KMSelectable Charge)
    {
        Charge.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Charge.transform);
        int index = Array.IndexOf(Charges, Charge);
        if (redIndex.Contains(index))
        {
            return;
        }
        else if (rng > 0)
        {
            Caps[index].GetComponent<MeshRenderer>().material = ChargeColors[2];
            rng--;
            Number.text = rng.ToString();
        }
        else
        {
            return;
        }
    }
    void submitPress(KMSelectable submit)
    {
        submit.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, submit.transform);
    }

    void Grid
    {
        new string[8,12]
        {
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        }
    }
}