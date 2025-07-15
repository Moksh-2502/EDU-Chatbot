using System;
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SubwaySurfers;
using UnityEngine.AddressableAssets;

#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif

public class ShopCharacterList : ShopList
{
    public override void Populate()
    {
        m_RefreshCallback = null;
        foreach (Transform t in listRoot)
        {
            Destroy(t.gameObject);
        }

        foreach (KeyValuePair<string, Character> pair in CharacterDatabase.dictionary)
        {
            Character c = pair.Value;
            if (c != null)
            {
                prefabItem.InstantiateAsync().Completed += (op) =>
                {
                    if (op.Result == null || !(op.Result is GameObject))
                    {
                        Debug.LogWarning(
                            string.Format("Unable to load character shop list {0}.", prefabItem.Asset.name));
                        return;
                    }

                    GameObject newEntry = op.Result;
                    newEntry.transform.SetParent(listRoot, false);

                    ShopItemListItem itm = newEntry.GetComponent<ShopItemListItem>();

                    itm.icon.sprite = c.icon;
                    itm.nameText.text = c.characterName;
                    itm.pricetext.text = c.cost.ToString();

                    itm.buyButton.image.sprite = itm.buyButtonSprite;

                    if (c.premiumCost > 0)
                    {
                        itm.premiumText.transform.parent.gameObject.SetActive(true);
                        itm.premiumText.text = c.premiumCost.ToString();
                    }
                    else
                    {
                        itm.premiumText.transform.parent.gameObject.SetActive(false);
                    }

                    itm.buyButton.onClick.AddListener(delegate() { Buy(c); });

                    m_RefreshCallback += delegate() { RefreshButtonAsync(itm, c).Forget(); };
                    RefreshButtonAsync(itm, c).Forget();
                };
            }
        }
    }

    protected async UniTaskVoid RefreshButtonAsync(ShopItemListItem itm, Character c)
    {
        try
        {
            var playerData = await IPlayerDataProvider.Instance.GetAsync();
            if (c.cost > playerData.coins)
            {
                itm.buyButton.interactable = false;
                itm.pricetext.color = Color.red;
            }
            else
            {
                itm.pricetext.color = Color.black;
            }

            if (c.premiumCost > playerData.premium)
            {
                itm.buyButton.interactable = false;
                itm.premiumText.color = Color.red;
            }
            else
            {
                itm.premiumText.color = Color.black;
            }

            if (playerData.characters.Contains(c.characterName))
            {
                itm.buyButton.interactable = false;
                itm.buyButton.image.sprite = itm.disabledButtonSprite;
                itm.buyButton.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Owned";
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }


    public void Buy(Character c)
    {
        IPlayerDataProvider.Instance.BuyCharacterAsync(c).Forget();

#if UNITY_ANALYTICS // Using Analytics Standard Events v0.3.0
        var transactionId = System.Guid.NewGuid().ToString();
        var transactionContext = "store";
        var level = PlayerData.instance.rank.ToString();
        var itemId = c.characterName;
        var itemType = "non_consumable";
        var itemQty = 1;

        AnalyticsEvent.ItemAcquired(
            AcquisitionType.Soft,
            transactionContext,
            itemQty,
            itemId,
            itemType,
            level,
            transactionId
        );
        
        if (c.cost > 0)
        {
            AnalyticsEvent.ItemSpent(
                AcquisitionType.Soft, // Currency type
                transactionContext,
                c.cost,
                itemId,
                PlayerData.instance.coins, // Balance
                itemType,
                level,
                transactionId
            );
        }

        if (c.premiumCost > 0)
        {
            AnalyticsEvent.ItemSpent(
                AcquisitionType.Premium, // Currency type
                transactionContext,
                c.premiumCost,
                itemId,
                PlayerData.instance.premium, // Balance
                itemType,
                level,
                transactionId
            );
        }
#endif

        // Repopulate to change button accordingly.
        Populate();
    }
}