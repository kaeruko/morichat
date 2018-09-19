﻿//Original Scripts by IIColour (IIColour_Spectrum)

using UnityEngine;
using System.Collections;

public class myPlayerMovement : MonoBehaviour {

	public static myPlayerMovement player;

	//before a script runs, it'll check if the player is busy with another script's GameObject.
	public GameObject busyWith = null;

	public bool moving = false;
	public bool still = true;
	public bool running = false;
	public bool surfing = false;
	public bool bike = false;
	public bool strength = false;
	public float walkSpeed = 0.3f; //time in seconds taken to walk 1 square.
	public float runSpeed = 0.15f;
	public float surfSpeed = 0.2f;
	public float speed;
	public int direction = 2; //0 = up, 1 = right, 2 = down, 3 = left

	public bool canInput = true;

	public float increment = 1f;

	private Transform pawn;
	private Transform pawnReflection;
	//private Material pawnReflectionSprite;
	private SpriteRenderer pawnSprite;
	private SpriteRenderer pawnReflectionSprite;

	public Transform hitBox;

	public MapCollider currentMap;
	public MapCollider destinationMap;

	public MapSettings accessedMapSettings;
	private AudioClip accessedAudio;
	private int accessedAudioLoopStartSamples;

	public Camera mainCamera;
	public Vector3 mainCameraDefaultPosition;
    public float mainCameraDefaultFOV;

	private SpriteRenderer mount;
	private Vector3 mountPosition;

	private string animationName;
	private Sprite[] spriteSheet;
	private Sprite[] mountSpriteSheet;

	private int frame;
	private int frames;
	private int framesPerSec;
	private float secPerFrame;
	private bool animPause;
	private bool overrideAnimPause;

	public int walkFPS = 7;
	public int runFPS = 12;

	private int mostRecentDirectionPressed = 0;
	private float directionChangeInputDelay = 0.08f;

//	private SceneTransition sceneTransition;

	private AudioSource PlayerAudio;

	public AudioClip bumpClip;
	public AudioClip jumpClip;
	public AudioClip landClip;
	void Awake () {

		PlayerAudio = transform.GetComponent<AudioSource>();

		//set up the reference to this script.
		player = this;

		canInput = true;
		speed = walkSpeed;


		mainCamera = transform.Find("Camera").GetComponent<Camera>();
		mainCameraDefaultPosition = mainCamera.transform.localPosition;
        mainCameraDefaultFOV = mainCamera.fieldOfView;

		pawn = transform.Find("Pawn");
		pawnReflection = transform.Find("PawnReflection");
		pawnSprite = pawn.GetComponent<SpriteRenderer>();
		pawnReflectionSprite = pawnReflection.GetComponent<SpriteRenderer>();

		//pawnReflectionSprite = transform.FindChild("PawnReflection").GetComponent<MeshRenderer>().material;

		hitBox = transform.Find("Player_Transparent");

		mount = transform.Find("Mount").GetComponent<SpriteRenderer>();
		mountPosition = mount.transform.localPosition;



	}

	void Start () {

		if(!surfing){
			updateMount(false);
		}

		updateAnimation("walk",walkFPS);
		StartCoroutine("animateSprite");
		animPause = true;

		updateDirection(direction);

		StartCoroutine(control());



		//Check current map
		RaycastHit[] hitRays = Physics.RaycastAll(transform.position+Vector3.up, Vector3.down);
		int closestIndex = -1;
		float closestDistance = float.PositiveInfinity;
		if (hitRays.Length > 0){
			for (int i = 0; i < hitRays.Length; i++){
// Debug.Log("gameObject "+hitRays[i].collider.gameObject);
				if(hitRays[i].collider.gameObject.GetComponent<MapCollider>() != null){
					if(hitRays[i].distance < closestDistance){

						//一番近いオブジェクトの位置
						closestDistance = hitRays[i].distance;
						closestIndex = i;
					}
				}
			}
		}
		if(closestIndex != -1){
			currentMap = hitRays[closestIndex].collider.gameObject.GetComponent<MapCollider>();}
		else{ 	//if no map found
			//Check for map in front of player's direction
			hitRays = Physics.RaycastAll(transform.position+Vector3.up+getForwardVectorRaw(), Vector3.down);
			closestIndex = -1;
			closestDistance = float.PositiveInfinity;
			if (hitRays.Length > 0){
				for (int i = 0; i < hitRays.Length; i++){
					if(hitRays[i].collider.gameObject.GetComponent<MapCollider>() != null){
						if(hitRays[i].distance < closestDistance){
							closestDistance = hitRays[i].distance;
							closestIndex = i;
						}
					}
				}
			}
			if(closestIndex != -1){
				currentMap = hitRays[closestIndex].collider.gameObject.GetComponent<MapCollider>();}
			else{
				Debug.Log ("no map found");}
		}


		if (currentMap != null){
			accessedMapSettings = currentMap.gameObject.GetComponent<MapSettings>();
			if (accessedAudio != accessedMapSettings.getBGM()){ //if audio is not already playing
				accessedAudio = accessedMapSettings.getBGM();
				accessedAudioLoopStartSamples = accessedMapSettings.getBGMLoopStartSamples();
			}
		}


		//check position for transparent bumpEvents
		Collider transparentCollider = null;
		Collider[] hitColliders = Physics.OverlapSphere (transform.position, 0.4f);
		if(hitColliders.Length > 0){
			for(int i = 0; i < hitColliders.Length; i++){
				if(hitColliders[i].name.ToLowerInvariant().Contains("_transparent")){
					if(!hitColliders[i].name.ToLowerInvariant().Contains("player")){
						transparentCollider = hitColliders[i];}
				}
			}
		}
		if(transparentCollider != null){
			//send bump message to the object's parent object
			transparentCollider.transform.parent.gameObject.SendMessage("bump", SendMessageOptions.DontRequireReceiver);
		}

		//DEBUG
		if(accessedMapSettings != null){
			WildPokemonInitialiser[] encounterList = accessedMapSettings.getEncounterList(WildPokemonInitialiser.Location.Standard);
			string namez = "";
			for(int i = 0; i < encounterList.Length; i++){
				namez += PokemonDatabase.getPokemon(encounterList[i].ID).getName() +", ";}
		}
Debug.Log("closestDistance"+ closestDistance);

	}



	//毎fps変わるところ
	void Update(){
		//ここでキー判定をしてる
		//check for new inputs, so that the new direction can be set accordingly
		if(Input.GetButtonDown("Horizontal")){
			if(Input.GetAxisRaw("Horizontal") > 0){
			//	Debug.Log("NEW INPUT: Right");
				mostRecentDirectionPressed = 1;
			}
			else if(Input.GetAxisRaw("Horizontal") < 0){
			//	Debug.Log("NEW INPUT: Left");
				mostRecentDirectionPressed = 3;
			}
		}
		else if(Input.GetButtonDown("Vertical")){
			if(Input.GetAxisRaw("Vertical") > 0){
			//	Debug.Log("NEW INPUT: Up");
				mostRecentDirectionPressed = 0;
			}
			else if(Input.GetAxisRaw("Vertical") < 0){
			//	Debug.Log("NEW INPUT: Down");
				mostRecentDirectionPressed = 2;
			}
		}

	}

	//上下左右に変更がないか
	private bool isDirectionKeyHeld(int directionCheck){
		//モデルなら常時true
		return true;

		bool directionHeld = false;
		if(directionCheck == 0 && Input.GetAxisRaw("Vertical") > 0){
			directionHeld = true;}
		else if(directionCheck == 1 && Input.GetAxisRaw("Horizontal") > 0){
			directionHeld = true;}
		else if(directionCheck == 2 && Input.GetAxisRaw("Vertical") < 0){
			directionHeld = true;}
		else if(directionCheck == 3 && Input.GetAxisRaw("Horizontal") < 0){
			directionHeld = true;}
		return directionHeld;
	}

	//startから実行。whileが続いて繰り返してる
	private IEnumerator control(){
		bool still;
		while(true){
			still = true; //the player is still, but if they've just finished moving a space, moving is still true for this frame (see end of coroutine)
			if(canInput){
				//サーフィンでもなくバイクでもない
				if(!surfing && !bike){
					if(Input.GetButton("Run")){
						running = true;
						if(moving){
							updateAnimation("run",runFPS);
						}else{
							updateAnimation("walk",walkFPS);
						}
						speed = runSpeed;
					}
					else{
						running = false;
						updateAnimation("walk",walkFPS);
						speed = walkSpeed;
					}
				}
				//メニュー画面を開く
				if(Input.GetButton("Start")){ //open Pause Menu
					if(moving || Input.GetButtonDown("Start")){
					//移動中の場合
						if(setCheckBusyWith(Scene.main.Pause.gameObject)){
							animPause = true;
							Scene.main.Pause.gameObject.SetActive(true);
							StartCoroutine(Scene.main.Pause.control());
							while(Scene.main.Pause.gameObject.activeSelf){
								yield return null;}
							unsetCheckBusyWith(Scene.main.Pause.gameObject);
						}
					}
				}
				else if (Input.GetButtonDown("Select")){
					interact();
				}

				//タップ
				else if(Input.touchCount > 0){
					// Debug.Log("=========tap start========");
					Touch touch = Input.GetTouch(0);
					// Debug.Log("=========tap end========");
					still = false;
					//向き変更
					yield return StartCoroutine(moveForward(touch.position));
				}
				//上下左右を押した場合
				else if(Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0){

					//if most recent direction pressed is held, but it isn't the current direction, set it to be
					//向きが違う場合直すまで待つ
					if(mostRecentDirectionPressed != direction && isDirectionKeyHeld(mostRecentDirectionPressed)){
						updateDirection(mostRecentDirectionPressed);
						if(!moving){	// unless player has just moved, wait a small amount of time to ensure that they have time to
							yield return new WaitForSeconds(directionChangeInputDelay);} // let go before moving (allows only turning)
					}
					//if a new direction wasn't found, direction would have been set, thus ending the update
					else{

						//if current direction is not held down, check for the new direction to turn to
						if(!isDirectionKeyHeld(direction)){
							//it's least likely to have held the opposite direction by accident
							int directionCheck = (direction+2 > 3)? direction-2 : direction+2;
							if(isDirectionKeyHeld(directionCheck)){
								updateDirection(directionCheck);
								if(!moving){
									yield return new WaitForSeconds(directionChangeInputDelay);}
							}
							else{
								//it's either 90 degrees clockwise, counter, or none at this point. prioritise clockwise.
								directionCheck = (direction+1 > 3)? direction-3 : direction+1;
								if(isDirectionKeyHeld(directionCheck)){
									updateDirection(directionCheck);
									if(!moving){
										yield return new WaitForSeconds(directionChangeInputDelay);}
								}
								else{
									directionCheck = (direction-1 < 0)? direction+3 : direction-1;
									if(isDirectionKeyHeld(directionCheck)){
										updateDirection(directionCheck);
										if(!moving){
											yield return new WaitForSeconds(directionChangeInputDelay);}
									}
								}
							}
						}
						//if current direction was held, then we want to attempt to move forward.
						else{
							moving = true;
						}
					}

					//if moving is true (including by momentum from the previous step) then attempt to move forward.
					if(moving){
						//ここから移動処理
						still = false;
						yield return StartCoroutine(moveForward());
					}


				}
				else if (Input.GetKeyDown("g")){ //DEBUG
					// Debug.Log(currentMap.getTileTag(transform.position));
				}
			}
			if(still){				//if still is true by this point, then no move function has been called
				animPause = true;
				moving = false;}	//set moving to false. The player loses their momentum.

			yield return null;
		}
	}

	public void updateDirection(int dir){
		direction = dir;
		pawnSprite.sprite = spriteSheet[direction*frames+frame];
		pawnReflectionSprite.sprite = pawnSprite.sprite;
		//pawnReflectionSprite.SetTextureOffset("_MainTex", GetUVSpriteMap(direction*frames+frame));
		if(mount.enabled){
			mount.sprite = mountSpriteSheet[dir];
		}
	}

	private void updateMount(bool enabled){
		mount.enabled = enabled;
	}

	private void updateMount(bool enabled, string spriteName){
		mount.enabled = enabled;
		mountSpriteSheet = Resources.LoadAll<Sprite>("PlayerSprites/"+spriteName);
		mount.sprite = mountSpriteSheet[direction];
	}

	public void updateAnimation(string newAnimationName, int fps){
		if(animationName != newAnimationName){
Debug.Log("animationName:"+animationName);
Debug.Log("newAnimationName:"+newAnimationName);
			animationName = newAnimationName;
//			spriteSheet = Resources.LoadAll<Sprite>("PlayerSprites/"+SaveData.currentSave.getPlayerSpritePrefix()+newAnimationName);
			spriteSheet = Resources.LoadAll<Sprite>("PlayerSprites/m_hgss_walk");
			//pawnReflectionSprite.SetTexture("_MainTex", Resources.Load<Texture>("PlayerSprites/"+SaveData.currentSave.getPlayerSpritePrefix()+newAnimationName));
			framesPerSec = fps;
			secPerFrame = 1f/(float)framesPerSec;
			frames = Mathf.RoundToInt((float)spriteSheet.Length/4f);
			if(frame >= frames){
				frame = 0;}
		}
	}

	public void reflect(bool setState){
		pawnReflectionSprite.enabled = setState;
	}

	private Vector2 GetUVSpriteMap(int index){
		int row = index/4;
		int column = index%4;

		return new Vector2(0.25f*column, 0.75f - (0.25f*row));
	}

	private IEnumerator animateSprite(){
		frame = 0;
		frames = 4;
		framesPerSec = walkFPS;
		secPerFrame = 1f/(float)framesPerSec;
		while(true){
			for(int i = 0; i < 4; i++){
				if(animPause && frame%2 != 0 && !overrideAnimPause){
					frame -= 1;}
				pawnSprite.sprite = spriteSheet[direction*frames+frame];
				pawnReflectionSprite.sprite = pawnSprite.sprite;
				//pawnReflectionSprite.SetTextureOffset("_MainTex", GetUVSpriteMap(direction*frames+frame));
				yield return new WaitForSeconds(secPerFrame/4f);
			}
			if(!animPause || overrideAnimPause){
				frame += 1;
				if(frame >= frames){
					frame = 0;}
			}
		}
	}
	public void setOverrideAnimPause(bool set){
		overrideAnimPause = set;}

	///Attempts to set player to be busy with "caller" and pauses input, returning true if the request worked.
	public bool setCheckBusyWith(GameObject caller){
		if(myPlayerMovement.player.busyWith == null){
			myPlayerMovement.player.busyWith = caller;}
		//if the player is definitely busy with caller object
Debug.Log(caller);
		if(myPlayerMovement.player.busyWith == caller){
			pauseInput();
			Debug.Log("Busy with "+myPlayerMovement.player.busyWith);
			return true;}
		return false;
	}

	///Attempts to unset player to be busy with "caller". Will unpause input only if
	///the player is still not busy 0.1 seconds after calling.
	public void unsetCheckBusyWith(GameObject caller){
		if(myPlayerMovement.player.busyWith == caller){
			myPlayerMovement.player.busyWith = null;}
		StartCoroutine(checkBusinessBeforeUnpause(0.1f));
	}

	public IEnumerator checkBusinessBeforeUnpause(float waitTime){
		yield return new WaitForSeconds(waitTime);
		if(myPlayerMovement.player.busyWith == null){
			unpauseInput();}
		else{
			Debug.Log("Busy with "+myPlayerMovement.player.busyWith);}
	}

	public void pauseInput(){
		canInput = false;
		if(animationName == "run"){
			updateAnimation("walk",walkFPS);}
		running = false;
	}

	public void pauseInput(float secondsToWait){
		pauseInput();
        StartCoroutine(checkBusinessBeforeUnpause(secondsToWait));
	}

	public void unpauseInput(){
		Debug.Log("unpaused");
		canInput = true;
	}

	public bool isInputPaused(){
		if(canInput){
			return false;}
		else{
			return true;}
	}
	public void activateStrength(){
		strength = true;
	}




	///returns the vector relative to the player direction, without any modifications.
	public Vector3 getForwardVectorRaw(){
		return getForwardVectorRaw(direction);}


	//何ポイント変えるか。positionではない
	public Vector3 getDistinationVectorRaw(Vector3 current , Vector3 dist){
		bool checkForBridge = true;
		//現在地と移動地の引き算
		Vector3 movement = new Vector3(0,0,0);
Debug.Log("nearClipPlane:::"+mainCamera.nearClipPlane);
		Vector3 scrn = mainCamera.ScreenToWorldPoint(new Vector3(dist.x, dist.y,mainCamera.nearClipPlane ));
		movement = new Vector3( scrn.x ,  0, scrn.z) - new Vector3( current.x , 0, current.z  );

		//position, 方向、距離
//		RaycastHit[] hitRays = Physics.RaycastAll(transform.position+movement+new Vector3(0,1.5f,0), Vector3.up);
		RaycastHit[] hitRays = Physics.RaycastAll(transform.position+new Vector3(0,1.5f,0), Vector3.up, 1f);

		RaycastHit[] hitColliders = Physics.RaycastAll(transform.position+movement+new Vector3(0,1.5f,0), Vector3.down);



		//movementを方向と移動量に分解する

Debug.Log("6/21 test.Length:"+hitRays.Length+" hitColliders.Length:"+hitColliders.Length);
			if (hitRays.Length > 0){
				for (int i = 0; i < hitRays.Length; i++){
	Debug.Log("gameObject "+hitRays[i].collider.gameObject);
				}
			}





		RaycastHit mapHit = new RaycastHit();
		RaycastHit bridgeHit = new RaycastHit();
		//cycle through each of the collisions
		if (hitColliders.Length > 0){
			for (int i = 0; i < hitColliders.Length; i++){
Debug.Log(hitColliders[i].collider.gameObject);
				//if map has not been found yet
				if(mapHit.collider == null){
					//if a collision's gameObject has a mapCollider, it is a map. set it to be the destination map.
					if(hitColliders[i].collider.gameObject.GetComponent<MapCollider>() != null){
						mapHit = hitColliders[i];
						destinationMap = mapHit.collider.gameObject.GetComponent<MapCollider>();
					}
				}
				else if(bridgeHit.collider != null && checkForBridge){ //if both have been found
					i = hitColliders.Length;	//stop searching
				}
				//if bridge has not been found yet
				if(bridgeHit.collider == null && checkForBridge){
					//if a collision's gameObject has a BridgeHandler, it is a bridge.
					if(hitColliders[i].collider.gameObject.GetComponent<BridgeHandler>() != null){
						bridgeHit = hitColliders[i];
					}
				}
				else if(mapHit.collider != null){ //if both have been found
					i = hitColliders.Length;	//stop searching
				}
			}
		}

		if(bridgeHit.collider != null){
			//modify the forwards vector to align to the bridge.
			movement -= new Vector3(0,(transform.position.y - bridgeHit.point.y),0);
		}
		//if no bridge at destination
		else if(mapHit.collider != null){
			//modify the forwards vector to align to the mapHit.
			movement -= new Vector3(0,(transform.position.y - mapHit.point.y),0);
		}

				return movement;

		// float currentSlope = Mathf.Abs(MapCollider.getSlopeOfPosition(transform.position, direction));
		// float destinationSlope = Mathf.Abs(MapCollider.getSlopeOfPosition(transform.position+getForwardVectorRaw(), direction, checkForBridge));
		// float yDistance = Mathf.Abs((transform.position.y+movement.y) - transform.position.y);
		// yDistance = Mathf.Round(yDistance*100f)/100f;

		// Debug.Log("currentSlope: "+currentSlope+", destinationSlope: "+destinationSlope+", yDistance: "+yDistance+" movement:"+movement);

		// //if either slope is greater than 1 it is too steep.
		// if(currentSlope <= 1 && destinationSlope <= 1){
		// 	//if yDistance is greater than both slopes there is a vertical wall between them
		// 	if(yDistance <= currentSlope || yDistance <= destinationSlope){
		// 		return movement;
		// 	}
		// }
		// return Vector3.zero;


		Debug.Log("dist:"+scrn);


		Debug.Log("current:"+current);
		Debug.Log("movement:"+movement);
		return movement;
		//タップポジション

	}

	public Vector3 getForwardVectorRaw(int direction){



		//set vector3 based off of direction
		Vector3 forwardVector = new Vector3(0,0,0);
		if(direction == 0){
			forwardVector = new Vector3(0,0,1f);}
		else if(direction == 1){
			forwardVector = new Vector3(1f,0,0);}
		else if(direction == 2){
			forwardVector = new Vector3(0,0,-1f);}
		else if(direction == 3){
			forwardVector = new Vector3(-1f,0,0);}
		return forwardVector;
	}


	public Vector3 getForwardVector(){
		return getForwardVector(direction, true);}
	public Vector3 getForwardVector(int direction){
		return getForwardVector(direction, true);}
	public Vector3 getForwardVector(int direction, bool checkForBridge){
		//set initial vector3 based off of direction
		Vector3 movement = getForwardVectorRaw(direction);

		//Check destination map	and bridge
		//0.5f to adjust for stair height
		//cast a ray directly downwards from the position directly in front of the player		//1f to check in line with player's head
		RaycastHit[] hitColliders = Physics.RaycastAll(transform.position+movement+new Vector3(0,1.5f,0), Vector3.down);
		RaycastHit mapHit = new RaycastHit();
		RaycastHit bridgeHit = new RaycastHit();
		//cycle through each of the collisions
Debug.Log("hitColliders.Length:"+hitColliders.Length);
		if (hitColliders.Length > 0){
			for (int i = 0; i < hitColliders.Length; i++){
				//if map has not been found yet
				if(mapHit.collider == null){
					//if a collision's gameObject has a mapCollider, it is a map. set it to be the destination map.
					if(hitColliders[i].collider.gameObject.GetComponent<MapCollider>() != null){
						mapHit = hitColliders[i];
						destinationMap = mapHit.collider.gameObject.GetComponent<MapCollider>();
					}
				}
				else if(bridgeHit.collider != null && checkForBridge){ //if both have been found
					i = hitColliders.Length;	//stop searching
				}
				//if bridge has not been found yet
				if(bridgeHit.collider == null && checkForBridge){
					//if a collision's gameObject has a BridgeHandler, it is a bridge.
					if(hitColliders[i].collider.gameObject.GetComponent<BridgeHandler>() != null){
						bridgeHit = hitColliders[i];
					}
				}
				else if(mapHit.collider != null){ //if both have been found
					i = hitColliders.Length;	//stop searching
				}
			}
		}

		if(bridgeHit.collider != null){
			//modify the forwards vector to align to the bridge.
			movement -= new Vector3(0,(transform.position.y - bridgeHit.point.y),0);
		}
		//if no bridge at destination
		else if(mapHit.collider != null){
			//modify the forwards vector to align to the mapHit.
			movement -= new Vector3(0,(transform.position.y - mapHit.point.y),0);
		}


		float currentSlope = Mathf.Abs(MapCollider.getSlopeOfPosition(transform.position, direction));
		float destinationSlope = Mathf.Abs(MapCollider.getSlopeOfPosition(transform.position+getForwardVectorRaw(), direction, checkForBridge));
		float yDistance = Mathf.Abs((transform.position.y+movement.y) - transform.position.y);
		yDistance = Mathf.Round(yDistance*100f)/100f;

		Debug.Log("currentSlope: "+currentSlope+", destinationSlope: "+destinationSlope+", yDistance: "+yDistance+" movement:"+movement);

		//if either slope is greater than 1 it is too steep.
		if(currentSlope <= 1 && destinationSlope <= 1){
			//if yDistance is greater than both slopes there is a vertical wall between them
			if(yDistance <= currentSlope || yDistance <= destinationSlope){
				return movement;
			}
		}
		return Vector3.zero;
	}

	///Make the player move one space in the direction they are facing
	//distがposition,ベクター3
	private IEnumerator moveForward(Vector3 dist){

		Vector3 movement = getDistinationVectorRaw(transform.position, dist);

		bool ableToMove = false;
		//コライダチェック
		if(movement != Vector3.zero){
			//check destination for objects/transparents
			Collider objectCollider = null;
			Collider transparentCollider = null;
			//小さな同心円状の中にあるオブジェクト判定 center, radius

//			Collider[] hitColliders = Physics.OverlapSphere (transform.position+movement+new Vector3(0,0.5f,0), 0.4f);
			Collider[] hitColliders = Physics.OverlapSphere (transform.position+movement+new Vector3(0,0.5f,0), 0.4f);

//			RaycastHit[] hitRays = Physics.RaycastAll(transform.position+Vector3.up, Vector3.down);
// 			RaycastHit[] hitRays = Physics.RaycastAll(transform.position, transform.forward, 3.0F);
// Debug.Log("6/14 hitRays.Length:"+hitRays.Length+" hitColliders.Length:"+hitColliders.Length);
// 			if (hitRays.Length > 0){
// 				for (int i = 0; i < hitRays.Length; i++){
// 	Debug.Log("gameObject "+hitRays[i].collider.gameObject);
// 				}
// 			}



			//オブジェクトがあるまでチェック shorthandlerは？


			if(hitColliders.Length > 0){
				for(int i = 0; i < hitColliders.Length; i++){
					if(hitColliders[i].name.ToLowerInvariant().Contains("_object")){
						objectCollider = hitColliders[i];
Debug.Log("objectCollider:" + objectCollider.transform.parent.gameObject);

					}
					else if(hitColliders[i].name.ToLowerInvariant().Contains("_transparent")){
						transparentCollider = hitColliders[i];}
				}
			}
			if(objectCollider != null){
				//send bump message to the object's parent object
				objectCollider.transform.parent.gameObject.SendMessage("bump", SendMessageOptions.DontRequireReceiver);
			}else{
				yield return StartCoroutine(move(movement));
			}


		}

	}

	private IEnumerator moveForward(){
		Vector3 movement = getForwardVector();

Debug.Log("moveForward movement:"+movement);
		bool ableToMove = false;

		//without any movement, able to move should stay false
		if(movement != Vector3.zero){
			//check destination for objects/transparents
			Collider objectCollider = null;
			Collider transparentCollider = null;
			Collider[] hitColliders = Physics.OverlapSphere (transform.position+movement+new Vector3(0,0.5f,0), 0.4f);
			if(hitColliders.Length > 0){
				for(int i = 0; i < hitColliders.Length; i++){
					if(hitColliders[i].name.ToLowerInvariant().Contains("_object")){
						objectCollider = hitColliders[i];}
					else if(hitColliders[i].name.ToLowerInvariant().Contains("_transparent")){
						transparentCollider = hitColliders[i];}
				}
			}

			if(objectCollider != null){
Debug.Log("objectCollider:" + objectCollider.transform.parent.gameObject);
				//send bump message to the object's parent object
				objectCollider.transform.parent.gameObject.SendMessage("bump", SendMessageOptions.DontRequireReceiver);
			}
			else{ //if no objects are in the way
				int destinationTileTag = destinationMap.getTileTag(transform.position+movement);
// Debug.Log("transform.position");
// Debug.Log(transform.position);
// Debug.Log("movement");
// Debug.Log(movement);
// Debug.Log("destinationTileTag:"+destinationTileTag);
				RaycastHit bridgeHit = MapCollider.getBridgeHitOfPosition(transform.position+movement+new Vector3(0,0.1f,0));
				if(bridgeHit.collider != null || destinationTileTag != 1){	//wall tile tag

					if(bridgeHit.collider == null && !surfing && destinationTileTag == 2){ //(water tile tag)
					}
					else{
						if(surfing && destinationTileTag != 2f){ //disable surfing if not headed to water tile
							updateAnimation("walk",walkFPS);
							speed = walkSpeed;
							surfing = false;
							StartCoroutine("dismount");
							BgmHandler.main.PlayMain(accessedAudio, accessedAudioLoopStartSamples);
						}

						if(destinationMap != currentMap){  //if moving onto a new map
							currentMap = destinationMap;
							accessedMapSettings = destinationMap.gameObject.GetComponent<MapSettings>();
							if (accessedAudio != accessedMapSettings.getBGM()){ //if audio is not already playing
								accessedAudio = accessedMapSettings.getBGM();
								accessedAudioLoopStartSamples = accessedMapSettings.getBGMLoopStartSamples();
								BgmHandler.main.PlayMain(accessedAudio, accessedAudioLoopStartSamples);}
							destinationMap.BroadcastMessage("repair", SendMessageOptions.DontRequireReceiver);
							Debug.Log(destinationMap.name +"   "+ accessedAudio.name);
						}

						if(transparentCollider != null){
							//send bump message to the transparents's parent object
							transparentCollider.transform.parent.gameObject.SendMessage("bump", SendMessageOptions.DontRequireReceiver);
						}

						ableToMove = true;
						yield return StartCoroutine(move(movement));
					}
				}
			}
		}

		//if unable to move anywhere, then set moving to false so that the player stops animating.
		if(!ableToMove){
			Invoke("playBump", 0.05f);
			moving = false;
			animPause = true;
		}
	}




	public IEnumerator move(Vector3 movement){
		yield return StartCoroutine(move(movement, true, false));}
	public IEnumerator move(Vector3 movement, bool encounter){
		yield return StartCoroutine(move(movement, encounter, false));}
	public IEnumerator move(Vector3 movement, bool encounter, bool lockFollower){
		if(movement != Vector3.zero){
			Vector3 startPosition = hitBox.position;
			moving = true;
			increment = 0;

			if(!lockFollower){
			}
			animPause = false;
			while(increment < 1f){ //increment increases slowly to 1 over the frames
				increment += (1f/speed)*Time.deltaTime; //speed is determined by how many squares are crossed in one second
				if(increment > 1){
					increment = 1;}
				transform.position = startPosition + (movement*increment);
				hitBox.position = startPosition + movement;
				yield return null;
			}

			//check for encounters unless disabled
			if(encounter){
				int destinationTag = currentMap.getTileTag(transform.position);
				if(destinationTag != 1){ //not a wall
					if(destinationTag == 2){ //surf tile
						StartCoroutine(myPlayerMovement.player.wildEncounter(WildPokemonInitialiser.Location.Surfing));}
					else{ //land tile
						StartCoroutine(myPlayerMovement.player.wildEncounter(WildPokemonInitialiser.Location.Standard));}
				}
			}
		}
	}

	public IEnumerator moveCameraTo(Vector3 targetPosition, float cameraSpeed){
		targetPosition += mainCameraDefaultPosition;
		Vector3 startPosition = mainCamera.transform.localPosition;
		Vector3 movement = targetPosition - startPosition;
		float increment = 0;
		if(cameraSpeed != 0){
			while(increment < 1f){ //increment increases slowly to 1 over the frames
				increment += (1f/cameraSpeed)*Time.deltaTime;
				if(increment > 1){
					increment = 1;}
				mainCamera.transform.localPosition = startPosition + (movement*increment);
				yield return null;
			}
		}
		mainCamera.transform.localPosition = targetPosition;
	}




	public void forceMoveForward(){
		StartCoroutine(forceMoveForwardIE(1));}

	public void forceMoveForward(int spaces){
		StartCoroutine(forceMoveForwardIE(spaces));}
	private IEnumerator forceMoveForwardIE(int spaces){
		overrideAnimPause = true;
		for(int i = 0; i < spaces; i++){
			Vector3 movement = getForwardVector();

			//check destination for transparents
			Collider objectCollider = null;
			Collider transparentCollider = null;
			Collider[] hitColliders = Physics.OverlapSphere (transform.position+movement+new Vector3(0,0.5f,0), 0.4f);
			if(hitColliders.Length > 0){
				for(int i2 = 0; i2 < hitColliders.Length; i2++){
					if(hitColliders[i2].name.ToLowerInvariant().Contains("_transparent")){
						transparentCollider = hitColliders[i2];}
				}
			}
			if(transparentCollider != null){
				//send bump message to the transparents's parent object
				transparentCollider.transform.parent.gameObject.SendMessage("bump", SendMessageOptions.DontRequireReceiver);
			}

			yield return StartCoroutine(move(movement,false));}
		overrideAnimPause = false;
	}

	private void interact(){
		Debug.Log("interact");
		Vector3 spaceInFront = getForwardVector();

		Collider[] hitColliders = Physics.OverlapSphere ((new Vector3(transform.position.x,(transform.position.y + 0.5f),transform.position.z) + spaceInFront), 0.4f);
		Collider currentInteraction = null;
		if (hitColliders.Length > 0){
			for (int i = 0; i < hitColliders.Length; i++){
				if(hitColliders[i].name.Contains("_Transparent")){ //Prioritise a transparent over a solid object.
					if(hitColliders[i].name != ("Player_Transparent")){
						currentInteraction = hitColliders[i];
						i = hitColliders.Length;} //Stop checking for other interactable events if a transparent was found.
				}
				else if (hitColliders[i].name.Contains("_Object")){
					currentInteraction = hitColliders[i];
				}
			}
		}
		if (currentInteraction != null){
			//sent interact message to the collider's object's parent object
			currentInteraction.transform.parent.gameObject.SendMessage ("interact", SendMessageOptions.DontRequireReceiver);
			currentInteraction = null;
		}
		else if(!surfing){
			if(currentMap.getTileTag(transform.position+spaceInFront) == 2){ //water tile tag
			}
		}
	}

	public IEnumerator jump(){
		//float currentSpeed = speed;
		//speed = walkSpeed;
		float increment = 0f;
		float parabola = 0;
		float height = 2.1f;
		Vector3 startPosition = pawn.position;

		playClip(jumpClip);

		while (increment < 1){
			increment += (1/walkSpeed)*Time.deltaTime;
			if (increment > 1){
				increment = 1;}
			parabola = -height*(increment*increment)+(height*increment);
			pawn.position = new Vector3(pawn.position.x, startPosition.y+parabola, pawn.position.z);
			yield return null;
		}

		playClip(landClip);

		//speed = currentSpeed;
	}

	private IEnumerator stillMount(){
		Vector3 holdPosition = mount.transform.position;
		float hIncrement = 0f;
		while(hIncrement < 1){
			hIncrement += (1/speed)*Time.deltaTime;
			mount.transform.position = holdPosition;
			yield return null;
		}
		mount.transform.position = holdPosition;
	}

	private IEnumerator dismount(){
		StartCoroutine("stillMount");
		yield return StartCoroutine("jump");
		mount.transform.localPosition = mountPosition;
		updateMount(false);
	}

	public IEnumerator wildEncounter(WildPokemonInitialiser.Location encounterLocation){
		if(accessedMapSettings.getEncounterList(encounterLocation).Length > 0){
			if(Random.value <= accessedMapSettings.getEncounterProbability()){
				if(setCheckBusyWith(Scene.main.Battle.gameObject)){

					BgmHandler.main.PlayOverlay(Scene.main.Battle.defaultWildBGM, Scene.main.Battle.defaultWildBGMLoopStart);

					yield return StartCoroutine(ScreenFade.main.FadeCutout(false, ScreenFade.slowedSpeed, null));
					//yield return new WaitForSeconds(sceneTransition.FadeOut(1f));
					Scene.main.Battle.gameObject.SetActive(true);
					StartCoroutine(Scene.main.Battle.control(accessedMapSettings.getRandomEncounter(encounterLocation)));

					while(Scene.main.Battle.gameObject.activeSelf){
						yield return null;}

					//yield return new WaitForSeconds(sceneTransition.FadeIn(0.4f));
					yield return StartCoroutine(ScreenFade.main.Fade(true, 0.4f));

					unsetCheckBusyWith(Scene.main.Battle.gameObject);
				}
			}
		}
	}

	private void playClip(AudioClip clip){
		PlayerAudio.clip = clip;
		PlayerAudio.volume = PlayerPrefs.GetFloat("sfxVolume");
		PlayerAudio.Play();
	}

	private void playBump(){
		if(!PlayerAudio.isPlaying){
			if(!moving && !overrideAnimPause){
				playClip(bumpClip);}
		}
	}

}
