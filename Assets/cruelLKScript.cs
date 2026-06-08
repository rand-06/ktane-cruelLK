using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class cruelLKScript : MonoBehaviour
{
	public KMAudio Audio;
	public AudioClip music;
	
	
	public List<KMSelectable> keys;
	public List<MeshRenderer> keysMesh;
	
	private List<Vector3> keysPositions;
	private float keyY;
	private int KEYMODE = 0; // 0 - only 0th button is pressable, 1 - no buttons are pressable, 2 - all buttons are pressable
	
	private bool ModuleSolved = false;

	private const float deltaX = -0.0461f - -0.0654f, deltaZ = 0.019f - 0.0579f;
	public TextMesh rotationIndicator, counterText, mainText;

	public Transform indicatorParent;

	private List<TextMesh> rotationIndicators;

	private int initKey, amountOfRotations, answer;
	private Vector3 initialPosition, initialRotation;
	
	static int ModuleIdCounter;
	int ModuleId;
	
	private List<Color> colors = new List<Color>
	{
		new Color(0.15f, 0.15f, 0.15f),
		new Color(0.5f, 0, 0),
		new Color(0, 0.5f, 0),
		new Color(0.5f, 0.5f, 0),
		new Color(0, 0, 0.5f),
		new Color(0.5f, 0, 0.5f),
		new Color(0, 0.5f, 0.5f),
		new Color(0.7f, 0.7f, 0.7f),
		new Color(0.4f, 0.4f, 0.4f),
		new Color(1, 0, 0),
		new Color(0, 1, 0),
		new Color(1, 1, 0),
		new Color(0, 0, 1),
		new Color(1, 0, 1),
		new Color(0, 1, 1),
		new Color(1, 1, 1)
	};

	private Color defaultColor = new Color(0.5f, 0.5f, 0.5f);
	private List<string> colorNames = new List<string>
	{
		"black","red","green","yellow","blue","magenta","cyan","white"
	};

	private List<int> rotations;

	void Awake ()
	{
		ModuleId = ++ModuleIdCounter;
		initKey = Rnd.Range(0, 16);
		amountOfRotations = Rnd.Range(300, 1000);
		keyY = keys[0].transform.localPosition.y;
		//initialPosition = keys[0].transform.localPosition;
		initialPosition = Vector3.zero;
		initialRotation = keys[0].transform.localRotation.eulerAngles;
		keysPositions = Enumerable.Range(0, 16).Select(i =>
			new List<Vector3>
			{
				new Vector3(1f, 0, 0),
				new Vector3(0, 1f, 0),
				new Vector3(0, 0, 1f),
				new Vector3(0.8f, 0.2f, 0.5f)
			}.Where(
				(_, j) => (i & (1 << j)) != 0
			).Aggregate(new Vector3(-.07f,keyY,-.06f), (current, t) => current + t / (1.8f/0.14f))
		).ToList();
		rotations = Enumerable.Range(0,32).Select(_=>Rnd.Range(0,16)).ToList();
		answer = getAnswer();
		log("Answer: " + (answer > 7 ? "bright " : "dark ") + colorNames[answer%8]);
		prepareMod();
		for (int i = 0; i < keys.Count; i++)
		{
			int i1 = i;
			keys[i1].OnInteract += delegate
			{
				pressKey(i1);
				return false;
			};
			keys[i1].OnHighlight += delegate
			{
				if (KEYMODE==2) mainText.text = (i1 > 7 ? "bright\n" : "dark\n") + colorNames[i1%8];
			};
			keys[i1].OnHighlightEnded += delegate
			{
				if (KEYMODE==2) mainText.text = "";
			};
		}
		foreach (var key in keysMesh) key.material.color = defaultColor;
		//for (int i = 0; i < keys.Count; i++) keysMesh[i].material.color = colors[i];
		//foreach (var key in keysMesh) StartCoroutine(freeSpinKey(key));
		//StartCoroutine(spinKeys(keys));
		//for (int i = 1; i < keys.Count; i++) keys[i].gameObject.SetActive(false);
	}

	private const float angleThreshold = 20f;
	float randomAngle() => (2 * Rnd.value - 1) * angleThreshold;

	void log(string msg)
	{
		print("[Cruel Limbo Keys #" + ModuleId + "] " + msg);
	}

	int getAnswer() // i'm sorry.
	{
		int ans = initKey;
		for (int i = 0; i <= amountOfRotations; i++)
			initKey = transformationWZYX(rotations[i % 32], initKey);
		return ans;
	}
	
	IEnumerator freeSpinKey(MeshRenderer key)
	{
		while (KEYMODE == 2)
		{
			Quaternion initRot = key.transform.localRotation;
			Quaternion goalRot = Quaternion.Euler(initRot.eulerAngles + new Vector3(randomAngle(), randomAngle(), randomAngle()));
			float timer = 0;
			float period = 1f + Rnd.value/3f;
			while (timer < period && KEYMODE == 2)
			{
				key.transform.localRotation = Quaternion.Lerp(initRot, goalRot, timer / period);
				timer += Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}
		}
	}

	void container(IEnumerator coroutine) => StartCoroutine(coroutine);

	IEnumerator initAnimation()
	{
		Vector3 startPos = keys[0].transform.localPosition;
		Quaternion startRot = keys[0].transform.localRotation;
		float animTime = 0.4f;
		List<int> order = Enumerable.Range(0, 16).ToList().Shuffle().ToList();
		foreach (var i in order)
		{
			float time = 0f;
			while (time < animTime)
			{
				keys[i].transform.localPosition = Vector3.Lerp(startPos, keysPositions[i], time / animTime);
				keys[i].transform.localRotation = Quaternion.Euler(
					startRot.eulerAngles + Vector3.Lerp(Vector3.zero, new Vector3(0f, 360f, 0f),
						time / animTime));
				time += Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			keys[i].transform.localRotation = startRot;
		}

		float flashTime = 0f;
		while (flashTime < 2f)
		{
			keysMesh[initKey].material.color = Color.Lerp(defaultColor,new Color(0.3f,1f,0),
				Mathf.Min(1f-Mathf.Abs(flashTime-1f),1f)
				);
			flashTime += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
	}
	
	IEnumerator startSeq()
	{
		KEYMODE = 1;
		for (int i = 1; i < keys.Count; i++) keys[i].gameObject.SetActive(true);
		Audio.PlaySoundAtTransform(music.name, transform);
		container(initAnimation());
		yield return new WaitForSeconds(9.3f);
		for (int i = 0; i < 32; i++)
		{
			container(rotate(rotations[i], 0.2f, i));
			yield return new WaitForSeconds(0.3f);
		}
		const int maxCounter = 360*6;
		int counter = -60;
		while (counter++ < 0)
		{
			for (int i = 0; i < keys.Count; i++)
			{
				keys[i].transform.localPosition = Vector3.Lerp(keysPositions[i],
					initialPosition,
					1f + (counter / 60f)
				);
			}
			yield return new WaitForSeconds(1/120f);
		}

		yield return spinKeys();
	}

	IEnumerator rotate(int rotation, float animTime, int number = -1)
	{
		List<int> rotationPos = Enumerable.Range(0, 16).Select(x=> transformationWZYX(rotation,x)).ToList();
		counterText.text = number.ToString();
		mainText.text = rotToString(rotation);
		float time = 0f;
		while (time < animTime)
		{
			for (int i = 0; i < keys.Count; i++)
				keys[i].transform.localPosition = Vector3.Lerp(keysPositions[i], keysPositions[rotationPos[i]],
					time / animTime);
			time += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		counterText.text = "";
		mainText.text = "";
		for (int i = 0; i < keys.Count; i++) keys[i].transform.localPosition = keysPositions[i];
	}

	IEnumerator flashRotText(TextMesh rotText)
	{
		float rep = 4f + Rnd.value*6f, timer = 0f;
		yield return new WaitForSeconds(Rnd.value*5f);
		while (KEYMODE == 2)
		{
			rotText.color = Color.Lerp(new Color(1,1,1,0),new Color(1,1,1,0.2f),
				(1 - Mathf.Cos(timer * 2 * Mathf.PI / rep))/2f);
			timer += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
	}

	IEnumerator spinKeys()
	{
		KEYMODE = 2;
		counterText.text = amountOfRotations + "?";
		foreach (MeshRenderer key in keysMesh) container(freeSpinKey(key));
		foreach (TextMesh rotText in rotationIndicators) container(flashRotText(rotText));
		float toCircle = 10f, velocity = 30f;
		float timer = 0f;
		while (!ModuleSolved)
		{
			for (int i = 0; i < keys.Count; i++)
			{
				if (KEYMODE == 1 && i==answer) continue;
				keys[i].transform.localPosition = new Vector3(
					Mathf.Sin(Mathf.PI / 8f * i + 2 * timer * Mathf.PI /velocity) * 0.07f, 
					keyY, 
					Mathf.Cos(Mathf.PI / 8f * i + + 2 * timer * Mathf.PI /velocity) * 0.07f
				) * Mathf.Min(1f, timer/toCircle);
				keysMesh[i].material.color = Color.Lerp(defaultColor,colors[i],timer/toCircle);
			}
			timer += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
	}

	void pressKey(int index)
	{
		if (KEYMODE == 1) return;
		if (KEYMODE == 0)
		{
			KEYMODE = 1;
			StartCoroutine(startSeq());
		}
		log("Pressing key " + index);

		if (KEYMODE == 2)
		{
			
			if (index != answer)
			{
				GetComponent<KMBombModule>().HandleStrike();
				container(deleteKey(index));
			}
			else
			{
				container(solve());
			}
		}
		
	}

	IEnumerator solve()
	{
		KEYMODE = 1;
		mainText.text = "";
		Vector3 startScale = keys[answer].transform.localScale;
		Vector3 position = keys[answer].transform.localPosition;
		Vector3 rotation = keys[answer].transform.localRotation.eulerAngles;
		List<Color> indicatorColors = rotationIndicators.Select(x=>x.color).ToList();
		List<float> indicatorValues = Enumerable.Range(0, indicatorColors.Count).Select(_=>Rnd.value).ToList();
		float timer = 0f, animation = 1f;
		while (timer < animation)
		{
			keys[answer].transform.localPosition = Vector3.Lerp(position, Vector3.zero, timer/animation);
			keys[answer].transform.localRotation = Quaternion.Euler(Vector3.Lerp(rotation, initialRotation, timer/animation));
			foreach (var key in keys.Where(x=>x!=keys[answer])) key.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer/animation);
			for (int i = 0; i < rotationIndicators.Count; i++)
				rotationIndicators[i].color = Color.Lerp(
					indicatorColors[i], new Color(1, 1, 1, 0), timer / animation * (1 + indicatorValues[i] * 5f)
				);
			timer += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		yield return deleteKey(answer);
		GetComponent<KMBombModule>().HandlePass();
		ModuleSolved = true;
		mainText.text = "Solved.";
		counterText.text = "GG!";
	}
	
	IEnumerator deleteKey(int index)
	{
		Vector3 startScale = keys[index].transform.localScale;
		float timer = 0f, period = 3f;
		while (timer < period)
		{
			keys[index].transform.localScale = Vector3.Lerp(startScale,Vector3.zero, timer/period);
			timer += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		keys[index].enabled = false;
	}
	
	string rotToString(int rot)
	{
		return new List<string>
			{ "XX", "XY", "XZ", "XW", "YX", "YY", "YZ", "YW", "ZX", "ZY", "ZZ", "ZW", "WX", "WY", "WZ", "WW" }[rot];
	}

	int transformationWZYX(int rot, int index)
	{
		int first = rot / 4, second = rot % 4;
		if (first == second) return index ^ (1 << first);
		int filtered = index & ~(1 << first) &  ~(1 << second);
		return (index & (1 << first)) == 0?
			(index & (1 << second)) == 0?
				filtered | (1<<first):
				filtered:
			(index & (1 << second)) == 0?
				filtered | (1<<second) |  (1<<first):
				filtered | (1<<second);
	}

	void prepareMod()
	{
		counterText.text = "";
		mainText.text = "";
		rotationIndicators = Enumerable.Range(0,32).Select(x=>x==0?rotationIndicator:Instantiate(rotationIndicator,indicatorParent)).ToList();
		Vector3 initPos = rotationIndicator.transform.localPosition;
		for (int i = 0; i < 32; i++)
		{
			rotationIndicators[i].color = new Color(1, 1, 1, 0);
			rotationIndicators[i].transform.localPosition = initPos + new Vector3(deltaX * (i%8), 0, deltaZ*(i/8));
			rotationIndicators[i].text = rotToString(rotations[i]);
		}
	}
	
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use !{0} start to start the sequence, !{0} remind to show the color of the initial key, !{0} (color) to submit colored key.";
#pragma warning restore 414
	
	public IEnumerator ProcessTwitchCommand(string Command)
	{
		yield return null;
		Command = Command.ToUpperInvariant();
		if (Command == "START" && KEYMODE == 0)
		{
			pressKey(0);
		}
		else if (Command == "REMIND" && KEYMODE == 2)
		{
			keys[initKey].OnHighlight();
		}
		else
		{
			List<string> words = Command.Split(' ').ToList();
			if (
				words.Count == 2 &&
				(words[0] == "BRIGHT" || words[0] == "DARK") &&
				colorNames.Contains(words[1].ToLowerInvariant()) &&
				KEYMODE == 2
			)
			{
				pressKey((words[0] == "BRIGHT" ? 8 : 0) + colorNames.IndexOf(words[1].ToLowerInvariant()));
			}
			else yield return "sendtochaterror Invalid command.";
				
		}
	}

	public IEnumerator TwitchHandleForcedSolve()
	{
		yield return null;
		yield return solve();
	}
}
