﻿ using UnityEngine;
using System.Collections;

public class LevelManager : MonoBehaviour {

	public GameObject currentCheckpoint;

	private PlayerController player;

	public GameObject deathParticle;
	public GameObject respawnParticle;

	public int pointsPenaltyOnDeath;

	public float respawnDelay;

	private CameraController camera;

	private float gravityStore;

	public HealthManager healthManager;

	// Use this for initialization
	void Start () {
		player = FindObjectOfType<PlayerController> ();

		camera = FindObjectOfType<CameraController> ();

		healthManager = FindObjectOfType<HealthManager> ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void RespawnPlayer()
	{
		StartCoroutine ("RespawnPlayerCo");
	}

	public IEnumerator RespawnPlayerCo()
	{
		Instantiate (deathParticle, player.transform.position, player.transform.rotation);
		player.enabled = false;
		//player.GetComponent<Renderer>().enabled = false;
        player.transform.FindChild("Graphics").GetComponent<Renderer>().enabled = false;
		camera.isFollowing = false;
		//gravityStore = player.GetComponent<Rigidbody2D> ().gravityScale;
		//player.GetComponent<Rigidbody2D> ().gravityScale = 0f;
		//player.GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
		ScoreManager.AddPoints (-pointsPenaltyOnDeath);
		Debug.Log ("Player Respawn");
		yield return new WaitForSeconds (respawnDelay);
		//player.GetComponent<Rigidbody2D> ().gravityScale = gravityStore;
		player.transform.position = currentCheckpoint.transform.position;
		player.knockbackCount = 0;
		player.enabled = true;
		//player.GetComponent<Renderer>().enabled = true;
        player.transform.FindChild("Graphics").GetComponent<Renderer>().enabled = true;
        healthManager.FullHealth ();
		healthManager.isDead = false;
		camera.isFollowing = true;
		Instantiate (respawnParticle, currentCheckpoint.transform.position, currentCheckpoint.transform.rotation);
        player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
	}
}
