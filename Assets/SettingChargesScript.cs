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
    public Material[] ChargeColors; //black, blue, red, white
    

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    int[] solution;
    int redCount;
    int colLength = 8;
    int rowLength = 12;
    int[,] TheGrid = new int[8, 12]; //[y,x]
    int placed = 0;

    bool DEBUGMODE = true;

    
    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        foreach (KMSelectable Charge in Charges)
        {
            Charge.OnInteract += delegate () { ChargePress(Charge); return false; };
        }
        Submit.OnInteract += delegate () { SubmitPress(Submit); return false; };

        Reset.OnInteract += delegate () { ClearPress(Reset); return false; };
        GeneratePuzzle();
    }

    void GeneratePuzzle()
    {
        int attempts = 0;

        //pick number of reds
        redCount = Rnd.Range(3,6);
        solution = new int[redCount];
        //TODO: put this number on the display, at the end of this func

        //choose random positions for said reds
        for (int r = 0; r < redCount; r++)
        {
            int spot;
            do {
                spot = Rnd.Range(0, 96);
            } while (solution.Contains(spot));
            solution[r] = spot;
        }

        //get each position's x & y, will make the rest much simpler
        int[] solXs = new int[redCount];
        int[] solYs = new int[redCount];
        for (int r = 0; r < redCount; r++) {
            solXs[r] = solution[r] / 8;
            solYs[r] = solution[r] % 8;
            TheGrid[solYs[r], solXs[r]] = 2;
        }

        //main loop
        tryAgainDumbfuck:
        attempts++;
        int placedBlues = 0;
        int[] lineYs = new int[redCount * 8];
        int[] lineXs = new int[redCount * 8];
        bool[] lineFlags = new bool[redCount * 8];
        for (int l = 0; l < redCount * 8; l++) {
            lineYs[l] = solYs[l / 8];
            lineXs[l] = solXs[l / 8];
        }
        while (lineFlags.Contains(false)) {
            for (int l = 0; l < redCount * 8; l++) { //for every single line
                if (lineFlags[l]) { continue; }
                switch (l % 8) { //move one tile in that direction
                    case 0: lineYs[l] -= 1; break; //UP
                    case 1: lineYs[l] -= 1; lineXs[l] += 1; break; //UP-RIGHT
                    case 2: lineXs[l] += 1; break; //RIGHT
                    case 3: lineYs[l] += 1; lineXs[l] += 1; break; //DOWN-RIGHT
                    case 4: lineYs[l] += 1; break; //DOWN
                    case 5: lineYs[l] += 1; lineXs[l] -= 1; break; //DOWN-LEFT
                    case 6: lineXs[l] -= 1; break; //LEFT
                    case 7: lineYs[l] -= 1; lineXs[l] -= 1; break; //UP-LEFT
                }
                /* //expirementally removed, it seems to do the same thing as assigning -1 to tiles
                bool earlierMatch = false;
                for (int e = 0; e < l; e++) {
                    if (lineFlags[e] == true) { continue; }
                    if (lineYs[e] == lineYs[l] && lineXs[e] == lineXs[l]) {
                        earlierMatch = true;
                    }
                }
                */
                if (lineYs[l] < 0 || lineYs[l] > 7 || lineXs[l] < 0 || lineXs[l] > 11 /* || earlierMatch */ ) { //TODO: explain more for caleb's sake
                    lineFlags[l] = true;
                    continue;
                } else if (TheGrid[lineYs[l], lineXs[l]] == 2 || TheGrid[lineYs[l], lineXs[l]] == -1) {
                    continue;
                } else if (TheGrid[lineYs[l], lineXs[l]] == 1) {
                    lineFlags[l] = true;
                    continue;
                }
                if (Rnd.Range(0, 4) == 0) {
                    TheGrid[lineYs[l], lineXs[l]] = 1;
                    placedBlues++;
                    lineFlags[l] = true;
                    continue;
                } else {
                    TheGrid[lineYs[l], lineXs[l]] = -1;
                }
            }
        }
        if (placedBlues < 10) {
            for (int p = 0; p < 96; p++) {
                if (TheGrid[p % 8, p / 8] == 1 || TheGrid[p % 8, p / 8] == -1) {
                    TheGrid[p % 8, p / 8] = 0;
                }
            }
            goto tryAgainDumbfuck;
        }
        Debug.LogFormat("<Setting Charges #{0}> Attempts: {1}", _moduleId, attempts);

        Number.text = redCount.ToString();
        for (int p = 0; p < 96; p++) {
            if (TheGrid[p % 8, p / 8] == 1) {
                Caps[p].GetComponent<MeshRenderer>().material = ChargeColors[1];
            } else if (TheGrid[p % 8, p / 8] == 2 && DEBUGMODE) {
                Caps[p].GetComponent<MeshRenderer>().material = ChargeColors[2];
            }
        }
    }

    void ClearPress(KMSelectable Reset)
    {
        Reset.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Reset.transform);
        for (int i = 0; i < 96; i++)
        {
            if (TheGrid[i % 8, i / 8] > 5){
                TheGrid[i % 8, i / 8] -= 10;
                Caps[i].GetComponent<MeshRenderer>().material = ChargeColors[0];
            }
        }
        placed = 0;
        Number.text = redCount.ToString();
    }

    void ChargePress(KMSelectable Charge)
    {
        Charge.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Charge.transform);
        int index = Array.IndexOf(Charges, Charge);
        int ixX = index / 8;
        int ixY = index % 8;
        if (TheGrid[ixY, ixX] == 1)
        {
            return;
        }
        else if (TheGrid[ixY, ixX] > 5)
        {
            TheGrid[ixY, ixX] -= 10;
            placed--;
            Caps[index].GetComponent<MeshRenderer>().material = ChargeColors[0];
        }
        else if (placed == redCount)
        {
           Module.HandleStrike();
        } 
        else {
            TheGrid[ixY, ixX] += 10;
            placed++;
            Caps[index].GetComponent<MeshRenderer>().material = ChargeColors[2];
        }
        Number.text = (redCount - placed).ToString();
    }
    void SubmitPress(KMSelectable submit)
    {
        submit.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, submit.transform);
        //TODO: Animation and solving
    }

}