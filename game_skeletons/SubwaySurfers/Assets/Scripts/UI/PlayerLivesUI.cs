using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Characters;

public class PlayerLivesUI : MonoBehaviour
{
    [SerializeField] protected Image lifeIndicatorPrefab;
    [SerializeField] protected RectTransform lifeContainer;

    private readonly List<Image> liveItems = new ();
    private void Awake()
    {
        IPlayerStateProvider.Instance.OnLivesChanged += OnLivesChanged;
        CreateLifeIndicators();
    }


	private void CreateLifeIndicators()
    {
        for (var i = 0; i < IPlayerStateProvider.Instance.MaxLives; i++)
        {
            var lifeItem = Instantiate(lifeIndicatorPrefab, lifeContainer);
            liveItems.Add(lifeItem);
        }
    }
	
    private void OnLivesChanged(int currentLives)
    {
        for (int i = 0; i < liveItems.Count; i++)
        {
            if (i < currentLives)
            {
                liveItems[i].color = Color.white; // Set to full color
            }
            else
            {
                liveItems[i].color = Color.black;
            }
        }
    }
	
}