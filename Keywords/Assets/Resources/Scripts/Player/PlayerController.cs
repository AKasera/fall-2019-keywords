﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerController : MonoBehaviour {
    private Rigidbody2D rb;
    private Inventory inventory;

    public GameObject activeSquare;//the grid square the player's currently on
    private GameObject TileContainer; //the parent object for the letter tiles

    //controls
    private PlayerInfo me;
    private KeyCode LeftBumper;
    private KeyCode RightBumper;
    private KeyCode AButton;
    private KeyCode BButton;

    private float pMovSpeedBase = 2.2f;
    private float pMovHandleBase = 0.8f; // Player movmement "handling" when player is "slow" (within max speed)
    private float pMovHandleFast = 0.05f; // When moving fast, drag/handling
    private bool pMovDisable = false; // Disables basic movement mechanics entirely; shouldn't be needed
    private float pMovSpeed;
    private Coroutine pMovSpeedResetCoroutine;
    private float pMovHandle; // Current value of movement handling: used to lerp velocity to input velocity (0 to 1)
    private Coroutine pMovHandleResetCoroutine;
    const float pickupRadius = 0.2f; //how far away can the player pick up an object?
    public Vector3 holdOffset; //what's the hold position of the currently held inventory item?
    private float epsilon = 0.001f;

    private int playerNum;
    private int keyboardControlledPlayer = 0; //for debug / testing without controllers - one player can be controlled by the keyboard at a time;

    //Idle variables
    public float timeUntilIdle = 3f;
    [HideInInspector]
    public bool idle;
    private bool idleLF;
    public float timeUntilLongIdle = 3f;
    [HideInInspector]
    public bool longIdle;
    private bool longIdleLF;

    private float timeSinceLastMoved;

    public Vector2 lsInput;
    private Func<string, float> GetAxis;

    private GameObject aimIndicator;

    private bool rt_pressed;
    private bool lt_pressed;

    // Use this for initialization
    void Start() {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        me = GetComponent<PlayerInfo>();
        playerNum = me.playerNum;
        inventory = GetComponent<Inventory>();
        TileContainer = GameObject.Find("Tiles");
        aimIndicator = transform.Find("AimIndicator").gameObject;
        SetControls();
        //Idle
        timeSinceLastMoved = 0f;
        idle = false;
        idleLF = false;

        // movement
        pMovSpeed = pMovSpeedBase;
        pMovHandle = pMovHandleBase;
    }

    // Update is called once per frame
    void Update() {
        //movement
        //rb.velocity = pMovSpeed * lsInput;

        //aiming and firing
        Vector2 aim_raw = new Vector2(GetAxis("Horizontal_R"), GetAxis("Vertical_R"));
        if (aim_raw.sqrMagnitude < epsilon) {
            aim_raw = Vector2.zero;
            aimIndicator.GetComponent<SpriteRenderer>().enabled = false;
        } else {
            aimIndicator.GetComponent<SpriteRenderer>().enabled = true;
        }
        Vector2 aim = aim_raw.normalized;
        float trigger = GetAxis("RTrigger");
        if (!rt_pressed && trigger > 0.9f) {
            //fire weapon/tool if aiming, else switch inventory slots
            rt_pressed = true;
            if (aim.Equals(Vector2.zero)) {
                inventory.IncSlot();
            } else {
                print("activating held item");
                if (inventory.Get()) {
                    Fireable f = inventory.Get().GetComponent<Fireable>();
                    if (f) {
                        f.Fire(aim, gameObject);
                    }
                } else {
                    // PUNCH
                }
            }
        }
        if (rt_pressed && trigger < 0.1f) {
            rt_pressed = false;
        }

        //debug
        aimIndicator.transform.position = (Vector2)transform.position + aim;
        aimIndicator.GetComponent<SpriteRenderer>().color = new Color(trigger, trigger, trigger);

        trigger = GetAxis("LTrigger");
        if (!lt_pressed && trigger > 0.9f) {
            //switch inventory slot
            lt_pressed = true;
            inventory.DecSlot();
        }
        if (lt_pressed && trigger < 0.1f) {
            lt_pressed = false;
        }


        if (rb.velocity.sqrMagnitude > float.Epsilon * float.Epsilon) {
            timeSinceLastMoved = 0f;
            idle = false;
            longIdle = false;
        } else {
            if (timeSinceLastMoved > timeUntilIdle) {
                idle = true;
            }

            if (timeSinceLastMoved > timeUntilLongIdle) {
                longIdle = true;
            }

            timeSinceLastMoved += Time.deltaTime;
        }

        if (idle && !idleLF) {
            GameManager.GetTextOverlayHandler(playerNum).AppearWords();
        } else if (!idle && idleLF) {
            GameManager.GetTextOverlayHandler(playerNum).DisappearWords();
        }

        if (longIdle && !longIdleLF) {
            GameManager.GetTextOverlayHandler(playerNum).AppearDefinitions();
        } else if (!longIdle && longIdleLF) {
            GameManager.GetTextOverlayHandler(playerNum).DisappearDefinitions();
        }

        idleLF = idle;
        longIdleLF = longIdle;

        ////Interact with world
        if (Input.GetKeyDown(AButton) || (me.playerNum == keyboardControlledPlayer && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)))) {
            Interact();
        } else if (Input.GetKeyDown(BButton) || (me.playerNum == keyboardControlledPlayer && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.R)))) {
            Drop();
        }

        ////Change which item is active
        if (Input.GetKeyDown(LeftBumper) || (me.playerNum == keyboardControlledPlayer && Input.GetKeyDown(KeyCode.LeftArrow))) {
            inventory.DecSlot();
        } else if (Input.GetKeyDown(RightBumper) || (me.playerNum == keyboardControlledPlayer && Input.GetKeyDown(KeyCode.RightArrow))) {
            inventory.IncSlot();
        }

        //make keyboardControlledPlayer adjustable by keyboard
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            keyboardControlledPlayer = 1;
        } else if (Input.GetKeyDown(KeyCode.Alpha2)) {
            keyboardControlledPlayer = 2;
        } else if (Input.GetKeyDown(KeyCode.Alpha3)) {
            keyboardControlledPlayer = 3;
        } else if (Input.GetKeyDown(KeyCode.Alpha4)) {
            keyboardControlledPlayer = 4;
        } else if (Input.GetKeyDown(KeyCode.Alpha0)) {
            keyboardControlledPlayer = 0;
        }
    }

    // runs every physics calculation frame, used for movement
    void FixedUpdate() {
        float axisX, axisY;
        if (playerNum == keyboardControlledPlayer) {
            axisX = Input.GetAxisRaw("Horizontal");
            axisY = Input.GetAxisRaw("Vertical");
        } else {
            axisX = GetAxis("Horizontal");
            axisY = GetAxis("Vertical");
        }
        lsInput = new Vector2(axisX, axisY);
        //rb.velocity = pMovSpeed * lsInput; 
        HandleMovement(axisX, axisY);
        // if (Input.GetKeyDown(AButton) || (me.playerNum == keyboardControlledPlayer && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)))) {
        //     DebugDash(axisX, axisY);
        // }
    }

    private void HandleMovement(float GetAxisX, float GetAxisY) {
        // Store movement vector.

        if (pMovDisable) return;

        Vector2 move = Vector2.ClampMagnitude(new Vector2(GetAxisX, GetAxisY), 1) * pMovSpeed;
        float handling = pMovHandle;
        // When above player max speed, we let reduce control so that momentum is preserved
        if (rb.velocity.magnitude > pMovSpeed) {
            handling = pMovHandleFast;
        } else {
            // can't reverse direction ezpz
            if (Vector2.Dot(rb.velocity, move) < -0.1) {
                handling *= 0.3f;
            }
        }

        rb.velocity = Vector2.Lerp(rb.velocity, move, handling);
    }
    private void DebugDash(float GetAxisX, float GetAxisY) {
        Vector2 move = Vector2.ClampMagnitude(new Vector2(GetAxisX, GetAxisY), 1);
        rb.velocity = move * pMovSpeed * 6;
    }

    private void SetControls() {
        AButton = me.GetKeyCode("A");
        BButton = me.GetKeyCode("B");
        LeftBumper = me.GetKeyCode("LeftBumper");
        RightBumper = me.GetKeyCode("RightBumper");
        GetAxis = me.GetAxisWindows;
        if (Game.IsOnOSX) {
            GetAxis = me.GetAxisOSX;
        } else if (Game.IsOnLinux) {
            GetAxis = me.GetAxisLinux;
        }
    }

    public void SetActiveSquare(GameObject newSquare) {
        activeSquare = newSquare;
    }

    //pseudocode of this:
    /*
	x = is player hovering over a grid square?
	y = is player currently holding something in inventory?
	z = is player holding a letter tile?
	w = is there a letter tile on the grid square?


	x y z w : swap inventory item with thing on the square
	x y z !w : place inventory item on the square 
	x y !z !w : Perform item’s action
	x !y !z w : take tile on square into inventory
	x !y !z !w : normal grab
	!x y !z !w : Perform item’s action
	!x !y !z !w : normal grab

	all other combinations are impossible or should do nothing
	 */
    private void Interact() {
        //		print ("interacting");
        bool x = (activeSquare != null);
        bool y = (inventory.Get() != null);
        bool z = y ? inventory.Get().GetComponent<Placeable>() : false;
        bool w = x ? activeSquare.GetComponent<GridSquare>().tile != null : false;
        //		print (x + " " + y + " " + z + " " + w);

        if (!y && !z && !w) {
            NormalGrab();
        } else if (y && !z && !w) {
            PerformItemAction();
        } else if (x) {
            if (y && z) {
                if (w) {
                    SwapWithSquare();
                } else {
                    PlaceOnSquare();
                }
            } else if (!y && !z && w) {
                TakeFromSquare();
            }
        }
    }
    private void PerformItemAction() {
        print("performing super cool item action");
    }
    private void Drop() {
        //		print ("dropping");
        if (activeSquare == null) {//do not drop if over a grid
            GameObject itemToDrop = inventory.Get();
            if (itemToDrop != null) {
                itemToDrop.transform.SetParent(TileContainer.transform);
                Game.RepositionHeight(itemToDrop, Height.OnFloor);
                Game.EnablePhysics(itemToDrop);
                inventory.Remove();
            }
        }
    }
    private void PlaceOnSquare() {
        //		print ("placing tile on square");
        GameObject itemToPlace = inventory.Get();
        itemToPlace.transform.SetParent(activeSquare.transform);
        itemToPlace.transform.position = activeSquare.transform.position;
        Game.RepositionHeight(itemToPlace, Height.OnGridSquare);
        activeSquare.GetComponent<GridSquare>().SetTile(itemToPlace);
        itemToPlace.GetComponent<Placeable>().PlaceOn(activeSquare, gameObject);
        inventory.Remove();
        if (itemToPlace.GetComponent<Flag>())
        {
            inventory.Remove();
            activeSquare.transform.parent.gameObject.GetComponent<GridControl>().SetOwnership(playerNum, gameObject);
        }
    }

    private void TakeFromSquare() {
        //		print ("taking from square");
        GameObject itemToTake = activeSquare.GetComponent<GridSquare>().tile;
        itemToTake.transform.SetParent(transform);
        inventory.Add(itemToTake);
        itemToTake.transform.localPosition = holdOffset;
        itemToTake.transform.rotation = Quaternion.identity;
        Game.RepositionHeight(itemToTake, Height.Held);
        activeSquare.GetComponent<GridSquare>().SetTile(null);
        itemToTake.GetComponent<Placeable>().TakeFrom(activeSquare, gameObject);
    }

    private void SwapWithSquare() {
        //		print ("swapping tile with square");
        GameObject temp = activeSquare.GetComponent<GridSquare>().tile;
        temp.transform.SetParent(transform);
        temp.transform.localPosition = holdOffset;
        temp.transform.rotation = Quaternion.identity;
        Game.RepositionHeight(temp, Height.Held);
        PlaceOnSquare();
        inventory.Add(temp);
    }

    private void NormalGrab() {
        //		print ("grabbing");
        //pick up nearest item within pickup radius
        GameObject closestObject = Game.ClosestItemInRadius(transform.position, pickupRadius);
        if (closestObject == null) {
            return;
        }
        //put item in inventory
        //inventory.Add(closestObject);
        closestObject.transform.SetParent(transform);
        inventory.Add(closestObject);
        closestObject.transform.localPosition = holdOffset;
        closestObject.transform.rotation = Quaternion.identity;
        Game.RepositionHeight(closestObject, Height.Held);
        Game.DisablePhysics(closestObject);

        // pick up flag
        if (closestObject.GetComponent<Flag>())
        {
            closestObject.GetComponent<Flag>().PickFlag(playerNum, gameObject);
        }
    }

    // movement modifier access
    public bool getMovDisabled() {
        return pMovDisable;
    }

    public void setMovDisabled(bool disabled) {
        pMovDisable = disabled;
    }
    public float getMovHandleBase() {
        return pMovHandleBase;
    }
    public float getMovHandle() {
        return pMovHandle;
    }
    public void setMovHandle(float value, float duration) {
        if (pMovHandleResetCoroutine != null) {
            StopCoroutine(pMovHandleResetCoroutine);
        }
        pMovHandle = value;
        pMovHandleResetCoroutine = StartCoroutine(resetMovHandling(pMovHandleBase, duration));
    }
    // Coroutine: Waits duration seconds, then sets pMovHandle to value.
    public IEnumerator resetMovHandling(float value, float duration) {
        yield return new WaitForSeconds(duration);
        pMovHandle = value;
    }
    public float getMovSpeedBase() {
        return pMovSpeedBase;
    }
    public float getMovSpeed() {
        return pMovSpeed;
    }
    public void setMovSpeed(float value, float duration) {
        if (pMovSpeedResetCoroutine != null) {
            StopCoroutine(pMovSpeedResetCoroutine);
        }
        pMovSpeed = value;
        pMovSpeedResetCoroutine = StartCoroutine(resetMovSpeed(pMovSpeedBase, duration));
    }
    // Coroutine: Waits duration seconds, then sets pMovSpeed to value.
    public IEnumerator resetMovSpeed(float value, float duration) {
        yield return new WaitForSeconds(duration);
        pMovSpeed = value;
    }


}