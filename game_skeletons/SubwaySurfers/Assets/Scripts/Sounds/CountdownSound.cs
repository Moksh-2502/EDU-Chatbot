using System;
using UnityEngine;

public class CountdownSound : MonoBehaviour
{
	[SerializeField] private bool _autoDisableOnEnd = true;
	protected AudioSource m_Source;
	protected float m_TimeToDisable;

    protected const float k_StartDelay = 0.5f;
	
	void OnEnable()
	{
		m_Source = GetComponent<AudioSource>();
		m_TimeToDisable = m_Source.clip.length;
        m_Source.PlayDelayed(k_StartDelay);
	}

	private void OnDisable()
	{
		m_TimeToDisable = 0;
	}

	void Update()
	{
		m_TimeToDisable -= Time.deltaTime;

		if (_autoDisableOnEnd && m_TimeToDisable < 0)
			gameObject.SetActive(false);
	}
}
