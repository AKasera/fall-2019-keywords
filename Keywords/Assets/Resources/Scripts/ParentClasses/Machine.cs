﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Machine : MonoBehaviour {
    protected GameObject slot;
    private GameObject progressIndicator;
    public float chargeTime;
    private float timer;
    protected bool ticking;
    private bool performedFirstAction;
    AudioSource dingSFX;

    // Use this for initialization
    protected virtual void Start() {
        ticking = false;
        slot = transform.GetChild(0).gameObject;
        progressIndicator = transform.GetChild(1).gameObject;
        timer = chargeTime;

        slot.GetComponent<SpriteRenderer>().color = GetComponent<SpriteRenderer>().color;
        slot.GetComponent<GridSquare>().normalColor = GetComponent<SpriteRenderer>().color;

        dingSFX = GameManager.instance.sfx["DingSFX"];
    }

    protected virtual void Update() {
        if (slot.GetComponent<GridSquare>().tile
            && slot.GetComponent<GridSquare>().tile.GetComponent<LetterTile>()
            && !slot.GetComponent<GridSquare>().tile.GetComponent<LetterTile>().infinite) {
            ticking = true;
            if (!performedFirstAction) {
                PerformMachineAction();
                performedFirstAction = true;
            }
        } else {
            ticking = false;
            timer = 0f;
            progressIndicator.GetComponent<SpriteRenderer>().color = Color.black;
        }
        if (ticking) {
            timer += Time.deltaTime;
            float frac = 0.7f * (timer / chargeTime) + 0.3f;
            progressIndicator.GetComponent<SpriteRenderer>().color = new Color(frac, frac, frac, 1f);
            if (timer >= chargeTime) {
                timer = 0f;
                PerformMachineAction();
            }
        }
    }

    protected virtual void PerformMachineAction() {
        dingSFX.Play();
    }

}
