﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;



public class EmotionBall : LookAtObj 
{

	public Ball followBall;
	public float speed = 8;

	public enum Emotions {
		red
		,green
		,bleu
		,yellow
		,orange
		,violet
		,white
	}

	public Emotions emotion;
	public AudioSource audioSource;
	private AudioClip audioClip;
	

	private bool _isPlaying = false;

	public float pulseSpeed = 0.2f;
	private float _scale = 1;
	public Vector3 basicScale;
	private Vector3 targetScale;

	private bool _stopPlaying = false;
	private float _stopPlayingTempo = 0;

	private float _lastPersonnalTempoPlay;
	private float _personalTempoDuration;

	private bool _launchedPlay = false;
	public Renderer graph;

	private ClipInfo _cClipInfo;

	public ClipInfo getCClipInfo()
	{
		return _cClipInfo;
	}

	public bool launchedPlay()
	{
		return _launchedPlay;
	}

	public bool isPlaying()
	{
		return _isPlaying;
	}
	
	public override void Setup ()
	{
		basicScale = transform.parent.localScale;
	}

	public override void Alive ()
	{
		base.Alive();
		followBall = null;
		_isPlaying = false;
		_stopPlaying = false;
		_scale = 1;
		transform.parent.localScale = basicScale;
		targetScale = basicScale;
		setupForEmotion();
		if (move)
			randomizeDirection();
	}

	public void setupForEmotion()
	{
		EmotionInfos infos = EmotionSoundConfig.Instance.emoDictionary[emotion];
		graph.material = infos.material;
	}

	public bool move = false;
	private Vector3 direction = Vector3.zero;

	public void randomizeDirection()
	{
		direction.x = UnityEngine.Random.value;
		direction.y = UnityEngine.Random.value;
		direction.Normalize();
	}


	// Update is called once per frame
	public override void Update ()
	{
		base.Update ();

		if (followBall != null)
			doFollowBall();
		else if (move)
		{
			transform.parent.rigidbody.MovePosition(transform.parent.position + direction * Time.deltaTime * speed);
		}

		if (_isPlaying && (_lastPersonnalTempoPlay + EmotionSoundConfig.Instance.generalTempo) <= Time.time)
		{
			OnEmotionTempo();
			_lastPersonnalTempoPlay = Time.time;
		}

		if (_stopPlaying && (_lastPersonnalTempoPlay + _stopPlayingTempo) <= Time.time)
		{
			audioSource.Stop();
			_stopPlaying = false;
			Die();
		}
	
	}

	public virtual void OnEmotionTempo()
	{
		if (followBall != null)
		{
			if ( !followBall.emotionsZone.Contains(emotion) )
			{
				diminutionScale();
			}
			else
			{
				addScale();
			}
		}
	}

	public virtual void diminutionScale()
	{
		if (!followBall.emotionsZone.Contains(emotion))
		{
			_scale -= pulseSpeed;
			applyScale();
		}

	}

	public virtual void addScale()
	{
		applyScale();
	}

	public virtual void resetScale()
	{
		_scale = 1;
		applyScale();
	}

	public virtual void applyScale()
	{
		_scale = Mathf.Max(0, Mathf.Min(_scale, 1));
		targetScale = basicScale * _scale;
		transform.parent.localScale = targetScale;
		if (_scale <= 0.1f)
		{
			removeAudioLoop();
		}
	}

	public virtual void doFollowBall()
	{
		if (followBall != null)
		{
			//Vector3 dir = lookAtInput(followBall.transform.position, false);
			//transform.parent.transform.right = dir;
			Vector3 dir = followBall.transform.position - transform.parent.position;
			dir.Normalize();
			float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
			//transform.parent.rotation = Quaternion.Euler(new Vector3(0,0,angle));
			transform.parent.rigidbody.MoveRotation( Quaternion.Euler(new Vector3(0, 0, angle)) );
			transform.parent.rigidbody.MovePosition(transform.position + (transform.parent.right * speed * Time.deltaTime));
		}
	}

	public List<string> bounceOthers;
	public LayerMask bounceLayer;

	public void OnTriggerEnter(Collider other)
	{
		if ( other.CompareTag("Player") && followBall == null)
		{
			Vector3 dir = lookAtInput(other.transform.position, false);
			transform.parent.gameObject.rigidbody.MoveRotation(Quaternion.Euler( dir ));
			followBall = other.GetComponent<Ball>();
			followBall.addEmotion(this);

			EmotionSoundConfig.Instance.addPlayingBall( this );
			_launchedPlay = false;
		}

		if (move && bounceOthers.Contains(other.tag))
		{
			bounce(other.gameObject);
		}
	}

	public void bounce(GameObject other)
	{

		Vector3 vToMe = other.transform.position - transform.position;
		vToMe.Normalize();
		RaycastHit hit;
		if ( Physics.Raycast( transform.position, vToMe.normalized, out hit, 50, bounceLayer.value) )
		{
			direction += hit.normal * 3;
		}
	}

	public void setupAudioLoop(ClipInfo info)
	{
		_cClipInfo = info;
		audioSource.clip = info.clip;
	}

	public void playAudioLoop( float time)
	{

		if (_isPlaying)
			return;

		audioSource.loop = true;
		audioSource.time = time;
		audioSource.volume =  _cClipInfo.volumeStart;
		EmotionSoundConfig.Instance.emoDictionary[emotion].setPlaying(true);

		_personalTempoDuration = _cClipInfo.getTempoDuration();
		_stopPlayingTempo = _cClipInfo.stopTempo;

		_lastPersonnalTempoPlay = Time.time;

		_isPlaying = true;
		audioSource.Play();
	}




	public void removeAudioLoop()
	{
		if (!_isPlaying)
			return;
		EmotionSoundConfig.Instance.removePlayingBall(this);
		followBall.removeEmotion(this);

		followBall = null;
		_isPlaying = false;
		audioSource.loop = false;
		_stopPlaying = true;
		_launchedPlay = false;
	}
	


	public void OnTriggerExit(Collider other)
	{
		/*
		if (followBall != null && other.CompareTag("Player"))
		{
			Ball cBall = other.GetComponent<Ball>();
			if (cBall.GetInstanceID() == followBall.GetInstanceID())
			{
				followBall = null;
			}
		}*/
	}
}
