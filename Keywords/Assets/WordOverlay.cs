﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WordOverlay : MonoBehaviour
{
	public float timeUntilDecay = 10f;
	public float timeSpentDecaying = 2f;

	public bool coroutineInitialized;
	public bool coroutinePaused;

	public TMP_Text text;

    // Start is called before the first frame update
    void Start() {
		coroutinePaused = false;
		coroutineInitialized = false;
    }

	public void InitializeWord(string word) {
		if (coroutineInitialized) return;
		coroutineInitialized = true;
		print("Initializing");
		text.SetText(word);
		StartCoroutine(WaitCR());
	}

	public void Appear () {
		StopAllCoroutines();
		StartCoroutine(FadeCR(1f));
	}

	public void Disappear () {
		StopAllCoroutines();
		StartCoroutine(FadeCR(0f));
	}

	//targetAlpha = 0f if disappearing, 1f if appearing.
	IEnumerator FadeCR (float targetAlpha) {
		Color startColor = text.color;
		Color endColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
		float t = 0f;

		while (t < 1f) {
			text.color = Color.Lerp(startColor, endColor, t);
			t += Time.deltaTime / timeSpentDecaying;
			yield return new WaitForEndOfFrame();
		}
	}

	IEnumerator WaitCR () {
		float t = 0f;
		while (t < timeUntilDecay) {
			if (!coroutinePaused) {
				t += Time.deltaTime;
				print(t);
				yield return new WaitForEndOfFrame();
			}
		}
		Disappear();
	}
}
