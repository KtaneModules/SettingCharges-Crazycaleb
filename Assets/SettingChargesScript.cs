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
    public KMSelectable[] Charges; //ordered like the grid; see below
    public KMSelectable Submit;
    public KMSelectable Reset;
    public TextMesh Number;
    public GameObject[] Caps; //ordered like the grid; see below
    public Material[] ChargeColors; //black, blue, red, white
    

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    int[] solution;
    int redCount;
    int colLength = 8;
    int rowLength = 12;
    int[,] TheGrid = new int[8, 12]; //[y,x]; columns top to bottom, then left to right | -1 or 0 is whatever, 1 is blue, 2 is red; a 10 is added whenever it's selected
    int placed = 0;

    bool DEBUGMODE = true; //if enabled, this makes the reds appear in the grid too

    
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

        redCount = Rnd.Range(3,6); //pick number of reds
        solution = new int[redCount];

        for (int r = 0; r < redCount; r++) //choose random positions for said reds
        {
            int spot;
            do {
                spot = Rnd.Range(0, 96);
            } while (solution.Contains(spot));
            solution[r] = spot;
        }

        int[] solXs = new int[redCount]; //get each position's x & y, will make the rest much simpler
        int[] solYs = new int[redCount];
        for (int r = 0; r < redCount; r++) {
            solXs[r] = solution[r] / 8;
            solYs[r] = solution[r] % 8;
            TheGrid[solYs[r], solXs[r]] = 2;
        }

        tryAgain:
        attempts++;
        int placedBlues = 0;
        int[] lineYs = new int[redCount * 8]; //every 8 in a row is each direction from each line, in order
        int[] lineXs = new int[redCount * 8];
        bool[] lineFlags = new bool[redCount * 8];
        for (int l = 0; l < redCount * 8; l++) {
            lineYs[l] = solYs[l / 8];
            lineXs[l] = solXs[l / 8];
        }
        while (lineFlags.Contains(false)) {
            for (int l = 0; l < redCount * 8; l++) { //for every single line
                if (lineFlags[l]) { continue; }
                switch (l % 8) { //move one tile in that direction; whichever it is
             /*up*/ case 0: lineYs[l] -= 1; break;
       /*up-right*/ case 1: lineYs[l] -= 1; lineXs[l] += 1; break;
          /*right*/ case 2: lineXs[l] += 1; break;
     /*down-right*/ case 3: lineYs[l] += 1; lineXs[l] += 1; break;
           /*down*/ case 4: lineYs[l] += 1; break;
      /*down-left*/ case 5: lineYs[l] += 1; lineXs[l] -= 1; break;
           /*left*/ case 6: lineXs[l] -= 1; break;
        /*up-left*/ case 7: lineYs[l] -= 1; lineXs[l] -= 1; break;
                }
                if (lineYs[l] < 0 || lineYs[l] > 7 || lineXs[l] < 0 || lineXs[l] > 11) { //if the line went off the grid, ignore from now on
                    lineFlags[l] = true;
                    continue;
                } else if (TheGrid[lineYs[l], lineXs[l]] == 2 || TheGrid[lineYs[l], lineXs[l]] == -1) { //if the line is now at a red, or if a blue could have been placed here, don't stop it
                    continue;
                } else if (TheGrid[lineYs[l], lineXs[l]] == 1) { //if another line at this position became a blue, this line should stop
                    lineFlags[l] = true;
                    continue;
                }
                if (Rnd.Range(0, 4) == 0) { //1 in 4 chance of the line becoming blue and stopping
                    TheGrid[lineYs[l], lineXs[l]] = 1;
                    placedBlues++;
                    lineFlags[l] = true;
                    continue;
                } else { //keep track of positions that could have become a line, as we cannot put a blues there
                    TheGrid[lineYs[l], lineXs[l]] = -1;
                }
            }
        }
        if (placedBlues < 10) { //start all over if there's less than 10 blues
            for (int p = 0; p < 96; p++) {
                if (TheGrid[p % 8, p / 8] == 1 || TheGrid[p % 8, p / 8] == -1) {
                    TheGrid[p % 8, p / 8] = 0;
                }
            }
            goto tryAgain;
        }
        Debug.LogFormat("<Setting Charges #{0}> Attempts: {1}", _moduleId, attempts);

        Number.text = redCount.ToString();
        for (int p = 0; p < 96; p++) { //display those blues
            if (TheGrid[p % 8, p / 8] == 1) {
                Caps[p].GetComponent<MeshRenderer>().material = ChargeColors[1];
            } else if (TheGrid[p % 8, p / 8] == 2 && DEBUGMODE) {
                Caps[p].GetComponent<MeshRenderer>().material = ChargeColors[2];
            }
        }

        //TODO: log grid and solution
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
        if (TheGrid[ixY, ixX] == 1) //if you're pressing a blue, a red cannot be placed
        {
            return;
        }
        else if (TheGrid[ixY, ixX] > 5) //if you're pressing a red, remove the red
        {
            TheGrid[ixY, ixX] -= 10;
            placed--;
            Caps[index].GetComponent<MeshRenderer>().material = ChargeColors[0];
        }
        else if (placed == redCount) //if you're placing a red, but you've placed that many reds already, strike
        {
           Module.HandleStrike();
        } 
        else { //otherwise you can place your red and the counter is decremented
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