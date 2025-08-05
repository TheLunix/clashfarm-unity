using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Button = UnityEngine.UI.Button;

public class PetPanel : MonoBehaviour
{
	private string NickName, SerialCode, nick = "Name", code = "SerialCode", PlayerID, id = "ID",jsonformat;
	public GameObject Panel, PanelBuy, PanelInfo, PanelTraining;
	public Button CloseButton;
	private int PetActive = 0;
	public LoadAndUpdateAccount Player;
	void Start()
    {
		this.NickName = PlayerPrefs.GetString(nick);
       	this.SerialCode = PlayerPrefs.GetString(code);
       	this.PlayerID = PlayerPrefs.GetString(id);
		//Buttons
		CloseButton.onClick.AddListener(ClosePanel);
    }
    public void OpenPanel()
	{
		PetActive = Player.PetActive;
		Panel.SetActive(true);
		if(PetActive == 0)
		{
			PanelBuy.SetActive(true);
			PanelInfo.SetActive(false);
			PanelTraining.SetActive(false);
		}
		else if(PetActive > 0)
		{
			PanelBuy.SetActive(false);
			PanelInfo.SetActive(true);
			PanelTraining.SetActive(false);
		}
	}

	public void ClosePanel()
	{
		Panel.SetActive(false);
	}
}
