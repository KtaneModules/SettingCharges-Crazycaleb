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
    int placedBlues = 0;
    int[,] TheGrid = new int[8, 12]; //[y,x]; columns top to bottom, then left to right | -1 or 0 is whatever, 1 is blue, 2 is red; a 10 is added whenever it's selected; 100 is added if shockwave is there
    int placedReds = 0;

    bool animating = false;

    
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

        int[] solYs = new int[redCount]; //get each position's y & x, will make the rest much simpler
        int[] solXs = new int[redCount];
        for (int r = 0; r < redCount; r++) {
            solYs[r] = solution[r] % 8;
            solXs[r] = solution[r] / 8;
            TheGrid[solYs[r], solXs[r]] = 2;
        }

        tryAgain:
        attempts++;
        placedBlues = 0;
        int[] lineYs = new int[redCount * 8]; //every 8 in a row is each direction from each line, in order
        int[] lineXs = new int[redCount * 8];
        bool[] lineFlags = new bool[redCount * 8];
        for (int l = 0; l < redCount * 8; l++) {
            lineYs[l] = solYs[l / 8];
            lineXs[l] = solXs[l / 8];
        }
        while (lineFlags.Contains(false)) {
            for (int l = 0; l < redCount * 8; l++) { //for every single line
                if (lineFlags[l]) { continue; } //provided it can still move
                switch (l % 8) { 
             /*up*/ case 0: lineYs[l] -= 1; break; //move one tile in that direction; whichever it is
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
            for (int t = 0; t < 96; t++) {
                if (TheGrid[t % 8, t / 8] == 1 || TheGrid[t % 8, t / 8] == -1) {
                    TheGrid[t % 8, t / 8] = 0;
                }
            }
            goto tryAgain;
        }
        Debug.LogFormat("<Setting Charges #{0}> Attempts: {1}", _moduleId, attempts);

        Number.text = redCount.ToString();
        for (int t = 0; t < 96; t++) { //display the blues
            if (TheGrid[t % 8, t / 8] == 1) {
                Caps[t].GetComponent<MeshRenderer>().material = ChargeColors[1];
            }
        }

        Debug.LogFormat("[Setting Charges #{0}] Grid with solution:", _moduleId);
        string logging = "";
        for (int r = 0; r < 8; r++) {
            for (int q = 0; q < 12; q++) {
                switch (TheGrid[r, q]) {
           /*blue*/ case 1: logging += "o"; break;
            /*red*/ case 2: logging += "x"; break;
        /*neither*/ default: logging += "."; break;
                }
            }
            if (r != 7) { logging += "|"; }
        }
        Debug.LogFormat("[Setting Charges #{0}] {1}", _moduleId, logging);
    }

    void ClearPress(KMSelectable Reset)
    {
        Reset.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Reset.transform);
        if (animating || _moduleSolved) { return; }

        for (int t = 0; t < 96; t++)
        {
            if (TheGrid[t % 8, t / 8] > 5){
                TheGrid[t % 8, t / 8] -= 10;
                Caps[t].GetComponent<MeshRenderer>().material = ChargeColors[0];
            }
        }
        placedReds = 0;
        Number.text = redCount.ToString();
    }

    void ChargePress(KMSelectable Charge)
    {
        Charge.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Charge.transform);
        if (animating || _moduleSolved) { return; }

        int index = Array.IndexOf(Charges, Charge);
        int ixY = index % 8;
        int ixX = index / 8;
        if (TheGrid[ixY, ixX] == 1) //if you're pressing a blue, a red cannot be placed
        {
            return;
        }
        else if (TheGrid[ixY, ixX] > 5) //if you're pressing a red, remove the red
        {
            TheGrid[ixY, ixX] -= 10;
            placedReds--;
            Caps[index].GetComponent<MeshRenderer>().material = ChargeColors[0];
        }
        else if (placedReds == redCount) //if you're placing a red, but you've placed that many reds already, strike
        {
            Debug.LogFormat("[Setting Charges #{0}] Attempted to place more charges than allowed, strike!", _moduleId);
            Module.HandleStrike();
        } 
        else { //otherwise you can place your red and the counter is decremented
            TheGrid[ixY, ixX] += 10;
            placedReds++;
            Caps[index].GetComponent<MeshRenderer>().material = ChargeColors[2];
        }
        Number.text = (redCount - placedReds).ToString();
    }

    void SubmitPress(KMSelectable submit)
    {
        submit.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, submit.transform);
        if (animating || _moduleSolved) { return; }
        
        if (placedReds == 0) {
            Debug.LogFormat("[Setting Charges #{0}] Attempted to submit without charges, strike!", _moduleId);
            Module.HandleStrike();
        } else {
            Debug.LogFormat("[Setting Charges #{0}] Attempted placement:", _moduleId);
            string logging = "";
            for (int r = 0; r < 8; r++) {
                for (int q = 0; q < 12; q++) {
                    switch (TheGrid[r, q]) {
               /*blue*/ case 1: logging += "o"; break;
            /*neither*/ case -1: case 0: case 2: logging += "."; break;
                /*red*/ default: logging += "x"; break;
                    }
                }
                if (r != 7) { logging += "|"; }
            }
            Debug.LogFormat("[Setting Charges #{0}] {1}", _moduleId, logging);

            animating = true;
            StartCoroutine(Animate());
        }
    }

    private IEnumerator Animate () {
        int hitBlues = 0;
        int[] shockYs = new int[placedReds * 8]; //like the lines, each set of 8 corresponds to a direction
        int[] shockXs = new int[placedReds * 8];
        bool[] shockFlags = new bool[placedReds * 8];
        int[] bluesHit = new int[placedBlues];

        int index = 0;
        for (int t = 0; t < 96; t++) {
            if (TheGrid[t % 8, t / 8] > 5) { //if we know this is where a red was placed
                TheGrid[t % 8, t / 8] -= 10;
                for (int s = 0; s < 8; s++) {
                    shockYs[index * 8 + s] = t % 8; //get their y & x values
                    shockXs[index * 8 + s] = t / 8;
                }
                Caps[t].GetComponent<MeshRenderer>().material = ChargeColors[0]; //and set to black
                index++;
            }
        }

        while (shockFlags.Contains(false)) { //while shockwaves are present
            yield return new WaitForSeconds(0.15f);
            for (int t = 0; t < 96; t++) { //set what was white from previous iteration of this loop to black
                if (TheGrid[t % 8, t / 8] > 50) {
                    TheGrid[t % 8, t / 8] -= 100;
                    Caps[t].GetComponent<MeshRenderer>().material = ChargeColors[0];
                }
            }

            for (int s = 0; s < placedReds * 8; s++) { //for every shockwave
                if (shockFlags[s]) { continue; } //provided it can still move
                switch (s % 8) {
             /*up*/ case 0: shockYs[s] -= 1; break; //move in the right direction; whatever it is
       /*up-right*/ case 1: shockYs[s] -= 1; shockXs[s] += 1; break;
          /*right*/ case 2: shockXs[s] += 1; break;
     /*down-right*/ case 3: shockYs[s] += 1; shockXs[s] += 1; break;
           /*down*/ case 4: shockYs[s] += 1; break;
      /*down-left*/ case 5: shockYs[s] += 1; shockXs[s] -= 1; break;
           /*left*/ case 6: shockXs[s] -= 1; break;
        /*up-left*/ case 7: shockYs[s] -= 1; shockXs[s] -= 1; break;
                }
                if (shockYs[s] < 0 || shockYs[s] > 7 || shockXs[s] < 0 || shockXs[s] > 11) { //if it's outside the grid, mark as done
                    shockFlags[s] = true;
                    continue;
                }
                Caps[shockXs[s] * 8 + shockYs[s]].GetComponent<MeshRenderer>().material = ChargeColors[3]; //otherwise color it white
                if (TheGrid[shockYs[s], shockXs[s]] < 50) { //if it's already marked as shockwave no need to do so again
                    TheGrid[shockYs[s], shockXs[s]] += 100;
                }
                if (TheGrid[shockYs[s], shockXs[s]] == 101) { //if it's where a blue was, we can stop here and mark as done
                    if (!bluesHit.Contains(shockXs[s] * 8 + shockYs[s] + 100)) { 
                        bluesHit[hitBlues] = shockXs[s] * 8 + shockYs[s] + 100; //we store the index of the blue hit so we do not over count; 100 is added because the default int in C# is 0, which would be top-left
                        hitBlues += 1;
                    }
                    shockFlags[s] = true;
                }
            }
        }

        if (hitBlues == placedBlues) {
            for (int t = 0; t < 96; t++) { //set final whites to black on solve
                if (TheGrid[t % 8, t / 8] > 50) {
                    TheGrid[t % 8, t / 8] -= 100;
                    Caps[t].GetComponent<MeshRenderer>().material = ChargeColors[0];
                }
            }
            Debug.LogFormat("[Setting Charges #{0}] All obstacles cleared, module solved.", _moduleId);
            Module.HandlePass();
            _moduleSolved = true;
        } else {
            yield return new WaitForSeconds(0.3f);
            for (int t = 0; t < 96; t++) {
                if (TheGrid[t % 8, t / 8] > 50) { //reset the board to the initial state by subtracting 100 when necessary
                    TheGrid[t % 8, t / 8] -= 100;
                }
                if (TheGrid[t % 8, t / 8] == 1) { //and setting blues back to blue
                    Caps[t].GetComponent<MeshRenderer>().material = ChargeColors[1];
                }
            }
            Debug.LogFormat("[Setting Charges #{0}] Didn't clear all obstacles, strike!", _moduleId);
            Module.HandleStrike();
            placedReds = 0;
            Number.text = redCount.ToString();
        }
        animating = false;
    }

}